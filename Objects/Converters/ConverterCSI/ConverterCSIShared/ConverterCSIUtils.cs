#nullable enable
using System;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using CSiAPIv1;
using Objects.Structural.CSI.Analysis;
using System.Linq;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    // warning: this delimiter string needs to be the same as the delimiter string in "connectorCSIUtils"
    public const string Delimiter = "::";

    // WARNING: These strings need to have the same value as the strings in ConnectorBindingsCSI.Settings
    private const string SendNodeResults = "sendNodeResults";
    private const string Send1DResults = "send1DResults";
    private const string Send2DResults = "send2DResults";

    private string? _modelUnits;

    private string? ModelUnits()
    {
      if (_modelUnits != null)
        return _modelUnits;

      var units = Model.GetPresentUnits();
      if (units != 0)
      {
        string[] unitsCat = units.ToString().Split('_');
        _modelUnits = unitsCat[1];
        return _modelUnits;
      }
      return null;
    }

    private double ScaleToNative(double value, string units)
    {
      var f = Speckle.Core.Kits.Units.GetConversionFactor(units, ModelUnits());
      return value * f;
    }

    private static IList<string> GetAllFrameNames(cSapModel model)
    {
      int num = 0;
      var names = Array.Empty<string>();

      int success = model.FrameObj.GetNameList(ref num, ref names);

      if (success != 0)
        throw new InvalidOperationException("Failed to retrieve names of all defined frame objects");

      return names;
    }

    private static List<string> GetAllFrameNameWithPrefix(cSapModel model, string prefix)
    {
      var areaNames = GetAllFrameNames(model);

      List<string> targetNames = new();

      string label = "";
      string story = "";

      foreach (string areas in areaNames)
      {
        model.FrameObj.GetLabelFromName(areas, ref label, ref story);

        if (label.ToLower().StartsWith(prefix))
        {
          targetNames.Add(areas);
        }
      }

      return targetNames;
    }

    private static List<string> GetColumnNames(cSapModel model)
    {
      const string columnPrefix = "c";
      return GetAllFrameNameWithPrefix(model, columnPrefix);
    }

    private static List<string> GetBeamNames(cSapModel model)
    {
      const string beamPrefix = "b";
      return GetAllFrameNameWithPrefix(model, beamPrefix);
    }

    private static List<string> GetBraceNames(cSapModel model)
    {
      const string bracePrefix = "d";
      return GetAllFrameNameWithPrefix(model, bracePrefix);
    }

    private static IList<string> GetAllAreaNames(cSapModel model)
    {
      int num = 0;
      var names = Array.Empty<string>();

      int success = model.AreaObj.GetNameList(ref num, ref names);
      if (success != 0)
        throw new InvalidOperationException("Failed to retrieve names of all defined area objects");

      return names;
    }

    /// <summary>
    /// Gets all <see cref="cAreaObj"/> names who's label starts with <paramref name="prefix"/>
    /// </summary>
    /// <param name="model"></param>
    /// <param name="prefix">the prefix to match</param>
    /// <returns>list of names</returns>
    private static List<string> GetAllAreaNamesWithPrefix(cSapModel model, string prefix)
    {
      var areaNames = GetAllAreaNames(model);

      List<string> targetNames = new();

      string label = "";
      string story = "";

      foreach (string areas in areaNames)
      {
        model.AreaObj.GetLabelFromName(areas, ref label, ref story);

        if (label.ToLower().StartsWith(prefix))
        {
          targetNames.Add(areas);
        }
      }

      return targetNames;
    }

    private static List<string> GetAllWallNames(cSapModel model)
    {
      const string wallPrefix = "w";
      return GetAllAreaNamesWithPrefix(model, wallPrefix);
    }

    private static List<string> GetAllFloorNames(cSapModel model)
    {
      const string floorPrefix = "f";
      return GetAllAreaNamesWithPrefix(model, floorPrefix);
    }

    public static IList<string> GetAllPointNames(cSapModel model)
    {
      int num = 0;
      var names = Array.Empty<string>();

      int success = model.PointObj.GetNameList(ref num, ref names);
      if (success != 0)
        throw new InvalidOperationException("Failed to retrieve names of all defined point objects");

      return names;
    }

    private Dictionary<string, string> GetAllGuids(cSapModel model)
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

    private bool ElementExistsWithApplicationId(string? applicationId, out string name)
    {
      name = "";
      if (string.IsNullOrEmpty(applicationId) || ReceiveMode == Speckle.Core.Kits.ReceiveMode.Create)
        return false;

      var projectIds = PreviousContextObjects.FirstOrDefault(o => o.applicationId == applicationId)?.CreatedIds;
      projectIds ??= new List<string> { applicationId! };

      foreach (var guid in projectIds)
      {
        if (ExistingObjectGuids.TryGetValue(guid, out name))
        {
          return true;
        }
      }

      return false;
    }

    private string? GetOriginalApplicationId(string? csiAppId)
    {
      if (string.IsNullOrEmpty(csiAppId))
        return csiAppId;

      var originalAppId = PreviousContextObjects.FirstOrDefault(o => o.CreatedIds.Contains(csiAppId))?.applicationId;

      return originalAppId ?? csiAppId;
    }

    private static ShellType ConvertShellType(eShellType eShellType)
    {
      ShellType shellType = eShellType switch
      {
        eShellType.Membrane => ShellType.Membrane,
        eShellType.ShellThick => ShellType.ShellThick,
        eShellType.ShellThin => ShellType.ShellThin,
        eShellType.Layered => ShellType.Layered,
        _ => ShellType.Null
      };

      return shellType;
    }

    private static bool[] RestraintToNative(Restraint restraint)
    {
      bool[] restraints = new bool[6];

      var code = restraint.code;

      int i = 0;
      foreach (char c in code)
      {
        restraints[i] = c.Equals('F'); // other assume default of released
        i++;
      }

      return restraints;
    }

    private static double[] PartialRestraintToNative(Restraint restraint)
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

    public Restraint RestraintToSpeckle(bool[]? releases)
    {
      var code = new List<string>() { "R", "R", "R", "R", "R", "R" }; // default to free
      if (releases != null)
      {
        for (int i = 0; i < releases.Length; i++)
        {
          if (releases[i])
            code[i] = "F";
        }
      }

      var restraint = new Restraint(string.Join("", code));
      return restraint;
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
