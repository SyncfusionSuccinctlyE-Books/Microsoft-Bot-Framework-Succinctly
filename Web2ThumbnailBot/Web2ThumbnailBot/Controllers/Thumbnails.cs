using Microsoft.Bot.Connector;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;

namespace Web2ThumbnailBot
{
    public class Thumbnails
    {
        public static async Task ProcessScreenshot(ConnectorClient
            connector, Activity msg)
        {
            Activity reply = msg.CreateReply($"Processing: {msg.Text}");
            await connector.Conversations.ReplyToActivityAsync(reply);

            await Task.Run(async () =>
            {
                string imgUrl = GetThumbnail(msg.Text);
                reply = CreateResponseCard(msg, imgUrl);

                await connector.
                    Conversations.ReplyToActivityAsync(reply);
            });
        }

        public static string GetThumbnail(string url)
        {
            string r = Constants.cStrApiParms + url;

            RestClient rc = new RestClient(Constants.cStrThumbApi);
            RestRequest rq = new RestRequest(r, Method.GET);

            IRestResponse response = rc.Execute(rq);

            return Constants.cStrThumbApi + r;
        }

        public static Activity CreateResponseCard(Activity msg, string
            imgUrl)
        {
            Activity reply = msg.CreateReply(imgUrl);

            reply.Recipient = msg.From;
            reply.Type = "message";
            reply.Attachments = new List<Attachment>();

            List<CardImage> cardImages = new List<CardImage>();
            cardImages.Add(new CardImage(imgUrl));

            ThumbnailCard plCard = new ThumbnailCard()
            {
                Subtitle = msg.Text,
                Images = cardImages
            };

            Attachment plAttachment = plCard.ToAttachment();
            reply.Attachments.Add(plAttachment);

            return reply;
        }
    }
}