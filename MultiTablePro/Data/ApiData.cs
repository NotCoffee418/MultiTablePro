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
            protected string[] _errors = new string[0];
            internal string[] errors {
                get { return _errors; }
                set { _errors = value; }
            }

            protected T _result;
            internal T result
            {
                get { return _result; }
                set { _result = value; }
            }

            // Constructors
            // public ApiResponse() { } // Empty needed for json
            [JsonConstructor]
            public ApiResponse(T result, string[] errors) {
                _result = result;
                _errors = errors.ToArray();
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

        }
    }
}
