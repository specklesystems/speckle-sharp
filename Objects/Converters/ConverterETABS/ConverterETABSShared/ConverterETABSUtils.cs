using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Geometry;
using ETABSv1;
using Objects.Structural.ETABS.Properties;
using Objects.Structural.ETABS.Analysis;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
    public string ModelUnits()
        {   
           
            var units = Model.GetDatabaseUnits();
            if (units != 0)
            {
                string[] unitsCat = units.ToString().Split('_');
                return unitsCat[1];
            }
            else
            {
                return null;
            }
}
    public ShellType ConvertShellType(eShellType eShellType)
        {
            ShellType shellType = new ShellType();

            switch (eShellType)
            {
                case eShellType.Membrane:
                    shellType = ShellType.Membrane;
                    break;
                case eShellType.ShellThick:
                    shellType = ShellType.ShellThick;
                    break;
                case eShellType.ShellThin:
                    shellType = ShellType.ShellThin;
                    break;
                case eShellType.Layered:
                    shellType = ShellType.Layered;
                    break;
                default:
                    shellType = ShellType.Null;
                    break;


            }

            return shellType;
        }
    public Restraint Restraint(bool[] releases)
        {
            Restraint restraint = new Restraint();
            restraint.stiffnessX = Convert.ToInt32(!releases[0]);
            restraint.stiffnessY = Convert.ToInt32(!releases[1]);
            restraint.stiffnessZ = Convert.ToInt32(!releases[2]);
            restraint.stiffnessXX = Convert.ToInt32(!releases[3]);
            restraint.stiffnessYY = Convert.ToInt32(!releases[4]);
            restraint.stiffnessZZ = Convert.ToInt32(!releases[5]);
            return restraint;

        }

        public enum ETABSConverterSupported {
        Element1D,
        Element2D,
        Model,
        }

        public enum ETABSAPIUsableTypes
    {
        Point = 1, // cPointObj
        Frame = 2, // cFrameObj
                   //Tendon = 3, 
        Area = 4, // cAreaObj
        //LoadPattern = 5,
        Model,
        //ColumnResults,
        //BeamResults,
        //BraceResults,
        //PierResults,
        //SpandrelResults
    }
}
}
