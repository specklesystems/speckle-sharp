using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Models.Scheduler;
using DesktopUI2.Views;
using ReactiveUI;
using Speckle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DesktopUI2.ViewModels
{
  public class SchedulerViewModel : ReactiveObject
  {

    public List<Trigger> Triggers { get; set; } = new List<Trigger> {
      new Trigger("On File Save", "save", "ContentSave"),
      new Trigger("On Sync To Central", "sync", "CloudSync"),
      new Trigger("On File Export", "export", "DatabaseExport")
    };

    List<StreamState> SavedStreams { get; set; } = new List<StreamState>();

    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

    private bool _enabled;
    public bool Enabled
    {
      get => _enabled;
      set
      {
        this.RaiseAndSetIfChanged(ref _enabled, value);
      }
    }

    private ObservableCollection<StreamState> _savedSenders = new ObservableCollection<StreamState>();
    public ObservableCollection<StreamState> SavedSenders
    {
      get => _savedSenders;
      set
      {
        this.RaiseAndSetIfChanged(ref _savedSenders, value);
        this.RaisePropertyChanged("HasSavedStreams");
      }
    }

    public bool HasSavedSenders => SavedSenders != null && SavedSenders.Any();

    private StreamState _selectedStream;
    public StreamState SelectedStream
    {
      get => _selectedStream;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedStream, value);
      }
    }

    private Trigger _selectedTrigger;
    public Trigger SelectedTrigger
    {
      get => _selectedTrigger;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedTrigger, value);
      }
    }

    [DependsOn(nameof(SelectedTrigger))]
    [DependsOn(nameof(SelectedStream))]
    private bool CanSaveCommand(object parameter)
    {
      if (SelectedTrigger == null || SelectedTrigger == null)
        return false;

      return true;
    }

    public string Title => "for " + Bindings.GetHostAppNameVersion();
    public string TitleFull => "Scheduler for " + Bindings.GetHostAppNameVersion();
    public SchedulerViewModel(ConnectorBindings _bindings)
    {
      Bindings = _bindings;
      Setup.Init(Bindings.GetHostAppNameVersion(), Bindings.GetHostAppName());
      LoadSavedStreams();

    }
    public SchedulerViewModel()
    {
      LoadSavedStreams();
    }

    public void LoadSavedStreams()
    {
      SavedStreams = Bindings.GetStreamsInFile();
      SavedSenders = new ObservableCollection<StreamState>(SavedStreams.Where(x => !x.IsReceiver));
      var scheduled = SavedSenders.FirstOrDefault(x => x.SchedulerEnabled);
      if (scheduled != null)
      {
        SelectedStream = scheduled;
        Enabled = true;
        SelectedTrigger = Triggers.FirstOrDefault(x => x.Slug == scheduled.SchedulerTrigger);
      }
    }


    private void SaveCommand()
    {
      try
      {
        //updating the list of streams, in case stuff has changes in the mian DUI
        SavedStreams = Bindings.GetStreamsInFile();
        foreach (var stream in SavedStreams)
        {
          if (stream.Id == SelectedStream.Id && Enabled)
          {
            stream.SchedulerEnabled = true;
            stream.SchedulerTrigger = SelectedTrigger.Slug;
          }
          else
            stream.SchedulerEnabled = false;
        }

        Bindings.WriteStreamsToFile(SavedStreams.ToList());

        Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Scheduler Set" } });


        if (HomeViewModel.Instance != null)
          HomeViewModel.Instance.UpdateSavedStreams(SavedStreams.ToList());

        Scheduler.Instance.Close();
      }
      catch (Exception ex)
      {

      }
    }




  }
}
