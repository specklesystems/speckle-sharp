using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;

namespace Speckle.Core.Credentials;

public class StreamWrapper
{
  private Account? _account;

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
    {
      StreamWrapperFromId(streamUrlOrId);
    }
    else
    {
      StreamWrapperFromUrl(streamUrlOrId);
    }
  }

  /// <summary>
  /// Creates a StreamWrapper by streamId, userId and serverUrl
  /// </summary>
  /// <param name="streamId"></param>
  /// <param name="userId"></param>
  /// <param name="serverUrl"></param>
  public StreamWrapper(string streamId, string? userId, string serverUrl)
  {
    UserId = userId;
    ServerUrl = serverUrl;
    StreamId = streamId;

    OriginalInput = $"{ServerUrl}/streams/{StreamId}{(UserId != null ? "?u=" + UserId : "")}";
  }

  //this needs to be public so it's serialized and stored in Dynamo
  public string? OriginalInput { get; set; }

  public string? UserId { get; set; }
  public string ServerUrl { get; set; }
  public string StreamId { get; set; }
  public string? CommitId { get; set; }

  /// <remarks>May be an ID instead for FE2 urls</remarks>
  public string? BranchName { get; set; }
  public string? ObjectId { get; set; }

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

      if (!string.IsNullOrEmpty(CommitId))
      {
        return StreamWrapperType.Commit;
      }

      if (!string.IsNullOrEmpty(BranchName))
      {
        return StreamWrapperType.Branch;
      }

      // If we reach here and there is no stream id, it means that the stream is invalid for some reason.
      return !string.IsNullOrEmpty(StreamId) ? StreamWrapperType.Stream : StreamWrapperType.Undefined;
    }
  }

  private void StreamWrapperFromId(string streamId)
  {
    Account? account = AccountManager.GetDefaultAccount();

    if (account == null)
    {
      throw new SpeckleException("You do not have any account. Please create one or add it to the Speckle Manager.");
    }

    ServerUrl = account.serverInfo.url;
    UserId = account.userInfo.id;
    StreamId = streamId;
  }

  /// <summary>
  /// The ReGex pattern to determine if a URL's AbsolutePath is a Frontend2 URL or not.
  /// This is used in conjunction with <see cref="ParseFe2ModelValue"/> to extract the correct values into the instance.
  /// </summary>
  private static readonly Regex s_fe2UrlRegex =
    new(
      @"/projects/(?<projectId>[\w\d]+)(?:/models/(?<model>[\w\d]+(?:@[\w\d]+)?)(?:,(?<additionalModels>[\w\d]+(?:@[\w\d]+)?))*)?"
    );

  /// <summary>
  /// Parses a FrontEnd2 URL Regex match and assigns it's data to this StreamWrapper instance.
  /// </summary>
  /// <param name="match">A regex match coming from <see cref="s_fe2UrlRegex"/></param>
  /// <exception cref="SpeckleException">Will throw when the URL is not properly formatted.</exception>
  /// <exception cref="NotSupportedException">Will throw when the URL is correct, but is not currently supported by the StreamWrapper class.</exception>
  private void ParseFe2RegexMatch(Match match)
  {
    var projectId = match.Groups["projectId"];
    var model = match.Groups["model"];
    var additionalModels = match.Groups["additionalModels"];

    if (!projectId.Success)
    {
      throw new SpeckleException("The provided url is not a valid Speckle url");
    }

    if (!model.Success)
    {
      throw new SpeckleException("The provided url is not pointing to any model in the project.");
    }

    if (additionalModels.Success || model.Value == "all")
    {
      throw new NotSupportedException("Multi-model urls are not supported yet");
    }

    if (model.Value.StartsWith("$"))
    {
      throw new NotSupportedException("Federation model urls are not supported");
    }

    var modelRes = ParseFe2ModelValue(model.Value);

    // INFO: The Branch endpoint is being updated to fallback to checking a branch ID if no name is found.
    // Assigning the BranchID as the BranchName is a workaround to support FE2 links in the old StreamWrapper.
    // A better solution must be redesigned taking into account all the new Frontend2 URL features.
    StreamId = projectId.Value;
    BranchName = modelRes.branchId;
    CommitId = modelRes.commitId;
    ObjectId = modelRes.objectId;
  }

  /// <summary>
  /// Parses the segment of the FE2 URL that represents a modelID, modelID@versionID or objectID.
  /// It is meant to parse a single value. If url is multi-model it should be used once per model.
  /// </summary>
  /// <param name="modelValue">The a single value of the model url segment</param>
  /// <returns>A tuple containing the branch, commit and object information for that value. Each value can be null</returns>
  /// <remarks>Determines if a modelValue is an ObjectId by checking it's length is exactly 32 chars long.</remarks>
  private static (string? branchId, string? commitId, string? objectId) ParseFe2ModelValue(string modelValue)
  {
    if (modelValue.Length == 32)
    {
      return (null, null, modelValue); // Model value is an ObjectID
    }

    if (!modelValue.Contains('@'))
    {
      return (modelValue, null, null); // Model has no version attached
    }

    var res = modelValue.Split('@');
    return (res[0], res[1], null); // Model has version attached
  }

  private void StreamWrapperFromUrl(string streamUrl)
  {
    Uri uri = new(streamUrl);
    ServerUrl = uri.GetLeftPart(UriPartial.Authority);

    var fe2Match = s_fe2UrlRegex.Match(uri.AbsolutePath);
    if (fe2Match.Success)
    {
      //NEW FRONTEND URL!
      ParseFe2RegexMatch(fe2Match);
      return;
    }

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
          if (uri.Segments[1].ToLowerInvariant() != "streams/")
          {
            throw new SpeckleException($"Cannot parse {uri} into a stream wrapper class.");
          }
          else
          {
            StreamId = uri.Segments[2].Replace("/", "");
          }

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
  /// <exception cref="SpeckleException">Throws exception if account fetching failed. This could be due to non-existent account or stream.</exception>
  /// <returns>The valid account object for this stream.</returns>
  public async Task<Account> GetAccount()
  {
    if (_account != null)
    {
      return _account;
    }

    // Step 1: check if direct account id (?u=)
    if (OriginalInput != null && OriginalInput.Contains("?u="))
    {
      var userId = OriginalInput.Split(new[] { "?u=" }, StringSplitOptions.None)[1];
      var acc = AccountManager.GetAccounts().FirstOrDefault(acc => acc.userInfo.id == userId);
      if (acc != null)
      {
        await ValidateWithAccount(acc).ConfigureAwait(false);
        _account = acc;
        return acc;
      }
    }

    // Step 2: check the default
    var defAcc = AccountManager.GetDefaultAccount();
    List<Exception> err = new();
    try
    {
      await ValidateWithAccount(defAcc).ConfigureAwait(false);
      _account = defAcc;
      return defAcc;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      err.Add(new SpeckleException($"Account {defAcc?.userInfo?.email} failed to auth stream wrapper", ex));
    }

    // Step 3: all the rest
    var accs = AccountManager.GetAccounts(ServerUrl).ToList();
    if (accs.Count == 0)
    {
      throw new SpeckleException($"You don't have any accounts for {ServerUrl}.");
    }

    foreach (var acc in accs)
    {
      try
      {
        await ValidateWithAccount(acc).ConfigureAwait(false);
        _account = acc;
        return acc;
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        err.Add(new SpeckleException($"Account {acc} failed to auth stream wrapper", ex));
      }
    }

    AggregateException inner = new(null, err);
    throw new SpeckleException("Failed to validate stream wrapper", inner);
  }

  public void SetAccount(Account acc)
  {
    _account = acc;
    UserId = _account.userInfo.id;
  }

  public bool Equals(StreamWrapper? wrapper)
  {
    if (wrapper == null)
    {
      return false;
    }

    if (Type != wrapper.Type)
    {
      return false;
    }

    return Type == wrapper.Type
        && ServerUrl == wrapper.ServerUrl
        && UserId == wrapper.UserId
        && StreamId == wrapper.StreamId
        && Type == StreamWrapperType.Branch
        && BranchName == wrapper.BranchName
      || Type == StreamWrapperType.Object && ObjectId == wrapper.ObjectId
      || Type == StreamWrapperType.Commit && CommitId == wrapper.CommitId;
  }

  /// <summary>
  /// Verifies that the state of the stream wrapper represents a valid Speckle resource e.g. points to a valid stream/branch etc.
  /// </summary>
  /// <param name="acc">The account to use to verify the current state of the stream wrapper</param>
  /// <exception cref="ArgumentException">The <see cref="ServerInfo"/> of the provided <paramref name="acc"/> is invalid or does not match the <see cref="StreamWrapper"/>'s <see cref="ServerUrl"/></exception>
  /// <exception cref="HttpRequestException">You are not connected to the internet</exception>
  /// <exception cref="SpeckleException">Verification of the current state of the stream wrapper with provided <paramref name="acc"/> was unsuccessful. The <paramref name="acc"/> could be invalid, or lack permissions for the <see cref="StreamId"/>, or the <see cref="StreamId"/> or <see cref="BranchName"/> are invalid</exception>
  public async Task ValidateWithAccount(Account acc)
  {
    Uri url;
    try
    {
      url = new(ServerUrl);
    }
    catch (UriFormatException ex)
    {
      throw new ArgumentException("Server Url is improperly formatted", nameof(acc), ex);
    }

    if (ServerUrl != acc.serverInfo.url && url != acc.serverInfo.migration?.movedFrom)
    {
      throw new ArgumentException($"Account is not from server {ServerUrl}", nameof(acc));
    }

    try
    {
      await Http.HttpPing(url).ConfigureAwait(false);
    }
    catch (HttpRequestException ex)
    {
      throw new HttpRequestException("You are not connected to the internet.", ex);
    }

    using var client = new Client(acc);
    // First check if the stream exists
    try
    {
      await client.StreamGet(StreamId).ConfigureAwait(false);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      throw new SpeckleException(
        $"You don't have access to stream {StreamId} on server {ServerUrl}, or the stream does not exist.",
        ex
      );
    }

    // Check if the branch exists
    if (Type == StreamWrapperType.Branch)
    {
      var branch = await client.BranchGet(StreamId, BranchName!, 0).ConfigureAwait(false);
      if (branch == null)
      {
        throw new SpeckleException(
          $"The branch with name '{BranchName}' doesn't exist in stream {StreamId} on server {ServerUrl}"
        );
      }
    }
  }

  public Uri ToServerUri()
  {
    if (_account != null)
    {
      return _account.serverInfo.frontend2 ? ToProjectUri() : ToStreamUri();
    }

    if (OriginalInput != null)
    {
      if (Uri.IsWellFormedUriString(OriginalInput, UriKind.Absolute))
      {
        Uri uri = new(OriginalInput);
        var fe2Match = s_fe2UrlRegex.Match(OriginalInput);
        return fe2Match.Success ? ToProjectUri() : ToStreamUri();
      }
    }

    // Default to old FE1
    return ToStreamUri();
  }

  private Uri ToProjectUri()
  {
    var uri = new Uri(ServerUrl);

    // TODO: THis has to be the branch ID or it won't work.
    var branchID = BranchName;
    var leftPart = $"projects/{StreamId}/models/";
    switch (Type)
    {
      case StreamWrapperType.Commit:
        leftPart += $"{branchID}@{CommitId}";
        break;
      case StreamWrapperType.Branch:
        leftPart += $"{branchID}";
        break;
      case StreamWrapperType.Object:
        leftPart += $"{ObjectId}";
        break;
    }
    var acc = $"{(UserId != null ? "?u=" + UserId : "")}";

    var finalUri = new Uri(uri, leftPart + acc);
    return finalUri;
  }

  private Uri ToStreamUri()
  {
    var uri = new Uri(ServerUrl);
    var leftPart = $"streams/{StreamId}";
    switch (Type)
    {
      case StreamWrapperType.Commit:
        leftPart += $"/commits/{CommitId}";
        break;
      case StreamWrapperType.Branch:
        leftPart += $"/branches/{BranchName}";
        break;
      case StreamWrapperType.Object:
        leftPart += $"/objects/{ObjectId}";
        break;
    }
    var acc = $"{(UserId != null ? "?u=" + UserId : "")}";

    var finalUri = new Uri(uri, leftPart + acc);
    return finalUri;
  }

  public override string ToString() => ToServerUri().ToString();
}

public enum StreamWrapperType
{
  Undefined,
  Stream,
  Commit,
  Branch,
  Object
}
