using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;

namespace AirfareAlertBot.Controllers
{
    // Handles the conversation flow with the users
    public partial class FlightFlow
    {
        // Interprets an unfollow request command from the user
        public static bool ProcessUnfollow(string value, ref ValidateResult result)
        {
            bool res = false;

            if (value.ToLower().Contains(GatherQuestions.cStrUnFollow.ToLower()))
            {
                // Use Azure Table Storage 
                using (TableStorage ts = new TableStorage())
                {
                    string guid = value.ToLower().Replace(
                        GatherQuestions.cStrUnFollow.ToLower(), string.Empty);

                    // Remove the flight details for the guid being followed
                    if (ts.RemoveEntity(guid))
                    {
                        string msg = GatherQuestions.cStrNoLongerFollowing + guid;
                        result = new ValidateResult { IsValid = false, Value = msg };
                        result.Feedback = msg;

                        res = true;
                    }
                    else
                        result = new ValidateResult { IsValid = false, Value = GatherErrors.cStrNothingToUnfollow };
                }
            }

            return res;
        }

        // Validates that the response to the user is never empty
        public static ValidateResult CheckValidateResult(ValidateResult result)
        {
            result.Feedback = ((result.Feedback == null ||
                result.Feedback == string.Empty) && 
                result.Value.ToString() == string.Empty) ?
                GatherQuestions.cStrGatherValidData : result.Feedback;

            return result;
        }

        // Validates the user's response for a direct flight (or not)
        public static ValidateResult ValidateDirect(TravelDetails state, string value)
        {
            ValidateResult result = new ValidateResult { IsValid = false, Value = string.Empty };

            string direct = (value != null) ? value.Trim() : string.Empty;

            if (ProcessUnfollow(direct, ref result))
                return result;

            if (direct != string.Empty)
            {
                // If direct flight response is correct
                if (direct.ToLower() == GatherQuestions.cStrYes ||
                    direct.ToLower() == GatherQuestions.cStrNo)
                    return new ValidateResult { IsValid = true, Value = direct.ToUpper() };
                else
                {
                    if (result.Feedback != null)
                        result.Feedback = GatherQuestions.cStrGatherProcessTryAgain;
                }
            }
            else
            {
                if (result.Feedback != null)
                    result.Feedback = GatherQuestions.cStrGatherProcessTryAgain;
            }

            return result;
        }

        // Validates the user's response to follow a flight request (for price changes)
        public static ValidateResult ValidateFollow(TravelDetails state, string value)
        {
            ValidateResult result = new ValidateResult { IsValid = false, Value = string.Empty };

            string follow = (value != null) ? value.Trim() : string.Empty;

            if (ProcessUnfollow(follow, ref result))
                return result;

            if (follow != string.Empty)
            {
                // If the response to follow a flight is correct
                if (follow.ToLower() == GatherQuestions.cStrYes ||
                    follow.ToLower() == GatherQuestions.cStrNo)
                    return new ValidateResult { IsValid = true, Value = follow.ToUpper() };
                else
                {
                    if (result.Feedback != null)
                        result.Feedback = GatherQuestions.cStrGatherProcessTryAgain;
                }
            }
            else
                result.Feedback = GatherQuestions.cStrGatherRequestProcessedNoGuid;

            return result;
        }  

        // Validates the user's number of passengers response 
        public static ValidateResult ValidateNumPassengers(TravelDetails state, string value)
        {
            ValidateResult result = new ValidateResult { IsValid = false, Value = string.Empty };

            string numPassengers = (value != null) ? value.Trim() : string.Empty;

            if (ProcessUnfollow(numPassengers, ref result))
                return result;

            if (numPassengers != string.Empty)
            {
                // Verifies the number of passengers and if it correct
                result = ValidateNumPassengerHelper.ValidateNumPassengers(numPassengers);

                if (!result.IsValid && (result.Feedback == null || result.Feedback == string.Empty))
                    result.Feedback = GatherQuestions.cStrGatherProcessTryAgain;
            }
            else
            {
                if (result.Feedback != null)
                    result.Feedback = GatherQuestions.cStrGatherProcessTryAgain;
            }

            return result;
        }

        // Validates the user's travel date (outbound or inbound) response
        public static ValidateResult ValidateDate(TravelDetails state, string value, bool checkOutInDatesSame)
        {
            ValidateResult result = new ValidateResult { IsValid = false, Value = string.Empty };
            string date = (value != null) ? value.Trim() : string.Empty;

            if (ProcessUnfollow(date, ref result))
                return result;

            if (checkOutInDatesSame && value.ToLower().Contains(GatherQuestions.cStrGatherProcessOneWay))
               return new ValidateResult { IsValid = true, Value = GatherQuestions.cStrGatherProcessOneWay };

            if (date != string.Empty)
            {
                DateTime res;

                // If it is a proper date
                if (DateTime.TryParse(value, out res))
                {
                    if (checkOutInDatesSame)
                    {
                        // Performs the actual date validation
                        result = ValidateDateHelper.ValidateGoAndReturnDates(
                            ValidateDateHelper.ToDateTime(state.OutboundDate),
                            ValidateDateHelper.ToDateTime(value),
                            ValidateDateHelper.FormatDate(value));
                    }
                    else
                        // If it is a date in the future
                        result = ValidateDateHelper.IsFutureDate(
                            ValidateDateHelper.ToDateTime(value), ValidateDateHelper.FormatDate(value));
                }
                else
                {
                    if (result.Feedback != null)
                        result.Feedback = GatherQuestions.cStrGatherProcessTryAgain;
                }
            }
            // If it is not a proper date
            else
            {
                if (result.Feedback != null)
                    result.Feedback = GatherQuestions.cStrGatherProcessTryAgain;
            }

            return result;
        }

        // Validates the user's response for origin and destination
        public static ValidateResult ValidateAirport(TravelDetails state, string value, bool checkOrigDestSame)
        {
            bool isValid = false;

            List<string> values = new List<string>();
            ValidateResult result = new ValidateResult { IsValid = false, Value = string.Empty };

            string city = (value != null) ? value.Trim() : string.Empty;

            if (ProcessUnfollow(city, ref result))
                return result;

            if (city != string.Empty)
            {
                // Get the IATA code (if any) corresponding to the user input
                isValid = Data.fd.IsIataCode(value.ToString(), ref values);

                if (isValid)
                {
                    // Processes the IATA code response
                    result = ValidateAirportHelpers.ProcessAirportIataResponse(state, checkOrigDestSame, values.ToArray());
                }
                // When multiple airports are found for a given city
                else
                {
                    // Get all the IATA codes for all the airports in a city
                    string[] codes = InternalGetIataCodes(value.ToString().Trim());
                    
                    if (codes.Length == 1)
                    {
                        // When the specific match is found
                        result = ValidateAirportHelpers.ProcessAirportResponse(state, checkOrigDestSame, codes);
                    }
                    else if (codes.Length > 1)
                    {
                        // When multiple options are found
                        result = new ValidateResult { IsValid = isValid, Value = string.Empty };
                        result = ValidateAirportHelpers.GetOriginOptions(codes, result);
                    }
                    else
                    {
                        if (result.Feedback != null)
                        {
                            result = new ValidateResult { IsValid = isValid, Value = string.Empty };
                            result.Feedback = GatherQuestions.cStrGatherProcessTryAgain;
                        }
                    }
                }
            }

            return result;
        }
    }
}