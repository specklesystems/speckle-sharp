
using System.Globalization;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

class ServerSettings
{
    public string StreamId;
    private Client _client;
    public string AccountId => _client.Account.id;

    public ServerSettings(string streamId, Client client)
    {
        StreamId = streamId;
        _client = client;
    }

    public static async Task<ServerSettings> Initialize()
    {

        // set up a user for latest.speckle.dev
        var latestAcc = AccountManager.GetAccounts().Where(a => a.serverInfo.url == "https://latest.speckle.dev").First();
        var client = new Client(latestAcc);
        var date = DateTime.Now.ToString("G", CultureInfo.GetCultureInfo("en-US"));
        var streamId = await client.StreamCreate(new StreamCreateInput() { name = $"Integration {date}" });
        return new ServerSettings(streamId, client);
    }
}

//1. list of supported applications, with a list of supported versions, and a list of filetypes
