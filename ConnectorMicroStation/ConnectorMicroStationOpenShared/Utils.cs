﻿using Bentley.DgnPlatformNET;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorMicroStationOpen
{
  public static class Utils
  {
#if MICROSTATION
    public static string VersionedAppName = HostApplications.MicroStation.GetVersion(HostAppVersion.v);
    public static string AppName = HostApplications.MicroStation.Name;
    public static string Slug = HostApplications.MicroStation.Slug;
#elif OPENROADS
    public static string VersionedAppName = HostApplications.OpenRoads.GetVersion(HostAppVersion.v);
    public static string AppName = HostApplications.OpenRoads.Name;
    public static string Slug = HostApplications.OpenRoads.Slug;
#elif OPENRAIL
    public static string VersionedAppName = HostApplications.OpenRail.GetVersion(HostAppVersion.v);
    public static string AppName = HostApplications.OpenRail.Name;
    public static string Slug = HostApplications.OpenRail.Slug;
#elif OPENBUILDINGS
    public static string VersionedAppName = HostApplications.OpenBuildings.GetVersion(HostAppVersion.v);
    public static string AppName = HostApplications.OpenBuildings.Name;
    public static string Slug = HostApplications.OpenBuildings.Slug;
#endif

    /// <summary>
    /// Gets the ids of all visible model objects that can be converted to Speckle
    /// </summary>
    /// <param name="model"></param>
    /// <param name="converter"></param>
    /// <returns></returns>
    public static List<string> ConvertibleObjects(this DgnModel model, ISpeckleConverter converter)
    {
      var objs = new List<string>();

      if (model == null)
      {
        return new List<string>();
      }

      var graphicElements = model.GetGraphicElements();
      var elementEnumerator = (ModelElementsEnumerator)graphicElements.GetEnumerator();
      var elements = graphicElements.Where(el => !el.IsInvisible).Select(el => el).ToList();

      foreach (var element in elements)
      {
        if (converter.CanConvertToSpeckle(element) && !element.IsInvisible)
          objs.Add(element.ElementId.ToString());
      }

      objs = graphicElements.Where(el => !el.IsInvisible).Select(el => el.ElementId.ToString()).ToList();
      return objs;
    }
  }
}
