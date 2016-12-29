using Google.Apis.QPXExpress.v1.Data;
using Microsoft.Azure;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AirfareAlertBot.Controllers
{
    // Here's where the magic with Azure Table Storage happens
    // This is used when a user requests to follow (get price change notifications)
    // for a flight (flight data is stored in Azure Table Storage and Azure Blobs
    // and also used when a user decices to unfollow a flight price change,
    // or when the flight itself has expired - the data is then removed from
    // Azure Table Storage
    public partial class TableStorage : IDisposable
    {
        // Main sub-method that is responsible for chedking and returning a flight price change result
        public string ShowUpdatedResults(Dictionary<string, Airport> airports, FlightDetails fd, out TripsSearchResponse result)
        {
            string guid = string.Empty;
            result = null;

            // Checks the current flight price for a stored flight (being followed by the user)
            string[] res = QpxExpressApiHelper.GetFlightPrices(false, airports, fd, out guid, out result).ToArray();

            // The result is formatted and returned to the interested user
            return ProcessFlight.OutputResult(res, guid);
        }

        // Main method for checking if any of the stored flights (being followed by users) have changed their prices
        public async Task<bool> CheckForPriceUpdates(Dictionary<string, Airport> airports, FlightDetails fd, 
            Activity activity, ConnectorClient connector, string guid)
        {
            bool changed = false;

            try
            {
                // Connects to Azure Table Storage in order to retrieve th details of
                // flights being watched by users
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    CloudConfigurationManager.GetSetting(StrConsts.cStrStorageConnectionStr));

                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                CloudTable table = tableClient.GetTableReference(
                    CloudConfigurationManager.GetSetting(StrConsts.cStrBotId));

                TableQuery<TravelEntity> query = new TableQuery<TravelEntity>().
                    Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, string.Empty));

                // Gets all the flights being watched
                IEnumerable<TravelEntity> results = table.ExecuteQuery(query);

                // Let's check if any of the flights stored have a change in price
                foreach (TravelEntity entity in results)
                {
                    TripsSearchResponse result = null;
                    TripOptionsRequest req = null;

                    // Get the details of a stored flight (being followed)
                    fd = GetStoredFlightData(entity, storageAccount, out req);

                    // If the stored flight was submitted by the user currently
                    // interacting with our bot - very important :)
                    if (fd.UserId.ToLower() == activity.From.Id.ToLower())
                    {
                        // Query the QPX Express API to see if the current price
                        // of the flight stored differs
                        string res = ShowUpdatedResults(airports, fd, out result);

                        // If the flight is still relevant (has not expired)
                        if (res != string.Empty)
                        {
                            // If indeed the current price differs from when it was
                            // originally requested by the user
                            if (PriceHasChanged(entity, result, storageAccount))
                            {
                                changed = true;

                                // Save the new flight price on Azure Storage Table
                                using (TableStorage ts = new TableStorage(req, result, fd))
                                    guid = ts.Save();

                                changed = true;

                                // Create the response with the current flight price that will be
                                // sent back to the user
                                Activity reply = activity.CreateReply(GatherQuestions.cStrPriceChange +
                                    StrConsts._NewLine + StrConsts._NewLine + res + StrConsts._NewLine +
                                    StrConsts._NewLine + GatherQuestions.cStrGatherRequestProcessed +
                                    StrConsts._NewLine + StrConsts._NewLine +
                                    GatherQuestions.cStrUnFollow + guid);

                                await connector.Conversations.ReplyToActivityAsync(reply);
                            }
                        }
                        else
                        {
                            // The flight is no longer relevant
                            // so it should be removed from Azure Storage
                            RemoveEntity(GetKey(fd));
                        }
                    }
                }
            }
            catch { }

            // If there has been a price change
            // return true
            return changed;
        }

        // Gets flight data stored within an Azure Blob
        protected string GetFromBlob(string guid, CloudStorageAccount storageAccount)
        {
            string res = string.Empty;

            try
            {
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer container = blobClient.GetContainerReference(
                    CloudConfigurationManager.GetSetting(StrConsts.cStrBotId).ToLower());

                container.CreateIfNotExists();

                CloudBlockBlob blob = container.GetBlockBlobReference(guid);

                using (var memoryStream = new MemoryStream())
                {
                    blob.DownloadToStream(memoryStream);
                    res = Encoding.UTF8.GetString(memoryStream.ToArray());
                }
            }
            catch { }

            return res;
        }

        // Removes flight main data stored on Azure Tables
        public bool RemoveEntity(string guid)
        {
            bool res = false;

            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    CloudConfigurationManager.GetSetting(StrConsts.cStrStorageConnectionStr));

                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference(
                        CloudConfigurationManager.GetSetting(StrConsts.cStrBotId));

                TableQuery<TravelEntity> query = new TableQuery<TravelEntity>().
                    Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, string.Empty));

                IEnumerable<TravelEntity> results = table.ExecuteQuery(query);

                foreach (TravelEntity entity in results)
                {
                    if (entity != null && guid.ToUpper() == entity.PartitionKey.ToUpper())
                    {
                        TableOperation deleteOperation = TableOperation.Delete(entity);
                        table.Execute(deleteOperation);

                        RemoveBlob(guid + StrConsts._FlightDetails, storageAccount);
                        RemoveBlob(guid + StrConsts._TravelReq, storageAccount);
                        RemoveBlob(guid + StrConsts._TravelRes, storageAccount);

                        res = true;
                        break;
                    }
                }
            }
            catch { }

            return res;
        }

        // Removes flight request and response data stored on Azure Blobs
        protected CloudBlockBlob RemoveBlob(string guid, CloudStorageAccount storageAccount)
        {
            CloudBlockBlob blob = null;

            try
            {
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer container = blobClient.GetContainerReference(
                    CloudConfigurationManager.GetSetting(StrConsts.cStrBotId).ToLower());

                container.CreateIfNotExists();

                blob = container.GetBlockBlobReference(guid.ToUpper());
                blob.DeleteIfExists();
            }
            catch
            { }

            return blob;
        }

        // Saves flight request and response data to Azure Blobs
        protected string SaveToBlob(string guid, string value, CloudStorageAccount storageAccount)
        {
            string res = guid.ToUpper();

            try
            {
                CloudBlockBlob blob = RemoveBlob(guid, storageAccount);

                if (blob != null)
                    using (var stream = new MemoryStream(Encoding.Default.GetBytes(value), false))
                        blob.UploadFromStream(stream);
            }
            catch { }

            return res;
        }

        // Creates or inserts into Azure Storage Tables the flight details the user is interested in watching
        protected void CreateOrInsertTable(string guid, string origenDest, string reqStr, string resStr, string fdStr)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    CloudConfigurationManager.GetSetting(StrConsts.cStrStorageConnectionStr));

                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                CloudTable table = tableClient.GetTableReference(
                    CloudConfigurationManager.GetSetting(StrConsts.cStrBotId));

                table.CreateIfNotExists();

                TravelEntity entity = new TravelEntity(guid, origenDest);

                entity.FlightDetails = SaveToBlob(guid + StrConsts._FlightDetails, fdStr, storageAccount);
                entity.TravelReq = SaveToBlob(guid + StrConsts._TravelReq, reqStr, storageAccount);
                entity.TravelRes = SaveToBlob(guid + StrConsts._TravelRes, resStr, storageAccount);

                TableOperation insertOperation = TableOperation.InsertOrReplace(entity);

                table.Execute(insertOperation);
            }
            catch { }
        }

        // Create a unique key that identifies the flight details a user is 
        // interested in following
        protected string GetKey(FlightDetails fd)
        {
            return (fd.OriginIata + "_" +
                fd.DestinationIata + "_" +
                fd.OutboundDate + "_" +
                fd.InboundDate + "_" +
                fd.Direct + "_" +
                fd.NumPassengers + "_" +
                fd.UserId).Replace(" ", "_");
        }

        // Save the flight details to Azure Table Storage and Blobs
        public string Save()
        {
            string res = GetKey(flightDetails);

            string reqStr = JsonConvert.SerializeObject(travelReq);
            string resStr = JsonConvert.SerializeObject(travelRes);
            string fdStr = JsonConvert.SerializeObject(flightDetails);

            CreateOrInsertTable(res, res, reqStr, resStr, fdStr);

            return res.ToString();
        }
    }
}