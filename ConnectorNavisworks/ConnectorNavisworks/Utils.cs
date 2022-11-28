using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Speckle.Core.Kits;

namespace Speckle.ConnectorNavisworks
{
    public static class Utils
    {
#if NAVMAN20
        public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2023);
        public static string AppName = HostApplications.Navisworks.Name;
        public static string Slug = HostApplications.Navisworks.Slug;
#elif NAVMAN19
    public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2022);
    public static string AppName = HostApplications.Navisworks.Name;
    public static string Slug = HostApplications.Navisworks.Slug;
#elif NAVMAN18
    public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2021);
    public static string AppName = HostApplications.Navisworks.Name;
    public static string Slug = HostApplications.AutNavisworksoCAD.Slug;
#elif NAVMAN17
    public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2020);
    public static string AppName = HostApplications.Navisworks.Name;
    public static string Slug = HostApplications.Navisworks.Slug;
#endif
        public static string InvalidChars = @"<>/\:;""?*|=,‘";
        public static string ApplicationIdKey = "applicationId";
    }
}