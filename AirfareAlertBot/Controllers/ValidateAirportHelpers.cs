using Microsoft.Bot.Builder.FormFlow;

namespace AirfareAlertBot.Controllers
{
    // A set of helper methods used to validate origin and destination airports
    public class ValidateAirportHelpers
    {
        // Checks that the origin and destination are not the same
        private static ValidateResult CheckOrigDestState(TravelDetails state, string field)
        {
            ValidateResult result = new ValidateResult { IsValid = false, Value = string.Empty };
            result.Feedback = GatherErrors.cStrGatherSameCities;

            if (state?.OriginIata.ToLower() != field.ToLower())
                result = new ValidateResult { IsValid = true, Value = field };

            return result;
        }

        // Checks that the IATA code submitted by the user for origin or destination is a valid code
        public static ValidateResult ProcessAirportIataResponse(TravelDetails state, bool checkOrigDestSame, string[] items)
        {
            string field = string.Empty;
            string[] airport = new string[] { items[0] + "|" + Data.fd.GetAirportCity(items[0]) };

            ValidateResult result = ProcessPrefix(state, checkOrigDestSame, airport, out field);
            result.Feedback = (result.IsValid) ? field : GatherErrors.cStrGatherSameCities;

            return result;
        }

        // Part of the validation of the origin and destination checks
        private static ValidateResult ProcessPrefix(TravelDetails state, bool checkOrigDestSame, string[] codes, out string field)
        {
            string[] code = codes[0].Split('|');

            string prefix = !checkOrigDestSame ? "Origin" : "Destination";
            field = $"{prefix}: {code[1]} ({code[0]})";

            ValidateResult result = (checkOrigDestSame) ? CheckOrigDestState(state, code[0]) :
                new ValidateResult { IsValid = true, Value = code[0] };

            return result;
        }

        // Part of the validation of the origin and destination checks
        public static ValidateResult ProcessAirportResponse(TravelDetails state, bool checkOrigDestSame, string[] codes)
        {
            string field = string.Empty;

            ValidateResult result = ProcessPrefix(state, checkOrigDestSame, codes, out field);
            result.Feedback = (result.IsValid) ? field : GatherErrors.cStrGatherSameCities;

            return result;
        }

        // Show the user the various airport options available
        public static ValidateResult GetOriginOptions(string[] values, ValidateResult result)
        {
            result.Feedback = GatherErrors.cStrGatherMOrigin + StrConsts._NewLine +
                                        StrConsts._NewLine;

            foreach (string o in values)
            {
                string[] parts = o.Split('|');
                string newLine = $"{parts[0]} = {parts[1]} ({parts[2]})";

                result.Feedback += newLine + StrConsts._NewLine +
                    StrConsts._NewLine;
            }

            return result;
        }
    }
}