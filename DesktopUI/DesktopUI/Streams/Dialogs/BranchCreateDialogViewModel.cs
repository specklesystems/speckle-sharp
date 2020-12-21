using System;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams.Dialogs
{
  public class BranchCreateDialogViewModel : Conductor<IScreen>
  {
    public BranchCreateDialogViewModel()
    {
      DisplayName = "Create Branch";
    }

    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(4));

    public ISnackbarMessageQueue Notifications
    {
      get => _notifications;
      set => SetAndNotify(ref _notifications, value);
    }

    private StreamState _streamState;

    public StreamState StreamState
    {
      get => _streamState;
      set => SetAndNotify(ref _streamState, value);
    }

    private string _branchName = "";

    public string BranchName
    {
      get => _branchName;
      set
      {
        SetAndNotify(ref _branchName, value);
        NotifyOfPropertyChange(nameof(CanCreateBranch));
      }
    }

    private string _branchDescription = "";

    public string BranchDescription
    {
      get => _branchDescription;
      set => SetAndNotify(ref _branchDescription, value);
    }

    public bool CanCreateBranch => BranchName.Length >= 3;

    public async void CreateBranch()
    {
      try
      {
        var branchId = await StreamState.Client.BranchCreate(new BranchCreateInput()
        {
          streamId = StreamState.Stream.id, name = BranchName, description = BranchDescription
        });
      }
      catch (Exception e)
      {
        Notifications.Enqueue($"Error: {e.Message}");
        return;
      }

      var branch = await StreamState.Client.BranchGet(StreamState.Stream.id, BranchName);

      DialogHost.CloseDialogCommand.Execute(branch, null);
    }

    public void CloseDialog() => DialogHost.CloseDialogCommand.Execute(null, null);
  }
}
