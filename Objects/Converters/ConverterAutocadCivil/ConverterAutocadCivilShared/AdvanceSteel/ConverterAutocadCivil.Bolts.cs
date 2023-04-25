#if ADVANCESTEEL2023
using System;
using System.Collections.Generic;
using Autodesk.AdvanceSteel.Modelling;
using Objects.BuiltElements.AdvanceSteel;
using ASBoltPattern = Autodesk.AdvanceSteel.Modelling.BoltPattern;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    private IAsteelObject FilerObjectToSpeckle(ASBoltPattern bolt, List<string> notes)
    {
      AsteelBolt asteelBolt = bolt is CircleScrewBoltPattern ? (AsteelBolt)new AsteelCircularBolt() : (AsteelBolt)new AsteelRectangularBolt();

      SetDisplayValue(asteelBolt, bolt);

      return asteelBolt;
    }
  }
}

#endif
