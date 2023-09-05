using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CSiAPIv1;
using Speckle.BatchUploader.ClientSdk;

namespace ConnectorCSIShared
{
  public class CSIApplicationLifetimeController : IApplicationLifetimeController
  {
    private const string ProgID_SAP2000 = "CSI.SAP2000.API.SapObject";
    private const string ProgID_ETABS = "CSI.ETABS.API.ETABSObject";
    private const string ProgID_CSiBridge = "CSI.CSiBridge.API.SapObject";
    private const string ProgID_SAFE = "CSI.SAFE.API.SAFEObject";

    private readonly cHelper cHelper = new Helper();
    private readonly string programId;
    private readonly TaskCompletionSource<bool> programExited;
    private cOAPI cOAPI;

    public CSIApplicationLifetimeController(string programId)
    {
      this.programId = programId;
    }

    public async Task StartConnectorProcessAndWaitForExit()
    {
      cOAPI = cHelper.CreateObjectProgID(programId);
      cOAPI.ApplicationStart();

      await programExited.Task.ConfigureAwait(false);
    }
    public void StopConnectorProcess()
    {
      programExited.SetResult(true);
    }

    public static CSIApplicationLifetimeController ETABS => new(ProgID_ETABS);
    public static CSIApplicationLifetimeController SAP => new(ProgID_SAP2000);
  }
}
