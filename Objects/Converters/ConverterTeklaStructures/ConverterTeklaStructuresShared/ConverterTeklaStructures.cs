using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using Speckle.Core.Logging;
using Tekla.Structures.Model;
using Tekla.Structures;


namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures : ISpeckleConverter
  {
#if TeklaStructures2021
    public static string TeklaStructuresAppName = Applications.TeklaStructures2021;
#elif TeklaStructures2020
  public static string TeklaStructuresAppName = Applications.TeklaStructures2020;
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
    /// <summary>
    /// <para>To know which other objects are being converted, in order to sort relationships between them.
    /// For example, elements that have children use this to determine whether they should send their children out or not.</para>
    /// </summary>
    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    /// <summary>
    /// <para>To keep track of previously received objects from a given stream in here. If possible, conversions routines
    /// will edit an existing object, otherwise they will delete the old one and create the new one.</para>
    /// </summary>
    public List<ApplicationPlaceholderObject> PreviousContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();


    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();

    public ProgressReport Report { get; private set; } = new ProgressReport();

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case BE.Beam b:
          return true;
        case BE.Area a:
          return true;
        default:
          return false;
          //_ => (@object as ModelObject).IsElementSupported()
      };
    }

    public bool CanConvertToSpeckle(object @object)
    {
      //return @object
      switch (@object)
      {
        case Beam b:
          return true;
        case PolyBeam pb:
          return true;
        case SpiralBeam sb:
          return true;
        case BoltGroup bg:
          return true;
        case ContourPlate cp:
            return true;
        default:
          return false;
          //_ => (@object as ModelObject).IsElementSupported()
      };
    }



    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        case BE.Beam o:
          BeamToNative(o);
          return true;
        case BE.Area o:
          ContourPlateToNative(o);
          return true;
        default:
          return false;
      }
    }

    public List<object> ConvertToNative(List<Base> objects) => objects.Select(ConvertToNative).ToList();

    public Base ConvertToSpeckle(object @object)
    {

      Base returnObject = null;
      switch (@object)
      {
        case Beam o:
          returnObject = BeamToSpeckle(o);
          Report.Log($"Created Beam");
          break;
        case PolyBeam o:
          returnObject = PolyBeamToSpeckle(o);
          Report.Log($"Created PolyBeam");
          break;
        case SpiralBeam o:
          returnObject = SpiralBeamToSpeckle(o);
          Report.Log($"Created SpiralBeam");
          break;
        case BoltGroup o:
          returnObject = BoltsToSpeckle(o);
          Report.Log($"Created Bolts");
          break;
        case ContourPlate o:
            returnObject = ContourPlateToSpeckle(o);
          Report.Log($"Created ContourPlate");
            break;
        default:
          ConversionErrors.Add(new Exception($"Skipping not supported type: {@object.GetType()}{GetElemInfo(@object)}"));
          returnObject = null;
          break;

      }
      return returnObject;
    }

    private string GetElemInfo(object o)
    {
      if (o is ModelObject e)
      {
        return $", name: {e.Identifier.GetType().ToString()}, id: {e.Identifier.ToString()}";
      }

      return "";
    }

    public List<Base> ConvertToSpeckle(List<object> objects) => objects.Select(ConvertToSpeckle).ToList();

    public IEnumerable<string> GetServicedApplications() => new string[] { TeklaStructuresAppName };


    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;
    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => PreviousContextObjects = objects;


    public void SetConverterSettings(object settings)
    {
      throw new NotImplementedException("This converter does not have any settings.");
    }
  }
}
