using Google.Apis.QPXExpress.v1;
using Google.Apis.QPXExpress.v1.Data;
using Google.Apis.Services;
using System.Linq;

using System.Collections.Generic;

namespace AirfareAlertBot.Controllers
{
    public class ExampleFlightData
    {
        private const string cStrApiKey = "<< Your QPX API Key >>";
        private const string cStrAppName = "AirfareAlertBot";

        public static string[] GetFlightPrice()
        {
            List<string> p = new List<string>();

            using (QPXExpressService service = new QPXExpressService(new BaseClientService.Initializer()
            {
                ApiKey = cStrApiKey,
                ApplicationName = cStrAppName
            }))
            {
                TripsSearchRequest x = new TripsSearchRequest();
                x.Request = new TripOptionsRequest();
                x.Request.Passengers = new PassengerCounts { AdultCount = 2 };
                x.Request.Slice = new List<SliceInput>();

                var s = new SliceInput() { Origin = "JFK", Destination = "BOS", Date = "2016-12-09" };

                x.Request.Slice.Add(s);
                x.Request.Solutions = 10;

                var result = service.Trips.Search(x).Execute();

                foreach (var trip in result.Trips.TripOption)
                    p.Add(trip.Pricing.FirstOrDefault().BaseFareTotal.ToString());
            }

            return p.ToArray();
        }
    }
}