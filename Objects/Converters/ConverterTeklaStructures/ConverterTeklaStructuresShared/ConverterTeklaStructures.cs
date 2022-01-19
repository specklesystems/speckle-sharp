using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using Objects.Converter.TeklaStructures;
using Speckle.Core.Logging;
using Tekla.Structures.Model;


namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures : ISpeckleConverter
  {
#if TeklaStructures2021
    public static string TeklaStructuresAppName = Applications.TeklaStructures2021;
#else
    public static string TeklaStructuresAppName = Applications.TeklaStructures;
#endif
    public string Description => "Default Speckle Kit for TeklaStructures";

    public string Name => nameof(ConverterTeklaStructures);

    public string Author => "Speckle";

    public string WebsiteOrEmail => "https://speckle.systems";

    public Model Model { get; private set; }

    public void SetContextDocument(object doc)
    {
      Model = (Model)doc;
    }

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();

    public ProgressReport Report { get; private set; } = new ProgressReport();

    public bool CanConvertToNative(Base @object)
    {
      foreach (var type in Enum.GetNames(typeof(ConverterTeklaStructures.TeklaStructuresConverterSupported)))
      {
        if (type == @object.ToString().Split('.').Last())
        {
          return true;
        }
      }
      return false;
    }

    public bool CanConvertToSpeckle(object @object)
    {
      foreach (var type in Enum.GetNames(typeof(ConverterTeklaStructures.TeklaStructuresAPIUsableTypes)))
      {
        if (type == @object.ToString())
        {
          return true;
        }
      }
      return false;
    }

    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        default:
          return null;
      }
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList();
    }

    public Base ConvertToSpeckle(object @object)
    {
      (string type, string name) = ((string, string))@object;
      Base returnObject = null;
      switch (type)
      {
        case "Beam":
          return BeamToSpeckle(type);

      }
      return returnObject;
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public IEnumerable<string> GetServicedApplications() => new string[] { TeklaStructuresAppName };


    public void SetContextObjects(List<ApplicationPlaceholderObject> objects)
    {
      throw new NotImplementedException();
    }

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects)
    {
      throw new NotImplementedException();
    }

    public void SetConverterSettings(object settings)
    {
      throw new NotImplementedException("This converter does not have any settings.");
    }
  }
}
