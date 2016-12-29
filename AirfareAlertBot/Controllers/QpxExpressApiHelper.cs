using Google.Apis.QPXExpress.v1;
using Google.Apis.QPXExpress.v1.Data;
using Google.Apis.Services;
using Microsoft.Azure;
using System;
using System.Collections.Generic;

namespace AirfareAlertBot.Controllers
{
    // Wrapper class around the Google QPX Express API
    // Here's where the bot gets real flight data
    public partial class QpxExpressApiHelper
    {
        // Retrieves all core data for a specific leg (flight connection) within the travel
        // itinerary, such as times, dates, cities, aircraft types, etc.
        protected static string GetSegmentLegs(ref int leg, Dictionary<string, Airport> airports,
            Google.Apis.QPXExpress.v1.Data.Data data, IList<SegmentInfo> segment)
        {
            string res = string.Empty;
            
            foreach (SegmentInfo sg in segment)
            {
                int i = leg;

                foreach (LegInfo li in sg.Leg)
                {
                    res += "Leg " + i.ToString() + " -> " + StrConsts._NewLine + StrConsts._NewLine;

                    string aircraft = GetAircraftName(data, li.Aircraft);
                    string airline = GetAirlineName(data, sg.Flight.Carrier);

                    string t1 = li.OriginTerminal ?? "Main";
                    string t2 = li.DestinationTerminal ?? "Main";

                    string kms = ToKms(li.Mileage ?? 0);
                    string departure = PrettierDate(li.DepartureTime);
                    string arrival = PrettierDate(li.ArrivalTime);

                    string originCity = GetCityName(airports, data, li.Origin) + " (" + li.Origin + ")";
                    string destinationCity = GetCityName(airports, data, li.Destination) + " (" + li.Destination + ")";

                    string duration = GetDuration(li.Duration ?? 0);

                    string line1 = $"From {originCity} to {destinationCity}" + StrConsts._NewLine + StrConsts._NewLine;
                    string line2 = $"Flight {sg.Flight.Number}" + StrConsts._NewLine + StrConsts._NewLine;
                    string line3 = $"Leaves: {departure}" + StrConsts._NewLine + StrConsts._NewLine;
                    string line4 = $"Arrives: {arrival}" + StrConsts._NewLine + StrConsts._NewLine;
                    string line5 = $"Duration: {duration}" + StrConsts._NewLine + StrConsts._NewLine;
                    string line6 = $"From terminal {t1} to terminal {t2}" + StrConsts._NewLine + StrConsts._NewLine;
                    string line7 = $"Meal: {li.Meal}" + StrConsts._NewLine + StrConsts._NewLine;
                    string line8 = $"Mileage: {li.Mileage} = ({kms} Kms)" + StrConsts._NewLine + StrConsts._NewLine;
                    string line9 = $"Aircraft: {aircraft}" + StrConsts._NewLine + StrConsts._NewLine;
                    string line10 = $"Airline: {airline}" + StrConsts._NewLine + StrConsts._NewLine;
                    string line11 = $"{li.OperatingDisclosure}" + StrConsts._NewLine + StrConsts._NewLine;

                    res += line1 + line2 + line3 + line4 + line5 + 
                        line6 + line7 + line8 + line9 + line10 + line11;

                    i++;                       
                }

                leg = i;
            }

            return res;
        }

        // Retrieves all the inner detail flight info for each of the trips and legs (connections)
        // for each flight related to the flight request submitted by the user
        protected static string GetDetails(Dictionary<string, Airport> airports, Google.Apis.QPXExpress.v1.Data.Data data, 
            TripOption to, string inb, out int leg)
        {
            leg = 1;
            string res = "Outbound --> " + StrConsts._NewLine + StrConsts._NewLine;

            for (int i = 0; i <= to.Slice.Count - 1; i++)
            {
                res += GetSegmentLegs(ref leg, airports, data, to.Slice[i].Segment);

                if (inb.ToLower() == GatherQuestions.cStrGatherProcessOneWay.ToLower())
                    break;
                else
                    if (i < to.Slice.Count - 1)
                        res += StrConsts._NewLine + StrConsts._NewLine +
                            StrConsts._NewLine + StrConsts._NewLine +
                            "Inbound (Return) --> " + StrConsts._NewLine + StrConsts._NewLine;
            }

            return res;
        }

        // Creates the footer flight details response for the user
        private static void SetFooter(ref List<string> p, string guid, string price, string details)
        {
            p.Add(GatherQuestions.cStrGatherRequestHeader +
                    StrConsts._NewLine +
                    StrConsts._NewLine);

            if (guid != string.Empty)
                p.Add(GatherQuestions.cStrGatherRequestId + guid +
                    StrConsts._NewLine +
                    StrConsts._NewLine);

            p.Add(price +
                StrConsts._NewLine +
                StrConsts._NewLine +
                details);

            p.Add(StrConsts._NewLine +
                StrConsts._NewLine +
                StrConsts._NewLine + GatherQuestions.cStrGatherRequestFooter);
        }

        // Determines if a result from the QPX Express API is suitable to be displayed
        // based on the criteria selected by the user - if direct flights have been chosen
        // or with multiple stops
        private static bool IsResultOk(FlightDetails fd, int legs)
        {
            bool res = false;

            if (fd.Direct.ToLower() == "yes")
            {
                if (fd.InboundDate.ToLower() == GatherQuestions.cStrGatherProcessOneWay.ToLower() && legs == 1)
                    res = true;

                if (fd.InboundDate.ToLower() != GatherQuestions.cStrGatherProcessOneWay.ToLower() && legs == 2)
                    res = true;
            }
            else
                res = true;

            return res;
        }

        // Main submethod for processing the results obtained from the QPX Express API
        // It basically loops through the results, determines the best one (price-wise)
        // and creates the output that will be sent to the user
        private static List<string> ProcessResult(bool save, Dictionary<string, Airport> airports, TripOptionsRequest req, 
            TripsSearchResponse result, FlightDetails fd, int? totalResults, 
            string inb, out string guid)
        {
            int legs = 1;
            int numResult = 0;
            List<string> p = new List<string>();

            string price = string.Empty;
            string details = string.Empty;
            guid = string.Empty;

            if (result.Trips.TripOption != null)
            {
                for (int i = 0; i <= totalResults - 1; i++)
                {
                    price = "Price: " + result.Trips.TripOption[i].SaleTotal;
                    details = GetDetails(airports, result.Trips.Data, result.Trips.TripOption[i], inb, out legs);

                    if (IsResultOk(fd, legs - 1))
                    {
                        numResult++;

                        if (fd.Follow.ToLower() == GatherQuestions.cStrYes)
                        {
                            fd.Posi = i;

                            using (TableStorage ts = new TableStorage(req, result, fd))
                            {
                                if (save)
                                    guid = ts.Save();
                            }
                        }

                        if (numResult == Convert.ToInt32(fd.NumResults))
                            break;
                    }
                    else
                    {
                        price = string.Empty;
                        details = string.Empty;
                    }
                }

                if (price != string.Empty && details != string.Empty)
                    SetFooter(ref p, guid, price, details);
            }

            return p;
        }

        // Checks if the request will be for direct flights or not
        private static bool DirectSelected(FlightDetails fd)
        {
            return (fd.Direct.ToLower() == GatherQuestions.cStrYes) ? true : false;
        }

        // Checks what number of results to fetch from the QPX Express API
        private static int? DetermineSolutions(FlightDetails fd)
        {
            return (DirectSelected(fd)) ? StrConsts.cNumSolutions : Convert.ToInt32(fd.NumResults);
        } 

        // Main submethod that executes the flight request to the QPX Express API
        private static string CreateExecuteRequest(FlightDetails fd, QPXExpressService service, out TripsSearchRequest x, out TripsSearchResponse result)
        {
            string failed = string.Empty;

            x = new TripsSearchRequest();
            result = null;

            try
            {
                x.Request = new TripOptionsRequest();

                int nump = Convert.ToInt32(fd.NumPassengers);
                x.Request.Passengers = new PassengerCounts { AdultCount = nump };

                x.Request.Slice = AddTrips(fd);
                x.Request.Solutions = DetermineSolutions(fd);

                result = service.Trips.Search(x).Execute();
            }
            catch 
            {
                failed = GatherQuestions.cStrGatherProcessTryAgain;
            }

            return failed;
        }

        // This is the main method responsible for getting flight data
        // It calls the QPX Express API and establishes authentication

        // It is called by FlightData.cs and also by the Timer that checks
        // if a price has changed
        public static List<string> GetFlightPrices(bool save, Dictionary<string, Airport> airports, FlightDetails fd, 
            out string guid, out TripsSearchResponse result)
        {
            List<string> p = new List<string>();
            guid = string.Empty;

            // Authentication with the QPX Express API
            using (QPXExpressService service = new QPXExpressService(new BaseClientService.Initializer()
            {
                ApiKey = CloudConfigurationManager.GetSetting(StrConsts.cStrQpxApiKey),
                ApplicationName = CloudConfigurationManager.GetSetting(StrConsts.cStrBotId)
            }))
            {
                TripsSearchRequest x = null;
                result = null;

                // Executes the flight / trip request using the QPX Express API
                string failed = CreateExecuteRequest(fd, service, out x, out result);

                if (result != null)
                    // Process the results obtained from the QPX Express API
                    p = ProcessResult(save, airports, x.Request, result, fd, x.Request.Solutions, fd.InboundDate, out guid);
                else
                    p.Add(failed + StrConsts._NewLine + StrConsts._NewLine + 
                        GatherErrors.cStrCouldFetchData);
            }

            return p;
        }
    }
}