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

        // Events
        public event PropertyChangedEventHandler PropertyChanged;
        private Timer ExpireCheckTimer;
        public event EventHandler ExpirationEvent;

        // fields with defaults
        private string _key = "";
        private DateTime? _expiresAt;
        private int _maxStake = 0;
        private BuildTypes _buildType = BuildTypes.BETA;

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
                bool expChanged = !_expiresAt.HasValue || _expiresAt.Value != value;
                _expiresAt = value;

                // Start checkign for expiration
                if (expChanged && ExpiresAt != null && ExpiresAt > DateTime.Now)
                    ExpireCheckTimer = new Timer(TriggerExpireTimer, null, 900000, 900000); // check every 15 mins
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

        // Restriction properties
        public int MaxStake // BB * 100 (buyin)
        {
            get { return _maxStake; }
            private set { _maxStake = value; }
        }
        public bool UnlimitedComputers { get; private set; } // NIY
        public BuildTypes BuildType // opt-in in settings - this is the max allowed
        {
            get { return _buildType; }
            private set { _buildType = value; }
        }

        // Access to builds through license
        public enum BuildTypes
        {
            RELEASE = 0,
            BETA = 1,
            INTERNAL = 2,
        }

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
            SetRestrictions(lData.Restrictions);

            // Save the key if it's valid
            if (IsValid)
                Save();
            
            // Return validity
            return IsValid;
        }

        private void SetRestrictions(Dictionary<string, string> restrictions)
        {
            if (restrictions == null) // happens on trials, assume defaults
                return;

            // Apply relevant restrictions, use defaults for everything else
            foreach(var r in restrictions)
            {
                switch (r.Key.ToUpper())
                {
                    case "MAX_STAKE":
                        int maxStake;
                        if (int.TryParse(r.Value, out maxStake))
                            MaxStake = maxStake;
                        else Logger.Log($"License: {r.Value} is not a valid integer for license restriction MAX_STAKE. Please contact support.",
                                Logger.Status.Fatal, showMessageBox: true); // should never happen
                        break;
                    case "UNLIMITED_COMPUTERS":
                        UnlimitedComputers = r.Value.ToUpper() == "TRUE" ? true : false;
                        break;
                    case "BUILDTYPE":
                        BuildTypes buildType;
                        if (Enum.TryParse(r.Value.ToUpper(), out buildType))
                            BuildType = buildType;
                        else Logger.Log($"License: {r.Value} is not a valid build type. Please contact support about this warning.",
                            Logger.Status.Warning, showMessageBox: true);
                        break;

                    // restriction NIY
                    default:
                        Logger.Log($"License: {r.Key}:{r.Value} is an undefined restriction. User may be on outdated version.",
                            Logger.Status.Warning, showMessageBox: false);
                        break;
                }
            }
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


        private void TriggerExpireTimer(object irrelevant)
        {
            var expireWarningTime = ExpiresAt.Value.Subtract(new TimeSpan(0, 30, 0));
            bool isExpired = DateTime.Now > ExpiresAt;
            bool expiresSoon = DateTime.Now > expireWarningTime;

            // Fire expired event
            if (isExpired || expiresSoon)
            {
                IsValid = false;
                if (ExpirationEvent != null)
                    ExpirationEvent(this, new ExpirationEventArgs(ExpiresAt.Value.Subtract(DateTime.Now)));
            }
        }

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public class ExpirationEventArgs : EventArgs
        {
            public ExpirationEventArgs(TimeSpan expiresIn)
            {
                ExpiresIn = expiresIn;
                IsExpired = ExpiresIn < TimeSpan.Zero;
            }

            public TimeSpan ExpiresIn { get; private set; }
            public bool IsExpired { get; private set; }
        }
    }
}