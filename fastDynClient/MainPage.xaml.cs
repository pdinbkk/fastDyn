using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace fastDynClient
{
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            object nvalue = value;
            var format = parameter as string;
            if (!String.IsNullOrEmpty(format))
            {
                //convert UTC to local first
                nvalue= DateTime.SpecifyKind(DateTime.Parse(value.ToString()), DateTimeKind.Utc).ToLocalTime();
                object ndate= String.Format(format, nvalue).Replace("Date: ", "");
                return (ndate);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public sealed partial class MainPage : Page
    {
        private readonly ObservableCollection<Hosts> _items = new ObservableCollection<Hosts>();
        public ObservableCollection<Hosts> items
        {
            get { return _items; }
        }


        public MainPage()
        {
            this.InitializeComponent();
        }

        private void btnGetHostList_Click(object sender, RoutedEventArgs e)
        {
            getDatafromAzure();
        }
        async void getDatafromAzure()
        {
            var credentials = new StorageCredentials("fastdyn", "+d2JKqRMCOZZZs1EQLe+keQFCTL/+//B0ahPXTAK6c7btyhrYQ5d965xFe2RmfIg6ww+ZAkj0DNNkPsN96LOZQ==");
            CloudStorageAccount storageAccount = new CloudStorageAccount(credentials, true);


            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference("hosts");

            // Create the table query.
            TableQuery<Hosts> rangeQuery = new TableQuery<Hosts>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, "blah"));


            // Loop through the results, displaying information about the entity.
            TableContinuationToken Token = new TableContinuationToken();
            TableQuerySegment<Hosts> x = await table.ExecuteQuerySegmentedAsync(rangeQuery, Token);
            foreach (Hosts host  in x)
            {
                //Hosts row1 = host.udate.ToString() + "\t" + host.PartitionKey + "\t" + host.ipaddress;
                _items.Add(host);

            }
            lvHosts.ItemsSource = items;
            lvHosts.SelectedIndex = 0;
            lvHosts.SelectionMode = ListViewSelectionMode.Single;

            return;
        }
    }
}
