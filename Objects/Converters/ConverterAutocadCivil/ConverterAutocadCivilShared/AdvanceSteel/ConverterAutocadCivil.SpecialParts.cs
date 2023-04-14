#if ADVANCESTEEL2023
using System.Collections.Generic;
using Objects.BuiltElements.AdvanceSteel;
using ASSpecialPart = Autodesk.AdvanceSteel.Modelling.SpecialPart;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    private IAsteelObject FilerObjectToSpeckle(ASSpecialPart specialPart, List<string> notes)
    {
      AsteelSpecialPart asteelSpecialPart = new AsteelSpecialPart();

      SetDisplayValue(asteelSpecialPart, specialPart);

      return asteelSpecialPart;
    }
  }
}

#endif
