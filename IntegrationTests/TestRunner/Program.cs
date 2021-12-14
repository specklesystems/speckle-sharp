using Sender;

Console.WriteLine("Starting Speckle connector integration test, \nThis will take a while, go boil a kettle.");

//1. set up the test server with an account, set default account to test acc just in case
Console.WriteLine("Setting up the connection to the test server");

//1. set up the test server with an account, set default account to test acc just in case
//1/a. set user account
//1/b. create new stream, share the stream with a given list of users, for dev purposes
var settings = await ServerSettings.Initialize();


//2. sync execution steps, we need to send all the data before we can receive
Console.WriteLine("Configuring send operations");
var executor = new TestExecutor(settings);

await executor.Setup();
var sendResults = (await executor.Send()).ToList();

Console.WriteLine("Send operations completed");

Console.WriteLine("Tests finished.");
Console.WriteLine("Tear down test environment? [Y]/[n]");
var res = Console.ReadLine();
if (res != "n") executor.Teardown();

