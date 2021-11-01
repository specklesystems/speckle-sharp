using Speckle.GSA.API;
using System;
using System.ComponentModel;

namespace ConnectorGSA.Models
{
  public class SpeckleAccountForUI : INotifyPropertyChanged
  {
    //INotifyPropertyChanged aspects
    public event PropertyChangedEventHandler PropertyChanged;

    protected void NotifyPropertyChanged(String info) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
    /*
    public SpeckleAccountForUI(string serverUrl, string emailAddress, string token, string clientName = null)
    {
      this.ServerUrl = serverUrl;
      this.EmailAddress = emailAddress;
      this.Token = token;
      if (!string.IsNullOrEmpty(clientName))
      {
        this.ClientName = clientName;
      }
    }

    public void Update(string serverUrl, string emailAddress, string token)
    {
      this.ServerUrl = serverUrl;
      this.EmailAddress = emailAddress;
      this.Token = token;
      NotifyPropertyChanged(null);
    }

    public void Update(string clientName)
    {
      this.ClientName = clientName;
      NotifyPropertyChanged(null);
    }
    */

    //public string ClientName { get; private set; }
    public string ClientName { get => ((GsaModel)Instance.GsaModel).Account.userInfo.name; }
    //public string ServerUrl { get; private set; }
    public string ServerUrl { get => ((GsaModel)Instance.GsaModel).Account.serverInfo.url; }
    //public string EmailAddress { get; private set; }
    public string EmailAddress { get => ((GsaModel)Instance.GsaModel).Account.userInfo.email; }
    //public string Token { get; private set; }
    public string Token { get => ((GsaModel)Instance.GsaModel).Account.token; }

    public string Summary { get => string.Join(" ", ClientName, EmailAddress); }

    public bool IsValid { get => !string.IsNullOrEmpty(ServerUrl) && !string.IsNullOrEmpty(EmailAddress) && !string.IsNullOrEmpty(Token); }
  }
}