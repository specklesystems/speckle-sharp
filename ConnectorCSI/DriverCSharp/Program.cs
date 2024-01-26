using System;
using Speckle.Core.Logging;
using System.Windows.Forms;
using CSiAPIv1;
using SpeckleConnectorCSI;

#if DEBUG
using System.Diagnostics;
#endif

namespace DriverCSharp;

class Program
{
  private const string ProgID_SAP2000 = "CSI.SAP2000.API.SapObject";
  private const string ProgID_ETABS = "CSI.ETABS.API.ETABSObject";
  private const string ProgID_CSiBridge = "CSI.CSiBridge.API.SapObject";
  private const string ProgID_SAFE = "CSI.SAFE.API.SAFEObject";

  static int Main(string[] args)
  {
    try
    {
#if DEBUG
      Debugger.Launch();
#endif
      //MessageBox.Show("Starting DriverCSharp");

      // dimension the SapObject as cOAPI type
      cOAPI mySapObject = null;

      // Use ret to check if functions return successfully (ret = 0) or fail (ret = nonzero)
      int ret = -1;

      // create API helper object
      cHelper myHelper = null;

      myHelper = new Helper();

      // attach to a running program instance

      // get the active SapObject
      // determine program type
      string progID = null;
      string[] arguments = Environment.GetCommandLineArgs();

      if (arguments.Length > 1)
      {
        string arg = arguments[1];
        if (string.Equals(arg, "SAP2000", StringComparison.CurrentCultureIgnoreCase))
        {
          progID = ProgID_SAP2000;
        }
        else if (string.Equals(arg, "ETABS", StringComparison.CurrentCultureIgnoreCase))
        {
          progID = ProgID_ETABS;
        }
        else if (string.Equals(arg, "SAFE", StringComparison.CurrentCultureIgnoreCase))
        {
          progID = ProgID_SAFE;
        }
        else if (string.Equals(arg, "CSiBridge", StringComparison.CurrentCultureIgnoreCase))
        {
          progID = ProgID_CSiBridge;
        }
      }

      if (progID != null)
      {
        mySapObject = myHelper.GetObject(progID);
      }
      else
      {
        // missing/unknown program type, try one by one
        progID = ProgID_SAP2000;
        mySapObject = myHelper.GetObject(progID);

        if (mySapObject == null)
        {
          progID = ProgID_ETABS;
          mySapObject = myHelper.GetObject(progID);
        }
        if (mySapObject == null)
        {
          progID = ProgID_CSiBridge;
          mySapObject = myHelper.GetObject(progID);
        }
      }

      if (mySapObject is null)
      {
        MessageBox.Show("No running instance of the program found");

        ret = -2;
        return ret;
      }

      // Get a reference to cSapModel to access all API classes and functions
      cSapModel mySapModel = mySapObject.SapModel;

      // call Speckle plugin
      cPlugin p = new();
      cPluginCallback cb = new PluginCallback();

      // DO NOT return from SpeckleConnectorETABS.cPlugin.Main() until all work is done.
      p.Main(ref mySapModel, ref cb);
      if (cb.Finished == true)
      {
        Environment.Exit(0);
      }

      return cb.ErrorFlag;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to initialize plugin");
      MessageBox.Show("Failed to initialize plugin: " + ex.Message);
      return -3;
    }
  }
}
