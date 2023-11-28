#if ADVANCESTEEL
using System.Collections.Generic;
using Objects.BuiltElements.AdvanceSteel;
using ASSlab = Autodesk.AdvanceSteel.Modelling.Slab;

namespace Objects.Converter.AutocadCivil;

public partial class ConverterAutocadCivil
{
  private IAsteelObject FilerObjectToSpeckle(ASSlab slab, List<string> notes)
  {
    AsteelSlab asteelSlab = new();

    SetDisplayValue(asteelSlab, slab);

    return asteelSlab;
  }
}

#endif
