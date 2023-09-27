namespace Speckle.BatchUploader.Sdk.Interfaces;

public interface IApplicationLifetimeController
{
  public Task StartConnectorProcessAndWaitForExit();
  public void StopConnectorProcess();
}
