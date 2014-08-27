﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

using Newtonsoft.Json;
using Windows.UI.Xaml.Data;


// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace Office365RESTExplorerforSites.Data
{
    public class RequestItem
    {
        public RequestItem(string apiUrl, string method, JsonObject headers, JsonObject body)
        {
            this.ApiUrl = apiUrl;

            if (String.Compare(method, "GET", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                this.Method = false;
            }

            else if (String.Compare(method, "POST", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                this.Method = true;
            }
            else
                throw new ArgumentOutOfRangeException("The HTTP method can only be GET or POST.");

            this.Headers = headers;
            this.Body = body;
        }

        public string ApiUrl { get; private set; }
        public JsonObject Headers { get; private set; }
        public JsonObject Body { get; private set; }

        public bool Method { get; private set; }
    }
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class DataItem
    {
        public DataItem(String uniqueId, String title, String subtitle, String imagePath)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.ImagePath = imagePath;
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string ImagePath { get; private set; }
        public string ApiUrl { get; private set; }
        public RequestItem Request { get; set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class DataGroup
    {
        public DataGroup(String uniqueId, String title, String subtitle, String imagePath, String moreInfoText, String moreInfoUri)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.ImagePath = imagePath;
            this.MoreInfoText = moreInfoText;
            this.MoreInfoUri = moreInfoUri;
            this.Items = new ObservableCollection<DataItem>();
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string ImagePath { get; private set; }
        public string MoreInfoUri { get; private set; }
        public string MoreInfoText { get; private set; }
        public ObservableCollection<DataItem> Items { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// SampleDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class DataSource
    {
        private static DataSource _sampleDataSource = new DataSource();

        private ObservableCollection<DataGroup> _groups = new ObservableCollection<DataGroup>();
        public ObservableCollection<DataGroup> Groups
        {
            get { return this._groups; }
        }

        public static async Task<IEnumerable<DataGroup>> GetGroupsAsync()
        {
            await _sampleDataSource.GetSampleDataAsync();

            return _sampleDataSource.Groups;
        }

        public static async Task<DataGroup> GetGroupAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<DataItem> GetItemAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetSampleDataAsync()
        {
            if (this._groups.Count != 0)
                return;

            Uri dataUri = new Uri("ms-appx:///DataModel/InitialData.json");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            string jsonText = await FileIO.ReadTextAsync(file);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                DataGroup group = new DataGroup(groupObject["UniqueId"].GetString(),
                                                            groupObject["Title"].GetString(),
                                                            groupObject["Subtitle"].GetString(),
                                                            groupObject["ImagePath"].GetString(),
                                                            groupObject["MoreInfoText"].GetString(),
                                                            groupObject["MoreInfoUri"].GetString());

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    JsonObject requestObject = itemObject["Request"].GetObject();

                    //Add the Authorization header with the access token.
                    JsonObject jsonHeaders = requestObject["Headers"].GetObject();
                    jsonHeaders["Authorization"] = JsonValue.CreateStringValue(jsonHeaders["Authorization"].GetString() + ApplicationData.Current.LocalSettings.Values["AccessToken"].ToString());

                    //Create the request object
                    RequestItem request = new RequestItem(requestObject["ApiUrl"].GetString(),
                                                       requestObject["Method"].GetString(),
                                                       jsonHeaders,
                                                       requestObject["Body"].GetObject());

                    //Create the data item object
                    DataItem item = new DataItem(itemObject["UniqueId"].GetString(),
                                                       itemObject["Title"].GetString(),
                                                       itemObject["Subtitle"].GetString(),
                                                       itemObject["ImagePath"].GetString()
                                                       );

                    // Add the request object to the item
                    item.Request = request;
                    
                    //Add the item to the group
                    group.Items.Add(item);


                }
                this.Groups.Add(group);
            }
        }
    }

    public class JsonObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            JsonObject jsonObject = (JsonObject)value;
            return JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            String strJson = (String)value;
            return JsonObject.Parse(strJson);
        }
    }

}