using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorDynamo.Functions
{
  public static class Stream
  {
    /// <summary>
    /// Create a new Stream
    /// </summary>
    /// <param name="account">Speckle account to use</param>
    /// <param name="name">Name of the stream (optional)</param>
    /// <param name="description">Description of the stream (optional)</param>
    /// <returns name="streamId">ID of the created Stream</returns>
    [NodeName("Create Stream")]
    [NodeCategory("Speckle")]
    [NodeDescription("Create a new Stream")]
    [NodeSearchTags("stream", "create", "speckle")]
    public static string Create([DefaultArgument("\"Anonymous stream\"")] string name, [DefaultArgument("\"\"")] string description, [DefaultArgument("null")] Account account = null)
    {
      if (account == null)
        account = AccountManager.GetDefaultAccount();

      var client = new Client(account);
      var res = client.StreamCreate(new StreamCreateInput
      {
        description = description,
        name = name
      }).Result;

      return res;
    }

    /// <summary>
    /// Get a Stream
    /// </summary>
    /// <param name="account">Speckle account to use</param>
    /// <param name="streamId">ID of the stream to get</param>
    /// <returns name="stream">ID of the created Stream</returns>
    [NodeName("Get Stream")]
    [NodeCategory("Speckle")]
    [NodeDescription("Create a new Stream")]
    [NodeSearchTags("stream", "get", "speckle")]
    public static Core.Api.Stream Get(string streamId, [DefaultArgument("null")] Account account = null)
    {
      if (account == null)
        account = AccountManager.GetDefaultAccount();
      var client = new Client(account);
      var res = client.StreamGet(streamId).Result;

      return res;
    }
  }
}
