using System;
using System.Threading.Tasks;
using System.Diagnostics;

using CSiAPIv1;

namespace SpeckleConnector
{
  public class cPlugin
  {
    private cSapModel m_SapModel;
    private cPluginCallback m_PluginCallback;

    public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
    {
      m_SapModel = SapModel;
      m_PluginCallback = ISapPlugin;

      string appPath = (new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
      appPath = System.IO.Path.GetDirectoryName(appPath);
      appPath = System.IO.Path.Combine(appPath, "DriverCSharp.exe");

      Process currentProcess = Process.GetCurrentProcess();
      string guiType = System.IO.Path.GetFileNameWithoutExtension(currentProcess.ProcessName);

      Process driver = new Process();
      {
        var withBlock = driver.StartInfo;
        withBlock.CreateNoWindow = true;
        withBlock.FileName = appPath;
        withBlock.UseShellExecute = false;
        withBlock.Arguments = guiType;
      }

      Task.Run(() =>
      {
        driver.Start();
        driver.WaitForExit();

        m_PluginCallback.Finish(driver.ExitCode);
      }
  );
    }

    public int Info(ref string Text)
    {
      Text = "This is a Speckle plugin for CSI Products";
      return 0;
    }
  }
}
