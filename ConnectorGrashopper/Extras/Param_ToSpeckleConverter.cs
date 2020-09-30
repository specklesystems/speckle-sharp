using Grasshopper.Kernel;
using Speckle.Converter.RhinoGh;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnectorGrashopper.Extras
{
  // TODO:
  // - Add Converter selection mechanism
  // - Persist Converter selection on writes/reads

  public class Param_ToSpeckleConverter : GH_Param<GH_SpeckleBase>
  {
    public override Guid ComponentGuid { get => new Guid("2092AF4C-51CD-4CB3-B297-5348C51FC49F"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    private ISpeckleConverter Converter;

    private ISpeckleKit Kit;

    public Param_ToSpeckleConverter() : base("To Speckle", "⇒ SPK", "Converts objects to their Speckle equivalents.", "Speckle 2", "Conversion", GH_ParamAccess.item)
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
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:");

      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} (${kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true, kit.Name == Kit.Name);
      }

      Menu_AppendSeparator(menu);
      Menu_AppendFlattenParameter(menu);
      Menu_AppendGraftParameter(menu);
    }

    private void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);

      ExpireSolution(true);
    }

    /// <summary>
    /// This is where we actually enforce the usage of the Speckle converters. 
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    protected override GH_SpeckleBase PreferredCast(object data)
    {
      object @object = null;

      try
      {
        @object = data.GetType().GetProperty("Value").GetValue(data);
      }
      catch
      {
        @object = data;
      }

      var canConvert = Converter.CanConvertToSpeckle(@object);

      if (canConvert)
      {
        return new GH_SpeckleBase() { Value = Converter.ConvertToSpeckle(@object) };
      }

      return null;
    }

  }
}
