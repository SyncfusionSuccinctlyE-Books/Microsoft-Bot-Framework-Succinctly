using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using AirfareAlertBot.Controllers;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System.Timers;
using Microsoft.Azure;
using System.Web.Http.Controllers;

namespace AirfareAlertBot
{
    // Main class responsible for main user-to-bot interactions
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        protected bool disposed;

        // Timer used to check for flight price changes
        private static Timer followUpTimer = null;
        // Set to true when flight price changes are checked
        private static bool timerBusy = false;
        // Set to true when user-to-bot interaction is ongoing
        private static bool msgBusy = false;

        // Initializes the flight price changes check Timer
        private static void SetFollowUpTimer()
        {
            if (followUpTimer == null)
            {
                followUpTimer = new Timer();
                followUpTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

                double interval = 10000;

                try
                {
                    string fupInterval = CloudConfigurationManager.GetSetting(StrConsts.cStrFollowUpInterval);
                    interval = Convert.ToDouble(fupInterval);
                }
                catch { }

                followUpTimer.Interval = interval;
                followUpTimer.Enabled = true;
                timerBusy = false;
            }
        }

        // Main method for running the flight price changes check
        private static Task ProcessTimer()
        {
            return Task.Run(async () =>
            {
                return await Task.Run(async () =>
                {
                    bool changed = false;

                    // The bot checks in Azure Table storage...
                    using (TableStorage ts = new TableStorage())
                    {
                        // ...If a stored flight request has had any price changes
                        // and if so, send the user this information...
                        changed = await ts.CheckForPriceUpdates(Data.fd.Airports, Data.fd.FlightDetails,
                            Data.initialActivity, Data.initialConnector, Data.currentText);

                        timerBusy = false;
                    }

                    return changed;
                });
            });
        }

        // Triggers the flight price changes check
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (!timerBusy && !msgBusy)
            {
                timerBusy = true;
                ProcessTimer();
            }
        }

        // FormFlow end method: Executed once the request details have been gathered from the user
        internal static IDialog<TravelDetails> MakeRootDialog()
        {
            return Chain.From(() => FormDialog.FromForm(TravelDetails.BuildForm, FormOptions.None))
                .Do(async (context, order) =>
                {
                    try
                    {
                        var completed = await order;

                        // Request processed
                        string res = await Data.fd.ProcessRequest();

                        // Request result sent to the user
                        await context.PostAsync(res);

                        Data.fd.FlightDetails = null;
                    }
                    // This also gets executed when a 'quit' command is issued
                    catch (FormCanceledException<TravelDetails> e)
                    {
                        Data.fd.FlightDetails = null;

                        string reply = string.Empty;

                        if (e.InnerException == null)
                            reply = GatherQuestions.cStrQuitMsg;
                        else
                            reply = GatherErrors.cStrShortCircuit;

                        await context.PostAsync(reply);
                    }
                });
        }

        // Inits the bot's conversational internal state
        private void InitState(Activity activity)
        {
            if (Data.stateClient == null)
                Data.stateClient = activity.GetStateClient();

            if (Data.fd.FlightDetails == null)
                Data.fd.FlightDetails = new FlightDetails();

            Data.fd.FlightDetails.UserId = activity.From.Id;
        }

        // Inits and invokes the ProcessFlight constructor
        private void InitFlightData()
        {
            if (Data.fd == null)
                Data.fd = new ProcessFlight();
        }

        // Gets the user and channel IDs of the conversation
        private void GetUserAndChannelId(Activity activity, ConnectorClient connector)
        {
            Data.channelId = activity.ChannelId;
            Data.userId = activity.From.Id;
            Data.initialActivity = activity;
            Data.initialConnector = connector;
        }

        ~MessagesController()
        {
            Data.fd.Dispose();
        }

        // Send the bot's default welcome message to the user
        private async void SendWelcomeMsg(ConnectorClient connector, Activity activity)
        {
            if (Data.fd == null)
            {
                Activity reply = activity.CreateReply(GatherQuestions.cStrWelcomeMsg);

                await connector.Conversations.ReplyToActivityAsync(reply);
            }
        }

        // Initializes the bot
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            SetFollowUpTimer();
        }

        // Process an unfollow command outside a conversation
        private async void ProcessUnfollow(ConnectorClient connector, Activity activity)
        {
            if (activity.Text.ToLower().Contains(GatherQuestions.cStrUnFollow)) {
                ValidateResult r = new ValidateResult { IsValid = false, Value = GatherErrors.cStrNothingToUnfollow };

                if (FlightFlow.ProcessUnfollow(activity.Text, ref r))
                {
                    Activity reply = activity.CreateReply(r.Feedback);

                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
            }
        }

        // This is the bot's main entry point for all user responses
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            try
            {
                // Inits the flight price change check timer
                SetFollowUpTimer();

                // Set the user-to-bot conversational status as ongoing 
                msgBusy = true;

                // Inits the Bot Framework Connector service
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                // Gets the user and channel id's
                GetUserAndChannelId(activity, connector);

                // When the user has typed a message
                if (activity.Type == ActivityTypes.Message)
                {
                    // Let's greet the user
                    SendWelcomeMsg(connector, activity);

                    // Init the state and flight request
                    InitFlightData();
                    InitState(activity);

                    ProcessUnfollow(connector, activity);

                    // Send the FormBuilder conversational dialog
                    await Conversation.SendAsync(activity, MakeRootDialog);
                }
                else
                    await HandleSystemMessage(connector, activity);
            }
            catch { }

            var response = Request.CreateResponse(HttpStatusCode.OK);

            // A response has been sent back to the user
            msgBusy = false;

            return response;
        }

        private Task<Activity> HandleSystemMessage(ConnectorClient connector, Activity message)
        {
            // Not used for now...
            // Here put any logic that gets on any of these Activity Types
            if (message.Type == ActivityTypes.DeleteUserData)
            {
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
            }
            else if (message.Type == ActivityTypes.Typing)
            {
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}