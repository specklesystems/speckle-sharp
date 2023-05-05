using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Speckle.Core.Api;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;

namespace Speckle.Core.Credentials;

public class StreamWrapper
{
  private Account _Account;

  public StreamWrapper() { }

  /// <summary>
  /// Creates a StreamWrapper from a stream url or a stream id
  /// </summary>
  /// <param name="streamUrlOrId">Stream Url eg: http://speckle.server/streams/8fecc9aa6d/commits/76a23d7179  or stream ID eg: 8fecc9aa6d</param>
  /// <exception cref="Exception"></exception>
  public StreamWrapper(string streamUrlOrId)
  {
    OriginalInput = streamUrlOrId;

    if (!Uri.TryCreate(streamUrlOrId, UriKind.Absolute, out _))
      StreamWrapperFromId(streamUrlOrId);
    else
      StreamWrapperFromUrl(streamUrlOrId);
  }

  /// <summary>
  /// Creates a StreamWrapper by streamId, userId and serverUrl
  /// </summary>
  /// <param name="streamId"></param>
  /// <param name="userId"></param>
  /// <param name="serverUrl"></param>
  public StreamWrapper(string streamId, string userId, string serverUrl)
  {
    UserId = userId;
    ServerUrl = serverUrl;
    StreamId = streamId;

    OriginalInput = $"{ServerUrl}/streams/{StreamId}{(UserId != null ? "?u=" + UserId : "")}";
  }

  //this needs to be public so it's serialized and stored in Dynamo
  public string OriginalInput { get; set; }

  public string UserId { get; set; }
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
        return StreamWrapperType.Object;

      if (!string.IsNullOrEmpty(CommitId))
        return StreamWrapperType.Commit;

      if (!string.IsNullOrEmpty(BranchName))
        return StreamWrapperType.Branch;

      // If we reach here and there is no stream id, it means that the stream is invalid for some reason.
      return !string.IsNullOrEmpty(StreamId) ? StreamWrapperType.Stream : StreamWrapperType.Undefined;
    }
  }

  private void StreamWrapperFromId(string streamId)
  {
    Account account = AccountManager.GetDefaultAccount();

    if (account == null)
      throw new SpeckleException("You do not have any account. Please create one or add it to the Speckle Manager.");

    ServerUrl = account.serverInfo.url;
    UserId = account.userInfo.id;
    StreamId = streamId;
  }

  private void StreamWrapperFromUrl(string streamUrl)
  {
    Uri uri = new(streamUrl, true);

    ServerUrl = uri.GetLeftPart(UriPartial.Authority);
    // Note: this is a hack. It's because new Uri() is parsed escaped in .net framework; wheareas in .netstandard it's not.
    // Tests pass in Core without this hack.
    if (uri.Segments.Length >= 4 && uri.Segments[3]?.ToLowerInvariant() == "branches/")
    {
      StreamId = uri.Segments[2].Replace("/", "");
      if (uri.Segments.Length > 5)
      {
        var branchSegs = uri.Segments.ToList().GetRange(4, uri.Segments.Length - 4);
        BranchName = Uri.UnescapeDataString(string.Concat(branchSegs));
      }
      else
      {
        BranchName = Uri.UnescapeDataString(uri.Segments[4]);
      }
    }
    else
    {
      switch (uri.Segments.Length)
      {
        case 3: // ie http://speckle.server/streams/8fecc9aa6d
          if (uri.Segments[1].ToLowerInvariant() == "streams/")
            StreamId = uri.Segments[2].Replace("/", "");
          else
            throw new SpeckleException($"Cannot parse {uri} into a stream wrapper class.");

          break;
        case 4: // ie https://speckle.server/streams/0c6ad366c4/globals/
          if (uri.Segments[3].ToLowerInvariant().StartsWith("globals"))
          {
            StreamId = uri.Segments[2].Replace("/", "");
            BranchName = Uri.UnescapeDataString(uri.Segments[3].Replace("/", ""));
          }
          else
          {
            throw new SpeckleException($"Cannot parse {uri} into a stream wrapper class");
          }

          break;
        case 5: // ie http://speckle.server/streams/8fecc9aa6d/commits/76a23d7179
          switch (uri.Segments[3].ToLowerInvariant())
          {
            // NOTE: this is a good practice reminder on how it should work
            case "commits/":
              StreamId = uri.Segments[2].Replace("/", "");
              CommitId = uri.Segments[4].Replace("/", "");
              break;
            case "globals/":
              StreamId = uri.Segments[2].Replace("/", "");
              BranchName = Uri.UnescapeDataString(uri.Segments[3].Replace("/", ""));
              CommitId = uri.Segments[4].Replace("/", "");
              break;
            case "branches/":
              StreamId = uri.Segments[2].Replace("/", "");
              BranchName = Uri.UnescapeDataString(uri.Segments[4].Replace("/", ""));
              break;
            case "objects/":
              StreamId = uri.Segments[2].Replace("/", "");
              ObjectId = uri.Segments[4].Replace("/", "");
              break;
            default:
              throw new SpeckleException($"Cannot parse {uri} into a stream wrapper class.");
          }

          break;

        default:
          throw new SpeckleException($"Cannot parse {uri} into a stream wrapper class.");
      }
    }

    var queryDictionary = HttpUtility.ParseQueryString(uri.Query);
    UserId = queryDictionary["u"];
  }

  /// <summary>
  /// Gets a valid account for this stream wrapper.
  /// <para>Note: this method ensures that the stream exists and/or that the user has an account which has access to that stream. If used in a sync manner, make sure it's not blocking.</para>
  /// </summary>
  /// <exception cref="Exception">Throws exception if account fetching failed. This could be due to non-existent account or stream.</exception>
  /// <returns>The valid account object for this stream.</returns>
  public async Task<Account> GetAccount()
  {
    Exception err = null;

    if (_Account != null)
      return _Account;

    // Step 1: check if direct account id (?u=)
    if (OriginalInput != null && OriginalInput.Contains("?u="))
    {
      var userId = OriginalInput.Split(new[] { "?u=" }, StringSplitOptions.None)[1];
      var acc = AccountManager.GetAccounts().FirstOrDefault(acc => acc.userInfo.id == userId);
      if (acc != null)
      {
        await ValidateWithAccount(acc).ConfigureAwait(false);
        _Account = acc;
        return acc;
      }
    }

    // Step 2: check the default
    var defAcc = AccountManager.GetDefaultAccount();
    try
    {
      await ValidateWithAccount(defAcc).ConfigureAwait(false);
      _Account = defAcc;
      return defAcc;
    }
    catch (Exception e)
    {
      err = e;
    }

    // Step 3: all the rest
    var accs = AccountManager.GetAccounts(ServerUrl);
    if (accs.Count() == 0)
      throw new SpeckleException($"You don't have any accounts for {ServerUrl}.");

    foreach (var acc in accs)
      try
      {
        await ValidateWithAccount(acc).ConfigureAwait(false);
        _Account = acc;
        return acc;
      }
      catch (Exception e)
      {
        err = e;
      }

    throw err;
  }

  public void SetAccount(Account acc)
  {
    _Account = acc;
    UserId = _Account.userInfo.id;
  }

  public bool Equals(StreamWrapper wrapper)
  {
    if (wrapper == null)
      return false;
    if (Type != wrapper.Type)
      return false;
    return Type == wrapper.Type
        && ServerUrl == wrapper.ServerUrl
        && UserId == wrapper.UserId
        && StreamId == wrapper.StreamId
        && Type == StreamWrapperType.Branch
        && BranchName == wrapper.BranchName
      || Type == StreamWrapperType.Object && ObjectId == wrapper.ObjectId
      || Type == StreamWrapperType.Commit && CommitId == wrapper.CommitId;
  }

  public async Task ValidateWithAccount(Account acc)
  {
    if (ServerUrl != acc.serverInfo.url)
      throw new SpeckleException($"Account is not from server {ServerUrl}", false);

    var hasInternet = await Http.UserHasInternet().ConfigureAwait(false);
    if (!hasInternet)
      throw new Exception("You are not connected to the internet.");

    using var client = new Client(acc);
    // First check if the stream exists
    try
    {
      await client.StreamGet(StreamId).ConfigureAwait(false);
    }
    catch
    {
      throw new SpeckleException(
        $"You don't have access to stream {StreamId} on server {ServerUrl}, or the stream does not exist.",
        false
      );
    }

    // Check if the branch exists
    if (Type == StreamWrapperType.Branch)
    {
      var branch = await client.BranchGet(StreamId, BranchName, 1).ConfigureAwait(false);
      if (branch == null)
        throw new SpeckleException(
          $"The branch with name '{BranchName}' doesn't exist in stream {StreamId} on server {ServerUrl}",
          false
        );
    }
  }

  public override string ToString()
  {
    var url = $"{ServerUrl}/streams/{StreamId}";
    switch (Type)
    {
      case StreamWrapperType.Commit:
        url += $"/commits/{CommitId}";
        break;
      case StreamWrapperType.Branch:
        url += $"/branches/{BranchName}";
        break;
      case StreamWrapperType.Object:
        url += $"/objects/{ObjectId}";
        break;
    }

    var acc = $"{(UserId != null ? "?u=" + UserId : "")}";
    return url + acc;
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
