using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConnectorGSA.Models
{
  public class TabCoordinator
  {
    public SpeckleAccountForUI Account { get; set; }
    public GsaLoadedFileType FileStatus { get; set; }
    public string FilePath { get; set; }
    public StreamList ServerStreamList { get; set; } = new StreamList();
    public DisplayLog DisplayLog { get; set; } = new DisplayLog();

    public LoggingMinimumLevel LoggingMinimumLevel { get; set; } = LoggingMinimumLevel.Information;
    public bool VerboseErrorInformation { get; set; } = false;

    public ReceiverTab ReceiverTab { get; set; } = new ReceiverTab();
    public SenderTab SenderTab { get; set; } = new SenderTab();
    public Version RunningVersion { get => getRunningVersion(); }

    #region app_resources

    //The SpeckleStreamManager is also used, but that is a static class so no need to store it as a member here
    //public SenderCoordinator gsaSenderCoordinator;
    //public ReceiverCoordinator gsaReceiverCoordinator;

    public Timer triggerTimer;

    #endregion

    public void Init()
    {
      /*
      GSA.Reset();

      GSA.Init(getRunningVersion().ToString());
      //SpeckleInitializer.Initialize();
      LocalContext.Init();

      //This will create the logger
      GSA.App.LocalSettings.LoggingMinimumLevel = 4;  //Debug

      gsaSenderCoordinator = new SenderCoordinator();
      gsaReceiverCoordinator = new ReceiverCoordinator();
      */
    }

    private Version getRunningVersion()
    {
      try
      {
        return ApplicationDeployment.CurrentDeployment.CurrentVersion;
      }
      catch (Exception)
      {
        return Assembly.GetExecutingAssembly().GetName().Version;
      }
    }

    /*
    internal bool RetrieveSavedSidStreamRecords()
    {
      ReceiverTab.ReceiverStreamStates.Clear();
      SenderTab.SenderStreamStates.Clear();

      if (HelperFunctions.GetStreamStates(Account.EmailAddress, Account.ServerUrl, GSA.App.Proxy, out var receiverStreamInfo, out var senderStreamInfo))
      {
        if (receiverStreamInfo != null && receiverStreamInfo.Count > 0)
        {
          for (int i = 0; i < receiverStreamInfo.Count; i++)
          {
            ReceiverTab.ReceiverStreamStates.Add(receiverStreamInfo[i]);
          }
          ReceiverTab.StreamStatesToStreamList();
        }
        if (senderStreamInfo != null && senderStreamInfo.Count > 0)
        {
          for (int i = 0; i < senderStreamInfo.Count; i++)
          {
            SenderTab.SenderStreamStates.Add(senderStreamInfo[i]);
          }
          SenderTab.StreamStatesToStreamList();
        }
      }
      return true;
    }
    */

    internal bool WriteStreamInfo()
    {
      /*
      string key = Account.EmailAddress + "&" + Account.ServerUrl.Replace(':', '&');
      string res = GSA.App.Proxy.GetTopLevelSid();

      List<string[]> sids = Regex.Matches(res, @"(?<={).*?(?=})").Cast<Match>()
              .Select(m => m.Value.Split(new char[] { ':' }))
              .Where(s => s.Length == 2)
              .ToList();

      sids.RemoveAll(S => S[0] == "SpeckleSender&" + key || S[0] == "SpeckleReceiver&" + key || string.IsNullOrEmpty(S[1]));

      if (SenderTab.SenderStreamStates != null)
      {
        var senderList = new List<string>();
        foreach (var si in SenderTab.SenderStreamStates)
        {
          senderList.AddRange(new[] { si.Bucket, si.StreamId, si.ClientId });
        }
        if (senderList.Count() > 0)
        {
          sids.Add(new string[] { "SpeckleSender&" + key, string.Join("&", senderList) });
        }
      }

      if (ReceiverTab.ReceiverStreamStates != null)
      {
        var receiverList = new List<string>();
        foreach (var si in ReceiverTab.ReceiverStreamStates)
        {
          receiverList.AddRange(new[] { si.StreamId, si.Bucket });
        }
        if (receiverList.Count() > 0)
        {
          sids.Add(new string[] { "SpeckleReceiver&" + key, string.Join("&", receiverList) });
        }
      }

      string StreamState = "";
      foreach (string[] s in sids)
      {
        StreamState += "{" + s[0] + ":" + s[1] + "}";
      }

      return GSA.App.Proxy.SetTopLevelSid(StreamState);
      */
      return true;
    }
  }
}
