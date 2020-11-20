using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rhino;
using Rhino.Geometry;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh : ISpeckleConverter
  {
    public string Description => "Default Speckle Kit for Rhino & Grasshopper";
    public string Name => nameof(ConverterRhinoGh);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";
    public IEnumerable<string> GetServicedApplications() => new string[] { Applications.Rhino, Applications.Grasshopper };

    public HashSet<Error> ConversionErrors { get; private set; } = new HashSet<Error>();

    public RhinoDoc Doc { get; private set; }

    public void SetContextDocument(object doc)
    {
      Doc = (RhinoDoc)doc;
    }

    public Base ConvertToSpeckle(object @object)
    {
      // TODO: Possible hack... check with Dimitrie!
      if (@object is Brep brep && brep.IsSurface)
      {
        // If a brep 'isSurface' means it only has one face and the trim is the boundary.
        // Basically... it's a nurbs surface! Flip the brep for the nurbs in `@object` before trying to convert.
        // TODO: This might also be a revSurface or other type... converting all to nurbs for now.
        var list = brep.Surfaces.ToList();
        @object = list[0].ToNurbsSurface();
      }
      var m = ConversionMethods(@object, "ToSpeckle");

      if (m == null)
        throw new NotSupportedException();

      var @base = m.Invoke(null, new[] { @object }) as Base;

      if (@base is IGeometry geom)
        SetUnits(geom);


      return @base; ;
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public object ConvertToNative(Base @object)
    {
      var m = ConversionMethods(@object, "ToNative");
      if (m == null)
        throw new NotSupportedException();

      return m.Invoke(null, new[] { @object });
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList();
    }

    private MethodInfo ConversionMethods(object @object, string methodName)
    {
      // TODO: Cache the result! 
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
  }
}
