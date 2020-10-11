using ConnectorGrashopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace ConnectorGrashopper.Conversion
{
  public class ToNativeConverterAsync : GH_AsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("98027377-5A2D-4EBA-B8D4-D72872593CD8"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    private ISpeckleConverter Converter;

    private ISpeckleKit Kit;

    public ToNativeConverterAsync() : base("To Native", "To Native", "Converts Speckle objects to their Grasshopper equivalents.", "Speckle 2", "Conversion")
    {
      SetDefaultKitAndConverter();
      BaseWorker = new ToNativeWorker(Converter);
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
      pManager.AddGenericParameter("Objects", "O", "Objects you want to convert back to GH", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Converterd", "C", "Converted objects.", GH_ParamAccess.tree);
    }

  }

  public class ToNativeWorker : WorkerInstance
  {
    GH_Structure<IGH_Goo> Objects;
    GH_Structure<GH_SpeckleGoo> ConvertedObjects;

    public ISpeckleConverter Converter { get; set; }

    public ToNativeWorker(ISpeckleConverter _Converter)
    {
      Converter = _Converter;
      Objects = new GH_Structure<IGH_Goo>();
      ConvertedObjects = new GH_Structure<GH_SpeckleGoo>();
    }

    public override WorkerInstance Duplicate() => new ToNativeWorker(Converter);

    private object TryConvertItem(object value)
    {
      object result = null;

      if (value is Grasshopper.Kernel.Types.IGH_Goo)
      {
        value = value.GetType().GetProperty("Value").GetValue(value);
      }

      if (Converter.CanConvertToNative(value as Base))
      {
        return Converter.ConvertToNative(value as Base);
      }

      return result;
    }

    public override void DoWork(Action<string, double> ReportProgress, Action<string, GH_RuntimeMessageLevel> ReportError, Action Done)
    {
      if (CancellationToken.IsCancellationRequested) return;

      int branchIndex = 0, completed = 0;
      foreach (var list in Objects.Branches)
      {
        var path = Objects.Paths[branchIndex];
        foreach (var item in list)
        {
          if (CancellationToken.IsCancellationRequested) return;

          var converted = TryConvertItem(item);
          ConvertedObjects.Append(new GH_SpeckleGoo() { Value = converted }, Objects.Paths[branchIndex]);
          ReportProgress(Id, ((completed++ + 1) / (double)Objects.Count()));
        }

        branchIndex++;
      }

      Done();
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;
      DA.SetDataTree(0, ConvertedObjects);
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested) return;

      GH_Structure<IGH_Goo> _objects;
      DA.GetDataTree(0, out _objects);

      int branchIndex = 0;
      foreach (var list in _objects.Branches)
      {
        var path = _objects.Paths[branchIndex];
        foreach (var item in list)
        {
          Objects.Append(item, _objects.Paths[branchIndex]);
        }
        branchIndex++;
      }
    }
  }
}
