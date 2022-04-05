using System;
using System.Collections.Generic;
using System.Linq;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Objects;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Logging = Speckle.Core.Logging;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Conversion
{
  public class ToSpeckleConverterAsync : SelectKitAsyncComponentBase
  {
    public override Guid ComponentGuid { get => new Guid("F1E5F78F-242D-44E3-AAD6-AB0257D69256"); }

    protected override System.Drawing.Bitmap Icon => Properties.Resources.ToSpeckle;

    public override bool Obsolete => true;
    public override bool CanDisableConversion => false;

    public override GH_Exposure Exposure => GH_Exposure.hidden;


    public ToSpeckleConverterAsync() : base("To Speckle", "To Speckle", "Convert data from Rhino to their Speckle Base equivalent.", ComponentCategories.SECONDARY_RIBBON, ComponentCategories.CONVERSION)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Data to convert to Speckle Base objects.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Base", "B", "Converted Base Speckle objects.", GH_ParamAccess.item);
      //pManager.AddParameter(new SpeckleBaseParam("Base", "B", "Converted Base Speckle objects.", GH_ParamAccess.item));
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      BaseWorker = new ToSpeckleWorker(Converter, this);
    }
  }

  public class ToSpeckleWorker : WorkerInstance
  {
    GH_Structure<IGH_Goo> Objects;
    GH_Structure<IGH_Goo> ConvertedObjects;

    public ISpeckleConverter Converter { get; set; }

    public ToSpeckleWorker(ISpeckleConverter _Converter, GH_Component parent) : base(parent)
    {
      Converter = _Converter;
      Objects = new GH_Structure<IGH_Goo>();
      ConvertedObjects = new GH_Structure<IGH_Goo>();
    }

    public override WorkerInstance Duplicate() => new ToSpeckleWorker(Converter, Parent);

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      try
      {
        if (CancellationToken.IsCancellationRequested) return;

        int branchIndex = 0, completed = 0;
        foreach (var list in Objects.Branches)
        {
          var path = Objects.Paths[branchIndex];
          foreach (var item in list)
          {
            if (CancellationToken.IsCancellationRequested) return;

            var converted = Utilities.TryConvertItemToSpeckle(item, Converter, true);
            if (converted == null)
              RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, $"Cannot convert item at {path}[{list.IndexOf(item)}] to Speckle."));
            else if (converted.GetType().IsSimpleType())
              ConvertedObjects.Append(new GH_ObjectWrapper(converted));
            else
              ConvertedObjects.Append(new GH_SpeckleBase { Value = converted as Base }, Objects.Paths[branchIndex]);
            ReportProgress(Id, Math.Round((completed++ + 1) / (double)Objects.Count(), 2));
          }

          branchIndex++;
        }

      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Logging.Log.CaptureException(e);
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message));
        Parent.Message = "Error";
      }

      Done();
    }
    List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new List<(GH_RuntimeMessageLevel, string)>();

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;

      // Report all conversion errors as warnings
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

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested) return;
      DA.DisableGapLogic();
      if (DA.Iteration == 0)
      {
        Logging.Analytics.TrackEvent(Logging.Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Convert To Speckle" } });
      }


      GH_Structure<IGH_Goo> _objects;
      DA.GetDataTree(0, out _objects);

      var branchIndex = 0;
      foreach (var list in _objects.Branches)
      {
        var path = _objects.Paths[branchIndex];
        foreach (var item in list)
        {
          Objects.Append(item, path);
        }
        branchIndex++;
      }
    }
  }

}
