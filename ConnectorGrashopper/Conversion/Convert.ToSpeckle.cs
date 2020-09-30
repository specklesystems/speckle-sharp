using ConnectorGrashopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Speckle.Core.Kits;
using System;
using System.Linq;
using System.Windows.Forms;

namespace ConnectorGrashopper.Conversion
{
  // TODO: Convert to task capable component / async so as to not block the ffffing ui
  public class ToSpeckleConverter : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("2092AF4C-51CD-4CB3-B297-5348C51FC49F"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    private ISpeckleConverter Converter;

    private ISpeckleKit Kit;

    public ToSpeckleConverter() : base("To Speckle", "⇒ SPK", "Converts objects to their Speckle equivalents.", "Speckle 2", "Conversion")
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        Message = $"Using the \n{Kit.Name}\n Kit Converter";
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

      Message = $"Using the \n{Kit.Name}\n Kit Converter";
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
      pManager.AddGenericParameter("Objects", "O", "Objects you want to convert to Speckle", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Converterd", "C", "Converted objects.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      object data = null;
      object @object = null;
      DA.GetData(0, ref data);

      try
      {
        @object = data.GetType().GetProperty("Value").GetValue(data);
      }
      catch
      {
        @object = data;
      }

      var canConvert = Converter.CanConvertToSpeckle(@object);
      object conversionResult = null;

      if (canConvert)
      {
        conversionResult = new GH_SpeckleBase() { Value = Converter.ConvertToSpeckle(@object) };
      }

      DA.SetData(0, conversionResult);
    }

  }
}
