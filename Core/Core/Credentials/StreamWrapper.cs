using System;
using System.Linq;
using System.Threading.Tasks;
using Speckle.Core.Api;

namespace Speckle.Core.Credentials
{
  public class StreamWrapper
  {
    private string originalInput; 

    public string AccountId { get; set; }
    public string ServerUrl { get; set; }
    public string StreamId { get; set; }
    public string CommitId { get; set; }
    public string BranchName { get; set; } 
    public string ObjectId { get; set; }

    /// <summary>
    /// Determines if the current stream wrapper contains a valid stream.
    /// </summary>
    public bool IsValid => Type != StreamWrapperType.Undefined;

    public StreamWrapperType Type
    {
      // Quick solution to determine whether a wrapper points to a branch, commit or stream.
      get
      {
        if (!string.IsNullOrEmpty(ObjectId))
        {
          return StreamWrapperType.Object;
        }

        if (!string.IsNullOrEmpty(BranchName))
        {
          return StreamWrapperType.Branch;
        }

        if (!string.IsNullOrEmpty(CommitId))
        {
          return StreamWrapperType.Commit;
        }

        if (!string.IsNullOrEmpty(StreamId))
        {
          return StreamWrapperType.Stream;
        }
        // If we reach here, it means that the stream is invalid for some reason.
        return StreamWrapperType.Undefined;
      }
    }

    public StreamWrapper()
    {
    }

    /// <summary>
    /// Creates a StreamWrapper from a stream url or a stream id
    /// </summary>
    /// <param name="streamUrlOrId">Stream Url eg: http://speckle.server/streams/8fecc9aa6d/commits/76a23d7179  or stream ID eg: 8fecc9aa6d</param>
    /// <exception cref="Exception"></exception>
    public StreamWrapper(string streamUrlOrId)
    {
      originalInput = streamUrlOrId;

      Uri uri;
      try
      {
        if (!Uri.TryCreate(streamUrlOrId, UriKind.Absolute, out uri))
        {
          StreamWrapperFromId(streamUrlOrId);
        }
        else
        {
          StreamWrapperFromUrl(streamUrlOrId);
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw;
      }
    }

    /// <summary>
    /// Creates a StreamWrapper by streamId, accountId and serverUrl
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="accountId"></param>
    /// <param name="serverUrl"></param>
    public StreamWrapper(string streamId, string accountId, string serverUrl)
    {
      AccountId = accountId;
      ServerUrl = serverUrl;
      StreamId = streamId;

      originalInput = $"{ServerUrl}/streams/{StreamId}{(AccountId != null ? "?u=" + AccountId : "")}";
    }

    private void StreamWrapperFromId(string streamId)
    {
      Account account = AccountManager.GetDefaultAccount();

      if (account == null)
      {
        throw new Exception(
          $"You do not have any account. Please create one or add it to the Speckle Manager.");
      }

      ServerUrl = account.serverInfo.url;
      AccountId = account.id;
      StreamId = streamId;
    }

    private void StreamWrapperFromUrl(string streamUrl)
    {
      Uri uri = new Uri(streamUrl);
      ServerUrl = uri.GetLeftPart(UriPartial.Authority);

      if (uri.Segments.Length < 3)
      {
        throw new Exception($"Cannot parse {uri} into a stream wrapper class.");
      }

      switch (uri.Segments.Length)
      {
        case 3: // ie http://speckle.server/streams/8fecc9aa6d
          if (uri.Segments[1].ToLowerInvariant() == "streams/")
          {
            StreamId = uri.Segments[2].Replace("/", "");
          }
          else
          {
            throw new Exception($"Cannot parse {uri} into a stream wrapper class.");
          }

          break;
        case 5: // ie http://speckle.server/streams/8fecc9aa6d/commits/76a23d7179
          if (uri.Segments[3].ToLowerInvariant() == "commits/")
          {
            StreamId = uri.Segments[2].Replace("/", "");
            CommitId = uri.Segments[4].Replace("/", "");
          }
          else if (uri.Segments[3].ToLowerInvariant() == "branches/")
          {
            StreamId = uri.Segments[2].Replace("/", "");
            BranchName = Uri.UnescapeDataString( uri.Segments[4].Replace("/", ""));
          }
          else if (uri.Segments[3].ToLowerInvariant() == "objects/")
          {
            StreamId = uri.Segments[2].Replace("/", "");
            ObjectId = uri.Segments[4].Replace("/", "");
          }
          else
          {
            throw new Exception($"Cannot parse {uri} into a stream wrapper class.");
          }

          break;
      }
    }

    private Account _Account;

    /// <summary>
    /// Gets a valid account for this stream wrapper. 
    /// <para>Note: this method ensures that the stream exists and/or that the user has an account which has access to that stream. If used in a sync manner, make sure it's not blocking.</para>
    /// </summary>
    /// <returns></returns>
    public async Task<Account> GetAccount()
    {
      if (_Account != null)
      {
        return _Account;
      }

      // Step 1: check if direct account id (?u=)
      if (originalInput.Contains("?u="))
      {
        var userId = originalInput.Split(new string[] { "?u=" }, StringSplitOptions.None)[1];
        var acc = AccountManager.GetAccounts().FirstOrDefault(acc => acc.userInfo.id == userId);
        if(acc!=null)
        {
          try
          {
            var client = new Client(acc);
            var res = await client.StreamGet(StreamId);
            _Account = acc;
            return acc;
          }
          catch { }
        }
      }

      // Step 2: check the default
      var defAcc = AccountManager.GetDefaultAccount();
      try
      {
        var client = new Client(defAcc);
        var res = await client.StreamGet(StreamId);
        _Account = defAcc;
        return defAcc;
      }
      catch { }

      // Step 3: all the rest
      var accs = AccountManager.GetAccounts(ServerUrl);
      if(accs.Count() == 0) 
      {
        throw new Exception($"You don't have any accounts for ${ServerUrl}.");
      }
      
      foreach (var acc in accs)
      {
        try
        {
          var client = new Client(acc);
          var res = await client.StreamGet(StreamId);
          _Account = acc;
          return acc;
        }
        catch { }
      }

      throw new Exception($"You don't have access to stream {StreamId} on server {ServerUrl}, or the stream does not exist.");
    }

    public override string ToString()
    {
      return originalInput;
    }
  }

  public enum StreamWrapperType
  {
    Undefined,
    Stream,
    Commit,
    Branch,
    Object
  }
}
