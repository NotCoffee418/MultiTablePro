using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiTablePro.Data
{
    sealed class ApiData
    {
        internal class ApiResponse<T>
        {
            private string[] _errors = new string[0];
            internal string[] Errors {
                get { return _errors; }
                set { _errors = value; }
            }

            private T _result;
            internal T Result
            {
                get { return _result; }
                set { _result = value; }
            }

            // Constructors
            public ApiResponse() { }

            // Json constructor
            [JsonConstructor]
            public ApiResponse(T result, string[] errors) {
                _result = result;
                _errors = errors;
            }

            // Constructor for failed requests
            public ApiResponse(Exception ex)
            {
                // Log the error
                Logger.Log("API: " + ex.Message, Logger.Status.Error);

                // Store the error in Error result type & return it
                _errors = new string[] { ex.Message };
            }
        }

        // License validation data
        internal struct ValidateLicense
        {
            public bool IsValid { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public string ProductName { get; private set; }
            public string ProductDescription { get; private set; }
            public string FirstName { get; private set; }
            public string LastName { get; private set; }
            public string Email { get; private set; }
            public Dictionary<string, string> Restrictions { get; private set; }
            public string LicenseStatusMessage { get; private set; }
            public bool IsTrial { get; private set; }

            [JsonConstructor]
            public ValidateLicense(bool is_valid, DateTime? expires_at, string product_name, 
                string product_description, string first_name, string last_name, string email, 
                Dictionary<string, string> restrictions, string license_status_message, bool is_trial = false)
            {
                IsValid = is_valid;
                ExpiresAt = expires_at;
                ProductName = product_name;
                ProductDescription = product_description;
                FirstName = first_name;
                LastName = last_name;
                Email = email;
                Restrictions = restrictions;
                LicenseStatusMessage = license_status_message;
                IsTrial = is_trial;
            }
        }
    }
}
