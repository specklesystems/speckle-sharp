using Speckle.Core.Api;
using Speckle.Core.Credentials;

public class TestContext
{
  public Account Account;
  public string TestStreamId;


  public string StreamLink => $"{Account.serverInfo.url}/streams/{TestStreamId}";

  public TestContext(Account account, string testStreamId)
  {
    Account = account;
    TestStreamId = testStreamId;
  }

  public void Dispose()
  {
  }

  public static async Task<TestContext> Initialize(string serverUrl, string authToken)
  {
    var account = new Account
    { 
      token = authToken,
      serverInfo = new ServerInfo{ url = serverUrl },
      userInfo = new UserInfo()
    };
    var client = new Client(account);

    var streamInput = new StreamCreateInput
    {
      name = $"Integration Test {DateTime.Now}",
      isPublic = false,
    };
    var testStreamId = await client.StreamCreate(streamInput);

    var info = new TestContext(account, testStreamId);
    return info;
  }
}

