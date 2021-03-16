using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Conversion
{
  public class ToNativeConverterAsync : GH_AsyncComponent
  {
    public override Guid ComponentGuid
    {
      get => new Guid("98027377-5A2D-4EBA-B8D4-D72872593CD8");
    }

    protected override Bitmap Icon => Properties.Resources.ToNative;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    private ISpeckleConverter Converter;

    private ISpeckleKit Kit;

    public ToNativeConverterAsync() : base("To Native", "To Native",
      "Convert data from Speckle's Base object to it`s Dynamo equivalent.", ComponentCategories.SECONDARY_RIBBON, ComponentCategories.CONVERSION)
    {
      SetDefaultKitAndConverter();
      BaseWorker = new ToNativeWorker(Converter, this);
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:");

      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true,
          kit.Name == Kit.Name);
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
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    private void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name)
      {
        return;
      }

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);
      Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);

      ((ToNativeWorker)BaseWorker).Converter = Converter;

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
        catch (Exception)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            $"Could not find the {kitName} kit on this machine. Do you have it installed? \n Will fallback to the default one.");
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
      pManager.AddParameter(new SpeckleBaseParam("Base", "B",
        "Speckle Base objects to convert to Grasshopper.", GH_ParamAccess.tree));
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Converted data in GH native format.", GH_ParamAccess.tree);
    }

    protected override void BeforeSolveInstance()
    {
      Tracker.TrackPageview(Tracker.CONVERT_TONATIVE);
      base.BeforeSolveInstance();
    }
  }

  public class ToNativeWorker : WorkerInstance
  {
    GH_Structure<GH_SpeckleBase> Objects;
    GH_Structure<IGH_Goo> ConvertedObjects;

    public ISpeckleConverter Converter { get; set; }

    public ToNativeWorker(ISpeckleConverter _Converter, GH_Component parent) : base(parent)
    {
      Converter = _Converter;
      Objects = new GH_Structure<GH_SpeckleBase>();
      ConvertedObjects = new GH_Structure<IGH_Goo>();
    }

    public override WorkerInstance Duplicate() => new ToNativeWorker(Converter, Parent);

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      try
      {
        if (CancellationToken.IsCancellationRequested)
        {
          return;
        }

        int branchIndex = 0, completed = 0;
        foreach (var list in Objects.Branches)
        {
          var path = Objects.Paths[branchIndex];
          foreach (var item in list)
          {
            if (CancellationToken.IsCancellationRequested)
            {
              return;
            }

            var converted = Utilities.TryConvertItemToNative(item?.Value, Converter);
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
        Log.CaptureException(e);
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

      GH_Structure<GH_SpeckleBase> _objects;
      DA.GetDataTree(0, out _objects);

      var branchIndex = 0;
      foreach (var list in _objects.Branches)
      {
        var path = _objects.Paths[branchIndex];
        foreach (var item in list)
        {
          if(!item.IsValid) 
             RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, $"Item at path {path}[{list.IndexOf(item)}] is not a Base object."));
          Objects.Append(item, path);
        }

        branchIndex++;
      }
    }
  }
}
