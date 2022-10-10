using Speckle.Core.Kits;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tekla.Structures;
using Tekla.Structures.Model;
using BE = Objects.BuiltElements;
using GE = Objects.Geometry;


namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures : ISpeckleConverter
  {
#if TeklaStructures2021
    public static string TeklaStructuresAppName = HostApplications.TeklaStructures.GetVersion(HostAppVersion.v2021);
#elif TeklaStructures2020
    public static string TeklaStructuresAppName = HostApplications.TeklaStructures.GetVersion(HostAppVersion.v2020);
#else
    public static string TeklaStructuresAppName = HostApplications.TeklaStructures.Name;
#endif
    public string Description => "Default Speckle Kit for TeklaStructures";

    public string Name => nameof(ConverterTeklaStructures);

    public Dictionary<string, string> Settings { get; private set; } = new Dictionary<string, string>();

    public string Author => "Speckle";

    public string WebsiteOrEmail => "https://speckle.systems";

    public Model Model { get; private set; }

    public ReceiveMode ReceiveMode { get; set; }

    public void SetContextDocument(object doc)
    {
      Model = (Model)doc;
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

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();

    public ProgressReport Report { get; private set; } = new ProgressReport();

    public bool CanConvertToNative(Base @object)
    {
      Settings.TryGetValue("recieve-objects-mesh", out string recieveModelMesh);
      if (bool.Parse(recieveModelMesh) == true)
      {
        return true;
      }

      switch (@object)
      {
        case BE.Beam b:
          return true;
        case BE.Column b:
          return true;
        case BE.Area a:
          return true;
        //recieving should first be ordered to place parts first before fittings, welds, booleans, bolts can be converted
        //case BE.TeklaStructures.Bolts b:
        //    return true;
        //case BE.TeklaStructures.Welds w:
        //    return true;
        //case BE.Opening o:
        //    return true;
        //case Geometry.Plane p:
        //    return true;
        //case Geometry.Plane p:
        //    return true;
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
        case Weld w:
          return true;
        case PolygonWeld pw:
          return true;
        case BooleanPart bp:
          return true;
        case Fitting ft:
          return true;
        default:
          return false;
          //_ => (@object as ModelObject).IsElementSupported()
      };
    }

    public object ConvertToNative(Base @object)
    {

      Settings.TryGetValue("recieve-objects-mesh", out string recieveModelMesh);
      if (bool.Parse(recieveModelMesh) == true)
      {
        try
        {
          var bases = BaseExtensions.Flatten(@object);
          foreach (var @base in bases)
          {
            try
            {
              List<GE.Mesh> displayValues = new List<GE.Mesh> { };
              var meshes = @base.GetType().GetProperty("displayValue").GetValue(@base) as List<GE.Mesh>;
              //dynamic property = propInfo;
              //List<GE.Mesh> meshes = (List<GE.Mesh>)property;       
              MeshToNative(@base, meshes);
            }
            catch
            {

            }
          }
          return true;
        }
        catch
        {

        }
      }

      switch (@object)
      {
        case BE.Beam o:
          BeamToNative(o);
          return true;
        case BE.Column o:
          ColumnToNative(o);
          return true;
        case BE.Area o:
          ContourPlateToNative(o);
          return true;
        case Geometry.Plane o:
          FittingToNative(o);
          return true;
        case BE.Opening o:
          BooleanPartToNative(o);
          return true;
        case BE.TeklaStructures.Bolts o:
          BoltsToNative(o);
          return true;
        case BE.TeklaStructures.Welds o:
          WeldsToNative(o);
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
        case RebarGroup o:
          returnObject = RebarGroupToSpeckle(o);
          Report.Log($"Created Rebars");
          break;
        case ContourPlate o:
          returnObject = ContourPlateToSpeckle(o);
          Report.Log($"Created ContourPlate");
          break;
        case Weld o:
          returnObject = WeldsToSpeckle(o);
          Report.Log($"Created Weld");
          break;
        case PolygonWeld o:
          returnObject = PoylgonWeldsToSpeckle(o);
          Report.Log($"Created PolygonWeld");
          break;
        case BooleanPart o:
          returnObject = BooleanPartToSpeckle(o);
          Report.Log($"Created BooleanPart");
          break;
        case Fitting o:
          returnObject = FittingsToSpeckle(o);
          Report.Log($"Created Fitting");
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
        return $", name: {e.Identifier.GetType().ToString()}, id: {e.Identifier.ToString()}";

      return "";
    }

    public List<Base> ConvertToSpeckle(List<object> objects) => objects.Select(ConvertToSpeckle).ToList();

    public IEnumerable<string> GetServicedApplications() => new string[] { TeklaStructuresAppName };

    public void SetContextObjects(List<ApplicationObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationObject> objects) => PreviousContextObjects = objects;

    public void SetConverterSettings(object settings)
    {
      Settings = settings as Dictionary<string, string>;
    }
  }
}
