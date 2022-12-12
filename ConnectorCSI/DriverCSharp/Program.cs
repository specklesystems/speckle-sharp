using System;
using System.Linq;
using System.Windows.Forms;
using CSiAPIv1;
using SpeckleConnectorCSI;

namespace DriverCSharp
{
  class Program
  {
    private const string ProgID_SAP2000 = "CSI.SAP2000.API.SapObject";
    private const string ProgID_ETABS = "CSI.ETABS.API.ETABSObject";
    private const string ProgID_CSIBridge = "CSI.CSiBridge.API.CSIBridgeObject";
    private const string ProgID_SAFE = "CSI.SAFE.API.SAFEObject";

    static int Main(string[] args)
    {
      //MessageBox.Show("Starting DriverCSharp");

      // dimension the SapObject as cOAPI type
      cOAPI mySapObject = null;

      // Use ret to check if functions return successfully (ret = 0) or fail (ret = nonzero)
      int ret = -1;

      // create API helper object
      cHelper myHelper = null;

      try
      {
        myHelper = new Helper();
      }
      catch (Exception ex)
      {
        MessageBox.Show("Cannot create an instance of the Helper object: " + ex.Message);
        ret = -1;
        return ret;
      }

      // attach to a running program instance 
      try
      {
        // get the active SapObject
        // determine program type
        string progID = null;
        string[] arguments = Environment.GetCommandLineArgs();

        if (arguments.Count() > 1)
        {
          string arg = arguments[1];
          if (string.Compare(arg, "SAP2000", true) == 0)
            progID = ProgID_SAP2000;
          else if (string.Compare(arg, "ETABS", true) == 0)
            progID = ProgID_ETABS;
          else if (string.Compare(arg, "SAFE", true) == 0)
            progID = ProgID_SAFE;
          else if (string.Compare(arg, "CSiBridge", true) == 0)
            progID = ProgID_SAFE;
        }

        if (progID != null)
          mySapObject = myHelper.GetObject(progID);
        else
        {
          // missing/unknown program type, try one by one
          try
          {
            progID = ProgID_SAP2000;
            mySapObject = myHelper.GetObject(progID);
          }
          catch (Exception ex)
          {
          }

          if (mySapObject == null)
          {
          try{ 
            progID = ProgID_ETABS;
            mySapObject = myHelper.GetObject(progID);
            }
            catch (Exception ex){ }
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show("No running instance of the program found or failed to attach: " + ex.Message);

        ret = -2;
        return ret;
      }

      // Get a reference to cSapModel to access all API classes and functions
      cSapModel mySapModel = mySapObject.SapModel;

      // call Speckle plugin
      try
      {
        cPlugin p = new cPlugin();
        cPluginCallback cb = new PluginCallback();

        // DO NOT return from SpeckleConnectorETABS.cPlugin.Main() until all work is done.
        p.Main(ref mySapModel, ref cb);
        if(cb.Finished == true)
        { Environment.Exit(0); }

        return cb.ErrorFlag;
      }
      catch (Exception ex)
      {
        MessageBox.Show("Failed to call plugin: " + ex.Message);

        ret = -3;
        return ret;
      }
    }
  }
}
