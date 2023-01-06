using System;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using CSiAPIv1;
using Objects.Structural.CSI.Analysis;
using System.Linq;
using System.Reflection;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    // warning: this delimter string needs to be the same as the delimter string in "connectorCSIUtils"
    public static string delimiter = "::";
    public string ModelUnits()
    {
      var units = Model.GetDatabaseUnits();
      if (units != 0)
      {
        string[] unitsCat = units.ToString().Split('_');
        return unitsCat[1];
      }
      else
      {
        return null;
      }
    }
    public double ScaleToNative(double value, string units)
    {
      var f = Speckle.Core.Kits.Units.GetConversionFactor(units, ModelUnits());
      return value * f;
    }
    public static List<string> GetAllFrameNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.FrameObj.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }

    public static List<string> GetColumnNames(cSapModel model)
    {
      var frameNames = GetAllFrameNames(model);

      List<string> columnNames = new List<string>();

      string frameLabel = "";
      string frameStory = "";

      foreach (var frameName in frameNames)
      {
        model.FrameObj.GetLabelFromName(frameName, ref frameLabel, ref frameStory);

        if (frameLabel.ToLower().StartsWith("c"))
        {
          columnNames.Add(frameName);
        }
      }

      return columnNames;
    }

    public static List<string> GetBeamNames(cSapModel model)
    {
      var frameNames = GetAllFrameNames(model);

      List<string> beamNames = new List<string>();

      string frameLabel = "";
      string frameStory = "";

      foreach (var frameName in frameNames)
      {
        model.FrameObj.GetLabelFromName(frameName, ref frameLabel, ref frameStory);

        if (frameLabel.ToLower().StartsWith("b"))
        {
          beamNames.Add(frameName);
        }
      }

      return beamNames;
    }

    public static List<string> GetBraceNames(cSapModel model)
    {
      var frameNames = GetAllFrameNames(model);

      List<string> braceNames = new List<string>();

      string frameLabel = "";
      string frameStory = "";

      foreach (var frameName in frameNames)
      {
        model.FrameObj.GetLabelFromName(frameName, ref frameLabel, ref frameStory);

        if (frameLabel.ToLower().StartsWith("d"))
        {
          braceNames.Add(frameName);
        }
      }

      return braceNames;
    }
    public static List<string> GetAllAreaNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.AreaObj.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllWallNames(cSapModel model)
    {
      var WallNames = GetAllAreaNames(model);

      List<string> WallName = new List<string>();

      string wallLabel = "";
      string wallStory = "";

      foreach (var wallName in WallNames)
      {
        model.AreaObj.GetLabelFromName(wallName, ref wallLabel, ref wallStory);

        if (wallLabel.ToLower().StartsWith("w"))
        {
          WallName.Add(wallName);
        }
      }

      return WallName;
    }
    public static List<string> GetAllFloorNames(cSapModel model)
    {
      var FloorNames = GetAllAreaNames(model);

      List<string> FloorName = new List<string>();

      string FloorLabel = "";
      string FloorStory = "";

      foreach (var floorName in FloorNames)
      {
        model.AreaObj.GetLabelFromName(floorName, ref FloorLabel, ref FloorStory);

        if (FloorLabel.ToLower().StartsWith("f"))
        {
          FloorName.Add(floorName);
        }
      }

      return FloorName;
    }

    public Dictionary<string, string> GetAllGuids(cSapModel model)
    {
      var guids = new Dictionary<string, string>();

      var names = GetAllFrameNames(model);
      var guid = "";
      foreach (var name in names)
      {
        var success = Model.FrameObj.GetGUID(name, ref guid);
        if (success != 0)
          continue;

        if (!guids.ContainsKey(guid))
          guids.Add(guid, name);
      }

      names = GetAllAreaNames(model);
      foreach (var name in names)
      {
        var success = Model.AreaObj.GetGUID(name, ref guid);
        if (success != 0)
          continue;

        if (!guids.ContainsKey(guid))
          guids.Add(guid, name);
      }

      //names = GetAllPointNames(model);
      //foreach (var name in names)
      //{
      //  var guid = "";
      //  var success = Model.PointObj.GetGUID(name, ref guid);
      //  if (success != 0)
      //    continue;

      //  if (!guids.ContainsKey(guid))
      //    guids.Add(guid, name);
      //}

      return guids;
    }

    public bool ElementExistsWithApplicationId(string applicationId, out string name)
    {
      name = "";
      if (string.IsNullOrEmpty(applicationId) || ReceiveMode == Speckle.Core.Kits.ReceiveMode.Create)
        return false;

      var projectIds = PreviousContextObjects.Where(o => o.applicationId == applicationId).FirstOrDefault()?.CreatedIds;
      projectIds = projectIds ?? new List<string> { applicationId };

      foreach (var guid in projectIds)
        if (ExistingObjectGuids.Keys.Contains(guid))
        {
          name = ExistingObjectGuids[guid];
          return true;
        }

      return false;
    }


    public ShellType ConvertShellType(eShellType eShellType)
    {
      ShellType shellType = new ShellType();

      switch (eShellType)
      {
        case eShellType.Membrane:
          shellType = ShellType.Membrane;
          break;
        case eShellType.ShellThick:
          shellType = ShellType.ShellThick;
          break;
        case eShellType.ShellThin:
          shellType = ShellType.ShellThin;
          break;
        case eShellType.Layered:
          shellType = ShellType.Layered;
          break;
        default:
          shellType = ShellType.Null;
          break;


      }

      return shellType;
    }

    public bool[] RestraintToNative(Restraint restraint)
    {
      bool[] restraints = new bool[6];

      var code = restraint.code;

      int i = 0;
      foreach (char c in code)
      {
        restraints[i] = c.Equals('F') ? true : false; // other assume default of released 
        i++;
      }

      return restraints;
    }

    public double[] PartialRestraintToNative(Restraint restraint)
    {
      double[] partialFix = new double[6];
      partialFix[0] = restraint.stiffnessX;
      partialFix[1] = restraint.stiffnessY;
      partialFix[2] = restraint.stiffnessZ;
      partialFix[3] = restraint.stiffnessXX;
      partialFix[4] = restraint.stiffnessYY;
      partialFix[5] = restraint.stiffnessZZ;
      return partialFix;
    }

    public Restraint RestraintToSpeckle(bool[] releases)
    {
      var code = new List<string>() { "R", "R", "R", "R", "R", "R" }; // default to free
      if (releases != null)
      {
        for (int i = 0; i < releases.Length; i++)
        {
          if (releases[i]) code[i] = "F";
        }
      }

      var restraint = new Restraint(string.Join("", code));
      return restraint;
    }
    public static List<string> GetAllPointNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PointObj.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }

    }

    public enum CSIConverterSupported
    {
      //CSINode,
      Node,
      Line,
      Element1D,
      Element2D,
      //Model,
    }

    public enum CSIAPIUsableTypes
    {
      Point,
      Frame,
      Area, // cAreaObj
      LoadPattern,
      Model,
      Column,
      Brace,
      Beam,
      Floor,
      Wall,
      Tendon,
      Links,
      Spandrel,
      Pier,
      Grids,
      //Diaphragm,
      BeamLoading,
      ColumnLoading,
      BraceLoading,
      FrameLoading,
      FloorLoading,
      AreaLoading,
      WallLoading,
      NodeLoading,
      //ColumnResults,
      //BeamResults,
      //BraceResults,
      //PierResults,
      //SpandrelResults
      AnalysisResults
    }
  }
}