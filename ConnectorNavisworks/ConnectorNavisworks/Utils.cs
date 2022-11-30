using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Autodesk.Navisworks.Api;
using Speckle.Core.Kits;

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
    }
}