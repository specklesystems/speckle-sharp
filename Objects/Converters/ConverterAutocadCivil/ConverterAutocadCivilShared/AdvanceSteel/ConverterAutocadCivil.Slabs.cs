#if ADVANCESTEEL2023
using System.Collections.Generic;
using Objects.BuiltElements.AdvanceSteel;
using ASSlab = Autodesk.AdvanceSteel.Modelling.Slab;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    private IAsteelObject FilerObjectToSpeckle(ASSlab slab, List<string> notes)
    {
      AsteelGrating asteelGrating = new AsteelGrating();

      SetDisplayValue(asteelGrating, slab);

      SetUnits(asteelGrating);

      return asteelGrating;
    }
  }
}

#endif
