using System;
using System.Linq;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamViewModel : Screen, IHandle<ApplicationEvent>
  {
    private readonly IEventAggregator _events;
    private readonly ViewManager _viewManager;
    private readonly IDialogFactory _dialogFactory;
    private readonly ConnectorBindings _bindings;

    public StreamViewModel(
      IEventAggregator events,
      ViewManager viewManager,
      IDialogFactory dialogFactory,
      ConnectorBindings bindings)
    {
      _events = events;
      _viewManager = viewManager;
      _dialogFactory = dialogFactory;
      _bindings = bindings;
    }

    private StreamState _streamState;

    public StreamState StreamState
    {
      get => _streamState;
      set
      {
        SetAndNotify(ref _streamState, value);
        Stream = StreamState.stream;
        Branch = StreamState.stream.branches.items[ 0 ];
      }
    }

    private Stream _stream;

    public Stream Stream
    {
      get => _stream;
      set => SetAndNotify(ref _stream, value);
    }

    private Branch _branch;

    public Branch Branch
    {
      get => _branch;
      set => SetAndNotify(ref _branch, value);
    }

    public async void ConvertAndSendObjects()
    {
      StreamState.IsSending = true;
      if ( !StreamState.placeholders.Any() )
      {
        _bindings.RaiseNotification("Nothing to send to Speckle.");
        StreamState.IsSending = false;
        return;
      }

      StreamState.IsSending = true;
      try
      {
        StreamState = await _bindings.SendStream(StreamState);
      }
      catch ( Exception e )
      {
        _bindings.RaiseNotification($"Error: {e.Message}");
        StreamState.IsSending = false;
        return;
      }

      NotifyOfPropertyChange(nameof(StreamState));
      _events.Publish(new StreamUpdatedEvent() {StreamId = Stream.id});
      StreamState.IsSending = false;
    }

    public async void ConvertAndReceiveObjects()
    {
      StreamState.IsReceiving = true;
      StreamState.stream = await StreamState.client.StreamGet(Stream.id);
      // var newCommitId = newStream.branches.items[ 0 ].commits.items[ 0 ].id;
      // var oldCommitId = Branch.commits.items[ 0 ].id;

      // if ( oldCommitId == newCommitId )
      // {
      //   _bindings.RaiseNotification($"Nothing to receive - stream is up to date");
      //   return;
      // }

      try
      {
        StreamState = await _bindings.ReceiveStream(StreamState);
        StreamState.ServerUpdates = false;
      }
      catch ( Exception e )
      {
        _bindings.RaiseNotification($"Error: {e.Message}");
      }

      StreamState.IsReceiving = false;
    }

    public async void ShowStreamUpdateDialog(StreamState state)
    {
      var viewmodel = _dialogFactory.CreateStreamUpdateDialog();
      viewmodel.StreamState = StreamState;
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "StreamDialogHost");
    }

    public async void ShowShareDialog(StreamState state)
    {
      var viewmodel = _dialogFactory.CreateShareStreamDialogViewModel();
      viewmodel.StreamState = StreamState;
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "StreamDialogHost");
    }

    public void RemoveStream()
    {
      _bindings.RemoveStream(Stream.id);
      _events.Publish(new StreamRemovedEvent() {StreamId = Stream.id});
      RequestClose();
    }

    public async void DeleteStream()
    {
      try
      {
        var deleted = await StreamState.client.StreamDelete(Stream.id);
        if ( !deleted )
        {
          // should we still remove the stream from client if they can't delete?
          _events.Publish(new ShowNotificationEvent() {Notification = "Could not delete stream from server"});
          return;
        }

        _bindings.RemoveStream(Stream.id);
        _events.Publish(new StreamRemovedEvent() {StreamId = Stream.id});
        RequestClose();
      }
      catch ( Exception e )
      {
        _events.Publish(new ShowNotificationEvent() {Notification = $"Error: {e}"});
      }
    }

    // TODO figure out how to call this from parent instead of
    // rewriting the method here
    public void CopyStreamId(string streamId)
    {
      Clipboard.SetText(streamId);
      _events.Publish(new ShowNotificationEvent() {Notification = "Stream ID copied to clipboard!"});
    }

    public void OpenHelpLink(string url)
    {
      Link.OpenInBrowser(url);
    }

    public void Handle(ApplicationEvent message)
    {
      switch ( message.Type )
      {
        case ApplicationEvent.EventType.DocumentClosed:
        {
          RequestClose();
          return;
        }
        default:
          return;
      }
    }
  }
}
