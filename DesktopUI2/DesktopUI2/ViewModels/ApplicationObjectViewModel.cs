using Avalonia;
using Avalonia.Layout;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
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

    public ApplicationObjectViewModel(ApplicationObject item, bool isReceiver)
    {
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
    private List<string> SurfaceDescriptorList = new List<string> { "surface", "plane", "solid", "brep", "box", "mesh" };
    private List<string> AnnotationDescriptorList = new List<string> { "hatch", "text", "dimension", "view", "family", "level" };
    private string GetIconFromDescriptor(string descriptor)
    {
      var lowercase = descriptor.ToLower();
      if (PointDescriptorList.Any(s => lowercase.Contains(s)))
        return "PlusCircleOutline";
      else if (CurveDescriptorList.Any(s => lowercase.Contains(s)))
        return "Audio";
      else if (SurfaceDescriptorList.Any(s => lowercase.Contains(s)))
        return "ImageFilterNone";
      else if (AnnotationDescriptorList.Any(s => lowercase.Contains(s)))
        return "Tag";
      else
        return "CubeOutline";
    }

    public void OpenApplicationObject()
    {
      if (Log.Count == 0)
        return;

      // TODO: expose log strings
      Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Report Item View" } });
    }
  }
}
