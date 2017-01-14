using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OASModels
{
    /*
     * Internal class used by OASClient to pass data to the web-service
     */
    public class ConversionSettings
    {
        public string Domain { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Content { get; set; }

        public ConversionOptions Options { get; set; }

        public ConversionSettings(string domain, string username, string password)
        {
            Domain = domain;
            Username = username;
            Password = password;
        }
    }
}
