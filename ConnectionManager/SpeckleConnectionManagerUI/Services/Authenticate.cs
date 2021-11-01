using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SpeckleConnectionManagerUI.Services
{
    public class Authenticate
    {
        private Guid _code = Guid.NewGuid();
        private Guid _suuid = Guid.NewGuid();

        public void RedirectToAuthPage(string serverUrl)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                RedirectMac(serverUrl);
            }
            else
            {
                RedirectWindows(serverUrl);
            }
            System.IO.File.WriteAllText(Constants.ServerLocationStore, serverUrl);
            System.IO.File.WriteAllText(Constants.ChallengeCodeStore, _code.ToString());
        }

        private void RedirectWindows(string serverUrl)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = $"{serverUrl}/authn/verify/sdm/{_code}?suuid={_suuid}",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void RedirectMac(string serverUrl)
        {
            Process.Start("open", $"{serverUrl}/authn/verify/sdm/{_code}?suuid={_suuid}");
        }
        
    }
}