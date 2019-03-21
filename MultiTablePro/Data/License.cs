﻿using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace MultiTablePro.Data
{
    class License : INotifyPropertyChanged
    {
        private string path = "https://multitablepro.com/api/validate_license";
        public License(string key) {
            _key = key;
            Config.Active.ActiveLicense = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private string _key = "";

        public string Key {            
            get {
                return _key;
            }
            set {
                _key = value;
                RaisePropertyChanged("Key");
            }
        }
        public string ExpDate { get; set; }
        public bool IsValid { get; set; }
        private string MacAd { get; set; }      
        
        public void Start()
        {
            MacAd = GetMac();
            ApiRequest(path);

        }
        private void ApiRequest(string path)
        {
            //Create web request with URL that is able to receive a request.
            WebRequest wReq = WebRequest.Create(path);
            //Set request method type
            wReq.Method = "POST";
            //Create the POST Data and convert it to byteArray
            string postData = $"macaddr={MacAd}&license_key={Key}&request_product_group=1";
            Logger.Log(postData.ToString());//remove
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            //Set the content type property of the web request and it's length.
            wReq.ContentType = "application/x-www-form-urlencoded";
            wReq.ContentLength = byteArray.Length;
            //Get the request stream
            Stream dataStream = wReq.GetRequestStream();
            //Write data to the request
            dataStream.Write(byteArray, 0, byteArray.Length);
            //Close the stream object
            dataStream.Close();
            //Get the response
            WebResponse wResponse = wReq.GetResponse();
            // Log HTTP Status code
            Logger.Log(((HttpWebResponse)wResponse).StatusDescription);
            //Get the stream of content getting returned by server
            dataStream = wResponse.GetResponseStream();
            //Open the stream with streamreader for easy access
            StreamReader reader = new StreamReader(dataStream);
            //Read the content
            string responseFromServer = reader.ReadToEnd();
            Logger.Log(responseFromServer);//Remove
            //Transform raw stream into JSON Object.
            dynamic jsonDecode = JsonConvert.DeserializeObject(responseFromServer);
            //Set expire date
            ExpDate = jsonDecode.result.expires_at;
            Logger.Log(ExpDate);
            //close remaining streams.
            reader.Close();
            dataStream.Close();
            wResponse.Close();
        }

        private string GetMac()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();
        }

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}