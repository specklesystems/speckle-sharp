using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DesktopUI2.ViewModels
{
  public class ProgressViewModel : ReactiveObject
  {
    public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

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

    public bool IsIndeterminate { get => Max != 0; }

    public bool IsProgressing { get => Value != 0; }



    public void Update(ConcurrentDictionary<string, int> pd)
    {
      //Avalonia.Threading.Dispatcher.UIThread.Post(() =>
      //{
      ProgressDict = pd;
      Value = pd.Values.Last();
      //}, Avalonia.Threading.DispatcherPriority.MaxValue);
    }
  }
}
