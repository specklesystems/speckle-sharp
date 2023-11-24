#if ADVANCESTEEL
using System.Collections.Generic;

using Objects.BuiltElements.AdvanceSteel;
using ASGrating = Autodesk.AdvanceSteel.Modelling.Grating;
namespace Objects.Converter.AutocadCivil;

public partial class ConverterAutocadCivil
{
  private IAsteelObject FilerObjectToSpeckle(ASGrating grating, List<string> notes)
  {
    AsteelGrating asteelGrating = new();

    SetDisplayValue(asteelGrating, grating);

    return asteelGrating;
  }
}

#endif
