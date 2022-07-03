using Avalonia;
using Avalonia.Layout;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class ApplicationObjectViewModel : ReactiveObject
  {
    public List<string> Log { get; set; }
    public List<string> Ids { get; set; }
    public string Name { get; set; }
    public bool IsSelected { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    private ApplicationObject _applicationObject { get; set; }

    public ApplicationObjectViewModel(ApplicationObject item, bool isReceiver)
    {
      _applicationObject = item;
      Name = item.Slug;
      Log = item.Log;
      Ids = isReceiver ? item.CreatedIds : new List<string>() { item.OriginalId };

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
    }
  }
}
