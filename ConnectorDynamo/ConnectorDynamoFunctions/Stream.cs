using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Functions;

public static class Stream
{
  /// <summary>
  /// Get an existing Stream
  /// </summary>
  /// <param name="stream">Stream to get with the specified account</param>
  /// <param name="account">Optional Speckle account to get the Stream with</param>
  /// <returns name="stream">A Stream</returns>
  [NodeCategory("Create")]
  public static object Get(
    [ArbitraryDimensionArrayImport] object stream,
    [DefaultArgument("null")] Core.Credentials.Account account
  )
  {
    var streams = Utils.InputToStream(stream);
    if (!streams.Any())
    {
      throw new SpeckleException("Please provide one or more Stream Ids.");
    }
    else if (streams.Count > 20)
    {
      throw new SpeckleException("Please provide less than 20 Stream Ids.");
    }

    try
    {
      foreach (var s in streams)
      {
        //lets ppl override the account for the specified stream
        Core.Credentials.Account accountToUse = null;
        if (account != null)
        {
          accountToUse = account;
        }
        else
        {
          accountToUse = Task.Run(async () => await s.GetAccount()).Result;
        }

        var client = new Client(accountToUse);

        //Exists?
        Core.Api.Stream res = Task.Run(async () => await client.StreamGet(s.StreamId)).Result;
        s.UserId = accountToUse.userInfo.id;

        AnalyticsUtils.TrackNodeRun(accountToUse, "Stream Get");
      }
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      Utils.HandleApiExeption(ex);
    }

    if (streams.Count() == 1)
    {
      return streams[0];
    }

    return streams;
  }

  /// <summary>
  /// Update a Stream details, use is limited to 1 stream at a time
  /// </summary>
  /// <param name="stream">Stream object to update</param>
  /// <param name="name">Name of the Stream</param>
  /// <param name="description">Description of the Stream</param>
  /// <param name="isPublic">True if the stream is to be publicly available</param>
  /// <returns name="stream">Updated Stream object</returns>
  public static StreamWrapper Update(
    [DefaultArgument("null")] object stream,
    [DefaultArgument("null")] string name,
    [DefaultArgument("null")] string description,
    [DefaultArgument("null")] bool? isPublic
  )
  {
    if (stream == null)
    {
      return null;
    }

    var wrapper = Utils.ParseWrapper(stream);

    if (wrapper == null)
    {
      throw new SpeckleException("Invalid stream.");
    }

    if (name == null && description == null && isPublic == null)
    {
      return null;
    }

    Core.Credentials.Account account = null;
    try
    {
      account = Task.Run(async () => await wrapper.GetAccount()).Result;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      throw ex.InnerException ?? ex;
    }

    var client = new Client(account);

    var input = new StreamUpdateInput { id = wrapper.StreamId };

    if (name != null)
    {
      input.name = name;
    }

    if (description != null)
    {
      input.description = description;
    }

    if (isPublic != null)
    {
      input.isPublic = (bool)isPublic;
    }

    AnalyticsUtils.TrackNodeRun(account, "Stream Update");

    try
    {
      var res = Task.Run(async () => await client.StreamUpdate(input)).Result;

      if (res)
      {
        return wrapper;
      }
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      Utils.HandleApiExeption(ex);
    }

    return null;
  }

  /// <summary>
  /// Extracts the details of a given stream, use is limited to max 20 streams
  /// </summary>
  /// <param name="stream">Stream object</param>
  [NodeCategory("Query")]
  [MultiReturn(
    new[] { "id", "name", "description", "createdAt", "updatedAt", "isPublic", "collaborators", "branches" }
  )]
  public static object Details([ArbitraryDimensionArrayImport] object stream)
  {
    var streams = Utils.InputToStream(stream);

    if (!streams.Any())
    {
      throw new SpeckleException("Please provide one or more Streams.");
    }

    if (streams.Count > 20)
    {
      throw new SpeckleException("Please provide less than 20 Streams.");
    }

    var details = new List<Dictionary<string, object>>();

    foreach (var streamWrapper in streams)
    {
      Core.Credentials.Account account;

      try
      {
        account = Task.Run(async () => await streamWrapper.GetAccount()).Result;
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        throw ex.InnerException ?? ex;
      }

      var client = new Client(account);

      try
      {
        Core.Api.Stream res = Task.Run(async () => await client.StreamGet(streamWrapper.StreamId)).Result;

        details.Add(
          new Dictionary<string, object>
          {
            { "id", res.id },
            { "name", res.name },
            { "description", res.description },
            { "createdAt", res.createdAt },
            { "updatedAt", res.updatedAt },
            { "isPublic", res.isPublic },
            { "collaborators", res.collaborators },
            { "branches", res.branches?.items }
          }
        );
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        Utils.HandleApiExeption(ex);
        return details;
      }
      AnalyticsUtils.TrackNodeRun(account, "Stream Details");
    }

    if (details.Count() == 1)
    {
      return details[0];
    }

    return details;
  }

  /// <summary>
  /// List all your Streams
  /// </summary>
  /// <param name="account">Speckle account to use, if not provided the default account will be used</param>
  /// <param name="limit">Max number of streams to get</param>
  /// <returns name="streams">Your Streams</returns>
  [NodeCategory("Query")]
  public static List<StreamWrapper> List(
    [DefaultArgument("null")] Core.Credentials.Account account = null,
    [DefaultArgument("10")] int limit = 10
  )
  {
    if (account == null)
    {
      account = AccountManager.GetDefaultAccount();
    }

    if (account == null)
    {
      Utils.HandleApiExeption(
        new SpeckleAccountManagerException(
          "No accounts found. Please use the Speckle Manager to manage your accounts on this computer."
        )
      );
    }

    var client = new Client(account);
    var streamWrappers = new List<StreamWrapper>();

    try
    {
      var res = Task.Run(async () => await client.StreamsGet(limit)).Result;
      res.ForEach(x =>
      {
        streamWrappers.Add(new StreamWrapper(x.id, account.userInfo.id, account.serverInfo.url));
      });
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      Utils.HandleApiExeption(ex);
    }

    AnalyticsUtils.TrackNodeRun(account, "Stream List");

    return streamWrappers;
  }
}
