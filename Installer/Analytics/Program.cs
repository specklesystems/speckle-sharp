using Piwik.Tracker;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net;

namespace analytics
{
    class Program
    {
        private static readonly string PiwikBaseUrl = "https://arupdt.matomo.cloud/";
        private static readonly int SiteId = 1;
        private static readonly string CacheLocation = "\\Speckle\\Accounts.db";

        static void Main(string[] args)
        {
            PiwikTracker _piwikTracker = new PiwikTracker(SiteId, PiwikBaseUrl);
            string _version = args[0];
            string _internalDomain = args[1];
            string _machineName = Environment.MachineName.ToLower(new CultureInfo("en-GB", false));
            string _domainName = Dns.GetHostEntry("").HostName;
            string _appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // If the cache has been set up this is an update, otherwise it's a new user
            string _installType = File.Exists(_appDataFolder + CacheLocation) ? "update" : "new";

            bool _isInternalDomain = _machineName.Contains(_internalDomain) || _domainName.Contains(_internalDomain);
            
            if(_isInternalDomain) {
                // Hash the username and send it with the installer info
                string _userEmail = Environment.UserName + "@" + _internalDomain + ".com";
                string _userId = _userEmail.ToLower(new CultureInfo("en-GB", false));
                _piwikTracker.SetUserId(ComputeSHA256Hash(_userId));

                // Send this information to Matomo
                _piwikTracker.DoTrackEvent("SpeckleInstallerV2", _installType, _version); 
            }
        }

        /**
        * Perform a one-way hash of the input text.
        **/
        private static string ComputeSHA256Hash(string text)
        {
            using (var sha256 = new SHA256Managed())
            {
                byte[] _encodedText = Encoding.UTF8.GetBytes(text);
                byte[] _hash = sha256.ComputeHash(_encodedText);
                string _hashString = BitConverter.ToString(_hash);
                return _hashString.Replace("-", "").ToLower(new CultureInfo("en-GB", false));
            }                
        }
    }
}
