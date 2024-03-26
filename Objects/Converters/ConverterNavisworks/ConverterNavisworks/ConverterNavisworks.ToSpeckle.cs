using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop.ComApi;
using Objects.BuiltElements;
using Objects.Core.Models;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using static Autodesk.Navisworks.Api.ComApi.ComApiBridge;

namespace Objects.Converter.Navisworks;

// ReSharper disable once UnusedType.Global
public partial class ConverterNavisworks
{
  public Base ConvertToSpeckle(object @object)
  {
    var _ = Settings.TryGetValue("_Mode", out var mode);

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
              pseudoId == ROOT_NODE_PSEUDO_ID
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
          case SavedViewpoint savedViewpoint:
            @base = ViewpointToBase(savedViewpoint);
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

  private static readonly int[] s_lowerBounds = new int[1] { 1 };

  public List<Base> ConvertToSpeckle(List<object> objects) => throw new NotImplementedException();

  public bool CanConvertToSpeckle(object @object)
  {
    if (@object is ModelItem modelItem)
    {
      return CanConvertToSpeckle(modelItem);
    }

    return false;
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

  private static Point ToPoint(InwLPos3f v) => new(v.data1, v.data2, v.data3);

  private static Vector ToVector(InwLVec3f v) => new(v.data1, v.data2, v.data3);

  private static View3D ViewpointToBase(Viewpoint viewpoint, string name = "Commit View")
  {
    var scaleFactor = UnitConversion.ScaleFactor(Application.ActiveDocument.Units, Units.Meters);

    var vp = viewpoint.CreateCopy();
    var anonView = ToInwOpAnonView(vp);
    var viewPoint = anonView.ViewPoint;

    var camera = viewPoint.Camera;

    var cameraDirection = camera.GetViewDir();
    var cameraUp = camera.GetUpVector();
    var cameraPosition = camera.Position;

    var viewDirection = ToVector(cameraDirection);
    var viewUp = ToVector(cameraUp);
    var viewPosition = ToPoint(cameraPosition);

    var focalDistance = 1.0;

    string cameraType;
    string zoom;
    double zoomValue;

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
          focalDistance = vp.FocalDistance;
        }
        catch (Exception ex)
        {
          switch (ex)
          {
            case NullReferenceException:
              SpeckleLog.Logger.Information(
                "A selected view's viewpoint has no focal distance set and the prop is null. The focal distance will be set to 1m"
              );
              break;
            case System.Runtime.InteropServices.COMException:
            case NotSupportedException:
              SpeckleLog.Logger.Information(
                "A selected view's viewpoint has no focal distance set and the getter throws either of two errors, this is rare but possible and frankly terrible from Navisworks. The focal distance will be set to 1m"
              );
              break;
            default:
              throw;
          }
        }

        zoomValue = focalDistance * scaleFactor;
        break;
      default:
        Console.WriteLine("No View");
        return null;
    }

    var origin = ScalePoint(viewPosition, scaleFactor);
    var target = ScalePoint(GetViewTarget(viewPosition, viewDirection, focalDistance), scaleFactor);

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

  private static Point ScalePoint(Point cameraPosition, double scaleFactor) =>
    new(cameraPosition.x * scaleFactor, cameraPosition.y * scaleFactor, cameraPosition.z * scaleFactor);

  private static Point GetViewTarget(Point cameraPosition, Vector viewDirection, double focalDistance) =>
    new(
      cameraPosition.x + viewDirection.x * focalDistance,
      cameraPosition.y + viewDirection.y * focalDistance,
      cameraPosition.z + viewDirection.z * focalDistance
    );

  private static View3D ViewpointToBase(SavedViewpoint savedViewpoint)
  {
    var view = ViewpointToBase(savedViewpoint.Viewpoint, savedViewpoint.DisplayName);

    return view;
  }

  private static Base ModelItemToSpeckle(ModelItem element)
  {
    if (IsElementHidden(element))
    {
      return null;
    }

    var @base = ConvertModelItemToSpeckle(element);

    var firstChild = element.Children.FirstOrDefault(c => !string.IsNullOrEmpty(c.DisplayName));
    var parent = element.Ancestors.FirstOrDefault(p => !string.IsNullOrEmpty(p.DisplayName));

    var resolvedName = string.IsNullOrEmpty(element.DisplayName)
      ? string.IsNullOrEmpty(firstChild?.DisplayName)
        ? parent?.DisplayName
        : firstChild.DisplayName
      : element.DisplayName;

    @base["name"] = string.IsNullOrEmpty(resolvedName)
      ? element.PropertyCategories.FindPropertyByName(PropertyCategoryNames.Item, DataPropertyNames.ItemIcon).ToString()
      : GetSanitizedPropertyName(resolvedName);

    // Geometry items have no children
    if (element.HasGeometry)
    {
      GeometryToSpeckle(element, @base);
      AddItemProperties(element, @base);

      return @base;
    }

    // This really shouldn't exist, but is included for the what if arising from arbitrary IFCs
    if (!element.Children.Any())
    {
      return null;
    }

    // Lookup ahead of time for wasted effort, collection is
    // invalid if it has no children, or no children through hiding
    if (element.Descendants.All(x => x.IsHidden))
    {
      return null;
    }

    // After the fact empty Collection post traversal is also invalid
    // Emptiness by virtue of failure to convert for whatever reason
    if (!element.Children.Any(CanConvertToSpeckle))
    {
      return null;
    }

    // ((Collection)@base).elements = elements;

    AddItemProperties(element, @base);

    return @base;
  }

  /// <summary>
  /// Converts a ModelItem to a Speckle object based on its properties.
  /// </summary>
  /// <param name="element">The ModelItem element to be converted.</param>
  /// <returns>
  /// Returns a GeometryNode if the element has geometry; otherwise, returns a Collection object with the category type set.
  /// </returns>
  private static Base ConvertModelItemToSpeckle(ModelItem element)
  {
    // Find the category property of the ModelItem
    var elementCategoryProperty = FindCategoryProperty(element);

    // Determine the type of the category
    var elementCategoryType = GetCategoryDisplayName(elementCategoryProperty);

    // Return either a GeometryNode or a Collection based on the element's properties
    return CreateSpeckleObject(element, elementCategoryType);
  }

  /// <summary>
  /// Finds the category property of a ModelItem element.
  /// </summary>
  /// <param name="element">The ModelItem element.</param>
  /// <returns>The found category property.</returns>
  private static DataProperty FindCategoryProperty(ModelItem element) =>
    element.PropertyCategories.FindPropertyByName(PropertyCategoryNames.Item, DataPropertyNames.ItemIcon);

  /// <summary>
  /// Gets the display name of a category property.
  /// </summary>
  /// <param name="categoryProperty">The category property.</param>
  /// <returns>The display name of the category.</returns>
  private static string GetCategoryDisplayName(DataProperty categoryProperty) =>
    categoryProperty.Value.ToNamedConstant().DisplayName;

  /// <summary>
  /// Creates a Speckle object based on the ModelItem element and its category type.
  /// </summary>
  /// <param name="element">The ModelItem element.</param>
  /// <param name="categoryType">The type of the category.</param>
  /// <returns>A Speckle object.</returns>
  private static Base CreateSpeckleObject(ModelItem element, string categoryType) =>
    element.HasGeometry ? new GeometryNode() : new Collection { collectionType = categoryType };

  private static void GeometryToSpeckle(ModelItem element, Base @base)
  {
    var geometry = new NavisworksGeometry(element) { IsUpright = IsUpright };

    PopulateModelFragments(geometry);
    var fragmentGeometry = TranslateFragmentGeometry(geometry);

    if (fragmentGeometry != null && fragmentGeometry.Count != 0)
    {
      @base["@displayValue"] = fragmentGeometry;
    }
  }

  private static bool CanConvertToSpeckle(ModelItem item)
  {
    // Only Geometry no children
    if (!item.HasGeometry || item.Children.Any())
    {
      return true;
    }

    const PrimitiveTypes ALLOWED_TYPES =
      PrimitiveTypes.Lines | PrimitiveTypes.Triangles | PrimitiveTypes.SnapPoints | PrimitiveTypes.Text;

    var primitives = item.Geometry.PrimitiveTypes;
    var primitiveTypeSupported = (primitives & ALLOWED_TYPES) == primitives;

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

    // ReSharper disable once RedundantExplicitArraySize
    var oneBasedArray = Array.CreateInstance(
      typeof(int),
      new int[1] { pathArray.Length },
      // ReSharper disable once RedundantExplicitArraySize
      s_lowerBounds
    );

    Array.Copy(pathArray, 0, oneBasedArray, 1, pathArray.Length);

    protoPath.ArrayData = oneBasedArray;

    return ToModelItem(protoPath);
  }
}
