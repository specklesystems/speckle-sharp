using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop.ComApi;
using static Autodesk.Navisworks.Api.ComApi.ComApiBridge;
using Autodesk.Navisworks.Api.Interop;
using Objects.BuiltElements;
using Objects.Geometry;

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
              element = PointerToModelItem(pseudoId);
              break;
            case ModelItem item:
              element = item;
              break;
            default:
              return null;
          }

          @base = ModelItemToBase(element);

          // convertedIds should be populated with all the pseudoIds of nested children already converted in traversal
          // the DescendantsAndSelf helper method means we don't need to keep recursing reference 
          // the "__" prefix is skipped in object serialization so we can use Base object to pass data back to the Connector
          var list = element.DescendantsAndSelf.Select(x =>
            ((Array)ToInwOaPath(x).ArrayData)
            .ToArray<int>().Aggregate("",
              (current, value) => current + (value.ToString().PadLeft(4, '0') + "-")).TrimEnd('-')).ToList();
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

    private SavedViewpoint ReferenceToSavedViewpoint(string referenceId)
    {
      var reference = referenceId.Split(':');

      var savedItemReference = Doc.ResolveReference(new SavedItemReference(reference[0], reference[1]));

      if (savedItemReference != null)
      {
        return (SavedViewpoint)savedItemReference;
      }

      return null;
    }


    private static Base ViewpointToBase(Viewpoint viewpoint, string name = "Commit View")
    {
      var position = viewpoint.Position;
      var forward = GetViewDir(viewpoint);
      var up = viewpoint.HasWorldUpVector ? viewpoint.WorldUpVector : new UnitVector3D(0, 0, 1);
      var focalDistance = viewpoint.HasFocalDistance ? viewpoint.FocalDistance : 1;
      var isOrtho = viewpoint.Projection == ViewpointProjection.Orthographic;
      var target = new Point3D(position.X + forward.X * focalDistance, position.Y + forward.Y * focalDistance,
        position.Z + forward.Z * focalDistance);

      var scaleFactor = UnitConversion.ScaleFactor(Application.ActiveDocument.Units, Units.Meters);

      var @view = new View3D
      {
        applicationId = name,
        name = name,
        origin = ScaleViewpointPosition( new Point(position.X, position.Y, position.Z),scaleFactor),
        target = ScaleViewpointPosition(new Point(target.X, target.Y, target.Z),scaleFactor),
        upDirection = ToSpeckleVector(up),
        forwardDirection = ToSpeckleVector(forward),
        isOrthogonal = isOrtho
      };
      return @view;
    }

    private static Point ScaleViewpointPosition(Point point, double scaleFactor = 1)
    {
      var newPoint = new Point(point.x * scaleFactor, point.y * scaleFactor, point.z * scaleFactor);
      
      return newPoint;
    }

    private static Base ViewpointToBase(SavedViewpoint savedViewpoint)
    {
      var @view = ViewpointToBase(savedViewpoint.Viewpoint, savedViewpoint.DisplayName);

      return @view;
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

    private Base ModelItemToBase(ModelItem element)
    {
      var @base = new Base
      {
        applicationId = PseudoIdFromModelItem(element),
        //["bbox"] = BoxToSpeckle(element.BoundingBox()),
      };

      

      if (element.HasGeometry)
      {
        var geometry = new NavisworksGeometry(element) { ElevationMode = ElevationMode };

        PopulateModelFragments(geometry);
        var fragmentGeometry = TranslateFragmentGeometry(geometry);

        if (fragmentGeometry != null && fragmentGeometry.Any())
        {
          @base["displayValue"] = fragmentGeometry;
        }
      }

      if (element.Children.Any())
      {
        var children = element.Children.ToList();
        var convertedChildren = ConvertToSpeckle(children);
        @base["@Elements"] = convertedChildren.ToList();
      }

      if (element.ClassDisplayName != null)
      {
        @base["ClassDisplayName"] = element.ClassDisplayName;
      }

      if (element.ClassName != null)
      {
        @base["ClassName"] = element.ClassName;
      }

      if (element.Model != null)
      {
        @base["Creator"] = element.Model.Creator;
      }

      if (element.DisplayName != null)
      {
        @base["DisplayName"] = element.DisplayName;
      }

      if (element.Model != null)
      {
        @base["Filename"] = element.Model.FileName;
      }

      if (element.InstanceGuid.ToByteArray().Select(x => (int)x).Sum() > 0)
      {
        @base["InstanceGuid"] = element.InstanceGuid;
      }

      if (element.IsCollection)
      {
        @base["NodeType"] = "Collection";
      }

      if (element.IsComposite)
      {
        @base["NodeType"] = "Composite Object";
      }

      if (element.IsInsert)
      {
        @base["NodeType"] = "Geometry Insert";
      }

      if (element.IsLayer)
      {
        @base["NodeType"] = "Layer";
      }

      if (element.Model != null)
      {
        @base["Source"] = element.Model.SourceFileName;
      }

      if (element.Model != null)
      {
        @base["Source Guid"] = element.Model.SourceGuid;
      }

      var propertiesBase = GetPropertiesBase(element, ref @base);

      @base["Properties"] = propertiesBase;

      return @base;
    }


    public List<Base> ConvertToSpeckle(List<object> objects) =>
      objects.Where(CanConvertToSpeckle)
        .Select(ConvertToSpeckle)
        .ToList();

    public List<Base> ConvertToSpeckle(List<ModelItem> modelItems) =>
      modelItems.Where(CanConvertToSpeckle).Select(ConvertToSpeckle).ToList();

    public bool CanConvertToSpeckle(object @object)
    {
      if (@object is ModelItem modelItem) return CanConvertToSpeckle(modelItem);

      // is expecting @object to be a pseudoId string
      if (!(@object is string pseudoId)) return false;

      var item = PointerToModelItem(pseudoId);

      return CanConvertToSpeckle(item);
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
