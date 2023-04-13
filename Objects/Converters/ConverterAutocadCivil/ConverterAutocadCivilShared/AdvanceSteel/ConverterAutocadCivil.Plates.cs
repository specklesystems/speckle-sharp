#if ADVANCESTEEL2023
using System.Collections.Generic;
using Objects.BuiltElements.AdvanceSteel;
using ASPlate = Autodesk.AdvanceSteel.Modelling.Plate;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    private IAsteelObject FilerObjectToSpeckle(ASPlate plate, List<string> notes)
    {
      AsteelSlab asteelSlab = new AsteelSlab();

      plate.GetBaseContourPolygon(0, out ASPoint3d[] ptsContour);

      asteelSlab.outline = PolycurveToSpeckle(ptsContour);

      asteelSlab.area = plate.GetPaintArea();

      SetDisplayValue(asteelSlab, plate);

      return asteelSlab;
    }
  }
}

#endif
