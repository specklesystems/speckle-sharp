using ConnectorGrashopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnectorGrashopper.Conversion
{
  public class ToSpeckleConverterAsync : GH_AsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("F1E5F78F-242D-44E3-AAD6-AB0257D69256"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    private ISpeckleConverter Converter;

    private ISpeckleKit Kit;

    public ToSpeckleConverterAsync() : base("To Speckle Async", "⇒ SPK", "Converts objects to their Speckle equivalents.", "Speckle 2", "Conversion")
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }

      Worker = new ToSpeckleWorker(Converter);
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

    private void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);

      ((ToSpeckleWorker)Worker).Converter = Converter;

      ExpireSolution(true);
    }

    public override bool Read(GH_IReader reader)
    {
      // TODO: Read kit name and instantiate converter
      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      // TODO: Write kit name to disk
      return base.Write(writer);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Objects", "O", "Objects you want to convert to Speckle", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Converterd", "C", "Converted objects.", GH_ParamAccess.list);
    }

  }

  public class ToSpeckleWorker : IAsyncComponentWorker
  {

    List<IGH_Goo> Objects;
    List<object> ConvertedObjects;

    public ISpeckleConverter Converter { get; set; }

    public ToSpeckleWorker(ISpeckleConverter _Converter)
    {
      Converter = _Converter;
      Objects = new List<IGH_Goo>();
      ConvertedObjects = new List<object>();
    }

    public IAsyncComponentWorker GetNewInstance()
    {
      return new ToSpeckleWorker(this.Converter);
    }

    public void CollectData(IGH_DataAccess DA)
    {
      var data = new List<IGH_Goo>();
      DA.GetDataList(0, data);

      Objects = data;
    }

    public void DoWork(CancellationToken token, Action<string> ReportProgress, Action SetData)
    {
      if (token.IsCancellationRequested)
      {
        Debug.Write("Task cancelled before it got started...");
        return;
      }

      for (int i = 0; i <= Objects.Count - 1; i++)
      {
        if (token.IsCancellationRequested) return;

        ConvertedObjects.Add(TryConvertItem(Objects[i]));

        ReportProgress(((double)(i + 1) / (double)Objects.Count).ToString("0.00%"));
      }

      SetData();

    }

    public void SetData(IGH_DataAccess DA)
    {
      DA.SetDataList(0, ConvertedObjects);
    }

    private object TryConvertItem(object value)
    {
      object result = null;

      if (value is Grasshopper.Kernel.Types.IGH_Goo)
      {
        value = value.GetType().GetProperty("Value").GetValue(value);
      }
      else if (value is Base || Speckle.Core.Models.Utilities.IsSimpleType(value.GetType()))
      {
        return value;
      }

      if (Converter.CanConvertToSpeckle(value))
      {
        return new GH_SpeckleBase() { Value = Converter.ConvertToSpeckle(value) };
      }

      return result;
    }
  }

}
