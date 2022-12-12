using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Objects.Structural.Analysis
{
  public class AnalyticalModel : Base
  {
    public ModelInfo specs { get; set; }
    public Dictionary<string, Node> nodes { get; set; } = new Dictionary<string, Node>();
    public Dictionary<string, Element1D> element1Ds { get; set; } = new Dictionary<string, Element1D>();
    public Dictionary<string, Element2D> element2Ds { get; set; } = new Dictionary<string, Element2D>();
    public Dictionary<string, Load> loads { get; set; } = new Dictionary<string, Load>();
    public Dictionary<string, Restraint> restraints { get; set; } = new Dictionary<string, Restraint>();
    public Dictionary<string, Property> properties { get; set; } = new Dictionary<string, Property>();
    public Dictionary<string, StructuralMaterial> materials { get; set; } = new Dictionary<string, StructuralMaterial>();
    public Dictionary<string, Base> miscTypes { get; set; } = new Dictionary<string, Base>();
    public List<string> updated { get; set; } = new List<string>();

    public void AddDiff<T>(T typedBase, ApplicationObject.State state)
    {
      AddElement(typedBase, state);
    }

    public void AddElement<T>(T typedBase, ApplicationObject.State state = ApplicationObject.State.Unknown)
    {
      try
      {
        if (!(typedBase is Base @base))
          throw new Exception($"Object type {typeof(T).FullName} does not extend Speckle.Core.Models.Base");

        @base.applicationId ??= @base.id;
        if (string.IsNullOrEmpty(@base.applicationId))
          throw new Exception($"Base object has no id or applicationId");

        var members = GetMembers();
        IDictionary dict = null;
        foreach (var member in members)
        {
          if (!(member.Value is IDictionary dictionary))
            continue;

          Type type = dictionary.GetType().GetGenericArguments().Last();
          MethodInfo castMethod = this.GetType().GetMethod("CanCast").MakeGenericMethod(type);
          bool canCast = (bool)castMethod.Invoke(null, new object[] { typedBase });

          if (!canCast)
            continue;

          dict = dictionary;
          break;
        }

        string diffType = "addition";

        // if there is no dictionary that can contain this Base object, add the object to the misc prop and return
        if (dict == null)
        {
          if (state != ApplicationObject.State.Unknown)
            @base["diffType"] = state;

          if (!miscTypes.ContainsKey(@base.applicationId))
            miscTypes.Add(@base.applicationId, @base);
          return;
        }

        // if there is no existing element with a matching id, add it and return
        if (!dict.Contains(@base.applicationId))
        {
          if (state != ApplicationObject.State.Unknown)
            @base["diffType"] = state;

          dict.Add(@base.applicationId, typedBase);
          return;
        }

        if (state == ApplicationObject.State.Unknown)
          return;

        var existingBase = (Base)dict[@base.applicationId];
        var editedProps = new Base();
        propsAreEqual(existingBase, @base, null, null, ref editedProps, 0);

        if (editedProps.GetMembers(DynamicBaseMemberType.Dynamic).Count > 0)
          existingBase["incomingEdits"] = editedProps;
        
      }
      catch (Exception ex)
      { }
    }

    public static bool CanCast<T>(object o)
    {
      if (o is T)
        return true;
      return false;
    }

    private List<string> propsToIgnore = new List<string>()
    {
      "units"
    };

    private bool propsAreEqual(object existingProp, object incomingProp, string existingUnits, string incomingUnits, ref Base editedProps, int depth)
    {
      depth++;
      var suffix = " - incoming";

      if (existingProp is Base existingBase && incomingProp is Base incomingBase)
      {
        var existingProps = existingBase.GetMembers();
        var incomingProps = incomingBase.GetMembers();

        existingProps.TryGetValue("units", out object eUnits);
        incomingProps.TryGetValue("units", out object iUnits);
        existingUnits = eUnits as string;
        incomingUnits = iUnits as string;

        foreach (var incomingChildProp in incomingBase.GetMembers())
        {
          if (propsToIgnore.Contains(incomingChildProp.Key))
            continue;

          existingProps.TryGetValue(incomingChildProp.Key, out object existingValue);

          if (propsAreEqual(existingValue, incomingChildProp.Value, existingUnits, incomingUnits, ref editedProps, depth))
            continue;

          if (depth == 1)
            editedProps[incomingChildProp.Key + suffix] = existingValue;
          else
            return false;
        }
        return true;
      }
      else
      {
        if (existingProp == null || incomingProp == null)
          return true;
        if (existingProp is Enum e1 && incomingProp is Enum e2)
          return e1.Equals(e2);

        if (existingProp is double d1 && incomingProp is double d2)
        {
          var f = Speckle.Core.Kits.Units.GetConversionFactor(existingUnits, incomingUnits);
          return Math.Abs(d1 * f - d2) < .0001;
        }

        return existingProp == incomingProp;
      }
    }

    public AnalyticalModel() { }
  }

  public class DiffAnalyticalModel : AnalyticalModel
  {
    public DiffAnalyticalModel() { }
  }
}
