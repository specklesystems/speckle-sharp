using Grasshopper.Kernel;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace ConnectorGrashopper.Extras
{
  /// <summary>
  /// Inherit from this class to create an asyncronous component that executes its tasks outside the UI thread.
  /// </summary>
  public abstract class GH_AsyncComponent : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("F1E5F78F-242D-44E3-AAD6-AB0257D69256"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public IAsyncComponentWorker Worker;

    IAsyncComponentWorker CurrentWorker;

    Task CurrentRun;

    ConcurrentBag<CancellationTokenSource> TokenSources = new ConcurrentBag<CancellationTokenSource>();

    Action<string> ReportProgress;

    Action SetData;

    int State = 0;

    Timer DisplayProgressTimer;

    protected GH_AsyncComponent(string name, string nickname, string description, string category, string subCategory) : base(name, nickname, description, category, subCategory)
    {

      DisplayProgressTimer = new Timer(333) { AutoReset = false };
      DisplayProgressTimer.Elapsed += (s, e) =>
      {
        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
        {
          OnDisplayExpired(true);
        });
      };

      ReportProgress = (progress) =>
      {
        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
        {
          Message = progress;
          if (!DisplayProgressTimer.Enabled) DisplayProgressTimer.Start();
        });
      };

      SetData = () =>
      {
        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
        {
          State = 1;
          ExpireSolution(true);
        });
      };
    }


    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (State == 0)
      {
        if (Worker == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Worker class not provided.");
          return;
        }

        CurrentWorker = Worker.GetNewInstance();
        if (CurrentWorker == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get a worker instance.");
          return;
        }

        // Request the cancellation of any old tasks.
        CancellationTokenSource oldTokenSource;
        while (TokenSources.TryTake(out oldTokenSource))
        {
          oldTokenSource?.Cancel();
        }

        // Let the worker collect data.
        CurrentWorker.CollectData(DA);

        // Create the task
        var tokenSource = new CancellationTokenSource();
        CurrentRun = new Task(() => CurrentWorker.DoWork(tokenSource.Token, ReportProgress, SetData), tokenSource.Token);

        // Add cancelation source to our bag
        TokenSources.Add(tokenSource);
        CurrentRun.Start();
        return;
      }

      OnDisplayExpired(true);
      CurrentWorker.SetData(DA);
      State = 0;
    }
  }

  public interface IAsyncComponentWorker
  {

    /// <summary>
    /// This function should return a duplicate instance of your class.
    /// </summary>
    /// <returns></returns>
    IAsyncComponentWorker GetNewInstance();

    /// <summary>
    /// Here you can safely set the data of your component, just like you would normally.
    /// </summary>
    /// <param name="DA"></param>
    void SetData(IGH_DataAccess DA);

    /// <summary>
    /// Here you can safely collect the data from your component, just like you would normally.
    /// </summary>
    /// <param name="DA"></param>
    void CollectData(IGH_DataAccess DA);

    /// <summary>
    /// This where the computation happens. Make sure to check the cancellation token often! 
    /// </summary>
    /// <param name="token"></param>
    /// <param name="ReportProgress"></param>
    /// <param name="SetData">When you are done computing, call this function to have the parent component invoke the SetData function.</param>
    void DoWork(CancellationToken token, Action<string> ReportProgress, Action SetData);

  }
}
