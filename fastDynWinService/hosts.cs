using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace fastDynClient
{
    public class Hosts : TableEntity
    {
        public Hosts(string hostname, string cdate)
        {
            this.PartitionKey = hostname;
            this.RowKey = cdate;
        }

        public Hosts() { }

        public string ipaddress { get; set; }
        public DateTime udate { get; set; }
    }

}
