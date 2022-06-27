using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace DesktopUI2.ViewModels
{
  public class AccountViewModel : ReactiveObject
  {
    public string Name { get; set; }
    public string Role { get; set; } = "contributor";
    public string Id { get; }

    public Account Account { get; private set; }

    public string SimpleName
    {
      get
      {
        if (HomeViewModel.Instance.Accounts.Any(x => x.Account.userInfo.id == Id))
          return "You";
        return Name;
      }
    }

    public string FullAccountName
    {
      get { return $"{Name} ({Account.userInfo.email})"; }
    }

    public string FullServerName
    {
      get { return $"{Account.serverInfo.name} ({Account.serverInfo.url})"; }
    }


    public AccountViewModel()
    {

    }

    public AccountViewModel(Account account)
    {
      Name = account.userInfo.name;
      Id = account.userInfo.id;
      AvatarUrl = account.userInfo.avatar;
      Account = account;

    }

    public AccountViewModel(User user)
    {
      Name = user.name;
      Id = user.id;
      AvatarUrl = user.avatar;

    }

    public AccountViewModel(Collaborator user)
    {
      Name = user.name;
      Id = user.id;
      AvatarUrl = user.avatar;
      Role = user.role;

    }



    public void DownloadImage(string url)
    {
      try
      {
        if (string.IsNullOrEmpty(url))
          return;

        using (WebClient client = new WebClient())
        {
          //client.Headers.Set("Authorization", "Bearer " + Client.ApiToken);
          client.DownloadDataAsync(new Uri(url));
          client.DownloadDataCompleted += DownloadComplete;
        }
      }
      catch (Exception ex)
      {

      }
    }



    public string _avatarUrl = "";
    public string AvatarUrl
    {
      get => _avatarUrl;
      set
      {

        if (value == null && Id != null)
        {
          this.RaiseAndSetIfChanged(ref _avatarUrl, $"https://robohash.org/{Id}.png?size=28x28");
        }
        else
        {
          this.RaiseAndSetIfChanged(ref _avatarUrl, value);
        }
        DownloadImage(AvatarUrl);
      }
    }

    private Avalonia.Media.Imaging.Bitmap _avatarImage = null;
    public Avalonia.Media.Imaging.Bitmap AvatarImage
    {
      get => _avatarImage;
      set => this.RaiseAndSetIfChanged(ref _avatarImage, value);
    }

    private void DownloadComplete(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        byte[] bytes = e.Result;

        System.IO.Stream stream = new MemoryStream(bytes);

        var image = new Avalonia.Media.Imaging.Bitmap(stream);
        _avatarImage = image;
        this.RaisePropertyChanged("AvatarImage");
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(ex);
        AvatarUrl = null; // Could not download...
      }
    }

  }
}
