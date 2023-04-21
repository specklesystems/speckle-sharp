using System;
using System.Diagnostics;
using Avalonia.Media;
using Material.Icons;
using ReactiveUI;
using Speckle.Core.Api;

namespace DesktopUI2.ViewModels;

public class NotificationViewModel : ReactiveObject
{
  private AccountViewModel _user;

  public NotificationViewModel() { }

  public NotificationViewModel(PendingStreamCollaborator invite, string serverUrl)
  {
    User = new AccountViewModel(invite.invitedBy);
    Message = $"{invite.invitedBy.name} is inviting you to collaborate on '{invite.streamName}'!";
    Launch = () =>
    {
      Process.Start(new ProcessStartInfo($"{serverUrl}/streams/{invite.streamId}") { UseShellExecute = true });
    };
  }

  public string Message { get; set; }

  public MaterialIconKind Icon { get; set; }
  public IBrush IconColor { get; set; }

  public AccountViewModel User
  {
    get => _user;
    set => this.RaiseAndSetIfChanged(ref _user, value);
  }

  public Action Launch { get; set; }

  public void LaunchCommand()
  {
    Launch.Invoke();
  }
}
