using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Functions
{
  public static class Stream
  {
    /// <summary>
    /// Create a new Stream
    /// </summary>
    /// <param name="account">Speckle account to use, if not provided the default account will be used</param>
    /// <returns name="stream">A new Stream</returns>
    [NodeCategory("Create")]
    public static string Create([DefaultArgument("null")] Core.Credentials.Account account = null)
    {
      Tracker.TrackEvent(Tracker.STREAM_CREATE);

      if (account == null)
        account = AccountManager.GetDefaultAccount();

      var client = new Client(account);
      var res = client.StreamCreate(new StreamCreateInput()).Result;

      return res;
    }

    /// <summary>
    /// Create a new Stream
    /// </summary>
    /// <param name="streamId">Id of the Stream to get</param>
    /// <param name="account">Speckle account to use, if not provided the default account will be used</param>
    /// <returns name="stream">A new Stream</returns>
    [NodeCategory("Create")]
    public static StreamWrapper Get(string streamId, [DefaultArgument("null")] Core.Credentials.Account account = null)
    {
      Tracker.TrackEvent(Tracker.STREAM_CREATE);

      try
      {


        if (account == null)
          account = AccountManager.GetDefaultAccount();

        var client = new Client(account);

        //Exists?
        Core.Api.Stream res = client.StreamGet(streamId).Result;
        return new StreamWrapper(res.id, account.id, account.serverInfo.url);

      }
      catch (Exception e)
      {
        throw e;
      }
    }


    /// <summary>
    /// Update a Stream details
    /// </summary>
    /// <param name="stream">Stream object to update</param>
    /// <param name="name">Name of the Stream</param>
    /// <param name="description">Description of the Stream</param>
    /// <param name="isPublic">True if the stream is to be publicly availables</param>
    /// <returns name="stream">Updated Stream object</returns>
    public static StreamWrapper Update(StreamWrapper stream, [DefaultArgument("null")] string name, [DefaultArgument("null")] string description, [DefaultArgument("null")] bool? isPublic)
    {
      Tracker.TrackEvent(Tracker.STREAM_UPDATE);

      if (name == null && description == null && isPublic == null)
        return stream;

      Core.Credentials.Account account = stream.GetAccount();

      var client = new Client(account);

      var input = new StreamUpdateInput
      {
        id = stream.StreamId
      };

      if (name != null)
        input.name = name;

      if (description != null)
        input.description = description;

      if (isPublic != null)
        input.isPublic = (bool)isPublic;


      var res = client.StreamUpdate(input).Result;

      if (res)
        return stream;

      return null;
    }

    /// <summary>
    /// Extracts the details of a given stream
    /// </summary>
    /// <param name="stream">Stream object</param>
    [NodeCategory("Query")]
    [MultiReturn(new[] { "id", "name", "description", "createdAt", "updatedAt", "isPublic", "collaborators", "branches" })]
    public static Dictionary<string, object> Details(StreamWrapper stream)
    {
      Tracker.TrackEvent(Tracker.STREAM_DETAILS);

      Core.Credentials.Account account = stream.GetAccount();

      var client = new Client(account);

      Core.Api.Stream res = client.StreamGet(stream.StreamId).Result;

      return new Dictionary<string, object> {
        { "id", res.id },
        { "name", res.name },
        { "description", res.description },
        { "createdAt", DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(res.createdAt)).DateTime },
        { "updatedAt", DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(res.updatedAt)).DateTime },
        { "isPublic", res.isPublic },
        { "collaborators", res.collaborators },
        { "branches", res.branches!=null ? res.branches.items : null}
      };
    }

    /// <summary>
    /// List all your Streams
    /// </summary>
    /// <param name="account">Speckle account to use, if not provided the default account will be used</param>
    /// <param name="limit">Max number of streams to get</param>
    /// <returns name="streams">Your Streams</returns>
    [NodeCategory("Query")]
    public static List<StreamWrapper> List([DefaultArgument("null")] Core.Credentials.Account account = null, [DefaultArgument("10")] int limit = 10)
    {
      Tracker.TrackEvent(Tracker.STREAM_LIST);

      if (account == null)
        account = AccountManager.GetDefaultAccount();

      var client = new Client(account);
      var res = client.StreamsGet(limit).Result;

      var streamWrappers = new List<StreamWrapper>();
      res.ForEach(x =>
      {
        streamWrappers.Add(new StreamWrapper(x.id, account.id, account.serverInfo.url));
      });

      return streamWrappers;
    }
  }
}
