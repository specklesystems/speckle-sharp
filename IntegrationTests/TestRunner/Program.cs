using System.Diagnostics;

Console.WriteLine("Starting Speckle connector integration test, \nThis will take a while, go boil a kettle.");

//1. set up the test server with an account, set default account to test acc just in case
Console.WriteLine("Setting up the connection to the test server");

//1. set up the test server with an account, set default account to test acc just in case
//1/a. set user account
//1/b. create new stream, share the stream with a given list of users, for dev purposes
var settings = ServerSettings.Initialize();


//2. sync execution steps, we need to send all the data before we can receive
Console.WriteLine("Configuring send operations");
var senders = Senders.Initialize(settings);

await senders.Send();


interface ISender {
    Task SendTo(string streamId);
}

class RevitSender: ISender
{
    private string _applicationPath;

    public RevitSender(string applicationPath)
    {
        _applicationPath = applicationPath;
    }

    public async Task SendTo(string streamId)
    {
        var process = Process.Start(_applicationPath);
        await process.WaitForExitAsync();
    }
}

class Senders 
{
    private IList<ISender> _items;
    private ServerSettings _settings;

    public Senders(ServerSettings settings, IList<ISender> items)
    {
        _settings = settings;
        _items = items;
    }

    public async Task Send()
    {
        foreach (var sender in _items)
        {
            // 1. create a new target branch

            // 2. store the stream for later reuse

            // 3. send with the sender to the stream
            await sender.SendTo(_settings.Stream.id);
        }
    }

    public static Senders Initialize(ServerSettings settings)
    {
        return new Senders(settings, new []{
            new RevitSender(@"/home/gergojedlicska/Speckle/speckle-sharp/IntegrationTests/ConsoleSender/bin/Debug/net6.0/ConsoleSender")
        });
    }
}

//1. list of supported applications, with a list of supported versions, and a list of filetypes
