using ConnectorGrasshopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Conversion
{
  public class ToSpeckleConverterAsync : GH_AsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("F1E5F78F-242D-44E3-AAD6-AB0257D69256"); }

    protected override System.Drawing.Bitmap Icon => Properties.Resources.ToSpeckle;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    private ISpeckleConverter Converter;

    private ISpeckleKit Kit;

    public ToSpeckleConverterAsync() : base("To Speckle", "To Speckle", "Convert data from Rhino to their Speckle Base equivalent.", "Speckle 2 Dev", "Conversion")
    {
      SetDefaultKitAndConverter();
      BaseWorker = new ToSpeckleWorker(Converter);
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:");

      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true, kit.Name == Kit.Name);
      }

      Menu_AppendSeparator(menu);
    }

    private void SetDefaultKitAndConverter()
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
        var x = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem;
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    private void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);
      Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);

      ((ToSpeckleWorker)BaseWorker).Converter = Converter;

      ExpireSolution(true);
    }

    public override bool Read(GH_IReader reader)
    {
      var kitName = "";
      reader.TryGetString("KitName", ref kitName);

      if (kitName != "")
      {
        try
        {
          SetConverterFromKit(kitName);
        }
        catch (Exception e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Could not find the {kitName} kit on this machine. Do you have it installed? \n Will fallback to the default one.");
          SetDefaultKitAndConverter();
        }
      }
      else
      {
        SetDefaultKitAndConverter();
      }

      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetString("KitName", Kit.Name);
      return base.Write(writer);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Objects", "O", "Objects to convert to Speckle Base.", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Objects", "O", "Converted Speckle objects.", GH_ParamAccess.item));
    }

    protected override void BeforeSolveInstance()
    {
      Tracker.TrackPageview("convert", "speckle");
      base.BeforeSolveInstance();
    }

  }

  public class ToSpeckleWorker : WorkerInstance
  {
    GH_Structure<IGH_Goo> Objects;
    GH_Structure<GH_SpeckleBase> ConvertedObjects;

    public ISpeckleConverter Converter { get; set; }

    public ToSpeckleWorker(ISpeckleConverter _Converter) : base(null)
    {
      Converter = _Converter;
      Objects = new GH_Structure<IGH_Goo>();
      ConvertedObjects = new GH_Structure<GH_SpeckleBase>();
    }

    public override WorkerInstance Duplicate() => new ToSpeckleWorker(Converter);

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

            var converted = Utilities.TryConvertItemToSpeckle(item, Converter) as Base;
            ConvertedObjects.Append(new GH_SpeckleBase { Value = converted }, Objects.Paths[branchIndex]);
            ReportProgress(Id, ((completed++ + 1) / (double)Objects.Count()));
          }

          branchIndex++;
        }

        Done();
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Log.CaptureException(e);
        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message);
        Parent.Message = "Error";
      }
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;

      DA.SetDataTree(0, ConvertedObjects);
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested) return;
      DA.DisableGapLogic();
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
