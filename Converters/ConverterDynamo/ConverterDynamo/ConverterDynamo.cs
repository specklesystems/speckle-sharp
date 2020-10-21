using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Speckle.Core.Logging;

namespace Objects.Converter.Dynamo
{
  public class ConverterDynamo : ISpeckleConverter
  {
    #region implemented methods
    public string Description => "Default Speckle Kit for Dynamo";
    public string Name => nameof(ConverterDynamo);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";
    public IEnumerable<string> GetServicedApplications() => new string[] { Applications.Dynamo };

    public HashSet<Error> ConversionErrors { get; private set; } = new HashSet<Error>();

    public Base ConvertToSpeckle(object @object)
    {
      if (@object is Base)
        return @object as Base;

      var m = ConversionMethods(@object, "ToSpeckle");
      if (m == null)
        throw new NotSupportedException();
      return m.Invoke(null, new[] { @object }) as Base;

      //switch (@object)
      //{
      //  case float o:
      //    return o.ToSpeckle();
      //  case long o:
      //    return o.ToSpeckle();
      //  case int o:
      //    return o.ToSpeckle();
      //  case double o:
      //    return o.ToSpeckle();
      //  case bool o:
      //    return o.ToSpeckle();
      //  case string o:
      //    return o.ToSpeckle();
      //  case DS.Point o:
      //    return o.ToSpeckle();
      //  case DS.Vector o:
      //    return o.ToSpeckle();
      //  case DS.Plane o:
      //    return o.ToSpeckle();
      //  case DS.Line o:
      //    return o.ToSpeckle();
      //  case DS.Rectangle o:
      //    return o.ToSpeckle();
      //  case DS.Polygon o:
      //    return o.ToSpeckle();
      //  case DS.Circle o:
      //    return o.ToSpeckle();
      //  case DS.Arc o:
      //    return o.ToSpeckle();
      //  case DS.Ellipse o:
      //    return o.ToSpeckle();
      //  case DS.EllipseArc o:
      //    return o.ToSpeckle();
      //  case DS.PolyCurve o:
      //    return o.ToSpeckle();
      //  case DS.NurbsCurve o:
      //    return o.ToSpeckle();
      //  case DS.Helix o:
      //    return o.ToSpeckle();
      //  case DS.Curve o:
      //    return o.ToSpeckle();
      //  case DS.Mesh o:
      //    return o.ToSpeckle();
      //  default:
      //    throw new NotSupportedException();
      //}
    }

    public object ConvertToNative(Base @object)
    {
      var m = ConversionMethods(@object, "ToNative");
      if (m == null)
        throw new NotSupportedException();
      return m.Invoke(null, new[] { @object });

      //switch (@object)
      //{
      //  case Number o:
      //    return o.ToNative();
      //  case Boolean o:
      //    return o.ToNative();
      //  case String o:
      //    return o.ToNative();
      //  case Point o:
      //    return o.ToNative();
      //  case Vector o:
      //    return o.ToNative();
      //  case Plane o:
      //    return o.ToNative();
      //  case Line o:
      //    return o.ToNative();
      //  case Polycurve o:
      //    return o.ToNative();
      //  case Circle o:
      //    return o.ToNative();
      //  case Arc o:
      //    return o.ToNative();
      //  case Ellipse o:
      //    return o.ToNative();
      //  case Curve o:
      //    return o.ToNative();
      //  case Brep o:
      //    return o.ToNative();
      //  case Mesh o:
      //    return o.ToNative();
      //  default:
      //    throw new NotSupportedException();
      //}
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList(); ;
    }

    private MethodInfo ConversionMethods(object @object, string methodName)
    {
      //is there any method that takes in the above type as input?
      return typeof(Conversion).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
        .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Any(p => p.ParameterType == @object.GetType()));
    }

    public bool CanConvertToSpeckle(object @object)
    {
      return ConversionMethods(@object, "ToSpeckle") != null;
    }

    public bool CanConvertToNative(Base @object)
    {
      return ConversionMethods(@object, "ToNative") != null;
    }

    public void SetContextDocument(object doc)
    {
      throw new SpeckleException("The Dynamo converter does not support this method");
    }

    #endregion

  }
}
