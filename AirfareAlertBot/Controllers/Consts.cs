using System;

namespace AirfareAlertBot.Controllers
{
    public class StrConsts
        {
            public const string cStrFollowUpInterval = "FollowUpInterval";

            public const string cStrBotId = "BotId";
            public const string cStrQpxApiKey = "QpxApiKey";
            public const string cStrStorageConnectionStr = "StorageConnectionString";

            public const string cStrIataCodesBase = "https://raw.githubusercontent.com/";
            public const string cStrIataCodePath = "ram-nadella/airport-codes/master/airports.json";

            public const int cNumSolutions = 50;

            public const string _FlightDetails = "_FlightDetails";
            public const string _TravelReq = "_TravelReq";
            public const string _TravelRes = "_TravelRes";

            public const string _NewLine = "\n\n";
        }

    public class GatherQuestions
    {
        public const string cStrYes = "yes";

        public const string cStrNo = "no";

        public const string cStrWelcomeMsg = "Welcome! Type quit to end this query";

        public const string cStrQuitMsg = "You've quit. Now say hi :)";

        public const string cStrGatherValidData = "Hey, give me some valid data :)";

        public const string cStrGatherQOrigin = "Origin (IATA code, Airport name or City)";

        public const string cStrGatherQDestination = "Destination (IATA code, Airport name or City)";

        public const string cStrGatherQInboundDate = "Please specify the return date as DD MMM YYYY (i.e. 29 NOV 2017) or type: one way";

        public const string cStrGatherQOutboundDate = "Please specify the travel date as DD MMM YYYY (i.e. 30 NOV 2017)";

        public const string cStrGatherQNumPassengers = "Please specify the number of passengers";

        public const string cStrGatherProcessingReq = "I'm processing your request, hold on tight! :)";

        public const string cStrGatherProcessTryAgain = ":( couldn't figure that one out, try again ...";

        public const string cStrGatherProcessOneWay = "one way";

        public const string cStrGatherRequestHeader = "Travel Details >>>>";
        public const string cStrGatherRequestId = "RequestId (please copy and save it): ";
        public const string cStrGatherRequestFooter = "<<<< End Details";

        public const string cStrGatherProcessFollow = "Follow price changes? Yes or No";

        public const string cStrGatherProcessDirect = "Direct? Yes or No (Recommended: No)";

        public const string cStrNowSayHi = "Now say hi :)";

        public const string cStrPriceChange = "<<<<<< PRICE CHANGE >>>>>>";

        public const string cStrGatherRequestProcessedNoGuid = "Request processed... " + cStrNowSayHi;

        public static string cStrGatherRequestProcessed = "Later, type unfollow followed by the " +
            StrConsts._NewLine + "RequestId (if you have chosen to follow a price change, " +
            StrConsts._NewLine + "and do not want to follow it any more)... ";

        public const string cStrUnFollow = "unfollow ";

        public const string cStrNoLongerFollowing = "No longer following: ";

        public static string cStrGatherRequestProcessedPost = StrConsts._NewLine + StrConsts._NewLine + "Example:" +
            StrConsts._NewLine + cStrUnFollow;
    }

    public class GatherErrors
    {
        public const string cStrGatherEOrigin = "Could not determine origin :(";

        public const string cStrGatherMOrigin = "Multiple airports found at this location.";

        public const string cStrGatherSameCities = "Origin and destination cannot be the same";

        public const string cStrGatherStateClear = "State has been cleared";

        public const string cStrGatherStatePastDate = "Date cannot be in the past";

        public const string cStrGatherStateFutureDate = "Date needs to be in the future";

        public const string cStrGatherStateInvalidNumPassengers = "Passengers must be between 1 and 100 inclusive";

        public const string cStrShortCircuit = "Sorry, I've had a short circuit. Please try again :)";

        public const string cStrNothingToUnfollow = "Nothing to unfollow...";

        public const string cStrCouldFetchData = "Couldn't fetch flight... try later. Now say hi :)";
    }
}