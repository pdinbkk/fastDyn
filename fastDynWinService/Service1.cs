using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure;
using fastDynClient;
using System.Timers;

namespace fastDynWinService
{
    public partial class fastDynSvc : ServiceBase
    {
        private System.Timers.Timer timer1 = null;
        int x = 0;
        public string myHostName = "NOT SET";
        static public string currentIP = "none";
        static string publicIPAddress = "1.1.1.1";
        static System.Diagnostics.EventLog appLog = new System.Diagnostics.EventLog();

        static string GetPublicIpAddress()
        {
            currentIP = publicIPAddress;
            var request = (HttpWebRequest)WebRequest.Create("http://myexternalip.com/raw");
            request.UserAgent = "curl";
            request.Method = "GET";
            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        publicIPAddress = reader.ReadToEnd();
                        publicIPAddress = publicIPAddress.Replace("\n", "");
                    }
                }
            }
            catch (Exception Ex)
            {
                appLog.WriteEntry(DateTime.Now.ToShortTimeString() + " IP Address lookup failed");
                appLog.WriteEntry(Ex.Message.ToString());
                //Thread.Sleep(TimeSpan.FromSeconds(60)); //let it sleep and try again in 1 minute
            }

            //verify that is is a valid IP address, not some junk string
            try
            {
                // Create an instance of IPAddress for the specified address string (in 
                // dotted-quad, or colon-hexadecimal notation).
                IPAddress address = IPAddress.Parse(publicIPAddress);
            }
            catch (Exception ex)
            {
                appLog.WriteEntry("Lookup call produced junk: "+ex.Message);
                publicIPAddress = currentIP; //set it back to the current known good IP
            }

            return publicIPAddress;
        }

        public fastDynSvc()
        {
            InitializeComponent();
            myHostName = System.Environment.MachineName;
        }

        protected override void OnStart(string[] args)
        {

            timer1 = new System.Timers.Timer();
            this.timer1.Interval = 60000;
            this.timer1.Elapsed += new System.Timers.ElapsedEventHandler(this.timer1_Tick);
            timer1.Enabled = true;
            appLog.Source = "fastDYN";
            appLog.WriteEntry("fastDYN started");

        }

        protected override void OnStop()
        {
            timer1.Enabled = false;
            appLog.Source = "fastDYN";
            appLog.WriteEntry("fastDYN stopped");

        }
        private void timer1_Tick(Object sender, ElapsedEventArgs e)
        {

            x++;

                string myIP = GetPublicIpAddress();
            if (myIP == currentIP)
            {
                if (x > 60) // write an entry every hour
                {
                    appLog.WriteEntry(DateTime.Now.ToShortTimeString() + " No Change - IP Address = " + myIP);
                    x = 0;
                }
            }
            else
            {
                try
                {
                    // Update host on Azure                    
                    // Parse the connection string and return a reference to the storage account.
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
                    // Create the table client.
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    // Retrieve a reference to the table.
                    CloudTable table = tableClient.GetTableReference("hosts");
                    // Create the table if it doesn't exist.
                    table.CreateIfNotExists();

                    // Create a new customer entity.
                    Hosts host1 = new Hosts(myHostName, DateTime.Now.ToShortTimeString());
                    host1.ipaddress = myIP;
                    host1.udate = DateTime.Now;

                    // Create the TableOperation object that inserts the customer entity.
                    TableOperation insertOperation = TableOperation.Insert(host1);

                    // Execute the insert operation.
                    table.Execute(insertOperation);

                    appLog.WriteEntry(DateTime.Now.ToShortTimeString() + " IP Address for " + myHostName + " Changed to: " + myIP);
                }
                catch (Exception ex)
                {
                    appLog.WriteEntry(DateTime.Now.ToShortTimeString() + " Azure update failed. Message: " + ex.Message);
                }
                try
                {
                    //update Dyndns
                    string target = string.Format("http://members.dyndns.org:8245/nic/update?hostname={0}&myip={1}", myHostName + ".dyndns.ws", myIP);
                    HttpWebRequest request = WebRequest.Create(target) as HttpWebRequest;
                    request.Method = "GET";
                    request.UserAgent = "fastDYN - fastDYN - 1.0";
                    NetworkCredential myCredentials = new NetworkCredential("apat", "aa42b188c3e811e48114dc53a2cf6791");
                    request.Credentials = myCredentials;
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    Stream reply = response.GetResponseStream();
                    StreamReader readReply = new StreamReader(reply);
                    string returnCode = readReply.ReadToEnd();
                    appLog.WriteEntry(DateTime.Now.ToShortTimeString() + " Dyndns Updated Successfully");
                }
                catch (Exception ex)
                {
                    appLog.WriteEntry(DateTime.Now.ToShortTimeString() + " DynDns.org update failed. Message: " + ex.Message);
                }
            }
         }
    }
}
