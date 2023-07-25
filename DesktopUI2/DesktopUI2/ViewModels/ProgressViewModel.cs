using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using Avalonia.Threading;
using ReactiveUI;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace DesktopUI2.ViewModels;

public class ProgressViewModel : ReactiveObject
{
  private bool _isPreviewProgressing;

  private bool _isProgressing;

  private double _max;

  private ConcurrentDictionary<string, int> _progressDict;

  private string _progressSummary;

  private string _progressTitle;

  private double _value;

  /// <summary>
  /// Cancellation token source for the current receive/send/preview.
  /// Avoid calling <see cref="System.Threading.CancellationTokenSource.Cancel()"/> unless the user has requested a Cancel.
  /// </summary>
  public CancellationTokenSource CancellationTokenSource { get; set; } = new();

  public CancellationToken CancellationToken => CancellationTokenSource.Token;

  public ProgressReport Report { get; set; } = new();

  public ConcurrentDictionary<string, int> ProgressDict
  {
    get => _progressDict;
    set
    {
      ProgressSummary = "";
      foreach (var kvp in value)
        ProgressSummary += $"{kvp.Key}: {kvp.Value} ";
      //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
      ProgressSummary += $"Total: {Max}";

      _progressDict = value;
      this.RaiseAndSetIfChanged(ref _progressDict, value);
    }
  }

  public string ProgressTitle
  {
    get => _progressTitle;
    set => this.RaiseAndSetIfChanged(ref _progressTitle, value);
  }

  public string ProgressSummary
  {
    get => _progressSummary;
    set => this.RaiseAndSetIfChanged(ref _progressSummary, value);
  }

  public double Value
  {
    get => _value;
    set
    {
      this.RaiseAndSetIfChanged(ref _value, value);
      this.RaisePropertyChanged(nameof(IsIndeterminate));
    }
  }

  public double Max
  {
    get => _max;
    set
    {
      this.RaiseAndSetIfChanged(ref _max, value);
      this.RaisePropertyChanged(nameof(IsIndeterminate));
    }
  }

  public bool IsIndeterminate => Value == 0 || Math.Abs(Max - Value) < Constants.Eps;

  public bool IsProgressing
  {
    get => _isProgressing;
    set
    {
      this.RaiseAndSetIfChanged(ref _isProgressing, value);
      if (!IsProgressing && Value != 0)
        ProgressSummary = "Done!";
    }
  }

  public bool IsPreviewProgressing
  {
    get => _isPreviewProgressing;
    set => this.RaiseAndSetIfChanged(ref _isPreviewProgressing, value);
  }

  public void Update(ConcurrentDictionary<string, int> pd)
  {
    Dispatcher.UIThread.Post(() =>
    {
      ProgressDict = pd;
      Value = pd.Values.Average();
    });
  }

  public void GetHelpCommand()
  {
    var report = "";
    if (Report.OperationErrorsCount > 0)
    {
      report += "OPERATION ERRORS\n\n";
      report += Report.OperationErrorsString;
    }

    var safeReport = HttpUtility.UrlEncode(report);
    Process.Start(
      new ProcessStartInfo(
        $"https://speckle.community/new-topic?title=I%20need%20help%20with...&body={safeReport}&category=help"
      )
      {
        UseShellExecute = true
      }
    );
  }

  public void CancelCommand()
  {
    CancellationTokenSource.Cancel();
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Cancel Progress" } });
  }
}
