using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;

namespace AirfareAlertBot.Controllers
{
    // Keeps the state of the conversation
    public class Data
    {
        public static ProcessFlight fd = null;
        public static StateClient stateClient = null;
        public static string channelId = string.Empty;
        public static string userId = string.Empty;
        public static Activity initialActivity = null;
        public static ConnectorClient initialConnector = null;
        public static string currentText = string.Empty;
    }

    public partial class FlightFlow
    {
        // Gets relevant IATA codes for a user's response
        private static string[] InternalGetIataCodes(object value)
        {
            string find = value.ToString().Trim();
            string[] codes = null;

            codes = Data.fd.GetIataCodes(string.Empty, find, string.Empty);

            if (codes.Length == 0)
                codes = Data.fd.GetIataCodes(find, string.Empty, string.Empty);

            return codes;
        }

        // Set the Bot's state as the internal state
        public static void AssignStateToFlightData(ValidateResult result, TravelDetails state)
        {
            if (result.IsValid)
            {
                string userId = Data.fd.FlightDetails.UserId;

                Data.fd.FlightDetails = new FlightDetails()
                {
                    OriginIata = state.OriginIata,
                    DestinationIata = state.DestinationIata,
                    OutboundDate = state.OutboundDate,
                    InboundDate = state.InboundDate,
                    NumPassengers = state.NumPassengers,
                    NumResults = "1",
                    Direct = state.Direct,
                    UserId = userId,
                    Follow = result.Value.ToString()
                };
            }
        }
    }
}