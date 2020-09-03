using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Speckle.DesktopUI.Accounts
{
  class AddAccountViewModel : BindableBase
  {
    public AddAccountViewModel()
    {
      _repo = new AccountsRepository();
      _messageQueue = new SnackbarMessageQueue();
      _submitServerLoading = false;
      SubmitUrlCommand = new RelayCommand<string>(OnSubmitUrl);
    }

    private AccountsRepository _repo;
    private bool _submitServerLoading;
    private SnackbarMessageQueue _messageQueue;

    public bool SubmitServerLoading
    {
      get => _submitServerLoading;
      set => SetProperty(ref _submitServerLoading, value);
    }
    public SnackbarMessageQueue MessageQueue
    {
      get => _messageQueue;
      set => SetProperty(ref _messageQueue, value);
    }
    public RelayCommand<string> SubmitUrlCommand { get; set; }

    private async void OnSubmitUrl(string url)
    {
      SubmitServerLoading = true;

      if (!Regex.IsMatch(url, "^(http|https)://.*"))
      {
        url = $"https://{url}";
      }
      try
      {
        var acct = await _repo.AuthenticateAccount(url);
        MessageQueue.Enqueue($"Added {acct.userInfo.name} from server {acct.serverInfo.name}");
        DialogHost.CloseDialogCommand.Execute(null, null);
        // TODO: closing the dialog here isn't working for some reason - check what's diff from streams
        // TODO: reload accounts list after adding a new account
      }
      catch (Exception e)
      {
        await Task.Factory.StartNew(() => MessageQueue.Enqueue($"Error: {e.Message}"));
      }
      SubmitServerLoading = false;
    }
  }
}
