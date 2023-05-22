using System;
using System.Collections.Generic;
using System.Linq;
using Archicad;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using static Speckle.Core.Models.ApplicationObject;

namespace Objects.Converter.Archicad
{
  public partial class ConverterArchicad : ISpeckleConverter
  {
    public string Description => "Default Speckle Kit for Archicad";
    public string Name => nameof(ConverterArchicad);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<string> GetServicedApplications() => new string[] { "Archicad" };

    public ConversionOptions ConversionOptions { get; set; }

    /// <summary>
    /// Keeps track of the conversion process
    /// </summary>
    public ProgressReport Report { get; private set; } = new ProgressReport();

    /// <summary>
    /// Decides what to do when an element being received already exists
    /// </summary>
    public ReceiveMode ReceiveMode { get; set; }

    // send
    public Base ConvertToSpeckle(object @object)
    {
      return null;
    }

    public List<Base> ConvertToSpeckle(List<object> objects) => objects.Select(ConvertToSpeckle).ToList();

    public bool CanConvertToSpeckle(object @object)
    {
      return false;
    }

    // receive
    public object ConvertToNative(Base @object)
    {
      return null;
    }

    public List<object> ConvertToNative(List<Base> objects) => objects.Select(ConvertToNative).ToList();

    public bool CanConvertToNativeImplemented(Base @object)
    {
      return @object
        switch
      {
        // Speckle BIM elements
        Objects.BuiltElements.Beam _ => true,
        Objects.BuiltElements.Column _ => true,
        Objects.BuiltElements.Floor _ => true,
        Objects.BuiltElements.Ceiling _ => true,
        Objects.BuiltElements.Roof _ => true,
        Objects.BuiltElements.Room _ => true,
        Objects.BuiltElements.Wall _ => true,

        // Archicad elements
        Objects.BuiltElements.Archicad.ArchicadDoor => true,
        Objects.BuiltElements.Archicad.ArchicadWindow => true,
        Objects.BuiltElements.Archicad.ArchicadSkylight => true,
        Objects.BuiltElements.Archicad.DirectShape _ => true,

        // Revit elements
        Objects.BuiltElements.Revit.FamilyInstance => true,
        Objects.Other.Revit.RevitInstance => true,

        // Speckle geomtries
        Mesh _ => true,
        Brep _ => true,

        _ => false
      };
    }

    public bool CanConvertToNativeNotImplemented(Base @object)
    {
      return @object
        switch
      {
        // Project info
        Objects.Organization.ModelInfo _ => true,

        _ => false
      };
    }

    public bool CanConvertToNative(Base @object)
    {
      return CanConvertToNativeImplemented(@object) || CanConvertToNativeNotImplemented(@object);
    }

    /// <summary>
    /// <para>To know which other objects are being converted, in order to sort relationships between them.
    /// For example, elements that have children use this to determine whether they should send their children out or not.</para>
    /// </summary>
    public List<ApplicationObject> ContextObjects { get; set; } = new List<ApplicationObject>();

    /// <summary>
    /// <para>To keep track of previously received objects from a given stream in here. If possible, conversions routines
    /// will edit an existing object, otherwise they will delete the old one and create the new one.</para>
    /// </summary>
    public List<ApplicationObject> PreviousContextObjects { get; set; } = new List<ApplicationObject>();

    public void SetContextDocument(object doc)
    {
    }

    public void SetContextObjects(List<ApplicationObject> objects) => ContextObjects = objects;

    /// <summary>
    /// Removes all inherited classes from speckle type string
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string SimplifySpeckleType(string type)
    {
      return type.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }

    public void SetContextObjects(List<TraversalContext> flattenObjects)
    {
      List<ApplicationObject> objects;

      foreach (var tc in flattenObjects)
      {
        var applicationObject = new ApplicationObject(tc.current.id, SimplifySpeckleType(tc.current.speckle_type))
        {
          applicationId = tc.current.applicationId,
          Convertible = true
        };

        ContextObjects.Add(applicationObject);
      }
    }

    public void SetPreviousContextObjects(List<ApplicationObject> objects) => PreviousContextObjects = objects;

    public void SetConverterSettings(object settings)
    {
    }

    public ConverterArchicad(ConversionOptions conversionOptions)
    {
      this.ConversionOptions = conversionOptions;

      var ver = System.Reflection.Assembly.GetAssembly(typeof(ConverterArchicad)).GetName().Version;
      Report.Log($"Using converter: {Name} v{ver}");
    }
  }
}
