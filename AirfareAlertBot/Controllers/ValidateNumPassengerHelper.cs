using Microsoft.Bot.Builder.FormFlow;
using System;

namespace AirfareAlertBot.Controllers
{
    public class ValidateNumPassengerHelper
    {
        // Validates the number of passengers response
        public static ValidateResult ValidateNumPassengers(string value)
        {
            ValidateResult result = new ValidateResult { IsValid = false, Value = string.Empty };

            try
            {
                int res = Convert.ToInt32(value);

                if (res >= 1 && res <= 100)
                    result = new ValidateResult { IsValid = true, Value = value };
                else
                    result.Feedback = GatherErrors.cStrGatherStateInvalidNumPassengers;
            }
            catch { }

            return result;
        }
    }
}