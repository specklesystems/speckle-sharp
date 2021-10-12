using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Geometry;
using ETABSv1;
using Objects.Structural.ETABS.Properties;
using Objects.Structural.ETABS.Analysis;
using System.Linq;

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
        public static List<string> GetAllAreaNames(cSapModel model)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                model.AreaObj.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllWallNames(cSapModel model)
        {
            var WallNames = GetAllAreaNames(model);

            List<string> WallName = new List<string>();

            string wallLabel = "";
            string wallStory = "";

            foreach (var wallName in WallNames)
            {
                model.AreaObj.GetLabelFromName(wallName, ref wallLabel, ref wallStory);

                if (wallLabel.ToLower().StartsWith("w"))
                {
                    WallName.Add(wallName);
                }
            }

            return WallName;
        }
        public static List<string> GetAllFloorNames(cSapModel model)
        {
            var FloorNames = GetAllAreaNames(model);

            List<string> FloorName = new List<string>();

            string FloorLabel = "";
            string FloorStory = "";

            foreach (var floorName in FloorNames)
            {
                model.AreaObj.GetLabelFromName(floorName, ref FloorLabel, ref FloorStory);

                if (FloorLabel.ToLower().StartsWith("f"))
                {
                    FloorName.Add(floorName);
                }
            }

            return FloorName;
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
        Column,
        Beam,
        Floor,
        Wall
        //ColumnResults,
        //BeamResults,
        //BraceResults,
        //PierResults,
        //SpandrelResults
    }
}
}
