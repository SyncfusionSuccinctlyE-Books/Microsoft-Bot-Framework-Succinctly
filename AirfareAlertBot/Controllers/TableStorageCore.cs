using Google.Apis.QPXExpress.v1.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;

namespace AirfareAlertBot.Controllers
{
    // This class represents how the flight data is stored on Azure
    public class TravelEntity : TableEntity
    {
        public TravelEntity(string guid, string origenDest)
        {
            // PartitionKey and RowKey are used for searching
            PartitionKey = guid;
            RowKey = origenDest;
        }

        public TravelEntity() { }

        // The QPX Express TripOptionsRequest object
        // stored as a JSON string on an Azure Blob
        public string TravelReq { get; set; }

        // The QPX Express TripsSearchResponse object
        // stored as a JSON string on an Azure Blob
        public string TravelRes { get; set; }

        // The user's flight details
        // stored as a JSON string on an Azure Blob
        public string FlightDetails { get; set; }
    }

    // (Continued from tableStorage.cs)
    public partial class TableStorage : IDisposable
    {
        protected bool disposed;

        private TripOptionsRequest travelReq = null;
        private TripsSearchResponse travelRes = null;
        private FlightDetails flightDetails = null;

        ~TableStorage()
        {
            Dispose(false);
        }

        public TableStorage()
        {
            travelReq = null;
            travelRes = null;
            flightDetails = null;
        }

        public TableStorage(TripOptionsRequest req, TripsSearchResponse res, FlightDetails fd)
        {
            travelReq = req;
            travelRes = res;
            flightDetails = fd;
        }

        public virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose any managed, IDisposable resources
                    travelReq = null;
                    travelRes = null;
                    flightDetails = null;
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

        // Refreshed the flight data details object
        protected FlightDetails GetUpdatedFlightData(FlightDetails fd)
        {
            FlightDetails ffd = new FlightDetails()
            {
                OriginIata = fd.OriginIata,
                DestinationIata = fd.DestinationIata,
                OutboundDate = fd.OutboundDate,
                InboundDate = fd.InboundDate,
                NumPassengers = fd.NumPassengers,
                NumResults = fd.NumResults
            };

            return ffd;
        }

        // Deserializes the JSON strings representing the 
        // TripOptionsRequest and FlightDetails objects 
        // stored on Azure Blobs
        protected FlightDetails GetStoredFlightData(TravelEntity entity, CloudStorageAccount storageAccount, out TripOptionsRequest to)
        {
            to = JsonConvert.DeserializeObject<TripOptionsRequest>(GetFromBlob(entity.TravelReq, storageAccount));
            return JsonConvert.DeserializeObject<FlightDetails>(GetFromBlob(entity.FlightDetails, storageAccount));
        }

        // Method that simply compares if the price of the stored flight 
        // is different than the current price for that same flight
        protected bool PriceHasChanged(TravelEntity entity, TripsSearchResponse result, CloudStorageAccount storageAccount)
        {
            bool res = false;

            FlightDetails fd = JsonConvert.DeserializeObject<FlightDetails>(GetFromBlob(entity.FlightDetails, storageAccount));

            TripsSearchResponse travelRes = JsonConvert.
                DeserializeObject<TripsSearchResponse>(GetFromBlob(entity.TravelRes, storageAccount));

            if (travelRes.Trips.TripOption[fd.Posi].SaleTotal != result.Trips.TripOption[fd.Posi].SaleTotal)
                res = true;

            return res;
        }
    }
}