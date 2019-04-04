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
using Microsoft.Win32;
using System.Threading;

namespace MultiTablePro.Data
{
    sealed class License : INotifyPropertyChanged
    {
        public License(string key) {
            _key = key;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private Timer ExpiresSoonTimer;
        private Timer ExpiredTimer;
        public event EventHandler ExpiresSoon;
        public event EventHandler Expired;

        private string _key = "";
        private DateTime? _expiresAt;

        // License properties
        public string Key {            
            get {
                return _key;
            }
            private set {
                _key = value;
                RaisePropertyChanged("Key");
            }
        }
        public bool IsValid { get; private set; }
        public bool IsTrial { get; private set; }
        public DateTime? ExpiresAt
        {
            get {
                return _expiresAt;
            }
            private set {
                bool expChanged = _expiresAt != value;
                _expiresAt = value;

                // Create expiration events
                if (expChanged && ExpiresAt != null && ExpiresAt > DateTime.Now)
                {
                    TimeSpan expiresIn = ExpiresAt.Value.Subtract(DateTime.Now);
                    TimeSpan expiresSoonDiff = new TimeSpan(0, 15, 0);
                    ExpiresSoonTimer = new Timer(TriggerExpireTimer, false, expiresIn.Subtract(expiresSoonDiff).Milliseconds, Timeout.Infinite);
                    ExpiredTimer = new Timer(TriggerExpireTimer, true, expiresIn.Milliseconds, Timeout.Infinite);
                }
            }
        }

        public string LicenseStatusMessage { get; private set; }

        // User properties
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }

        // Product Properties
        public string ProductName { get; private set; }
        public string ProductDescription { get; private set; }
        public Dictionary<string, string> Restrictions { get; private set; }

        public bool Validate()
        {
            // Make API request
            var postData = new Dictionary<string, string>()
            {
                { "macaddr", GetMac() },
                { "license_key", Key },
                { "request_product_group", "1" }
            };
            var apiOutput = Api.ApiRequest<ApiData.ValidateLicense>("validate_license", postData);

            // Errors occurred (invalid license, internet connection etc..)
            if (apiOutput.Errors.Count() > 0)
            {
                // Log errors
                foreach (string err in apiOutput.Errors)
                    Logger.Log("License Validate: " + err, Logger.Status.Error);
                return false;
            }

            // Store license data
            ApiData.ValidateLicense lData = apiOutput.Result;
            Email = lData.Email;
            ExpiresAt = lData.ExpiresAt;
            FirstName = lData.FirstName;
            LastName = lData.LastName;
            LicenseStatusMessage = lData.LicenseStatusMessage;
            IsValid = lData.IsValid;
            IsTrial = lData.IsTrial;
            ProductName = lData.ProductName;
            ProductDescription = lData.ProductDescription;
            Restrictions = lData.Restrictions;

            // Return validity
            return IsValid;
        }

        /// <summary>
        /// Saves the current License to registry
        /// Only call on valid license keys & when license key was changed
        /// </summary>
        public void Save()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\MultiTable Pro", true);
            registryKey.SetValue("licensekey", Key);
        }

        /// <summary>
        /// Loads last known license or trial license
        /// </summary>
        /// <returns>Instance of License class</returns>
        public static License GetKnownLicense()
        {
            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\MultiTable Pro", true);
                object rLicKeyValue = registryKey.GetValue("licensekey"); // null when key doesn't exist,
                if (rLicKeyValue == null)
                    return new License("TRIAL");
                else return new License((string)rLicKeyValue);
            }
            catch // can occur when user manually changes registry value to non-string
            {
                Logger.Log("Registry error while trying to grab license key. Using TRIAL instead.", Logger.Status.Warning);
                return new License("TRIAL");
            }
        }
        
        private string GetMac()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();
        }


        private void TriggerExpireTimer(object state)
        {
            // false is warning for expires soon - true means is expired
            bool isExpired = (bool)state;

            // Don't show warning or shut down if user has renewed since
            DateTime previousExpTime = ExpiresAt.Value;
            Validate(); // check with API again
            if (ExpiresAt.HasValue && previousExpTime < ExpiresAt.Value)
                return; // new timers are created

            // Fire expired event
            if (isExpired)
            {
                IsValid = false;
                if (Expired != null)
                    Expired(this, new EventArgs());
            }
            else // fire warning event
            {
                if (ExpiresSoon != null)
                    ExpiresSoon(this, new EventArgs());
            }
        }

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}