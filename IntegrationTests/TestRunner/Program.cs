Console.WriteLine("Starting Speckle connector integration test, \nThis will take a while, go boil a kettle.");

//1. set up the test server with an account, set default account to test acc just in case
Console.WriteLine("Setting up the connection to the test server");

//1. set up the test server with an account, set default account to test acc just in case
//1/a. set user account
//1/b. create new stream, share the stream with a given list of users, for dev purposes
// var settings = await ServerSettings.Initialize();
var testServerUrl = Environment.GetEnvironmentVariable("SPECKLE_TEST_SERVER_URL") ?? throw new Exception("Test server url not set");
var token = Environment.GetEnvironmentVariable("SPECKLE_TEST_TOKEN") ?? throw new Exception("Test actor token not set");

var config = ConfigLoader.Load();

var context = await TestContext.Initialize(testServerUrl, token);
var executor = TestExecutor.Initialize(context);


// //2. sync execution steps, we need to send all the data before we can receive
// Console.WriteLine("Configuring send operations");
// var executor = new TestExecutor(settings);

// await executor.Setup();
// var sendResults = (await executor.Send()).ToList();

// Console.WriteLine("Send operations completed");

// Console.WriteLine("Tests finished.");
// Console.WriteLine("Tear down test environment? [Y]/[n]");
// var res = Console.ReadLine();
// if (res != "n") executor.Teardown();

// var rhinoSender = new RhinoSender();

// var res = await rhinoSender.Send();

var testResult = await executor.Execute();

Console.Write("Done");
Console.Write($"Check out the result {testResult.StreamUrl}");