using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop.ComApi;
using Objects.BuiltElements;
using Objects.Core.Models;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using static Autodesk.Navisworks.Api.ComApi.ComApiBridge;

namespace Objects.Converter.Navisworks;

public partial class ConverterNavisworks
{
  public Base ConvertToSpeckle(object @object)
  {
    var unused = Settings.TryGetValue("_Mode", out var mode);

    Base @base;

    switch (mode)
    {
      case "objects":

        // is expecting @object to be a pseudoId string, a ModelItem or a ViewProxy that prompts for View generation.
        ModelItem element;

        switch (@object)
        {
          case string pseudoId:
            element =
              pseudoId == RootNodePseudoId
                ? Application.ActiveDocument.Models.RootItems.First
                : PointerToModelItem(pseudoId);
            break;
          case ModelItem item:
            element = item;
            break;
          default:
            return null;
        }

        @base = ModelItemToSpeckle(element);

        if (@base == null)
          return null;

        // convertedIds should be populated with all the pseudoIds of nested children already converted in traversal
        // the DescendantsAndSelf helper method means we don't need to reference recursion
        // the "__" prefix is skipped in object serialization so we can use Base object to pass data back to the Connector
        var list = element.DescendantsAndSelf
          .Select(
            x =>
              ((Array)ToInwOaPath(x).ArrayData)
                .ToArray<int>()
                .Aggregate(
                  "",
                  (current, value) => current + value.ToString(CultureInfo.InvariantCulture).PadLeft(4, '0') + "-"
                )
                .TrimEnd('-')
          )
          .ToList();
        @base["__convertedIds"] = list;

        return @base;

      case "views":
      {
        switch (@object)
        {
          case string referenceOrGuid:
            @base = ViewpointToBase(ReferenceOrGuidToSavedViewpoint(referenceOrGuid));
            break;
          case Viewpoint item:
            @base = ViewpointToBase(item);
            break;
          default:
            return null;
        }

        return @base;
      }
      default:
        return null;
    }
  }

  public List<Base> ConvertToSpeckle(List<object> objects)
  {
    return objects.Where(CanConvertToSpeckle).Select(ConvertToSpeckle).ToList();
  }

  public bool CanConvertToSpeckle(object @object)
  {
    if (@object is ModelItem modelItem)
      return CanConvertToSpeckle(modelItem);

    // is expecting @object to be a pseudoId string
    if (@object is not string pseudoId)
      return false;

    if (pseudoId == RootNodePseudoId)
      return CanConvertToSpeckle(Application.ActiveDocument.Models.RootItems.First);

    var item = PointerToModelItem(pseudoId);

    return CanConvertToSpeckle(item);
  }

  private static SavedViewpoint ReferenceOrGuidToSavedViewpoint(string referenceOrGuid)
  {
    SavedViewpoint savedViewpoint;

    if (Guid.TryParse(referenceOrGuid, out var guid))
    {
      savedViewpoint = (SavedViewpoint)Doc.SavedViewpoints.ResolveGuid(guid);
    }
    else
    {
      var parts = referenceOrGuid.Split(':');
      using var savedItemReference = new SavedItemReference(parts[0], parts[1]);
      savedViewpoint = parts.Length != 2 ? null : (SavedViewpoint)Doc.ResolveReference(savedItemReference);
    }

    return savedViewpoint;
  }

  private static Point ToPoint(InwLPos3f v)
  {
    return new Point(v.data1, v.data2, v.data3);
  }

  private static Vector ToVector(InwLVec3f v)
  {
    return new Vector(v.data1, v.data2, v.data3);
  }

  private static Base ViewpointToBase(Viewpoint viewpoint, string name = "Commit View")
  {
    var scaleFactor = UnitConversion.ScaleFactor(Application.ActiveDocument.Units, Units.Meters);

    var vp = viewpoint.CreateCopy();
    var anonView = ToInwOpAnonView(vp);
    var viewPoint = anonView.ViewPoint;

    var camera = viewPoint.Camera;

    var viewDirection = ToVector(camera.GetViewDir());
    var viewUp = ToVector(camera.GetUpVector());

    var focalDistance = viewPoint.FocalDistance;

    var position = ToPoint(camera.Position);

    var origin = ScalePoint(position, scaleFactor);
    var target = ScalePoint(GetViewTarget(position, viewDirection, focalDistance), scaleFactor);

    string cameraType;
    string zoom;
    double zoomValue = 1;

    switch (vp.Projection)
    {
      case ViewpointProjection.Orthographic:

        cameraType = "Orthogonal Camera";
        zoom = "ViewToWorldScale";

        var dist = vp.VerticalExtentAtFocalDistance / 2 * scaleFactor;
        zoomValue = 3.125 * dist / viewUp.Length;

        break;
      case ViewpointProjection.Perspective:

        cameraType = "PerspectiveCamera";
        zoom = "FieldOfView";

        try
        {
          zoomValue = vp.FocalDistance * scaleFactor;
        }
        catch (NullReferenceException err)
        {
          Console.WriteLine($"No Focal Distance, Are you looking at anything?\n{err.Message}");
        }

        break;
      default:
        Console.WriteLine("No View");
        return null;
    }

    var view = new View3D
    {
      applicationId = name,
      name = name,
      origin = origin,
      target = target,
      upDirection = viewUp,
      forwardDirection = viewDirection,
      isOrthogonal = cameraType == "Orthogonal Camera",
      ["Camera Type"] = cameraType,
      ["Zoom Strategy"] = zoom,
      ["Zoom Value"] = zoomValue,
      ["Field of View"] = camera.HeightField,
      ["Aspect Ratio"] = camera.AspectRatio,
      ["Focal Distance"] = focalDistance,
      // TODO: Handle Clipping planes when the Speckle Viewer supports it or if some smart BCF interop comes into scope.
      ["Clipping Planes"] = JsonConvert.SerializeObject(anonView.ClippingPlanes())
    };

    return view;
  }

  private static Point ScalePoint(Point cameraPosition, double scaleFactor)
  {
    return new Point(cameraPosition.x * scaleFactor, cameraPosition.y * scaleFactor, cameraPosition.z * scaleFactor);
  }

  private static Point GetViewTarget(Point cameraPosition, Vector viewDirection, double focalDistance)
  {
    return new Point(
      cameraPosition.x + viewDirection.x * focalDistance,
      cameraPosition.y + viewDirection.y * focalDistance,
      cameraPosition.z + viewDirection.z * focalDistance
    );
  }

  private static Base ViewpointToBase(SavedViewpoint savedViewpoint)
  {
    var view = ViewpointToBase(savedViewpoint.Viewpoint, savedViewpoint.DisplayName);

    return view;
  }

  private static Base CategoryToSpeckle(ModelItem element)
  {
    var applicationId = PseudoIdFromModelItem(element);

    var elementCategory = element.PropertyCategories.FindPropertyByName(
      PropertyCategoryNames.Item,
      DataPropertyNames.ItemIcon
    );
    var elementCategoryType = elementCategory.Value.ToNamedConstant().DisplayName;

    return elementCategoryType switch
    {
      "Geometry" => new GeometryNode { applicationId = applicationId },
      _ => new Collection { applicationId = applicationId, collectionType = elementCategoryType }
    };
  }

  private static Base ModelItemToSpeckle(ModelItem element)
  {
    if (IsElementHidden(element))
      return null;

    var @base = CategoryToSpeckle(element);

    // Geometry items have no children
    if (element.HasGeometry)
    {
      GeometryToSpeckle(element, @base);
      AddItemProperties(element, @base);

      return @base;
    }

    // This really shouldn't exist, but is included for the what if arising from arbitrary IFCs
    if (!element.Children.Any())
      return null;

    // Lookup ahead of time for wasted effort, collection is
    // invalid if it has no children, or no children through hiding
    if (element.Descendants.All(x => x.IsHidden))
      return null;

    ((Collection)@base).name = element.DisplayName;

    var elements = element.Children.Select(ModelItemToSpeckle).Where(childBase => childBase != null).ToList();

    // After the fact empty Collection post traversal is also invalid
    // Emptiness by virtue of failure to convert for whatever reason
    if (!elements.Any())
      return null;

    ((Collection)@base).elements = elements;

    AddItemProperties(element, @base);

    return @base;
  }

  private static void GeometryToSpeckle(ModelItem element, Base @base)
  {
    var geometry = new NavisworksGeometry(element) { ElevationMode = ElevationMode };

    PopulateModelFragments(geometry);
    var fragmentGeometry = TranslateFragmentGeometry(geometry);

    if (fragmentGeometry != null && fragmentGeometry.Any())
      @base["@displayValue"] = fragmentGeometry;
  }

  private static bool CanConvertToSpeckle(ModelItem item)
  {
    // Only Geometry no children
    if (!item.HasGeometry || item.Children.Any())
      return true;

    const PrimitiveTypes allowedTypes = PrimitiveTypes.Lines | PrimitiveTypes.Triangles | PrimitiveTypes.SnapPoints;

    var primitives = item.Geometry.PrimitiveTypes;
    var primitiveTypeSupported = (primitives & allowedTypes) == primitives;

    return primitiveTypeSupported;
  }

  private static ModelItem PointerToModelItem(object @string)
  {
    int[] pathArray;

    try
    {
      pathArray = @string
        .ToString()
        .Split('-')
        .Select(x => int.TryParse(x, out var value) ? value : throw new FormatException("malformed path pseudoId"))
        .ToArray();
    }
    catch (FormatException)
    {
      return null;
    }

    var protoPath = (InwOaPath)State.ObjectFactory(nwEObjectType.eObjectType_nwOaPath);

    var oneBasedArray = Array.CreateInstance(typeof(int), new int[1] { pathArray.Length }, new int[1] { 1 });

    Array.Copy(pathArray, 0, oneBasedArray, 1, pathArray.Length);

    protoPath.ArrayData = oneBasedArray;

    return ToModelItem(protoPath);
  }
}
