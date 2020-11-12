using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamViewModel : Screen, IHandle<ApplicationEvent>, IHandle<StreamUpdatedEvent>
  {
    private readonly IEventAggregator _events;
    private readonly ViewManager _viewManager;
    private readonly IDialogFactory _dialogFactory;
    private readonly ConnectorBindings _bindings;
    private StreamsRepository _repo;

    public StreamViewModel(
      IEventAggregator events,
      ViewManager viewManager,
      IDialogFactory dialogFactory,
      StreamsRepository streamsRepo,
      ConnectorBindings bindings)
    {
      _events = events;
      _viewManager = viewManager;
      _dialogFactory = dialogFactory;
      _repo = streamsRepo;
      _bindings = bindings;

      DisplayName = "Stream XXX";

      _events.Subscribe(this);
    }

    private CancellationTokenSource _cancellationToken = new CancellationTokenSource();

    private StreamState _streamState;
    public StreamState StreamState
    {
      get => _streamState;
      set
      {
        SetAndNotify(ref _streamState, value);
        Branch = StreamState.Stream.branches.items[ 0 ];
        NotifyOfPropertyChange(nameof(LatestCommit));
      }
    }

    private Branch _branch;
    public Branch Branch
    {
      get => _branch;
      set => SetAndNotify(ref _branch, value);
    }

    public Commit LatestCommit
    {
      get => StreamState.LatestCommit(Branch.name);
    }

    public async void Send()
    {
      StreamState.IsSending = true;
      Tracker.TrackPageview(Tracker.SEND);
      _cancellationToken = new CancellationTokenSource();
      StreamState.CancellationToken = _cancellationToken.Token;

      var res = await Task.Run(() => _repo.ConvertAndSend(StreamState));
      if ( res != null )
      {
        StreamState = res;
        _events.Publish(new StreamUpdatedEvent(StreamState.Stream));
      }

      StreamState.IsSending = false;
      await StreamState.Progress.ResetProgress();
    }

    public async void Receive()
    {
      StreamState.IsReceiving = true;
      Tracker.TrackPageview(Tracker.RECEIVE);
      _cancellationToken = new CancellationTokenSource();
      StreamState.CancellationToken = _cancellationToken.Token;

      var res = await Task.Run(() => _repo.ConvertAndReceive(StreamState));
      if ( res != null ) StreamState = res;

      StreamState.IsReceiving = false;
      await StreamState.Progress.ResetProgress();
    }

    public void CancelToken()
    {
      _cancellationToken.Cancel();
    }

    public async void ShowStreamUpdateDialog(int slide = 0)
    {
      Tracker.TrackPageview("stream", "dialog-update");
      var viewmodel = _dialogFactory.CreateStreamUpdateDialog();
      viewmodel.StreamState = StreamState;
      viewmodel.SelectedSlide = slide;
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "RootDialogHost");
    }

    public async void ShowShareDialog(StreamState state)
    {
      Tracker.TrackPageview("stream", "dialog-share");
      var viewmodel = _dialogFactory.CreateShareStreamDialogViewModel();
      viewmodel.StreamState = StreamState;
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "RootDialogHost");
    }

    public void RemoveStream()
    {
      Tracker.TrackPageview("stream", "remove");
      _bindings.RemoveStream(StreamState.Stream.id);
      _events.Publish(new StreamRemovedEvent() {StreamId = StreamState.Stream.id});
      RequestClose();
    }

    public async void DeleteStream()
    {
      Tracker.TrackPageview("stream", "delete");
      var deleted = await _repo.DeleteStream(StreamState);
      if ( !deleted )
      {
        DialogHost.CloseDialogCommand.Execute(null, null);
        return;
      }

      _events.Publish(new StreamRemovedEvent() {StreamId = StreamState.Stream.id});
      DialogHost.CloseDialogCommand.Execute(null, null);
      RequestClose();
    }

    // TODO figure out how to call this from parent instead of
    // rewriting the method here
    public void CopyStreamId(string streamId)
    {
      Clipboard.SetText(streamId);
      _events.Publish(new ShowNotificationEvent() {Notification = "Stream ID copied to clipboard!"});
    }

    public void OpenStreamInWeb(StreamState state)
    {
      Tracker.TrackPageview("stream", "web");
      Link.OpenInBrowser($"{state.ServerUrl}/streams/{state.Stream.id}");
    }

    public void Handle(ApplicationEvent message)
    {
      switch ( message.Type )
      {
        case ApplicationEvent.EventType.DocumentOpened:
        case ApplicationEvent.EventType.DocumentClosed:
        {
          RequestClose();
          break;
        }
        default:
          return;
      }
    }

    public void Handle(StreamUpdatedEvent message)
    {
      if ( message.StreamId != StreamState.Stream.id ) return;
      StreamState.Stream = message.Stream;
    }
  }
}
