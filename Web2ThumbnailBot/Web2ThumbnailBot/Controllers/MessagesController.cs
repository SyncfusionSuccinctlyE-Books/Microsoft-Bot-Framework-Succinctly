using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;

namespace Web2ThumbnailBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity
            activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new
                    Uri(activity.ServiceUrl));

                // return our reply to the user
                await ProcessResponse(connector, activity);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        public bool CheckUri(string uri, out string exMsg)
        {
            exMsg = string.Empty;
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead(uri))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                exMsg = ex.Message;
                return false;
            }
        }

        private bool IsValidUri(string uriName, out string exMsg)
        {
            uriName = uriName.ToLower().Replace(
                Constants.cStrHttps, string.Empty);
            uriName = (uriName.ToLower().Contains(Constants.cStrHttp)) ?
                uriName : Constants.cStrHttp + uriName;

            return CheckUri(uriName, out exMsg);
        }

        private async Task ProcessResponse(ConnectorClient connector,
            Activity input)
        {
            Activity reply = null;
            string exMsg = string.Empty;

            if (IsValidUri(input.Text, out exMsg))
            {
                await Thumbnails.ProcessScreenshot(connector, input);
            }
            else
            {
                reply = input.CreateReply(
                    "Hi, what is the URL you want a thumbnail for?");

                await connector.Conversations.
                    ReplyToActivityAsync(reply);
            }
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being      
                // added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved  
                // and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what 
                // happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}