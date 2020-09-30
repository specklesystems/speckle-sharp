using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnectorGrashopper.Extras
{
  public class Param_ToNativeConverter : GH_Param<IGH_Goo>
  {
    public override Guid ComponentGuid { get => new Guid("AA4565F5-A9B2-4A0F-B7E5-7CDBB2826B4F"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    private ISpeckleConverter Converter;

    private ISpeckleKit Kit;

    public Param_ToNativeConverter() : base("To Native", "SPK ⇒", "Converts speckle objects to their Grasshopper equivalents.", "Speckle 2", "Conversion", GH_ParamAccess.item)
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
    /// 
    protected override IGH_Goo PreferredCast(object data)
    {
      Base @object = null;

      try
      {
        @object = data.GetType().GetProperty("Value").GetValue(data) as Base;
      }
      catch
      {
        @object = data as Base;
      }

      var canConvert = Converter.CanConvertToNative(@object);

      if (canConvert)
      {
        return new GH_SpeckleBase() { Value = Converter.ConvertToSpeckle(@object) };
      }

      return null;
    }
  }
}
