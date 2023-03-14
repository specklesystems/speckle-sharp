using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace ConnectorGrasshopper
{
  public class DeserializeTaskCapableComponent: GH_SpeckleTaskCapableComponent<Base>
  {
    internal static Guid internalGuid => new Guid("0336F3D1-2FEE-4B66-980D-63DB624980C9");
    internal static GH_Exposure internalExposure => SpeckleGHSettings.ShowDevComponents ? GH_Exposure.secondary : GH_Exposure.hidden;

    public override Guid ComponentGuid => internalGuid;
    public override GH_Exposure Exposure => internalExposure;
    
    private CancellationTokenSource source;
    protected override System.Drawing.Bitmap Icon => Properties.Resources.Deserialize;

    static DeserializeTaskCapableComponent()
    {
      SpeckleGHSettings.SettingsChanged += (_, args) =>
      {
        if (args.Key != SpeckleGHSettings.SHOW_DEV_COMPONENTS) return;
        
        var proxy = Grasshopper.Instances.ComponentServer.ObjectProxies.FirstOrDefault(p => p.Guid == internalGuid);
        if (proxy == null) return;
        proxy.Exposure = internalExposure;
      };
    }
    public DeserializeTaskCapableComponent() : base("Deserialize", "Deserialize",
      "Deserializes a JSON string to a Speckle Base object.", ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.CONVERSION)
    {
    }    
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Json", "J", "Serialized base objects in JSON format.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Base", "B", "Deserialized Speckle Base objects.", GH_ParamAccess.item));
    }

    public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        if (RunCount == 1)
          source = new CancellationTokenSource();

        string item = null;
        DA.GetData(0, ref item);
        var task = Task.Run(() => DoWork(item, DA), source.Token);
        TaskList.Add(task);
        return;
      }

      if (source.IsCancellationRequested || !GetSolveResults(DA, out var data))
      {
        DA.AbortComponentSolution();
        return;
      }

      DA.SetData(0, data);
    }

    private Base DoWork(string item, IGH_DataAccess DA)
    {
      if (string.IsNullOrEmpty(item))
        return null;
      
      try
      {
        return Operations.Deserialize(item);
      }
      catch (Exception e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          $"Cannot deserialize object at path {{{DA.ParameterTargetPath(0)}}}[{DA.ParameterTargetIndex(0)}]: {e.ToFormattedString()}");
        return null;
      }
    }
  }
}
