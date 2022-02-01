using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    //public Base GroupToSpeckle(Group revitGroup)
    //{
    //  var hostedElementIds = revitGroup.GetMemberIds();
    //  if (!hostedElementIds.Any())
    //    return null;

    //  var @base = new Base();
    //  @base["name"] = revitGroup.Name;
    //  @base["type"] = "group";
    //  @base["level"] = ConvertAndCacheLevel(revitGroup, BuiltInParameter.GROUP_LEVEL); ;

    //  var convertedHostedElements = new List<Base>();

    //  foreach (var elemId in hostedElementIds)
    //  {
    //    var element = Doc.GetElement(elemId);
    //    // if it's already part of the selection, skip it
    //    // we can't prevent the other element (with same id) to be converted, like we do for hosted elements
    //    if (ContextObjects.Any(x => x.applicationId == element.UniqueId))
    //      continue;

    //    if (CanConvertToSpeckle(element))
    //    {
    //      var obj = ConvertToSpeckle(element);

    //      if (obj != null)
    //        convertedHostedElements.Add(obj);
    //    }
    //  }

    //  if (!convertedHostedElements.Any())
    //    return null;


    //  @base["elements"] = convertedHostedElements;
    //  return @base;
    //}
  }
}