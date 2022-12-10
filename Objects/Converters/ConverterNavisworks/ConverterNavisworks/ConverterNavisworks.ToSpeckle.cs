using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;
using Autodesk.Navisworks.Api.Interop.ComApi;
using static Speckle.Core.Models.ApplicationObject;

namespace Objects.Converter.Navisworks
{
  public partial class ConverterNavisworks
  {
    public Base ConvertToSpeckle(object @object)
    {
      // is expecting @object to be a pseudoId string
      if (!(@object is string pseudoId)) return null;

      ModelItem item = PointerToModelItem(pseudoId);

      var @base = new Base
      {
        ["_convertedIds"] = item.DescendantsAndSelf.Select(x => ((Array)ComApiBridge.ToInwOaPath(item).ArrayData)
          .ToArray<int>().Aggregate("",
            (current, value) => current + (value.ToString().PadLeft(4, '0') + "-")).TrimEnd('-')).ToList()
      };

      return @base;
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(ConvertToSpeckle).ToList();
    }

    public bool CanConvertToSpeckle(object @object)
    {
      // is expecting @object to be a pseudoId string
      if (!(@object is string pseudoId)) return false;

      ModelItem item = PointerToModelItem(pseudoId);

      switch (item.ClassDisplayName)
      {
        case "Solid":
          return true;
        case null:
          return false;
        default:
          return false;
      }
    }
  }
}