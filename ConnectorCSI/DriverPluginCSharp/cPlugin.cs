using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CSiAPIv1;

namespace SpeckleConnector;

public class cPlugin
{
  private cSapModel m_SapModel;
  private cPluginCallback m_PluginCallback;
  private Process connectorProcess;

  public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
  {
    m_SapModel = SapModel;
    m_PluginCallback = ISapPlugin;

    string appPath = (new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
    appPath = System.IO.Path.GetDirectoryName(appPath);
    appPath = System.IO.Path.Combine(appPath, "DriverCSharp.exe");

    Process currentProcess = Process.GetCurrentProcess();
    string guiType = System.IO.Path.GetFileNameWithoutExtension(currentProcess.ProcessName);

    connectorProcess = new Process();
    {
      var withBlock = connectorProcess.StartInfo;
      withBlock.CreateNoWindow = true;
      withBlock.FileName = appPath;
      withBlock.UseShellExecute = false;
      withBlock.Arguments = guiType;
    }

    connectorProcess.Start();

    // wait a few seconds for the Speckle connector to launch before showing the "plugin successful" UI
    Task.Delay(4000).Wait();

    // we need to immediately call this or else the program UI will be blocked by the connector process which will cause API calls to hang
    m_PluginCallback.Finish(0);

    // setup events
    connectorProcess.EnableRaisingEvents = true;
    connectorProcess.Exited += ConnectorProcess_Exited;
    System.Windows.Forms.Application.ApplicationExit += Application_ApplicationExit;
  }

  private void ConnectorProcess_Exited(object sender, EventArgs e)
  {
    // unsubscribe from the event because the "closeMainWindow" will throw an error if the process has already exited
    System.Windows.Forms.Application.ApplicationExit -= Application_ApplicationExit;
  }

  private void Application_ApplicationExit(object sender, EventArgs e)
  {
    connectorProcess?.CloseMainWindow();
  }

  public int Info(ref string Text)
  {
    Text = "This is a Speckle plugin for CSI Products";
    return 0;
  }
}
