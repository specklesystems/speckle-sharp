using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using System.Linq;
using Speckle.ConnectorTeklaStructures.UI;
using Tekla.Structures.Model;
namespace Speckle.ConnectorTeklaStructures.Util
{
  class ConnectorTeklaStructuresUtils
  {
#if TeklaStructures2021
    public static string TeklaStructuresAppName = Applications.TeklaStructures2021;
#else
    public static string TeklaStructuresAppName = Applications.TeklaStructures;
#endif

    public static Dictionary<string, (string, string)> ObjectIDsTypesAndNames { get; set; }

    public List<SpeckleException> ConversionErrors { get; set; }

    public static void GetObjectIDsTypesAndNames(Model model)
    {
      ObjectIDsTypesAndNames = new Dictionary<string, (string, string)>();
      foreach (var objectType in Enum.GetNames(typeof(TeklaStructuresAPIUsableTypes)))
      {
        var names = new List<string>();
        try
        {
          names = GetAllNamesOfObjectType(model, objectType);
        }
        catch { }
        if (names.Count > 0)
        {
          foreach (string name in names)
          {
            ObjectIDsTypesAndNames.Add(string.Concat(objectType, ": ", name), (objectType, name));
          }
        }
      }
    }

    public static bool IsTypeTeklaStructuresAPIUsable(string type)
    {
      return Enum.GetNames(typeof(TeklaStructuresAPIUsableTypes)).Contains(type);
    }

    public static List<string> GetAllNamesOfObjectType(Model model, string objectType)
    {
      switch (objectType)
      {
      
        default:
          return null;
      }
    }
    #region Get List Names
    #endregion

    public enum TeklaStructuresAPIUsableTypes
    {
      Beam,


      //ColumnResults,
      //BeamResults,
      //BraceResults,
      //PierResults,
      //SpandrelResults,
      //AnalysisResults
    }

    /// <summary>
    /// same as ObjectType in TeklaStructures cSelect.GetSelected API function
    /// </summary>
    public enum TeklaStructuresViewSelectableTypes
    {
      Point = 1,
      Frame = 2,
      Area = 4
    }
  }
}
