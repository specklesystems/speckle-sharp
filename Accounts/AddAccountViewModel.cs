using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Speckle.DesktopUI.Accounts
{
  class AddAccountViewModel : BindableBase
  {
    public AddAccountViewModel()
    {
      _repo = new AccountsRepository();
      _messageQueue = new SnackbarMessageQueue();
      SubmitServerLoading = false;
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
      SubmitServerLoading = true; // this isn't working quite right atm

      if (!Regex.IsMatch(url, "^(http|https)://.*"))
      {
        url = $"https://{url}";
      }
      try
      {
        var acct = await _repo.AuthenticateAccount(url); // redirecting to 24707 and failing
      }
      catch (Exception e)
      {
        await Task.Factory.StartNew(() => MessageQueue.Enqueue($"Error: {e.Message}"));
      }

      SubmitServerLoading = false;

      //TODO: close window
    }
  }
}
