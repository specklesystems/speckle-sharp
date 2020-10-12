using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class AllStreamsViewModel : Screen, IHandle<StreamAddedEvent>, IHandle<StreamUpdatedEvent>, IHandle<
    StreamRemovedEvent>, IHandle<ApplicationEvent>
  {
    private readonly IViewManager _viewManager;
    private readonly IStreamViewModelFactory _streamViewModelFactory;
    private readonly IDialogFactory _dialogFactory;
    private readonly IEventAggregator _events;
    private readonly ConnectorBindings _bindings;

    public AllStreamsViewModel(
      IViewManager viewManager,
      IStreamViewModelFactory streamViewModelFactory,
      IDialogFactory dialogFactory,
      IEventAggregator events,
      ConnectorBindings bindings)
    {
      _repo = new StreamsRepository();
      _events = events;
      DisplayName = "Home";
      _viewManager = viewManager;
      _streamViewModelFactory = streamViewModelFactory;
      _dialogFactory = dialogFactory;
      _bindings = bindings;

      _streamList = new BindableCollection<StreamState>(_bindings.GetFileContext());
#if DEBUG
      if ( _streamList.Count == 0 )
        _streamList = _repo.LoadTestStreams();
#endif
      events.Subscribe(this);
    }

    private StreamsRepository _repo;
    private BindableCollection<StreamState> _streamList;
    private Stream _selectedStream;
    private Branch _selectedBranch;

    public BindableCollection<StreamState> StreamList
    {
      get => _streamList;
      set => SetAndNotify(ref _streamList, value);
    }

    public Stream SelectedStream
    {
      get => _selectedStream;
      set => SetAndNotify(ref _selectedStream, value);
    }

    public Branch SelectedBranch
    {
      get => _selectedBranch;
      set => SetAndNotify(ref _selectedBranch, value);
    }

    public void ShowStreamInfo(StreamState state)
    {
      var item = _streamViewModelFactory.CreateStreamViewModel();
      item.StreamState = state;
      item.Stream = state.stream;
      // get main branch for now
      // TODO allow user to select branch
      item.Branch = _repo.GetMainBranch(state.stream.branches.items);
      var parent = ( StreamsHomeViewModel ) Parent;
      parent.ActivateItem(item);
    }

    public async void ConvertAndSendObjects(StreamState state)
    {
      state.IsSending = true;
      if ( !state.placeholders.Any() )
      {
        _bindings.RaiseNotification("Nothing to send to Speckle.");
        state.IsSending = false;
        return;
      }

      var index = StreamList.IndexOf(state);

      try
      {
        StreamList[ index ] = await _bindings.SendStream(state).ConfigureAwait(false);
      }
      catch ( Exception e )
      {
        _bindings.RaiseNotification($"Error: {e.Message}");
      }

      state.IsSending = false;
      StreamList.Refresh();
    }

    public async void ReceiveStream(StreamState state)
    {
      //
    }

    public async void ShowStreamCreateDialog()
    {
      var viewmodel = _dialogFactory.CreateStreamCreateDialog();
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "AllStreamsDialogHost");
    }

    public async void ShowShareDialog(StreamState state)
    {
      var viewmodel = _dialogFactory.CreateShareStreamDialogViewModel();
      viewmodel.StreamState = state;
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "AllStreamsDialogHost");
    }

    public void CopyStreamId(string streamId)
    {
      Clipboard.SetText(streamId);
      _events.Publish(new ShowNotificationEvent() {Notification = "Stream ID copied to clipboard!"});
    }

    public void OpenHelpLink(string url)
    {
      Link.OpenInBrowser(url);
    }

    public void Handle(StreamAddedEvent message)
    {
      StreamList.Insert(0, message.NewStream);
    }

    public void Handle(StreamUpdatedEvent message)
    {
      StreamList.Refresh();
    }

    public void Handle(StreamRemovedEvent message)
    {
      var state = StreamList.First(s => s.stream.id == message.StreamId);
      StreamList.Remove(state);
    }

    public void Handle(ApplicationEvent message)
    {
      switch ( message.Type )
      {
        case ApplicationEvent.EventType.DocumentClosed:
        {
          StreamList.Clear();
          return;
        }
        case ApplicationEvent.EventType.DocumentOpened:
        {
          StreamList = new BindableCollection<StreamState>(_bindings.GetFileContext());
          return;
        }
        case ApplicationEvent.EventType.DocumentModified:
        {
          // warn that stream data may be expired
          return;
        }
        default:
          return;
      }
    }
  }
}
