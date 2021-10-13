using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace DesktopUI2.ViewModels
{
  public class ProgressViewModel : ReactiveObject
  {
    public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

    /// <summary>
    /// Keeps track of the conversion process
    /// </summary>
    public List<string> ConversionLog { get; set; } = new List<string>();

    public string ConversionLogString
    {
      get
      {
        return string.Join("\n", ConversionLog);
      }
    }
    /// <summary>
    /// Keeps track of errors in the conversions.
    /// </summary>
    public List<Exception> ConversionErrors { get; set; } = new List<Exception>();
    public string ConversionErrorsString
    {
      get
      {
        return string.Join("\n", ConversionErrors.Select(x => x.Message));
      }
    }

    /// <summary>
    /// Keeps track of errors in the operations of send/receive.
    /// </summary>
    public List<Exception> OperationErrors { get; set; } = new List<Exception>();
    public string OperationErrorsString
    {
      get
      {
        return string.Join("\n", OperationErrors.Select(x => x.Message));
      }
    }


    private ConcurrentDictionary<string, int> _progressDict;
    public ConcurrentDictionary<string, int> ProgressDict
    {
      get => _progressDict;
      set
      {
        ProgressSummary = "";
        foreach (var kvp in value)
        {
          ProgressSummary += $"{kvp.Key}: {kvp.Value} ";
        }
        //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
        ProgressSummary += $"Total: {Max}";
        _progressDict = value;
        this.RaiseAndSetIfChanged(ref _progressDict, value);
      }
    }

    public string ProgressSummary { get; set; } = "";

    private int _value = 0;
    public int Value
    {
      get => _value;
      set
      {
        this.RaiseAndSetIfChanged(ref _value, value);
        this.RaisePropertyChanged(nameof(IsProgressing));
      }
    }

    private int _max = 0;
    public int Max
    {
      get => _max;
      set
      {
        this.RaiseAndSetIfChanged(ref _max, value);
        this.RaisePropertyChanged(nameof(IsIndeterminate));
      }
    }

    public bool IsIndeterminate { get => Max == 0; }

    public bool IsProgressing { get => Value != 0; }



    public void Update(ConcurrentDictionary<string, int> pd)
    {
      //Avalonia.Threading.Dispatcher.UIThread.Post(() =>
      //{
      ProgressDict = pd;
      Value = pd.Values.Last();
      //}, Avalonia.Threading.DispatcherPriority.MaxValue);
    }

    public void GetHelpCommand()
    {
      Process.Start(new ProcessStartInfo("https://speckle.community/") { UseShellExecute = true });
    }
  }
}
