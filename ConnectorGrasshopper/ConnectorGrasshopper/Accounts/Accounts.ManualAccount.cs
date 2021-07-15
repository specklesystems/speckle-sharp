using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Accounts
{
  public class ManualAccountComponent : GH_TaskCapableComponent<Account>
  {
    CancellationTokenSource source;
    const int delay = 100000;

    /// <summary>
    /// Initializes a new instance of the Accounts class.
    /// </summary>
    public ManualAccountComponent()
      : base("Manual Account", "ManAcc", "Given a token this component will retrieve a specific account", ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS)
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Token", "T", "The token you created from the server", GH_ParamAccess.item);
      pManager.AddTextParameter("Url", "U", "The server address", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Account", "A", "Account to use with Speckle", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if(RunCount == 1)
      {
        source = new CancellationTokenSource(delay);
      }
      if (InPreSolve)
      {
        var token = "";
        DA.GetData(0, ref token);
        var url = "";
        DA.GetData(1, ref url);
        var task = Task.Run(async () =>
        {
          return await AccountManager.GetAccount(token, url);
        });
        TaskList.Add(task);
        return;
      }

      if (source.IsCancellationRequested)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Run out of time!");
      }
      else if (!GetSolveResults(DA, out Account account))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Couldn't solve this using parallel compute. If you disabled it please re-anable");
      }
      else
      {
        AccountManager.AddLocalAccount(account);
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Created: {account}");
        DA.SetData(0, account.userInfo.id);
      }
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        return Resources.ManualAccount;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("adf585df-b113-468d-9f6d-18028a984a95"); }
    }
  }
}
