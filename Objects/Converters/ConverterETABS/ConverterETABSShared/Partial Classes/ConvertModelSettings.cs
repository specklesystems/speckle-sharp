using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Analysis;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public ModelSettings modelSettingsToSpeckle()
        {
            var speckleModelSettings = new ModelSettings();
            var units = Model.GetDatabaseUnits();
            var speckleModelUnits = new ModelUnits();
            speckleModelSettings.modelUnits = speckleModelUnits;
            if(units != 0)
            {
                string [] unitsCat = units.ToString().Split('_');
                speckleModelSettings.modelUnits.temperature = unitsCat[2];
                speckleModelSettings.modelUnits.length = unitsCat[1];
                speckleModelSettings.modelUnits.force = unitsCat[0];
            }
            return speckleModelSettings;
        }
                                
    }
}
