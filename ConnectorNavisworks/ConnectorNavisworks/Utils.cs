using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Autodesk.Navisworks.Api;
using Speckle.Core.Kits;
using System.Drawing;
using Console = Colorful.Console;

namespace Speckle.ConnectorNavisworks
{
    public static class Utils
    {
#if NAVMAN20
        public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2023);
#elif NAVMAN19
        public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2022);
#elif NAVMAN18
        public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2021);
#elif NAVMAN17
        public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2020);
#endif
        public static string InvalidChars = @"<>/\:;""?*|=,‘";
        public static string ApplicationIdKey = "applicationId";


        public static void ConsoleLog(string message, ConsoleColor color = ConsoleColor.Blue, string hex = "#3b82f6")
        {
            Console.WriteLine(message, color: ColorTranslator.FromHtml(hex));
        }

        public static void WarnLog(string warningMessage)
        {
            ConsoleLog(warningMessage, hex: "#f59e0b");
        }

        public static void ErrorLog(Exception err)
        {
            ErrorLog(err.Message);
            throw err;
        }

        public static void ErrorLog(string errorMessage) => ConsoleLog(errorMessage, hex: "#ef4444");
    }
}