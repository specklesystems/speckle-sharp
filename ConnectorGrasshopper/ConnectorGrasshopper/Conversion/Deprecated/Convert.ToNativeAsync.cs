using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ConnectorGrasshopper.Objects;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Logging = Speckle.Core.Logging;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Conversion
{
  public class ToNativeConverterAsync : SelectKitAsyncComponentBase
  {
    public override Guid ComponentGuid => new Guid("98027377-5A2D-4EBA-B8D4-D72872593CD8");

    protected override Bitmap Icon => Properties.Resources.ToNative;

    public override bool CanDisableConversion => false;
    public override bool Obsolete => true;

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public ToNativeConverterAsync() : base("To Native", "To Native",
      "Convert data from Speckle's Base object to its Rhino equivalent.", ComponentCategories.SECONDARY_RIBBON, ComponentCategories.CONVERSION)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Base", "B",
        "Speckle Base objects to convert to Grasshopper.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Converted data in GH native format.", GH_ParamAccess.tree);
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      BaseWorker = new ToNativeWorker(Converter, this);
    }
  }

  public class ToNativeWorker : WorkerInstance
  {
    GH_Structure<GH_ObjectWrapper> Objects;
    GH_Structure<IGH_Goo> ConvertedObjects;

    public ISpeckleConverter Converter { get; set; }

    public ToNativeWorker(ISpeckleConverter _Converter, GH_Component parent) : base(parent)
    {
      Converter = _Converter;
      Objects = new GH_Structure<GH_ObjectWrapper>();
      ConvertedObjects = new GH_Structure<IGH_Goo>();
    }

    public override WorkerInstance Duplicate() => new ToNativeWorker(Converter, Parent);

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      try
      {
        if (CancellationToken.IsCancellationRequested)
          return;

        int branchIndex = 0, completed = 0;
        foreach (var list in Objects.Branches)
        {
          var path = Objects.Paths[branchIndex];
          foreach (var item in list)
          {
            if (CancellationToken.IsCancellationRequested)
              return;
            var converted = Utilities.TryConvertItemToNative(item?.Value, Converter, true);
            ConvertedObjects.Append(converted, path);
            ReportProgress(Id, (completed++ + 1) / (double)Objects.Count());
          }

          branchIndex++;
        }

        Done();
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Logging.Log.CaptureException(e);
        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message);
        Parent.Message = "Error";
      }
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        return;
      }

      foreach (var error in Converter.Report.ConversionErrors)
      {
        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, error.Message + ": " + error.InnerException?.Message);
      }

      foreach (var (level, message) in RuntimeMessages)
      {
        Parent.AddRuntimeMessage(level, message);
      }

      DA.SetDataTree(0, ConvertedObjects);
    }

    List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new List<(GH_RuntimeMessageLevel, string)>();

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        return;
      }

      if (DA.Iteration == 0)
        (Parent as GH_SpeckleAsyncComponent)?.Tracker.TrackNodeRun("Convert To Native");



      GH_Structure<IGH_Goo> _objects;
      DA.GetDataTree(0, out _objects);

      var branchIndex = 0;
      foreach (var list in _objects.Branches)
      {
        var path = _objects.Paths[branchIndex];
        foreach (var item in list)
        {
          if (!item.IsValid)
            RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, $"Item at path {path}[{list.IndexOf(item)}] is not a Base object."));
          var scriptVariable = item.ScriptVariable();
          Objects.Append(new GH_ObjectWrapper(scriptVariable), path);
        }

        branchIndex++;
      }
    }
  }
}
