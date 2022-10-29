using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using Speckle.Core.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DesktopUI2.ViewModels
{
  public class NotificationViewModel : ReactiveObject
  {
    public string Message { get; set; }

    public MaterialIconKind Icon { get; set; }
    public IBrush IconColor { get; set; }
    private AccountViewModel _user;
    public AccountViewModel User
    {
      get => _user;
      set => this.RaiseAndSetIfChanged(ref _user, value);
    }
    public Action Launch { get; set; }
    public NotificationViewModel()
    {

    }
    public NotificationViewModel(PendingStreamCollaborator invite, string serverUrl)
    {
      User = new AccountViewModel(invite.invitedBy);
      Message = $"{invite.invitedBy.name} is inviting you to collaborate on '{invite.streamName}'!";
      Launch = () =>
      {
        Process.Start(new ProcessStartInfo($"{serverUrl}/streams/{invite.streamId}") { UseShellExecute = true });
      };
    }

    public void LaunchCommand()
    {
      Launch.Invoke();
    }
  }
}
