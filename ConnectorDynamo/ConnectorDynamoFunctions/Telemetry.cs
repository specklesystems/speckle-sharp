using Piwik.Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorDynamo.Functions
{
  public static class Telemetry
  {
    private const string UA = "Dynamo";
    private static readonly string PiwikBaseUrl = "https://speckle.matomo.cloud/";
    private static readonly int SiteId = 2;

    public const string LOADED = "loaded";

    public const string RECEIVE = "receive";
    public const string SEND = "send";

    public const string NEW_RECEIVE = "new-receive";
    public const string NEW_SEND = "new-send";

    public const string RECEIVE_LOCAL = "receive-local";
    public const string SEND_LOCAL = "send-local";

    public const string STREAM_CREATE = "stream-create";
    public const string STREAM_UPDATE = "stream-update";
    public const string STREAM_DETAILS = "stream-details";
    public const string STREAM_LIST = "stream-list";

    public const string ACCOUNT_DEFAULT = "account-default";
    public const string ACCOUNT_DETAILS = "account-details";
    public const string ACCOUNT_LIST = "account-list";


    private static PiwikTracker _tracker;
    internal static PiwikTracker Tracker
    {
      get
      {
        if (_tracker == null)
        {
          _tracker = new PiwikTracker(SiteId, PiwikBaseUrl);
          _tracker.SetUserAgent(UA);
        }
        return _tracker;
      }
    }

    public static void TrackView (string viewName)
    {
      Task.Run(() => Tracker.DoTrackPageView(viewName));
    }

  }
}
