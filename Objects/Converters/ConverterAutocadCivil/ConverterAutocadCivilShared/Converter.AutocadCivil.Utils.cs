using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;

using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;


#if CIVIL2021 || CIVIL2022 || CIVIL2023
using Autodesk.Aec.ApplicationServices;
#endif

namespace Objects.Converter.AutocadCivil
{
  public static class Utils
  {
    public static BlockTableRecord GetModelSpace(this Database db)
    {
      return (BlockTableRecord)SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(OpenMode.ForWrite);
    }
    public static ObjectId Append(this BlockTableRecord owner, Entity entity)
    {
      if (!entity.IsNewObject)
        return entity.Id;
      var tr = owner.Database.TransactionManager.TopTransaction;
      var id = owner.AppendEntity(entity);
      tr.AddNewlyCreatedDBObject(entity, true);
      return id;
    }
  }

  public partial class ConverterAutocadCivil
  {
    public static string invalidAutocadChars = @"<>/\:;""?*|=,‘";

    private Dictionary<string, ObjectId> _lineTypeDictionary = new Dictionary<string, ObjectId>();
    public Dictionary<string, ObjectId> LineTypeDictionary
    {
      get
      {
        if (_lineTypeDictionary.Values.Count == 0)
        {
          var lineTypeTable = (LinetypeTable)Trans.GetObject(Doc.Database.LinetypeTableId, OpenMode.ForRead);
          foreach (ObjectId lineTypeId in lineTypeTable)
          {
            var linetype = (LinetypeTableRecord)Trans.GetObject(lineTypeId, OpenMode.ForRead);
            _lineTypeDictionary.Add(linetype.Name, lineTypeId);
          }
        }
        return _lineTypeDictionary;
      }
    }

    /// <summary>
    /// Removes invalid characters for Autocad layer and block names
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string RemoveInvalidAutocadChars(string str)
    {
      // using this to handle rhino nested layer syntax
      // replace "::" layer delimiter with "$" (acad standard)
      string cleanDelimiter = str.Replace("::", "$");

      // remove all other invalid chars
      return Regex.Replace(cleanDelimiter, $"[{invalidAutocadChars}]", string.Empty);
    }
    #region Reference Point

    // CAUTION: these strings need to have the same values as in the connector bindings
    const string InternalOrigin = "Internal Origin (default)";
    const string UCS = "Current User Coordinate System";
    private Matrix3d _transform;
    private Matrix3d ReferencePointTransform
    {
      get
      {
        if (_transform == null || _transform == new Matrix3d())
        {
          // get from settings
          var referencePointSetting = Settings.ContainsKey("reference-point") ? Settings["reference-point"] : string.Empty;
          _transform = GetReferencePointTransform(referencePointSetting);
        }
        return _transform;
      }
    }

    private Matrix3d GetReferencePointTransform(string type)
    {
      var referencePointTransform = Matrix3d.Identity;

      switch (type)
      {
        case InternalOrigin:
          break;
        case UCS:
          var cs = Doc.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
          if (cs != null)
            referencePointTransform = Matrix3d.AlignCoordinateSystem(
                Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                cs.Origin, cs.Xaxis, cs.Yaxis, cs.Zaxis);
          break;
        default: // try to see if this is a named UCS
          using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
          {
            var UCSTable = tr.GetObject(Doc.Database.UcsTableId, OpenMode.ForRead) as UcsTable;
            if (UCSTable.Has(type))
            {
              var ucsRecord = tr.GetObject(UCSTable[type], OpenMode.ForRead) as UcsTableRecord;
              referencePointTransform = Matrix3d.AlignCoordinateSystem(
                Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                ucsRecord.Origin, ucsRecord.XAxis, ucsRecord.YAxis, ucsRecord.XAxis.CrossProduct(ucsRecord.YAxis));
            }
            tr.Commit();
          }
          break;
      }

      return referencePointTransform;
    }

    /// <summary>
    /// For sending out of AutocadCivil, transforms a point relative to the reference point
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public Point3d ToExternalCoordinates(Point3d p)
    {
      return p.TransformBy(ReferencePointTransform.Inverse());
    }

    /// <summary>
    /// For sending out of AutocadCivil, transforms a vector relative to the reference point
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public Vector3d ToExternalCoordinates(Vector3d v)
    {
      return v.TransformBy(ReferencePointTransform.Inverse());
    }

    /// <summary>
    /// For receiving in to AutocadCivil, transforms a point relative to the reference point
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public Point3d ToInternalCoordinates(Point3d p)
    {
      return p.TransformBy(ReferencePointTransform);
    }

    /// <summary>
    /// For receiving in to AutocadCivil, transforms a vector relative to the reference point
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public Vector3d ToInternalCoordinates(Vector3d v)
    {
      return v.TransformBy(ReferencePointTransform);
    }
    #endregion

    #region app props
    public static string AutocadPropName = "AutocadProps";
    #endregion

    #region units
    private string _modelUnits;
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
        {
          _modelUnits = UnitToSpeckle(Doc.Database.Insunits);

#if CIVIL2021 || CIVIL2022 || CIVIL2023
          if (_modelUnits == Units.None)
          {
            // try to get the drawing unit instead
            using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
            {
              var id = DrawingSetupVariables.GetInstance(Doc.Database, false);
              var setupVariables = (DrawingSetupVariables)tr.GetObject(id, OpenMode.ForRead);
              var linearUnit = setupVariables.LinearUnit;
              _modelUnits = Units.GetUnitsFromString(linearUnit.ToString());
              tr.Commit();
            }
          }
#endif
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

    // Note: Difference between International Foot and US Foot is ~ 0.0000006 as described in: https://www.pobonline.com/articles/98788-us-survey-feet-versus-international-feet
    private string UnitToSpeckle(UnitsValue units)
    {
      switch (units)
      {
        case UnitsValue.Millimeters:
          return Units.Millimeters;
        case UnitsValue.Centimeters:
          return Units.Centimeters;
        case UnitsValue.Meters:
          return Units.Meters;
        case UnitsValue.Kilometers:
          return Units.Kilometers;
        case UnitsValue.Inches:
        case UnitsValue.USSurveyInch:
          return Units.Inches;
        case UnitsValue.Feet:
        case UnitsValue.USSurveyFeet:
          return Units.Feet;
        case UnitsValue.Yards:
        case UnitsValue.USSurveyYard:
          return Units.Yards;
        case UnitsValue.Miles:
        case UnitsValue.USSurveyMile:
          return Units.Miles;
        case UnitsValue.Undefined:
          return Units.None;
        default:
          throw new Speckle.Core.Logging.SpeckleException($"The Unit System \"{units}\" is unsupported.");
      }
    }
    #endregion

  }
}
