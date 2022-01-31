

using System.Diagnostics;
using Sender;

class RhinoHandler : IHandler
{
  public Task<IEnumerable<ISender>> CreateSenders()
  {
    throw new NotImplementedException();
  }

  public Task Setup()
  {
    // git clone the repo, get the rhino test c# project
    // run dotnet restore on the project??
    // run dotnet test, with the path to the project
    // for now we only support rhino 7 cause running 6 is a bit too hacky
    // we assume that every major version will be a separate dotnet test project
    throw new NotImplementedException();
  }

  public void Teardown()
  {
    throw new NotImplementedException();
  }
}

class RhinoSender : ISender
{
  public SendResult? SendResult => throw new NotImplementedException();

  public async Task<SendResult> Send()
  {
    // this should run dotnet test with the target project
    var process = Process.Start(
      "dotnet",
      "test /home/gergojedlicska/Speckle/speckle-sharp/IntegrationTests/TestFaker/TestFaker.csproj"
    );
    return new SendResult(true, "we've done it");
  }
}