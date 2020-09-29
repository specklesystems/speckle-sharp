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

  public class ParamToSpeckleConverter : GH_Param<GH_SpeckleBase>
  {
    public override Guid ComponentGuid { get => new Guid("2092AF4C-51CD-4CB3-B297-5348C51FC49F"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public ISpeckleConverter Converter;

    public ParamToSpeckleConverter() : base("To Speckle", "⇒ SPK", "Converts objects to their Speckle equivalents.", "Speckle 2", "Conversion", GH_ParamAccess.item)
    {
      // TODO: Converter = new DefaultConverter();
      Converter = new ConverterRhinoGh();
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);

      // TODO: Add kit selectors

      Menu_AppendSeparator(menu);

      base.AppendAdditionalMenuItems(menu);
    }

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
