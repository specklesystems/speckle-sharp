using System;
using Avalonia.Media;
using Material.Icons;
using ReactiveUI;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Helpers;

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
      Open.Url($"{serverUrl}/streams/{invite.streamId}");
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
