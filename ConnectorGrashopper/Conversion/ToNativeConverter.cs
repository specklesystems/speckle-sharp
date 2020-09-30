using GH_IO.Serialization;
using Grasshopper.Kernel;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace ConnectorGrashopper.Conversion
{
  public class ToNativeConverter : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("98027377-5A2D-4EBA-B8D4-D72872593CD8"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    private ISpeckleConverter Converter;

    private ISpeckleKit Kit;

    public ToNativeConverter() : base("To Native", "SPK ⇒", "Converts Speckle objects to their Grasshopper equivalents.", "Speckle 2", "Conversion")
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        Message = $"Using the {Kit.Name} Converter";
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
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

      Message = $"Using the {Kit.Name} Converter";
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
      pManager.AddGenericParameter("Objects", "O", "Objects you want to convert back to GH", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Converterd", "C", "Converted objects.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      object data = null;
      Base @object = null;
      DA.GetData(0, ref data);

      try
      {
        @object = data.GetType().GetProperty("Value").GetValue(data) as Base;
      }
      catch
      {
        @object = data as Base;
      }

      var canConvert = Converter.CanConvertToNative(@object);
      object conversionResult = null;

      if (canConvert)
      {
        conversionResult = Converter.ConvertToNative(@object);
      }

      if (conversionResult == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not convert object.");
      }

      DA.SetData(0, conversionResult);
    }

  }
}
