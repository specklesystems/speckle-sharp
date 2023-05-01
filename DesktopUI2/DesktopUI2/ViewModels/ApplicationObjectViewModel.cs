using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Splat;

namespace DesktopUI2.ViewModels;

public class ApplicationObjectViewModel : ReactiveObject
{
  private ProgressReport _Progress;

  private ConnectorBindings Bindings;

  private bool previewOn;

  public ApplicationObjectViewModel(ApplicationObject item, bool isReceiver, ProgressReport progress)
  {
    //use dependency injection to get bindings
    Bindings = Locator.Current.GetService<ConnectorBindings>();
    _Progress = progress;
    var cleanLog = item.Log.Where(o => !string.IsNullOrEmpty(o)).ToList();

    Id = item.OriginalId;
    Name = item.Descriptor;
    Log = cleanLog.Count == 0 ? null : string.Join("\n", cleanLog);
    Status = item.Status.ToString();
    ApplicationIds = isReceiver ? item.CreatedIds : new List<string> { item.OriginalId };

    var logString = cleanLog.Count != 0 ? string.Join(" ", Log) : "";
    var createdIdsString = string.Join(" ", item.CreatedIds);
    SearchText = $"{Id} {Name} {Status} {logString} {createdIdsString}";

    if (ApplicationIds.Count == 0)
      PreviewEnabled = false;

    switch (item.Status)
    {
      case ApplicationObject.State.Created:
        Icon = "CheckBold";
        break;
      case ApplicationObject.State.Updated:
        Icon = "Refresh";
        break;
      case ApplicationObject.State.Skipped:
        Icon = "Refresh";
        Opacity = 0.6;
        break;
      case ApplicationObject.State.Removed:
        Icon = "MinusThick";
        Opacity = 0.6;
        break;
      case ApplicationObject.State.Failed:
        Icon = "Cancel";
        Color = "Red";
        Opacity = 0.6;
        break;
      default:
        Icon = "Help";
        Opacity = 0.6;
        break;
    }
  }

  public string Log { get; set; }
  public List<string> ApplicationIds { get; set; }
  public string Name { get; set; }
  public string Id { get; set; }
  public string Icon { get; set; }
  public string Color { get; set; } = "Gray";
  public string Status { get; set; }
  public double Opacity { get; set; } = 1;
  public bool PreviewEnabled { get; set; } = true;

  public string SearchText { get; set; }

  public bool PreviewOn
  {
    get => previewOn;
    set => this.RaiseAndSetIfChanged(ref previewOn, value);
  }

  public string GetSummary()
  {
    var summary = new string[2] { $"{Name} {Id} {Status}", Log };
    return string.Join("\n", summary);
  }

  public async void PreviewCommand()
  {
    PreviewOn = !PreviewOn;
    if (PreviewOn)
    {
      foreach (var applicationId in ApplicationIds)
        if (!_Progress.SelectedReportObjects.Contains(applicationId))
          _Progress.SelectedReportObjects.Add(applicationId);
      Bindings.SelectClientObjects(_Progress.SelectedReportObjects);
      Analytics.TrackEvent(
        Analytics.Events.DUIAction,
        new Dictionary<string, object> { { "name", "Viewed Report Item" } }
      );
    }
    else
    {
      foreach (var applicationId in ApplicationIds)
        _Progress.SelectedReportObjects.Remove(applicationId);
      Bindings.SelectClientObjects(ApplicationIds, true);
    }
  }
}
