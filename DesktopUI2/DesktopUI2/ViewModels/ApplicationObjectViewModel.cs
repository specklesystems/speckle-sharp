﻿using Avalonia;
using Avalonia.Layout;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Splat;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class ApplicationObjectViewModel : ReactiveObject
  {
    public List<string> Log { get; set; }
    public List<string> ApplicationIds { get; set; }
    public string Name { get; set; }
    public string Id { get; set; }
    public bool IsSelected { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }

    private ConnectorBindings Bindings;

    public ApplicationObjectViewModel(ApplicationObject item, bool isReceiver)
    {
      //use dependency injection to get bindings
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      Id = item.OriginalId;
      Name = item.Descriptor;
      Log = item.Log;
      ApplicationIds = isReceiver ? item.CreatedIds : new List<string>() { item.OriginalId };

      switch (item.Status)
      {
        case ApplicationObject.ConversionStatus.Created:
        case ApplicationObject.ConversionStatus.Updated:
          Color = "White";
          break;
        case ApplicationObject.ConversionStatus.Skipped:
          Color = "Grey";
          break;
        case ApplicationObject.ConversionStatus.Failed:
          Color = "Orange";
          break;
      }

      Icon = GetIconFromDescriptor(item.Descriptor);
    }

    private List<string> PointDescriptorList = new List<string>{"point"};
    private List<string> CurveDescriptorList = new List<string> { "curve", "line", "vector", "arc", "circle", "ellipse", "spiral", "spline" };
    private List<string> SurfaceDescriptorList = new List<string> { "plane" };
    private List<string> AnnotationDescriptorList = new List<string> { "hatch", "text", "dimension", "view", "family", "level", "annotation" };
    private string GetIconFromDescriptor(string descriptor)
    {
      var lowercase = descriptor.ToLower();
      if (PointDescriptorList.Any(s => lowercase.Contains(s)))
        return "SquareSmall";
      else if (CurveDescriptorList.Any(s => lowercase.Contains(s)))
        return "MathIntegral";
      else if (SurfaceDescriptorList.Any(s => lowercase.Contains(s)))
        return "SquareRounded";
      else if (AnnotationDescriptorList.Any(s => lowercase.Contains(s)))
        return "TagOutline";
      else
        return "CubeOutline";
    }

    public void SelectApplicationObject()
    {
      Bindings.SelectClientObjects(ApplicationIds);

      Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Viewed Report Item" } });
    }
  }
}
