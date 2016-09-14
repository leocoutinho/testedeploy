using easydeploy.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace easydeploy
{
    public class DataManager : IDisposable
    {
        #region [ Private Constants Fields ]

        private const int FeedTimerIntervalInMiliseconds = 60000;

        #endregion

        #region [ Private Static Readonly Fields ]

        private static readonly MediaTypeWithQualityHeaderValue JsonMediaTypeWithQualityHeaderValue = new MediaTypeWithQualityHeaderValue("application/json");
        private static readonly Queue<HttpClient> HttpClientQueue = new Queue<HttpClient>();

        #endregion

        #region [ Private Fields ]

        private string accountName = string.Empty;
        private string appKey = string.Empty;
        private string appToken = string.Empty;
        private string instrumentationkey = string.Empty;

        private EventHubClient ordersEventHub = null;
        private Timer feedTimer = null;
        private TelemetryClient appInsightsClient = null;
        private ApplicationInfo appInfo = null;

        /// <summary>
        /// When true insted of getting orders from vtex oms API it generates random values
        /// </summary>
        private bool GenerateRandomValues
        {
            get
            {
                try { return bool.Parse(ConfigurationManager.AppSettings["GenerateRandomValues"]); }
                catch
                {
                    return false;
                }
            }
        }

        #endregion

        public ApplicationInfo AppInfo()
        {
            return appInfo;
        }

        #region [ Constructors ]

        public DataManager()
        {
            this.InitializeApplicationInfo();

            this.InitializeConfigurationsFields();
            this.InitializeEventHub();
            this.InitializeAppInsights();
            this.InitializeFeedTimer();
        }

        private void InitializeApplicationInfo()
        {
            appInfo = new ApplicationInfo();
            appInfo.CheckApplicationSettings();
        }

        #endregion

        #region [ Initialization Methods ]
        /// <summary>
        /// Initialize Event Hub
        /// </summary>
        private void InitializeEventHub()
        {
            try
            {
                ordersEventHub = EventHubClient.CreateFromConnectionString(ConfigurationManager.AppSettings["servicebusconnectionstring"], ConfigurationManager.AppSettings["eventhubname"]);
            }
            catch (Exception ex)
            {
                appInfo.UpdateFeatureInfo(KPIName.AzureEventHub, "InitializeEventHub", FeatureStatus.Error, ex.Message);
                appInfo.CriticalError = true;
            }
        }

        private void InitializeAppInsights()
        {
            this.appInsightsClient = new TelemetryClient();
            if (!String.IsNullOrEmpty(this.instrumentationkey))
                this.appInsightsClient.InstrumentationKey = this.instrumentationkey;
            else
                appInfo.UpdateFeatureInfo(KPIName.AzureApplicationInsights, "InitializeAppInsights", FeatureStatus.Error, "Instrumentation Key is null or empty. It's not possible to use Application Insights, not a critical error");
        }

        /// <summary>
        /// Create data retrieve timer
        /// </summary>
        private void InitializeFeedTimer()
        {
            appInfo.UpdateFeatureInfo(KPIName.ApplicationPoolingTime, FeedTimerIntervalInMiliseconds.ToString(), FeatureStatus.Success, " Amount of time in milliseconds ");

            this.feedTimer = new Timer(FeedTimerIntervalInMiliseconds);
            this.feedTimer.AutoReset = false;
            this.feedTimer.Enabled = true;
            this.feedTimer.Elapsed += this.FeedTimer_Elapsed;
            this.FeedTimer_Elapsed(this, null);
        }

        /// <summary>
        /// Initialize fields from configuration application settings
        /// </summary>
        private void InitializeConfigurationsFields()
        {
            try
            {
                this.accountName = ConfigurationManager.AppSettings["vtexaccountname"];
                this.appKey = ConfigurationManager.AppSettings["vtexappkey"];
                this.appToken = ConfigurationManager.AppSettings["vtexapptoken"];
                this.instrumentationkey = ConfigurationManager.AppSettings["instrumentationkey"];
            }
            catch (Exception ex)
            {
                appInsightsClient.TrackTrace(ex.Message, SeverityLevel.Critical);
                appInsightsClient.Flush();
                appInfo.CriticalError = true;
            }
        }

        
        #endregion
       
        #region [ Retrieve Data Methods ]

        private void FeedTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            

            if (appInfo.CriticalError)
            {
                appInfo.UpdateFeatureInfo(KPIName.ApplicationDeployment, "", FeatureStatus.Error, "FeedTimer_Elapsed - Critical error detected before try to connect to the services, probably becase there's something wrong with other main services check other Errors bellow");
                appInsightsClient.TrackTrace("Application in Critical Error status, please fix the error prior to use it", SeverityLevel.Critical);
                appInsightsClient.Flush();
                return;
            }

            appInfo.UpdateFeatureInfo(KPIName.DataLastFeed, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"), FeatureStatus.Success, "Consuming feed");

            if (!GenerateRandomValues)
            {
                appInfo.UpdateFeatureInfo(KPIName.OperationMode, "Consuming Vtex OMS Feed", FeatureStatus.Success, "Reading the data from your vtex installation");

                var retrieveTask = this.RetreiveOmsFeedData();
                if (retrieveTask.Status != TaskStatus.Faulted)
                {
                    retrieveTask.ConfigureAwait(false);
                    retrieveTask.ContinueWith(task =>
                    {
                        JArray omsFeedJsonData = task.Result;
                        List<VtexFeedOrder> feedOrders = this.TransformToVtexFeedOrders(omsFeedJsonData);
                        this.appInsightsClient.TrackMetric("FeedRetrievedItens", feedOrders.Count);
                        foreach (var feedOrder in feedOrders)
                        {
                            var order = this.GetVtexOrder(feedOrder).Result;
                            this.ProcessOrder(order);
                            this.CommitFeedToken(feedOrder.CommitToken).Wait();
                            appInfo.UpdateFeatureInfo(KPIName.DataLastOrder, order.Timestamp.ToString("MM/dd/yyyy HH:mm:ss"), FeatureStatus.Success, string.Format("Order data Feed - AffiliateId: {0}", order.AffiliateId));
                        }
                        this.feedTimer.Start();
                    });
                }
            }
            else
            {
                appInfo.UpdateFeatureInfo(KPIName.OperationMode, "Demostration Mode - Generating Values Randomically", FeatureStatus.Success, "Use this mode to test the integration with PowerBI");

                appInfo.UpdateFeatureInfo(KPIName.VtexOmsFeed, "Not applyable, because de Application is Running is Demostration Mode", FeatureStatus.Unavailable, "In demostration mode the vtex service is not tested");

                var feedOrders = this.GetRandomOrders(10);
                foreach (VtexOrder order in feedOrders)
                {
                    this.ProcessOrder(order);
                    appInfo.UpdateFeatureInfo(KPIName.DataLastOrder, order.Timestamp.ToString("MM/dd/yyyy HH:mm:ss"), FeatureStatus.Success, string.Format("Order generated Ramdomically - AffiliateId: {0}", order.AffiliateId));
                }
                this.feedTimer.Start();
            }
        }

        private List<VtexOrder> GetRandomOrders(int NumOrders)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            List<VtexOrder> VtexOrders = new List<VtexOrder>();
            Random rnd = new Random();
            for (int i = 0; i <= NumOrders; i++)
            {
                int status = -1;
                VtexFeedOrder vtexfeed = new VtexFeedOrder();
                vtexfeed.CommitToken = string.Format("{0}-{1}", "CommitToken", rnd.Next(0, int.MaxValue).ToString());
                vtexfeed.DateTime = DateTime.Now.ToString();
                vtexfeed.OrderId = rnd.Next(0, int.MaxValue).ToString();
                status = rnd.Next(0, 3);

                switch (status)
                {
                    case (int)OrderStatus.Canceled: vtexfeed.Status = "canceled"; break;
                    case (int)OrderStatus.PaymentApproved: vtexfeed.Status = "payment-approved"; break;
                    case (int)OrderStatus.WaitingForSellerConfirmation: vtexfeed.Status = "waiting-for-seller-confirmation"; break;
                }

                var orderValue = rnd.Next(1, 10000).ToString();
                StringBuilder jsobnstr = new StringBuilder("");
                jsobnstr.AppendFormat("{{\"orderId\": \"{0}\", \"sequence\": \"67155135\", \"marketplaceOrderId\": \"\",\"marketplaceServicesEndpoint\": \"http://portal.vtexcommerce.com.br/api/oms?an=shopvtex\",", vtexfeed.OrderId);
                jsobnstr.AppendFormat("\"sellerOrderId\": \"00-921743325842-01\", \"origin\": \"Random Orders\",\"affiliateId\": \"\",\"salesChannel\": \"2\",\"status\": \"{0}\",\"statusDescription\": \"Faturado\",\"value\": {1},", vtexfeed.Status, orderValue);
                jsobnstr.AppendFormat("\"creationDate\": \"{0}\",\"lastChange\": \"{0}\",\"orderGroup\": \"421743525842\",\"totals\": [{{\"id\": \"Items\", \"name\": \"Items Total\",\"value\": {1}}},", DateTime.Now.ToString(), orderValue);
                jsobnstr.Append("{\"id\": \"Discounts\",\"name\": \"Discounts Total\"},{\"id\": \"Shipping\", \"name\": \"Shipping Total\",\"value\": 756},{\"id\": \"Tax\", \"name\": \"Tax Total\"}]}");

                vtexfeed.JsonOrderContent = jsobnstr.ToString();

                var order = TransformToVtexOrder(vtexfeed.OrderId, vtexfeed.Status, vtexfeed.JsonOrderContent);
                VtexOrders.Add(order);
            }

            return VtexOrders;

        }


        private HttpClient DequeueHttpClientInstance()
        {
            HttpClient client = null;

            if (HttpClientQueue.Count > 0)
            {
                try
                {
                    client = HttpClientQueue.Dequeue();
                }
                catch (InvalidOperationException)
                {
                }
            }
            if (client == null)
                client = GetNewHttpClientInstance();
            return client;
        }

        private void EnqueueHttpClientInstance(HttpClient client)
        {
            HttpClientQueue.Enqueue(client);
        }

        private HttpClient GetNewHttpClientInstance()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-vtex-api-appKey", this.appKey);
            client.DefaultRequestHeaders.Add("x-vtex-api-appToken", this.appToken);
            client.DefaultRequestHeaders.Accept.Add(JsonMediaTypeWithQualityHeaderValue);
            return client;
        }

        /// <summary>
        /// Retrieve OMS Feed data
        /// </summary>
        private async Task<JArray> RetreiveOmsFeedData()
        {
            JArray result = null;
            string response = string.Empty;
            HttpClient client = this.DequeueHttpClientInstance();
            string uri = "http://" + this.accountName + ".vtexcommercestable.com.br/api/oms/pvt/feed/orders/status";

            try
            {
                Uri address = new Uri(uri);
                HttpResponseMessage httpResponse = await client.GetAsync(address);
                result = await HandleHttpResponse("RetreiveOmsFeedData", httpResponse);
            }
            catch (Exception ex)
            {
                appInfo.UpdateFeatureInfo(KPIName.VtexOmsFeed, "Error: RetreiveOmsFeedData", FeatureStatus.Error, "Error Occurred trying to read vtex OMS Feed - " + uri +" -  " + ex.Message);

                this.appInsightsClient.TrackException(ex);
                this.EnqueueHttpClientInstance(client);
                throw ex.GetBaseException();
            }
            return result;
        }

        /// <summary>
        /// Transform json array to OMS Feed data
        /// </summary>
        private List<VtexFeedOrder> TransformToVtexFeedOrders(JArray jsonData)
        {
            List<VtexFeedOrder> orders = new List<VtexFeedOrder>();
            foreach (var jsonFeedOrder in jsonData)
            {
                VtexFeedOrder feedOrder = new VtexFeedOrder(jsonFeedOrder);
                orders.Add(feedOrder);
            }
            return orders;
        }

        private VtexOrder TransformToVtexOrder(string orderId, string status, string orderJsonContent)
        {
            JObject jsonObject = JObject.Parse(orderJsonContent);
            return new VtexOrder(this.accountName, orderId, status, jsonObject);
        }

        private async Task<VtexOrder> GetVtexOrder(VtexFeedOrder feedOrder)
        {
            VtexOrder result = null;
            string response = string.Empty;
            HttpClient client = this.DequeueHttpClientInstance();
            string uri = "http://" + this.accountName + ".vtexcommercestable.com.br/api/oms/pvt/orders/" + feedOrder.OrderId;
            try
            {
                Uri address = new Uri(uri);

                HttpResponseMessage httpResponse = await client.GetAsync(address);
                JArray jsonArray = await HandleHttpResponse("GetVtexOrder", httpResponse);
                response = await httpResponse.Content.ReadAsStringAsync();

                result = this.TransformToVtexOrder(feedOrder.OrderId, feedOrder.Status, response);
            }
            catch (Exception ex)
            {
                appInfo.UpdateFeatureInfo(KPIName.VtexOmsFeed, "Error: RetreiveOmsFeedData", FeatureStatus.Error, "Error Occurred trying to read vtex OMS Feed - " + uri +" -  " + ex.Message);

                this.appInsightsClient.TrackException(ex);
                this.EnqueueHttpClientInstance(client);
                throw ex.GetBaseException();
            }

            return result;
        }



        private async Task<JArray> HandleHttpResponse(String method, HttpResponseMessage httpResponse)
        {
            JArray result = null;
            String response = string.Empty;

            if (httpResponse.IsSuccessStatusCode)
            {
                response = await httpResponse.Content.ReadAsStringAsync();
                result = JArray.Parse(response);
            }
            else
            {
                switch (httpResponse.StatusCode)
                {
                    case System.Net.HttpStatusCode.InternalServerError:

                        this.appInsightsClient.TrackTrace(string.Format("{0} - HttpResponse: {1}", method, httpResponse.StatusCode), SeverityLevel.Critical);
                        appInfo.UpdateFeatureInfo(KPIName.VtexOmsFeed, "Internal Server Error", FeatureStatus.Error, " Something is wrong with the vtex server");

                        break;
                    case System.Net.HttpStatusCode.Forbidden:
                        this.appInsightsClient.TrackTrace(string.Format("{0} - HttpResponse: {1}", method, httpResponse.StatusCode), SeverityLevel.Critical);
                        appInfo.CriticalError = true;
                        appInfo.UpdateFeatureInfo(KPIName.VtexOmsFeed, "Access to the API is Forbidden", FeatureStatus.Error, " ckeck the vtex token, vtex account, vtex apkey");

                        break;
                    default:
                        this.appInsightsClient.TrackTrace(string.Format("{0} - HttpResponse: {1}", method, httpResponse.StatusCode), SeverityLevel.Warning);
                        appInfo.CriticalError = true;
                        appInfo.UpdateFeatureInfo(KPIName.VtexOmsFeed, string.Format("{0} - HttpResponse: {1}", method, httpResponse.StatusCode), FeatureStatus.Error, " ckeck the vtex token, vtex account, vtex apkey");

                        break;
                }

            }

            return result;
        }


        private async Task CommitFeedToken(string feedCommitToken)
        {
            HttpClient client = this.DequeueHttpClientInstance();
            Uri address = new Uri("http://" + this.accountName + ".vtexcommercestable.com.br/api/oms/pvt/feed/orders/status/confirm");
            try
            {
                feedCommitToken = feedCommitToken.Replace("\"", "\\\"");
                string postContent = "[{\"commitToken\":\"" + feedCommitToken + "\"}]";
                var response = await client.PostAsync(address, new StringContent(postContent, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                this.appInsightsClient.TrackException(ex);
                this.EnqueueHttpClientInstance(client);
                throw ex.GetBaseException();
            }
        }

        #endregion

        #region [ Process Data Methods ]

        private void ProcessOrder(VtexOrder order)
        {
            //Eventhub (for all the status)
            this.RegisterEventHubOrder(ordersEventHub, order);

        }

        private void RegisterEventHubOrder(EventHubClient eventhubclient, VtexOrder order)
        {
            if (eventhubclient == null || order == null)
                return;

            var memoryStream = new MemoryStream();
            var sw = new StreamWriter(memoryStream);
            sw.Write(JsonConvert.SerializeObject(order));
            sw.Flush();

            RegisterAppInsightEvent(order);

            memoryStream.Position = 0;
            EventData data = new EventData(memoryStream);
            try
            {
                var eventHubTask = eventhubclient.SendAsync(data);
                eventHubTask.ConfigureAwait(true);
                appInfo.UpdateFeatureInfo(KPIName.AzureEventHub, "Data writeen with success", FeatureStatus.Success, "The data was written to the event hub with success!" );
            }
            catch(Exception ex)
            {
                appInfo.UpdateFeatureInfo(KPIName.AzureEventHub, "Error trying to write data", FeatureStatus.Error, ex.Message);
            }
        }

        private void RegisterAppInsightEvent(VtexOrder order)
        {
            try
            {
                var realValue = Double.Parse(order.Value) / 100; //TODO: Check how to make this configurable for account that use more than 2 digits
                this.TrackEvent(order, realValue);
                this.appInsightsClient.Flush();
                appInfo.UpdateFeatureInfo(KPIName.AzureApplicationInsights, "Application Insights is working", FeatureStatus.Success, "The events are tracking");
            }
            catch (Exception ex)
            {
                appInfo.UpdateFeatureInfo(KPIName.AzureApplicationInsights, "Application Insights is not working", FeatureStatus.Error, ex.Message);
            }
        }


        private void TrackEvent(VtexOrder order, double realValue)
        {
            EventTelemetry telemetry = new EventTelemetry("Order");
            telemetry.Properties.Add("AccountName", order.AccountName);
            telemetry.Properties.Add("AffiliateId", order.AffiliateId);
            telemetry.Properties.Add("LastChange", DateTimeOffset.Parse(order.LastChange).ToString("o"));
            telemetry.Properties.Add("Origin", order.Origin);
            telemetry.Properties.Add("SalesChannel", order.SalesChannel);
            telemetry.Properties.Add("Status", order.Status);
            telemetry.Timestamp = DateTimeOffset.Parse(order.CreationDate);
            telemetry.Metrics.Add("Value", realValue);
            this.appInsightsClient.TrackEvent(telemetry);
        }

        private void TrackMetric(VtexOrder order, double realValue)
        {
            MetricTelemetry telemetry = new MetricTelemetry("OrderAmount", realValue);
            telemetry.Timestamp = DateTimeOffset.Parse(order.CreationDate);
            telemetry.Properties.Add("AccountName", order.AccountName);
            telemetry.Properties.Add("AffiliateId", order.AffiliateId);
            telemetry.Properties.Add("LastChange", DateTimeOffset.Parse(order.LastChange).ToString("o"));
            telemetry.Properties.Add("Origin", order.Origin);
            telemetry.Properties.Add("SalesChannel", order.SalesChannel);
            telemetry.Properties.Add("Status", order.Status);
            this.appInsightsClient.TrackMetric(telemetry);
        }


        #endregion

        #region [ IDisposable Interface Methods ]

        public void Dispose()
        {
            this.feedTimer.Stop();
            this.feedTimer.Dispose();
            this.feedTimer = null;
        }

        #endregion
    }
}