﻿using Autodesk.Revit.DB;
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
    public Base GroupToSpeckle(Group revitGroup)
    {
      var elIdsToConvert = GetDependentElementIds(revitGroup);
      if (!elIdsToConvert.Any())
        return null;

      var @base = new Base();
      @base["name"] = revitGroup.Name;
      @base["type"] = "group";
      @base["level"] = ConvertAndCacheLevel(revitGroup, BuiltInParameter.GROUP_LEVEL);

      var convertedHostedElements = new List<Base>();

      // loop backward through the list so you can remove elements as you go through it
      for (int i = elIdsToConvert.Count - 1; i >= 0; i--)
      {
        var element = Doc.GetElement(elIdsToConvert[i]);
        // if it's already part of the selection, remove this element from the list of element
        // we can't prevent the other element (with same id) to be converted, like we do for hosted elements
        if (ContextObjects.Any(x => x.applicationId == element.UniqueId))
          elIdsToConvert.RemoveAt(i);
        // otherwise, add the elements to the ContextObjects before converting them because a group 
        // may contain a wall that has a window, so we still want the window to search through the contextObjects
        // and recognize that it's host, the wall, is listed in there and not to convert itself
        else
          ContextObjects.Add(new ApplicationObject(null, null) { applicationId = element.UniqueId });
      }

      GetHostedElementsFromIds(@base, revitGroup, elIdsToConvert, out List<string> notes);
      return @base;
    }
  }
}