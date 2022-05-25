using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;

using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.DgnPlatformNET.Elements;
using Bentley.EC.Persistence.Query;
using Bentley.ECObjects;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;

using System.Diagnostics;

namespace Objects.Converter.Bentley
{
  public partial class ConverterBentley
  {
    /// <summary>
    /// Computes the Speckle Units of the current model. The active MicroStation model is passed as a reference, so it will always be up to date.
    /// </summary>  
    private string _modelUnits;
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
        {
          DgnModel Model = Session.Instance.GetActiveDgnModel();
          var us = Model.GetModelInfo().GetMasterUnit().GetName(true, true);
          _modelUnits = UnitToSpeckle(us);
        }
        return _modelUnits;
      }
    }

    private void SetUnits(Base geom)
    {
      geom["units"] = ModelUnits;
    }

    private double ScaleToNative(double value, string units)
    {
      var f = Units.GetConversionFactor(units, ModelUnits);
      return value * f;
    }

    private double ScaleToNative(double value, string units, double uor)
    {
      var f = Units.GetConversionFactor(units, ModelUnits);
      return value * f * uor;
    }

    private double ScaleToSpeckle(double value, double uor)
    {
      return value / uor;
    }

    private string UnitToSpeckle(string us)
    {
      switch (us)
      {
        //case "Micrometers":
        //    break;
        case "Millimeter":
          return Units.Millimeters;
        case "Centimeter":
          return Units.Centimeters;
        case "Meter":
          return Units.Meters;
        case "Kilometer":
          return Units.Kilometers;
        //case "Microinches":
        //    break;
        //case "Mils":
        //break;
        case "Inches":
          return Units.Inches;
        case "Feet":
        case "Foot":
          return Units.Feet;
        case "Yards":
          return Units.Yards;
        case "Miles":
          return Units.Miles;
        //case "Nautical Miles":
        //    break;
        default:
          throw new System.Exception("The current Unit System is unsupported.");
      }
    }

    public Objects.Other.DisplayStyle GetStyle(Element obj)
    {
      var style = new Objects.Other.DisplayStyle();
      Element entity = obj as Element;

      throw new NotImplementedException();

      //try
      //{
      //    // get color
      //    int color = System.Drawing.Color.Black.ToArgb();
      //    switch (entity.Color.ColorMethod)
      //    {
      //        case ColorMethod.ByLayer:
      //            using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      //            {
      //                if (entity.LayerId.IsValid)
      //                {
      //                    var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
      //                    color = layer.Color.ColorValue.ToArgb();
      //                }
      //                tr.Commit();
      //            }
      //            break;
      //        case ColorMethod.ByBlock:
      //        case ColorMethod.ByAci:
      //        case ColorMethod.ByColor:
      //            color = entity.Color.ColorValue.ToArgb();
      //            break;
      //    }
      //    style.color = color;

      //    // get linetype
      //    style.linetype = entity.Linetype;
      //    if (entity.Linetype == "BYLAYER")
      //    {
      //        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      //        {
      //            if (entity.LayerId.IsValid)
      //            {
      //                var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
      //                var linetype = (LinetypeTableRecord)tr.GetObject(layer.LinetypeObjectId, OpenMode.ForRead);
      //                style.linetype = linetype.Name;
      //            }
      //            tr.Commit();
      //        }
      //    }

      //    // get lineweight
      //    try
      //    {
      //        double lineWeight = 0.25;
      //        switch (entity.LineWeight)
      //        {
      //            case LineWeight.ByLayer:
      //                using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      //                {
      //                    if (entity.LayerId.IsValid)
      //                    {
      //                        var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
      //                        if (layer.LineWeight == LineWeight.ByLineWeightDefault || layer.LineWeight == LineWeight.ByBlock)
      //                            lineWeight = (int)LineWeight.LineWeight025;
      //                        else
      //                            lineWeight = (int)layer.LineWeight;
      //                    }
      //                    tr.Commit();
      //                }
      //                break;
      //            case LineWeight.ByBlock:
      //            case LineWeight.ByLineWeightDefault:
      //            case LineWeight.ByDIPs:
      //                lineWeight = (int)LineWeight.LineWeight025;
      //                break;
      //            default:
      //                lineWeight = (int)entity.LineWeight;
      //                break;
      //        }
      //        style.lineweight = lineWeight / 100; // convert to mm
      //    }
      //    catch { }

      //    return style;
      //}
      //catch
      //{
      //    return null;
      //}
    }

    public static DgnECInstanceCollection GetElementProperties(Element element)
    {
      DgnECManager manager = DgnECManager.Manager;
      var properties = manager.GetElementProperties(element, ECQueryProcessFlags.SearchAllClasses);
      return properties;
    }

    public IECPropertyValue GetElementProperty(Element element, string propName)
    {
      using (var properties = GetElementProperties(element))
      {
        foreach (var prop in properties)
        {
          var value = prop.GetPropertyValue(propName);
          if (value != null)
          {
            return value;
          }
        }
      };
      return null;
    }

#if (OPENBUILDINGS)
    public static IECClass[] GetSearchClasses(IECSchema schema)
    {
      List<IECClass> classes = new List<IECClass>();

      if (null == schema)
        return classes.ToArray();

      int nClasses = schema.ClassCount;
      if (0 != nClasses)
      {
        foreach (IECClass ecClass in schema.GetClasses())
        {
          classes.Add(ecClass);
        }
      }
      return classes.ToArray();
    }

    internal IList<ItemTypeLibrary> GetItemTypeLibraries()
    {
      IList<ItemTypeLibrary> itemTypes = ItemTypeLibrary.PopulateListFromFile(Session.Instance.GetActiveDgnFile());
      return itemTypes;
    }

    public static IQueryable<IDgnECInstance> GetInstances(string schemaName)
    {
      bool IncludeReferences = true;
      IECSchema schema = GetItemTypeSchema(schemaName);
      return GetInstances(GetSearchClasses(schema), schemaName, !IncludeReferences);
    }

    public static IQueryable<IDgnECInstance> GetInstances(IECClass[] classes, string schemaName, bool includeReferences)
    {
      ECQuery query = new ECQuery(classes);
      query.SelectClause.SelectAllProperties = true;
      DgnECManager manager = DgnECManager.Manager;
      DgnFile file = Session.Instance.GetActiveDgnFile();
      FindInstancesScope scope = FindInstancesScope.CreateScope(file, new FindInstancesScopeOption(DgnECHostType.Element, includeReferences));
      DgnECInstanceCollection instances = manager.FindInstances(scope, query);
      return instances.AsQueryable();
    }

    static public IECSchema GetItemTypeSchema(string schemaName)
    {
      bool IncludeAttachments = true;
      FindInstancesScope scope = FindInstancesScope.CreateScope(Session.Instance.GetActiveDgnFile(), new FindInstancesScopeOption(DgnECHostType.All, !IncludeAttachments));
      IECSchema schema = DgnECManager.Manager.LocateSchemaInScope(scope, schemaName, 1, 0, SchemaMatchType.Latest);
      return schema;
    }
#endif
  }
}

