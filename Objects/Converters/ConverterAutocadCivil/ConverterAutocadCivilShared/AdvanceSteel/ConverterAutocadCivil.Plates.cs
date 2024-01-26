#if ADVANCESTEEL
using System.Collections.Generic;
using Objects.BuiltElements.AdvanceSteel;
using ASPlate = Autodesk.AdvanceSteel.Modelling.Plate;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;

namespace Objects.Converter.AutocadCivil;

public partial class ConverterAutocadCivil
{
  private IAsteelObject FilerObjectToSpeckle(ASPlate plate, List<string> notes)
  {
    AsteelPlate asteelPlate = new();

    plate.GetBaseContourPolygon(0, out ASPoint3d[] ptsContour);

    asteelPlate.outline = PolycurveToSpeckle(ptsContour);

    asteelPlate.area = plate.GetPaintArea();

    SetDisplayValue(asteelPlate, plate);

    return asteelPlate;
  }
}

#endif
