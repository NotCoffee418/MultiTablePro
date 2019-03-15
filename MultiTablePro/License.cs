using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace MultiTablePro
{
    class License : INotifyPropertyChanged
    {
        private string path = "https://multitablepro.com/api/validate_license";
        public License(string key) {
            _key = key;
            
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
            WebRequest wReq = WebRequest.Create(path);
            wReq.Method = "POST";
            string postData = $"macaddr={MacAd}&license_key={Key}&request_product_group=1";
            Logger.Log(postData.ToString());
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            wReq.ContentType = "application/x-www-form-urlencoded";
            wReq.ContentLength = byteArray.Length;
            Stream dataStream = wReq.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            WebResponse wResponse = wReq.GetResponse();
            Logger.Log(((HttpWebResponse)wResponse).StatusDescription);
            dataStream = wResponse.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            Logger.Log(responseFromServer);
            dynamic jsonDecode = JsonConvert.DeserializeObject(responseFromServer);
            ExpDate = jsonDecode.result.expires_at;
            Logger.Log(ExpDate);
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