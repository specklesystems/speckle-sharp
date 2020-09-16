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
    /// <param name="account">Speckle account to use, if not provided the default account will be used</param>
    /// <returns name="streamId">ID of the created Stream</returns>
    [NodeCategory("Create")]
    public static string Create([DefaultArgument("null")] Account account = null)
    {
      if (account == null)
        account = AccountManager.GetDefaultAccount();

      var client = new Client(account);
      var res = client.StreamCreate(new StreamCreateInput()).Result;

      return res;
    }

    /// <summary>
    /// Update a Stream details
    /// </summary>
    /// <param name="streamId">ID of the Stream to update</param>
    /// <param name="name">Name of the Stream</param>
    /// <param name="description">Description of the Stream</param>
    /// <param name="isPublic">Weather the Stream is public or not</param>
    /// <param name="account">Speckle account to use, if not provided the default account will be used</param>
    /// <returns name="streamId">ID of the updated Stream</returns>
    public static string Update(string streamId, [DefaultArgument("null")] string name, [DefaultArgument("null")] string description, [DefaultArgument("null")] bool? isPublic, [DefaultArgument("null")] Account account = null)
    {
      if (name == null && description == null && isPublic == null)
        return streamId;

      if (account == null)
        account = AccountManager.GetDefaultAccount();

      var client = new Client(account);

      var input = new StreamUpdateInput
      {
        id = streamId
      };

      if (name != null)
        input.name = name;

      if (description != null)
        input.description = description;

      if (isPublic != null)
        input.isPublic = (bool)isPublic;


      var res = client.StreamUpdate(input).Result;

      if (res)
        return streamId;

      return "Something went wrong...";
    }

    /// <summary>
    /// Get a Stream details
    /// </summary>
    /// <param name="account">Speckle account to use</param>
    /// <param name="streamId">ID of the stream to get</param>
    [NodeCategory("Query")]
    [MultiReturn(new[] { "id", "name", "description", "createdAt", "updatedAt", "isPublic", "collaborators", "branches" })]
    public static Dictionary<string, object> Details(string streamId, [DefaultArgument("null")] Account account = null)
    {
      if (account == null)
        account = AccountManager.GetDefaultAccount();
      var client = new Client(account);
      var res = client.StreamGet(streamId).Result;

      return new Dictionary<string, object> {
        { "id", res.id },
        { "name", res.name },
        { "description", res.description },
        { "createdAt", DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(res.createdAt)).DateTime },
        { "updatedAt", DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(res.updatedAt)).DateTime },
        { "isPublic", res.isPublic },
        { "collaborators", res.collaborators },
        { "branches", res.branches.items }
      };
    }
  }
}
