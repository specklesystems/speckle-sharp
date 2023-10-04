#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autodesk.AdvanceSteel.DocumentManagement;
using Autodesk.AdvanceSteel.DotNetRoots.Units;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Internal.DatabaseServices;
using Objects.Structural.Analysis;
using Sentry;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json.Linq;
using static Autodesk.AdvanceSteel.DotNetRoots.Units.Unit;

namespace Objects.Converter.AutocadCivil
{
  public abstract class ASBaseProperties<T> : IASProperties where T : class
  {
    public Type ObjectType { get => typeof(T); }

    public abstract Dictionary<string, ASProperty> BuildedPropertyList();

    /// <summary>
    /// Insert item at properties dictionary
    /// </summary>
    /// <param name="dictionary"></param>
    /// <param name="description"></param>
    /// <param name="memberName">Member name of AS Object - May be Get/Set property or Get Method(without parameter)</param>
    /// <param name="unitType"></param>
    protected void InsertProperty(Dictionary<string, ASProperty> dictionary, string description, string memberName, eUnitType? unitType = null)
    {
      dictionary.Add(description, new ASProperty(ObjectType, description, memberName, unitType));
    }

    /// <summary>
    /// Insert item at properties dictionary
    /// </summary>
    /// <param name="dictionary"></param>
    /// <param name="description"></param>
    /// <param name="methodInfoGet">Method name of Get Custom Function on properties class</param>
    /// <param name="methodInfoSet">Method name of Set Custom Function on properties class</param>
    /// <param name="unitType"></param>
    protected void InsertCustomProperty(Dictionary<string, ASProperty> dictionary, string description, string methodInfoGet, string methodInfoSet, eUnitType? unitType = null)
    {
      ASPropertyMethods propertyMethods = new ASPropertyMethods(this.GetType(), methodInfoGet, methodInfoSet);
      dictionary.Add(description, new ASProperty(ObjectType, description, propertyMethods, unitType));
    }

    protected static double Round(double value)
    {
      return Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    protected static double DegreeToRadian(double angle)
    {
      return (System.Math.PI / 180.0) * angle;
    }
  }
}
#endif
