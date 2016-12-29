using System.Collections.Generic;
using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Threading.Tasks;
using Google.Apis.QPXExpress.v1.Data;

namespace AirfareAlertBot.Controllers
{
    // Used in order to store worldwide airport data
    public class Airport
    {
        public string Name { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Iata { get; set; }
        public string Icao { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Altitude { get; set; }
        public string Timezone { get; set; }
        public string Dst { get; set; }
    }
    
    // Used in order to store a flight request
    public class FlightDetails
    {
        public string OriginIata { get; set; }
        public string DestinationIata { get; set; }
        public string OutboundDate { get; set; }
        public string InboundDate { get; set; }
        public string NumPassengers { get; set; }
        public string NumResults { get; set; }
        public string Follow { get; set; }
        public string Direct { get; set; }
        public string UserId { get; set; }
        public int Posi { get; set; }
    }

    // Responsible for processing a flight request
    public class ProcessFlight : IDisposable
    {
        protected bool disposed;
        public Dictionary<string, Airport> Airports { get; set; }
        public FlightDetails FlightDetails { get; set; }

        public ProcessFlight()
        {
            FlightDetails = new FlightDetails()
            {
                OriginIata = string.Empty,
                DestinationIata = string.Empty,
                OutboundDate = string.Empty,
                InboundDate = string.Empty,
                NumPassengers = string.Empty,
                NumResults = string.Empty
            };

            Airports = GetAirports();
        }

        // Gets a list of all airports worldwide
        protected Dictionary<string, Airport> GetAirports()
        {
            string res = string.Empty;

            RestClient client = new RestClient(StrConsts.cStrIataCodesBase);
            RestRequest request = new RestRequest(StrConsts.cStrIataCodePath, Method.GET);

            request.RequestFormat = DataFormat.Json;
            IRestResponse response = client.Execute(request);
            JsonDeserializer deserial = new JsonDeserializer();

            return deserial.Deserialize<Dictionary<string, Airport>>(response);
        }

        // Checks if a string is an IATA code
        public bool IsIataCode(string input, ref List<string> tmpList)
        {
            bool res = false;

            foreach (KeyValuePair<string, Airport> p in Airports)
            {
                if (p.Key.ToLower() == input.ToLower())
                {
                    tmpList.Add(p.Key);
                    res = true;

                    break;
                }
            }

            return res;
        }

        // City that corresponds to an IATA code
        public string GetAirportCity(string code)
        {
            string res = string.Empty;

            foreach (KeyValuePair<string, Airport> p in Airports)
            {
                if (p.Key.ToLower() == code.ToLower())
                {
                    res = p.Value.City;
                    break;
                }
            }

            return res;
        }

        // List of IATA codes
        public List<string> GetCodesList()
        {
            List<string> codes = new List<string>();

            foreach (KeyValuePair<string, Airport> p in Airports)
            {
                codes.Add(p.Key);
            }

            return codes;
        }

        protected bool HasAirportParamValue(string v, string p)
        {
            return (p != string.Empty && v.ToLower() == p.ToLower()) ? true : false;
        }

        // Searches for IATA codes based on the city or airport name
        public string[] GetIataCodes(string name, string city, string country)
        {
            List<string> codes = new List<string>();

            foreach (KeyValuePair<string, Airport> p in Airports)
            {
                if (city != string.Empty)
                {
                    if ((HasAirportParamValue(p.Value.Name, name) ||
                        HasAirportParamValue(p.Value.City, city)) ||

                        HasAirportParamValue(p.Value.Country, country))
                    {
                        codes.Add(p.Key + "|" + p.Value.Name + "|" + p.Value.Country);
                    }
                }
                else if (name != string.Empty)
                {
                    if ((HasAirportParamValue(p.Value.City, city) ||
                        HasAirportParamValue(p.Value.Name, name)) ||

                        HasAirportParamValue(p.Value.Country, country))
                    {
                        codes.Add(p.Key + "|" + p.Value.Name + "|" + p.Value.Country);
                    }
                }
            }

            return codes.ToArray();
        }

        // Footer of a flight request
        protected static string SetOutputFooter(string guid)
        {
            return StrConsts._NewLine +
                    StrConsts._NewLine +
                    ((guid != string.Empty) ?
                    GatherQuestions.cStrGatherRequestProcessed +
                    GatherQuestions.cStrGatherRequestProcessedPost +
                    guid : string.Empty) +
                    StrConsts._NewLine +
                    StrConsts._NewLine + GatherQuestions.cStrNowSayHi;
        }

        // Creates a flight request output
        public static string OutputResult(string[] lines, string guid)
        {
            string r = string.Empty;

            foreach (string l in lines)
                r += l + StrConsts._NewLine;

            if (lines.Length > 1)
                r += SetOutputFooter(guid);

            return r;
        }

        // Main method for processing a flight request
        public async Task<string> ProcessRequest()
        {
            return await Task.Run(() =>
            {
                string guid = string.Empty;
                TripsSearchResponse result = null;

                string[] res = QpxExpressApiHelper.GetFlightPrices(true, Airports, 
                    FlightDetails, out guid, out result).ToArray();

                return OutputResult(res, guid);
            });
        }

        // Destructor
        ~ProcessFlight()
        {
            // Our finalizer should call our Dispose(bool) method with false
            Dispose(false);
        }

        public virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose any managed, IDisposable resources
                    FlightDetails = null;
                }

                // Dispose of undisposed unmanaged resources
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}