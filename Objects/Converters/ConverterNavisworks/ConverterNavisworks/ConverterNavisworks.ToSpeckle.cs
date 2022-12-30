using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;
using Autodesk.Navisworks.Api.Interop.ComApi;

namespace Objects.Converter.Navisworks
{
  public partial class ConverterNavisworks
  {
    public Base ConvertToSpeckle(object @object)
    {
      // is expecting @object to be a pseudoId string or a ModelItem
      ModelItem element = null;

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

      var @base = ModelItemToBase(element);

      // convertedIds should be populated with all the pseudoIds of nested children already converted in traversal
      // the DescendantsAndSelf helper method means we don't need to keep recursing reference 
      // the "__" prefix is skipped in object serialization so we can use Base object to pass data back to the Connector
      List<string> list = element.DescendantsAndSelf.Select(x =>
        ((Array)ComApiBridge.ToInwOaPath(x).ArrayData)
        .ToArray<int>().Aggregate("",
          (current, value) => current + (value.ToString().PadLeft(4, '0') + "-")).TrimEnd('-')).ToList();
      @base["__convertedIds"] = list;

      return @base;
    }


    private Base ModelItemToBase(ModelItem element)
    {
      var handle = PseudoIdFromModelItem(element);

      var @base = new Base
      {
        applicationId = handle,
        ["bbox"] = BoxToSpeckle(element.BoundingBox()),
      };

      if (element.HasGeometry)
      {
        var geometry = new NavisworksGeometry(element);

        geometry.ElevationMode = ElevationMode;

        AddFragments(geometry);

        @base["displayValue"] = TranslateFragmentGeometry(geometry);
      }

      if (element.Children.Any())
      {
        List<ModelItem> children = element.Children.ToList();
        List<Base> convertedChildren = ConvertToSpeckle(children);
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


    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Where(CanConvertToSpeckle).Select(ConvertToSpeckle).ToList();
    }

    public List<Base> ConvertToSpeckle(List<ModelItem> modelItems)
    {
      var canConvert = modelItems.Where(CanConvertToSpeckle);
      var converted = canConvert.Select(ConvertToSpeckle);

      return modelItems.Where(CanConvertToSpeckle).Select(ConvertToSpeckle).ToList();
    }

    public bool CanConvertToSpeckle(object @object)
    {
      if (@object is ModelItem modelItem) return CanConvertToSpeckle(modelItem);

      // is expecting @object to be a pseudoId string
      if (!(@object is string pseudoId)) return false;

      ModelItem item = PointerToModelItem(pseudoId);

      return CanConvertToSpeckle(item);
    }

    private static bool CanConvertToSpeckle(ModelItem item)
    {
      // Only Geometry and Geometry with Mesh
      if (item.HasGeometry && (item.Geometry.PrimitiveTypes & PrimitiveTypes.Triangles) != 0) return true;

      if (!item.HasGeometry) return true;

      return false;
    }


    private static ModelItem PointerToModelItem(object @string)
    {
      int[] pathArray;

      try
      {
        pathArray = @string.ToString().Split('-').Select(x =>
        {
          if (int.TryParse(x, out var value))
          {
            return value;
          }

          throw (new Exception("malformed path pseudoId"));
        }).ToArray();
      }
      catch
      {
        return null;
      }

      InwOpState10 oState = ComApiBridge.State;
      InwOaPath protoPath = (InwOaPath)oState.ObjectFactory(nwEObjectType.eObjectType_nwOaPath);

      Array oneBasedArray = Array.CreateInstance(
        typeof(int),
        // ReSharper disable once RedundantExplicitArraySize
        new int[1] { pathArray.Length },
        // ReSharper disable once RedundantExplicitArraySize
        new int[1] { 1 });

      Array.Copy(pathArray, 0, oneBasedArray, 1, pathArray.Length);

      protoPath.ArrayData = oneBasedArray;

      ModelItem m = ComApiBridge.ToModelItem(protoPath);

      return m;
    }
  }
}