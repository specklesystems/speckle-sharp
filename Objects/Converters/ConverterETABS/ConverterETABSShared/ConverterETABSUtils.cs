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

        public bool[] RestraintToNative(Restraint restraint)
        {
            bool[] restraints = new bool[6];

            var code = restraint.code;

            int i = 0;
            foreach (char c in code)
            {
                restraints[i] = c.Equals('F') ? true : false; // other assume default of released 
                i++;
            }

            return restraints;
        }

        public Restraint RestraintToSpeckle(bool[] releases)
        {
            var code = new List<string>() { "R", "R", "R", "R", "R", "R" }; // default to free
            if (releases != null)
            {
                for (int i = 0; i < releases.Length; i++)
                {
                    if (releases[i]) code[i] = "F";
                }
            }

            var restraint = new Restraint(string.Join("", code));
            return restraint;
        }

        public enum ETABSConverterSupported
        {
            Node,
            Line,
            Element1D,
            Element2D,
            Model,
        }

        public enum ETABSAPIUsableTypes
        {
            Point,
            Frame,
            Area, // cAreaObj
            LoadPattern,
            Model,
            Column,
            Brace,
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
