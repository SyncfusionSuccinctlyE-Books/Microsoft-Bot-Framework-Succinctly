using Google.Apis.QPXExpress.v1.Data;
using System.Collections.Generic;

namespace AirfareAlertBot.Controllers
{
    public partial class QpxExpressApiHelper
    {
        // User for making the Months in a date more human readable
        private static string[] months = new string[] { "01=JAN", "02=FEB", "03=MAR", "04=APR",
            "05=MAY", "06=JUN", "07=JUL", "08=AUG", "09=SEP", "10=OCT", "11=NOV", "12=DEC" };

        // Gets the month within the months array 
        // (the number part)
        private static string GetMonth(string month)
        {
            string res = string.Empty;

            foreach (string m in months)
            {
                if (m.ToLower().Contains(month.ToLower()))
                {
                    res = m.Substring(0, m.IndexOf("="));
                    break;
                }
            }

            return res;
        }

        // Gets the month within the months array
        // (the letters part)
        private static string GetMonthInv(string month)
        {
            string res = string.Empty;

            foreach (string m in months)
            {
                string mm = m.Substring(0, m.IndexOf("="));

                if (mm.ToLower().Contains(month.ToLower()))
                {
                    res = m.Substring(m.IndexOf("=") + 1, 3);
                    break;
                }
            }

            return res;
        }

        // Formats the date to the format used by the QPX Express API
        // YYYY-MM-DD
        private static string ToQpxDateFormat(string dt)
        {
            List<string> res = new List<string>();
            string[] parts = dt.Split('-');

            for (int i = 0; i <= parts.Length - 1; i++)
            {
                switch (i)
                {
                    case 0:
                        res.Add(parts[2].
                            PadLeft(2, '0'));
                        break;

                    case 1:
                        res.Add(GetMonth(parts[1]).
                            PadLeft(2, '0'));
                        break;

                    case 2:
                        res.Add(parts[0].
                            PadLeft(2, '0'));
                        break;
                }
            }

            string str = string.Empty;
            if (res.Count > 0)
            {
                foreach (string r in res)
                {
                    str += r;
                }

                str = string.Join("-", res);
            }

            return str;
        }

        // Adds the trips that will be part of the flight request
        // that will be sent to the QPX Express API
        private static IList<SliceInput> AddTrips(FlightDetails fd)
        {
            List<SliceInput> trips = new List<SliceInput>();

            string goDate = ToQpxDateFormat(fd.OutboundDate);

            // Outbound trip request details
            SliceInput goTrip = new SliceInput()
            {
                Origin = fd.OriginIata,
                Destination = fd.DestinationIata,
                Date = goDate
            };

            trips.Add(goTrip);

            // Inbound trip request details
            // (if the flight request is not 'one way'
            if (fd.InboundDate.ToLower() != GatherQuestions.cStrGatherProcessOneWay.ToLower())
            {
                string returnDate = ToQpxDateFormat(fd.InboundDate);

                SliceInput returnTrip = new SliceInput()
                {
                    Origin = fd.DestinationIata,
                    Destination = fd.OriginIata,
                    Date = returnDate
                };

                trips.Add(returnTrip);
            }

            return trips;
        }

        // Retrieves the city name from the list of worldwide airports
        protected static string GetCityNameFromAirports(Dictionary<string, Airport> airports, string code)
        {
            string city = string.Empty;

            foreach (KeyValuePair<string, Airport> airport in airports)
            {
                if (airport.Key.ToLower() == code.ToLower())
                {
                    city = airport.Value.City;
                    break;
                }
            }

            return city;
        }

        // Attemps to get the city from the list of cities contained within
        // the QPX Express flight results and if not found, gets it from the
        // worldwide airports list
        protected static string GetCityName(Dictionary<string, Airport> airports,
            Google.Apis.QPXExpress.v1.Data.Data data, string code)
        {
            string res = string.Empty;

            // Checks if the city exists within the QPX Express flight results
            foreach (CityData city in data.City)
            {
                if (city.Code.ToLower() == code.ToLower())
                {
                    res = city.Name;
                    break;
                }
            }

            // If not found within the QPX Express flight results
            // the city is retrieved from the worldwide airports list
            return (res != string.Empty) ? res :
                GetCityNameFromAirports(airports, code);
        }

        // Gets the aircraft name from the QPX Express flight results
        protected static string GetAircraftName(Google.Apis.QPXExpress.v1.Data.Data data, string code)
        {
            string res = string.Empty;

            foreach (AircraftData craft in data.Aircraft)
            {
                if (craft.Code.ToLower() == code.ToLower())
                {
                    res = craft.Name;
                    break;
                }
            }

            return res;
        }

        // Gets the airline name from the QPX Express flight results
        protected static string GetAirlineName(Google.Apis.QPXExpress.v1.Data.Data data, string code)
        {
            string res = string.Empty;

            foreach (CarrierData carrier in data.Carrier)
            {
                if (carrier.Code.ToLower() == code.ToLower())
                {
                    res = carrier.Name;
                    break;
                }
            }

            return res;
        }

        // Miles to Kilometers 
        // (part of the flight data output to the user)
        protected static string ToKms(int? miles)
        {
            string kms = string.Empty;

            try
            {
                int k = (int)(miles * 1.609344);
                kms = k.ToString();
            }
            catch { }

            return kms;
        }

        // Makes the dates more human readable
        // (instead of using the default QPX Express API date format)
        public static string PrettierDate(string dt)
        {
            string res = string.Empty;

            string tmp = dt.Substring(0, dt.IndexOf('T'));

            string year = tmp.Substring(0, tmp.IndexOf('-'));
            string month = tmp.Substring(tmp.IndexOf('-') + 1, 2);
            string day = tmp.Substring(tmp.LastIndexOf('-') + 1, 2);

            res = dt.Remove(0, dt.IndexOf('T'));
            res = res.Replace("T", " ").Replace("+", " UTC+").
                Replace("-", " UTC-");

            res = (day + "-" + GetMonthInv(month) + "-" + year) + res;

            return res;
        }

        protected static string ConvertMinsToHours(int minutes)
        {
            int hours = (minutes - minutes % 60) / 60;
            return hours + ":" + (minutes - hours * 60);
        }

        // Gets the duration string (output) for a flight
        protected static string GetDuration(int? dur)
        {
            string res = string.Empty;

            if (dur != null)
            {
                //double? d = Math.Round((double)dur / 60, 2);
                string mins = ConvertMinsToHours((int)dur);

                //res = (dur > 59) ? (d.ToString().Replace(".", "h") +
                    //"m (" + dur.ToString() + ") Mins") : d.ToString() + " Mins";

                res = (dur > 59) ? (mins.ToString().Replace(":", "h") +
                    "m (" + dur.ToString() + ") Mins") : dur.ToString() + " Mins";
            }

            return res;
        }
    }
}