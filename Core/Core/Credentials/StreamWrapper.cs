using System;
using System.Linq;

namespace Speckle.Core.Credentials
{
  public class StreamWrapper
  {
    public string AccountId { get; set; }
    public string ServerUrl { get; set; }
    public string StreamId { get; set; }
    public string CommitId { get; set; }
    public string BranchName { get; set; } // To be used later! 

    public StreamWrapper() { }

    /// <summary>
    /// Creates a StreamWrapper from a stream url
    /// </summary>
    /// <param name="streamUrl">Stream Url eg: http://speckle.server/streams/8fecc9aa6d/commits/76a23d7179</param>
    /// <exception cref="Exception"></exception>
    public StreamWrapper(string streamUrl)
    {
      Account account = null;
      Uri uri = null;
      if (streamUrl.Contains("?u="))
      {
        uri = new Uri(streamUrl.Split(new string[] { "?u=" }, StringSplitOptions.None)[0]);
        ServerUrl = uri.GetLeftPart(UriPartial.Authority);

        AccountId = streamUrl.Split(new string[] { "?u=" }, StringSplitOptions.None)[1];
        account = AccountManager.GetAccounts().FirstOrDefault(a => a.id == AccountId);

        if (account == null)
        {
          account = AccountManager.GetAccounts(ServerUrl).FirstOrDefault();
          AccountId = account.id;
        }
      }
      else
      {
        uri = new Uri(streamUrl);
        ServerUrl = uri.GetLeftPart(UriPartial.Authority);
        account = AccountManager.GetAccounts(ServerUrl).FirstOrDefault();
      }

      if (account == null)
      {
        throw new Exception($"You do not have an account for {ServerUrl}. Please create one or add it to the Speckle Manager.");
      }

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
            BranchName = uri.Segments[4].Replace("/", "");
          }
          else
          {
            throw new Exception($"Cannot parse {uri} into a stream wrapper class.");
          }
          break;
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
    }

    public override string ToString()
    {
      return $"{ServerUrl}/streams/{StreamId}{(CommitId != null ? "/commits/" + CommitId : "")}{(AccountId != null ? "?u=" + AccountId : "")}";
    }

    public Account GetAccount()
    {
      Account account = null;

      account = AccountManager.GetAccounts().FirstOrDefault(a => a.id == AccountId);
      if (account == null)
      {
        account = AccountManager.GetAccounts(ServerUrl).FirstOrDefault();
      }

      if (account != null)
      {
        AccountId = account.id;
      }

      return account;
    }
  }
}
