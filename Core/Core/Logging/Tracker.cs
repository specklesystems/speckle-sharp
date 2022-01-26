using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Piwik.Tracker;

namespace Speckle.Core.Logging
{
  /// <summary>
  ///  Anonymous telemetry to help us understand how to make a better Speckle.
  ///  This really helps us to deliver a better open source project and product!
  /// </summary>
  public static class Tracker
  {
    private static readonly string PiwikBaseUrl = "https://speckle.matomo.cloud";
    private static readonly int SiteId = 2;

    #region String constants helpers

    public const string RECEIVE = "receive";
    public const string RECEIVE_MANUAL = "receive/manual";
    public const string RECEIVE_AUTO = "receive/auto";
    public const string RECEIVE_ADDED = "receive/added";
    public const string RECEIVE_LOCAL = "receive/local";


    public const string SEND = "send";
    public const string SEND_MANUAL = "send/manual";
    public const string SEND_AUTO = "send/auto";
    public const string SEND_ADDED = "send/added";
    public const string SEND_LOCAL = "send/local";

    public const string STREAM_CREATE = "stream/create";
    public const string STREAM_GET = "stream/get";
    public const string STREAM_UPDATE = "stream/update";
    public const string STREAM_DETAILS = "stream/details";
    public const string STREAM_LIST = "stream/list";
    public const string STREAM_VIEW = "stream/view";

    public const string ACCOUNT_DEFAULT = "account/default";
    public const string ACCOUNT_DETAILS = "account/details";
    public const string ACCOUNT_LIST = "account/list";

    public const string CONVERT_TONATIVE = "convert/tonative";
    public const string CONVERT_TOSPECKLE = "convert/tospeckle";

    public const string SERIALIZE = "serialization/serialize";
    public const string DESERIALIZE = "serialization/deserialize";

    #endregion

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

    [Obsolete("The Tracker class will be deprecated soon, please use the new Analytics.TrackEvent method.")]
    public static void TrackPageview(params string[] segments)
    {
#if !DEBUG
      Task.Run(() =>
      {
        try
        {
          var builder = new StringBuilder();
          builder.Append($"http://connectors/{Setup.HostApplication}/");

          foreach (var segment in segments)
          {
            builder.Append(segment + "/");
          }
          var path = string.Join("/", segments);
          PiwikTracker.SetUrl(builder.ToString());
          PiwikTracker.DoTrackPageView(path);
          PiwikTracker.DoTrackEvent(Setup.HostApplication, path);
        }
        catch (Exception e)
        {
          // POKEMON: Gotta catch 'em all!
        }

      });
#endif

    }

  }
}
