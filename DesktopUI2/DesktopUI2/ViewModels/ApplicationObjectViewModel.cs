using Avalonia;
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
    public string Icon { get; set; }
    public string TextColor { get; set; }
    public string IconColor { get; set; }
    public string Status { get; set; }
    public bool Visible { get; set; } = true;
    public bool HasAlert { get; set; } = false;
    public bool PreviewEnabled { get; set; } = true;

    private ConnectorBindings Bindings;

    private bool previewOn = false;
    public bool PreviewOn
    {
      get => previewOn;
      set => this.RaiseAndSetIfChanged(ref previewOn, value);
    }

    public ApplicationObjectViewModel(ApplicationObject item, bool isReceiver)
    {
      //use dependency injection to get bindings
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      Id = item.OriginalId;
      Name = item.Descriptor;
      Log = item.Log;
      HasAlert = item.Convertible;
      Status = item.Status.ToString();
      ApplicationIds = isReceiver ? item.CreatedIds : new List<string>() { item.OriginalId };

      if (ApplicationIds.Count == 0) PreviewEnabled = false;

      switch (item.Status)
      {
        case ApplicationObject.State.Created:
          TextColor = "Black";
          Icon = "PlusThick";
          IconColor = "Green";
          break;
        case ApplicationObject.State.Updated:
          TextColor = "Black";
          Icon = "Refresh";
          IconColor = "Green";
          break;
        case ApplicationObject.State.Skipped:
          TextColor = "Grey";
          Icon = "Cancel";
          IconColor = "Grey";
          break;
        case ApplicationObject.State.Removed:
          TextColor = "Black";
          Icon = "MinusThick";
          IconColor = "Red";
          break;
        case ApplicationObject.State.Failed:
          TextColor = "Red";
          Icon = "Cancel";
          IconColor = "Red";
          break;
        default:
          TextColor = "Black";
          Icon = "Help";
          IconColor = "Black";
          break;
      }
    }

    public async void PreviewCommand()
    {
      PreviewOn = !PreviewOn;
      if (PreviewOn)
      {
        Bindings.SelectClientObjects(ApplicationIds);
        Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Viewed Report Item" } });
      }
      else
      {
        Bindings.SelectClientObjects(ApplicationIds, true);
      }
    }
  }
}
