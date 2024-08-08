// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Objects.BuiltElements;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;

var w = new Wall();

var receiveTransport = new ServerTransport(
  new Account
  {
    token = "5689faeedd5bc670c2d6d9c793d1cfd5186fbdcdf8",
    serverInfo = new ServerInfo { url = "https://testing.speckle.dev" }
  },
  "3fbcca650f"
);

var objectId = "eedf8fa00e9655d4b734afc7a665b4fc";

var obj = await Operations.Receive(objectId, receiveTransport).ConfigureAwait(false);

Console.WriteLine(obj.id);

var localServer = "https://testing.speckle.dev";
var localToken = "5689faeedd5bc670c2d6d9c793d1cfd5186fbdcdf8";

var localAccount = new Account
{
  token = localToken,
  serverInfo = new ServerInfo { url = localServer }
};
var client = new Client(localAccount);

var stopwatch = new Stopwatch();
stopwatch.Start();

for (int i = 0; i < 1; i++)
{
  var projectId = await client
    .StreamCreate(new StreamCreateInput { name = $"test @ {DateTime.Now}" })
    .ConfigureAwait(false);

  if (projectId is null)
  {
    throw new Exception("failed to create project");
  }

  var targetServer = "https://testing.speckle.dev";

  localAccount.serverInfo.url = targetServer;

  var localServerTransport = new ServerTransport(localAccount, projectId);
  var objId = await Operations.Send(obj, new[] { localServerTransport }).ConfigureAwait(false);

  // var rec = await Operations.Receive(objId, localServerTransport, localTransport: null).ConfigureAwait(false);
  // Console.WriteLine(rec.id);
}

stopwatch.Stop();
Console.WriteLine($"took {stopwatch.ElapsedMilliseconds}");

client.Dispose();
