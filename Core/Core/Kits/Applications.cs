using System;

namespace Speckle.Core.Kits
{
  [Obsolete("Use VersionedHostApplications or HostApplications instead.")]
  public static class Applications
  {
    public const string Rhino6 = "Rhino6";
    public const string Rhino7 = "Rhino7";
    public const string Grasshopper = "Grasshopper";
    public const string Revit2019 = "Revit2019";
    public const string Revit2020 = "Revit2020";
    public const string Revit2021 = "Revit2021";
    public const string Revit2022 = "Revit2022";
    public const string Revit2023 = "Revit2023";
    public const string DynamoSandbox = "DynamoSandbox";
    public const string DynamoRevit = "DynamoRevit";
    public const string DynamoRevit2021 = "DynamoRevit2021";
    public const string DynamoRevit2022 = "DynamoRevit2022";
    public const string DynamoRevit2023 = "DynamoRevit2023";
    public const string Unity = "Unity";
    public const string Excel = "Excel";
    public const string GSA = "GSA";
    public const string Civil2021 = "Civil2021";
    public const string Civil2022 = "Civil2022";
    public const string Civil2023 = "Civil2023";
    public const string Autocad2021 = "AutoCAD2021";
    public const string Autocad2022 = "AutoCAD2022";
    public const string Autocad2023 = "AutoCAD2023";
    public const string MicroStation = "MicroStation";
    public const string OpenRoads = "OpenRoads";
    public const string OpenRail = "OpenRail";
    public const string OpenBuildings = "OpenBuildings";
    public const string ETABSv18 = "ETABSv18";
    public const string ETABSv19 = "ETABSv19";
    public const string TeklaStructures2021 = "TeklaStructures2021";
    public const string TeklaStructures2020 = "TeklaStructures2020";
    public const string Archicad = "Archicad";
    public const string Script = "Script";
    public const string Other = "Other";
    public const string All = "All";
  }

  /// <summary>
  /// List of Host Applications - their slugs should match our ghost tags and ci/cd slugs
  /// </summary>
  public static class HostApplications
  {

    public static class Rhino
    {
      public const string Name = "Rhino";
      public const string Slug = "rhino";
    }

    public static class Grasshopper
    {
      public const string Name = "Grasshopper";
      public const string Slug = "grasshopper";
    }

    public static class Revit
    {
      public const string Name = "Revit";
      public const string Slug = "revit";
    }

    public static class Dynamo
    {
      public const string Name = "Dynamo";
      public const string Slug = "dynamo";
    }

    public static class Unity
    {
      public const string Name = "Unity";
      public const string Slug = "unity";
    }

    public static class GSA
    {
      public const string Name = "GSA";
      public const string Slug = "gsa";
    }

    public static class Civil
    {
      public const string Name = "Civil 3D";
      public const string Slug = "civil3d";
    }

    public static class AutoCAD
    {
      public const string Name = "AutoCAD";
      public const string Slug = "autocad";
    }

    public static class MicroStation
    {
      public const string Name = "MicroStation";
      public const string Slug = "microstation";
    }

    public static class OpenRoads
    {
      public const string Name = "OpenRoads";
      public const string Slug = "openroads";
    }

    public static class OpenRail
    {
      public const string Name = "OpenRail";
      public const string Slug = "openrail";
    }

    public static class OpenBuildings
    {
      public const string Name = "OpenBuildings";
      public const string Slug = "openbuildings";
    }

    public static class ETABS
    {
      public const string Name = "ETABS";
      public const string Slug = "etabs";
    }

    public static class SAP2000
    {
      public const string Name = "SAP2000";
      public const string Slug = "sap2000";
    }
    public static class CSIBridge
    {
      public const string Name = "CSIBridge";
      public const string Slug = "csibridge";
    }
    public static class SAFE
    {
      public const string Name = "SAFE";
      public const string Slug = "safe";
    }

    public static class CSI
    {
      public const string Name = "CSI";
      public const string Slug = "csi";
    }

    public static class TeklaStructures
    {
      public const string Name = "Tekla Structures";
      public const string Slug = "teklastructures";
    }

  }

  /// <summary>
  /// List of Host Applications with their versions 
  /// Do not change these - they must match the converter names!
  /// </summary>
  public static class VersionedHostApplications
  {
    public const string Rhino6 = "Rhino6";
    public const string Rhino7 = "Rhino7";
    public const string Grasshopper6 = "Grasshopper6";
    public const string Grasshopper7 = "Grasshopper7";
    public const string Revit2019 = "Revit2019";
    public const string Revit2020 = "Revit2020";
    public const string Revit2021 = "Revit2021";
    public const string Revit2022 = "Revit2022";
    public const string Revit2023 = "Revit2023";
    public const string Dxf = "Dxf";
    public const string DynamoSandbox = "DynamoSandbox";
    public const string DynamoRevit = "DynamoRevit";
    public const string DynamoRevit2021 = "DynamoRevit2021";
    public const string DynamoRevit2022 = "DynamoRevit2022";
    public const string DynamoRevit2023 = "DynamoRevit2023";
    public const string Unity = "Unity";
    public const string Excel = "Excel";
    public const string GSA = "GSA";
    public const string Civil2021 = "Civil2021";
    public const string Civil2022 = "Civil2022";
    public const string Civil2023 = "Civil2023";
    public const string Autocad2021 = "AutoCAD2021";
    public const string Autocad2022 = "AutoCAD2022";
    public const string Autocad2023 = "AutoCAD2023";
    public const string MicroStation = "MicroStation";
    public const string OpenRoads = "OpenRoads";
    public const string OpenRail = "OpenRail";
    public const string OpenBuildings = "OpenBuildings";
    public const string ETABS = "ETABS";
    public const string SAP2000 = "SAP2000";
    public const string CSIBridge = "CSIBridge";
    public const string SAFE = "SAFE";
    public const string CSI = "CSI";
    public const string TeklaStructures2021 = "TeklaStructures2021";
    public const string TeklaStructures2020 = "TeklaStructures2020";
    public const string Script = "Script";
    public const string Other = "Other";
    public const string All = "All";
  }

}
