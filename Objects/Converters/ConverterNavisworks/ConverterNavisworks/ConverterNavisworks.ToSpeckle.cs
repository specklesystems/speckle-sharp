using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop.ComApi;
using Objects.BuiltElements;
using Objects.Converter.Navisworks.Objects;
using Objects.Geometry;
using Objects.Organization;
using Objects.Structural;
using Speckle.Core.Models;
using static Autodesk.Navisworks.Api.ComApi.ComApiBridge;
using Application = Autodesk.Navisworks.Api.Application;

namespace Objects.Converter.Navisworks
{
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
              element = pseudoId == RootNodePseudoId
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

          if (@base == null) return null;

          // convertedIds should be populated with all the pseudoIds of nested children already converted in traversal
          // the DescendantsAndSelf helper method means we don't need to keep recursing reference 
          // the "__" prefix is skipped in object serialization so we can use Base object to pass data back to the Connector
          var list = element.DescendantsAndSelf.Select(x =>
            ((Array)ToInwOaPath(x).ArrayData)
            .ToArray<int>().Aggregate("",
              (current, value) => current + value.ToString().PadLeft(4, '0') + "-").TrimEnd('-')).ToList();
          @base["__convertedIds"] = list;

          return @base;


        case "views":
        {
          switch (@object)
          {
            case string reference:
              @base = ViewpointToBase(ReferenceToSavedViewpoint(reference));
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

    public static List<Base> DescendantsAndSelf(Base element)
    {
      var descendants = new HashSet<Base>();

      switch (element)
      {
        case Objects.Geometry geometry:
          descendants.Add(geometry);
          break;
        case Collection collection:
        {
          var collectionProperties = collection.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(Collection));

          foreach (var property in collectionProperties)
          {
            var childCollection = (Collection)property.GetValue(collection);

            descendants.UnionWith(DescendantsAndSelf(childCollection));
          }

          break;
        }
        default:
          throw new ArgumentException("Item must be a Geometry or ElementCollection object.");
      }

      return descendants.ToList();
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Where(CanConvertToSpeckle)
        .Select(ConvertToSpeckle)
        .ToList();
    }

    public bool CanConvertToSpeckle(object @object)
    {
      if (@object is ModelItem modelItem) return CanConvertToSpeckle(modelItem);

      // is expecting @object to be a pseudoId string
      if (!(@object is string pseudoId)) return false;

      if (pseudoId == RootNodePseudoId) return CanConvertToSpeckle(Application.ActiveDocument.Models.RootItems.First);

      var item = PointerToModelItem(pseudoId);


      return CanConvertToSpeckle(item);
    }

    private SavedViewpoint ReferenceToSavedViewpoint(string referenceId)
    {
      var reference = referenceId.Split(':');

      var savedItemReference = Doc.ResolveReference(new SavedItemReference(reference[0], reference[1]));

      if (savedItemReference != null) return (SavedViewpoint)savedItemReference;

      return null;
    }


    private static Base ViewpointToBase(Viewpoint viewpoint, string name = "Commit View")
    {
      var position = viewpoint.Position;
      var forward = GetViewDir(viewpoint);
      var up = viewpoint.HasWorldUpVector
        ? viewpoint.WorldUpVector
        : new UnitVector3D(0, 0, 1);
      var focalDistance = viewpoint.HasFocalDistance
        ? viewpoint.FocalDistance
        : 1;
      var isOrtho = viewpoint.Projection == ViewpointProjection.Orthographic;
      var target = new Point3D(position.X + forward.X * focalDistance, position.Y + forward.Y * focalDistance,
        position.Z + forward.Z * focalDistance);

      var scaleFactor = UnitConversion.ScaleFactor(Application.ActiveDocument.Units, Units.Meters);

      var view = new View3D
      {
        applicationId = name,
        name = name,
        origin = ScaleViewpointPosition(new Point(position.X, position.Y, position.Z), scaleFactor),
        target = ScaleViewpointPosition(new Point(target.X, target.Y, target.Z), scaleFactor),
        upDirection = ToSpeckleVector(up),
        forwardDirection = ToSpeckleVector(forward),
        isOrthogonal = isOrtho
      };
      return view;
    }

    private static Point ScaleViewpointPosition(Point point, double scaleFactor = 1)
    {
      var newPoint = new Point(point.x * scaleFactor, point.y * scaleFactor, point.z * scaleFactor);

      return newPoint;
    }

    private static Base ViewpointToBase(SavedViewpoint savedViewpoint)
    {
      var view = ViewpointToBase(savedViewpoint.Viewpoint, savedViewpoint.DisplayName);

      return view;
    }

    private static Vector ToSpeckleVector(Vector3D forward)
    {
      return new Vector(forward.X, forward.Y, forward.Z);
    }

    private static Vector3D GetViewDir(Viewpoint vp)
    {
      var negZ = new Rotation3D(0, 0, -1, 0);
      var tempRot = MultiplyRotation3D(negZ, vp.Rotation.Invert());
      var viewDirRot = MultiplyRotation3D(vp.Rotation, tempRot);
      var viewDir = new Vector3D(viewDirRot.A, viewDirRot.B, viewDirRot.C);
      return viewDir.Normalize();
    }


    private static Rotation3D MultiplyRotation3D(Rotation3D first, Rotation3D second)
    {
      var result = new Rotation3D(
        second.D * first.A + second.A * first.D + second.B * first.C - second.C * first.B,
        second.D * first.B + second.B * first.D + second.C * first.A - second.A * first.C,
        second.D * first.C + second.C * first.D + second.A * first.B - second.B * first.A,
        second.D * first.D - second.A * first.A - second.B * first.B - second.C * first.C
      );
      result.Normalize();
      return result;
    }

    private static Base CategoryToSpeckle(ModelItem element)
    {
      var applicationId = PseudoIdFromModelItem(element);

      var elementCategory = element.PropertyCategories
        .FindPropertyByName(PropertyCategoryNames.Item, DataPropertyNames.ItemIcon);
      var elementCategoryType = elementCategory.Value.ToNamedConstant().DisplayName;

      switch (elementCategoryType)
      {
        case "Geometry":
          return new Objects.Geometry { applicationId = applicationId };
        default:
          return new Collection { applicationId = applicationId, collectionType = elementCategoryType };
      }
    }

    private static Base ModelItemToSpeckle(ModelItem element)
    {
      if (IsElementHidden(element)) return null;


      var @base = CategoryToSpeckle(element);


      var properties =
        !bool.TryParse(Settings.FirstOrDefault(x => x.Key == "include-properties").Value, out var result) || result;

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

      ((Collection)@base).name = element.DisplayName ?? null;

      var elements = element.Children.Select(ModelItemToSpeckle).Where(childBase => childBase != null).ToList();

      // After the fact empty Collection post traversal is also invalid
      // Emptiness by virtue of failure to convert for whatever reason
      if (!elements.Any())
      {
        return null;
      }

      ((Collection)@base).elements = elements;

      AddItemProperties(element, @base);

      return @base;
    }

    private static void GeometryToSpeckle(ModelItem element, Base @base)
    {
      var geometry = new NavisworksGeometry(element) { ElevationMode = ElevationMode };

      PopulateModelFragments(geometry);
      var fragmentGeometry = TranslateFragmentGeometry(geometry);

      if (fragmentGeometry != null && fragmentGeometry.Any()) @base["displayValue"] = fragmentGeometry;
    }


    public List<Base> ConvertToSpeckle(List<ModelItem> modelItems)
    {
      return modelItems.Where(CanConvertToSpeckle).Select(ConvertToSpeckle).ToList();
    }

    private static bool CanConvertToSpeckle(ModelItem item)
    {
      // Only Geometry no children
      if (!item.HasGeometry || item.Children.Any()) return true;

      const PrimitiveTypes allowedTypes = PrimitiveTypes.Lines | PrimitiveTypes.Triangles;

      var primitives = item.Geometry.PrimitiveTypes;
      var primitiveTypeSupported = (primitives & allowedTypes) == primitives;

      return primitiveTypeSupported;
    }


    private static ModelItem PointerToModelItem(object @string)
    {
      int[] pathArray;

      try
      {
        pathArray = @string.ToString()
          .Split('-')
          .Select(x => int.TryParse(x,
            out var value)
            ? value
            : throw new Exception("malformed path pseudoId"))
          .ToArray();
      }
      catch
      {
        return null;
      }

      var protoPath = (InwOaPath)State.ObjectFactory(nwEObjectType.eObjectType_nwOaPath);

      var oneBasedArray = Array.CreateInstance(
        typeof(int),
        new int[1] { pathArray.Length },
        new int[1] { 1 });

      Array.Copy(pathArray, 0, oneBasedArray, 1, pathArray.Length);

      protoPath.ArrayData = oneBasedArray;

      return ToModelItem(protoPath);
    }
  }
}
