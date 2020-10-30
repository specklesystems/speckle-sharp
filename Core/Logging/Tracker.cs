using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DeviceId;
using Piwik.Tracker;

namespace Speckle.Core.Logging
{
  /// <summary>
  ///  Anonymous telemetry to help us understand how to make a better Speckle
  ///  PLEASE DO NOT REMOVE
  ///  This really helps us to deliver a good natured open source project!
  /// </summary>
  public static class Tracker
  {
    private static readonly string PiwikBaseUrl = "https://speckle.matomo.cloud/";
    private static readonly int SiteId = 2;

    public const string SESSION_START = "session-start";
    public const string SESSION_END = "session-end";

    public const string RECEIVE = "receive-run";
    public const string SEND = "send-run";

    public const string RECEIVE_ADDED = "receive-added";
    public const string SEND_ADDED = "send-added";

    public const string RECEIVE_LOCAL = "receive-local";
    public const string SEND_LOCAL = "send-local";

    public const string STREAM_CREATE = "stream-create";
    public const string STREAM_GET = "stream-get";
    public const string STREAM_UPDATE = "stream-update";
    public const string STREAM_DETAILS = "stream-details";
    public const string STREAM_LIST = "stream-list";

    public const string ACCOUNT_DEFAULT = "account-default";
    public const string ACCOUNT_DETAILS = "account-details";
    public const string ACCOUNT_LIST = "account-list";


    private static PiwikTracker _tracker;
    private static PiwikTracker PiwikTracker
    {
      get
      {
        if (_tracker == null)
        {
          _tracker = new PiwikTracker(SiteId, PiwikBaseUrl);
          _tracker.SetCustomVariable(1, "hostApplication", Setup.HostApplication);
          _tracker.SetUserId(Setup.SUUID);
        }
        return _tracker;
      }
    }

    [Obsolete("Pageview tracking seems a bit better?")]
    public static void TrackEvent(string eventName)
    {
      var eventData = eventName.Split('-'); 
      //Task.Run(async () => PiwikTracker.DoTrackEvent(eventData[0], eventData[1]));
      
      TrackPageview(eventData[0], eventData[1]);
    }

    public static void TrackPageview(params string[] segments)
    {
      var builder = new StringBuilder();
      builder.Append($"http://connectors/{Setup.HostApplication}/");
      foreach (var segment in segments)
      {
        builder.Append(segment + "/");
      }
      
      PiwikTracker.SetUrl(builder.ToString());
      PiwikTracker.DoTrackPageView(String.Join("/", segments));
    }

  }
}
