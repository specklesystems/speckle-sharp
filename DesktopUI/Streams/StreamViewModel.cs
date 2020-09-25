using System;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamViewModel : Screen
  {
    private readonly IEventAggregator _events;
    private readonly ViewManager _viewManager;
    private readonly IDialogFactory _dialogFactory;
    public StreamState State { get; set; }
    public Stream Stream { get; set; }
    public Branch Branch { get; set; }

    public StreamViewModel(
      IEventAggregator events,
      ViewManager viewManager,
      IDialogFactory dialogFactory)
    {
      _events = events;
      _viewManager = viewManager;
      _dialogFactory = dialogFactory;
    }

    public async void ShowStreamUpdateDialog(StreamState state)
    {
      var viewmodel = _dialogFactory.CreateStreamUpdateDialog();
      viewmodel.StreamState = State;
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "StreamDialogHost");
    }

    public async void ShowShareDialog(StreamState state)
    {
      var viewmodel = _dialogFactory.CreateShareStreamDialogViewModel();
      viewmodel.StreamState = State;
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "StreamDialogHost");
    }

    // TODO figure out how to call this from parent instead of
    // rewriting the method here
    public void CopyStreamId(string streamId)
    {
      Clipboard.SetText(streamId);
      _events.Publish(new ShowNotificationEvent()
      {
        Notification = "Stream ID copied to clipboard!"
      });
    }

    public void OpenHelpLink(string url)
    {
      Link.OpenInBrowser(url);
    }
  }
}
