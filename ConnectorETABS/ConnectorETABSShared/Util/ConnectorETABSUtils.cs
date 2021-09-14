using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using System.Linq;
using ConnectorETABSShared.UI;
using ETABSv1;

namespace ConnectorETABSShared.Util
{
    class ConnectorETABSUtils
    {
#if ETABSV18
        public static string ETABSAppName = Applications.ETABSv18;
#elif ETABSV19
        public static string ETABSAppName = Applications.ETABSv19;
#endif

        public static Dictionary<string, (string, string)> ObjectIDsTypesAndNames { get; set; }

        public List<SpeckleException> ConversionErrors { get; set; }

        public static void GetObjectIDsTypesAndNames(ConnectorETABSDocument doc)
        {
            ObjectIDsTypesAndNames = new Dictionary<string, (string, string)>();
            foreach (var objectType in Enum.GetNames(typeof(ETABSAPIUsableTypes)))
            {
                var names = new List<string>();
                try
                {
                    names = GetAllNamesOfObjectType(doc, objectType);
                }
                catch { }
                if (names.Count > 0)
                {
                    foreach (string name in names)
                    {
                        ObjectIDsTypesAndNames.Add(string.Concat(objectType, ": ", name), (objectType, name));
                    }
                }
            }
        }

        public static bool IsTypeETABSAPIUsable(string type)
        {
            return Enum.GetNames(typeof(ETABSAPIUsableTypes)).Contains(type);
        }

        public static List<string> GetAllNamesOfObjectType(ConnectorETABSDocument doc, string objectType)
        {
            switch (objectType)
            {
                case "Point":
                    return GetAllPointNames(doc);
                case "Frame":
                    return GetAllFrameNames(doc);
                case "Tendon":
                    return GetAllTendonNames(doc);
                case "Area":
                    return GetAllAreaNames(doc);
                case "Link":
                    return GetAllLinkNames(doc);
                case "PropMaterial":
                    return GetAllPropMaterialNames(doc);
                //case "Rebar":
                //    return GetAllPropRebarNames(doc);
                case "PropFrame":
                    return GetAllPropFrameNames(doc);
                case "LoadCase":
                    return GetAllLoadCaseNames(doc);
                case "LoadPattern":
                    return GetAllLoadPatternNames(doc);
                //case "Group":
                //    return GetAllGroupNames(doc);
                case "GridSys":
                    return GetAllGridNames(doc);
                case "Combo":
                    return GetAllComboNames(doc);
                //case "Constraint":
                //    return GetAllConstraintNames(doc);
                case "DesignSteel":
                    return GetAllSteelDesignNames(doc);
                case "DesignConcrete":
                    return GetAllConcreteDesignNames(doc);
                case "Story":
                    return GetAllStoryNames(doc);
                case "Diaphragm":
                    return GetAllDiaphragmNames(doc);
                //case "Line":
                //return GetAllLineNames(doc);
                case "PierLabel":
                    return GetAllPierLabelNames(doc);
                case "PropAreaSpring":
                    return GetAllPropAreaSpringNames(doc);
                case "PropLineSpring":
                    return GetAllPropLineSpringNames(doc);
                case "PropPointSpring":
                    return GetAllPropPointSpringNames(doc);
                case "SpandrelLabel":
                    return GetAllSpandrelLabelNames(doc);
                //case "Tower":
                //    return GetAllTowerNames(doc);
                case "PropTendon":
                    return GetAllPropTendonNames(doc);
                case "PropLink":
                    return GetAllPropLinkNames(doc);
                default:
                    return null;
            }
        }

        public static List<string> GetAllPointNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.PointObj.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }

        }
        public static List<string> GetAllFrameNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.FrameObj.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllTendonNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.TendonObj.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllAreaNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.AreaObj.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllLinkNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.LinkObj.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllPropMaterialNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.PropMaterial.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllPropRebarNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.PropRebar.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllPropFrameNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.PropFrame.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllLoadCaseNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.LoadCases.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllGroupNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.GroupDef.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllGridNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.GridSys.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllComboNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.RespCombo.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllConstraintNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.ConstraintDef.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllLoadPatternNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.LoadPatterns.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllSteelDesignNames(ConnectorETABSDocument doc)
        {
            var name = "";
            try
            {
                doc.Document.DesignSteel.GetCode(ref name);
                return new List<string>() { name };
            }
            catch { return null; }
        }
        public static List<string> GetAllConcreteDesignNames(ConnectorETABSDocument doc)
        {
            var name = "";
            try
            {
                doc.Document.DesignConcrete.GetCode(ref name);
                return new List<string>() { name };
            }
            catch { return null; }
        }
        public static List<string> GetAllStoryNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.Story.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllDiaphragmNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.Diaphragm.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllLineNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.LineElm.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllPierLabelNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.PierLabel.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllPropAreaSpringNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.PropAreaSpring.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllPropLineSpringNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.PropLineSpring.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllPropPointSpringNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.PropPointSpring.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllSpandrelLabelNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            var isMultiStory = new bool[] { };
            try
            {
                doc.Document.SpandrelLabel.GetNameList(ref num, ref names, ref isMultiStory);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllTowerNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.Tower.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllPropTendonNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.PropTendon.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }
        public static List<string> GetAllPropLinkNames(ConnectorETABSDocument doc)
        {
            int num = 0;
            var names = new string[] { };
            try
            {
                doc.Document.PropLink.GetNameList(ref num, ref names);
                return names.ToList();
            }
            catch { return null; }
        }


        public static List<(string, string)> SelectedObjects(ConnectorETABSDocument doc)
        {
            int num = 0;
            var types = new int[] { };
            var names = new string[] { };
            doc.Document.SelectObj.GetSelected(ref num, ref types, ref names);
            var typesAndNames = new List<(string, string)>();
            if (num < 1)
            {
                return null;
            }
            for (int i = 0; i < num; i++)
            {
                switch (types[i])
                {
                    case 1:
                        typesAndNames.Add(("Point", names[i]));
                        break;
                    case 2:
                        typesAndNames.Add(("Frame", names[i]));
                        break;
                    case 3:
                        typesAndNames.Add(("Cable", names[i]));
                        break;
                    case 4:
                        typesAndNames.Add(("Tendon", names[i]));
                        break;
                    case 5:
                        typesAndNames.Add(("Area", names[i]));
                        break;
                    case 6:
                        typesAndNames.Add(("Solid", names[i]));
                        break;
                    case 7:
                        typesAndNames.Add(("Link", names[i]));
                        break;
                    default:
                        break;
                }
            }
            return typesAndNames;
        }

        public enum ETABSAPIUsableTypes
        {
            Point = 1, // cPointObj
            Frame = 2, // cFrameObj
                       //Tendon = 3, 
            Area = 4, // cAreaObj
            Link = 5, // cLinkObj
            PropMaterial = 6, // cPropFrame which is material property
            //PropRebar = 7, // cPropRebar doesn't have set methods
            PropFrame = 8, // cPropFrame which is Frame section property
            LoadCase = 9, // cLoadCases
            LoadPattern = 10, // cLoadPatterns
            //Group = 11, // cGroup
            GridSys = 12, // cGridSys
            Combo = 13, // cCombo
            //Constraint = 14, // cConstraint; api manual says use diaphragm instead
            DesignSteel = 15, // cDesignSteel
            DesignConcrete = 16, // cDesignConcrete
            Story = 17, /// cStory
            Diaphragm = 18, // cDiaphragm
                            // Line = 19, // cLineElm
            PierLabel = 20, // cPierLabel
            PropAreaSpring = 21, // cPropAreaSpring 
            PropLineSpring = 22, // cPropLineSpring
            PropPointSpring = 23, // cPropPointSpring
            SpandrelLabel = 24, // cSpandrelLabel
                                //Tower = 25, // cTower
                                // Cable = 26,
                                // Solid = 27,
                                // DesignProcedure = 28,
                                // DesignStrip = 29,
            PropTendon = 30,
            PropLink = 31
        }

        /// <summary>
        /// same as ObjectType in ETABS cSelect.GetSelected API function
        /// </summary>
        public enum ETABSViewSelectableTypes
        {
            Point = 1,
            Frame = 2,
            Cable = 3,
            Tendon = 4,
            Area = 5,
            Solid = 6,
            Link = 7,
        }
    }





    ///<summary>
    ///same as in eAreaDesignOrientation Enumeration in ETABS API
    ///</summary>
    //public enum ETABSAreaType
    //{
    //    Wall = 1,
    //    Floor = 2,
    //    Ramp_DO_NOT_USE = 3,
    //    Null = 4,
    //    Other = 5,
    //}

    ///<summary>
    ///same as in eFramePropType Enumeration
    ///</summary>


    ///<summary>
    /// same as in eLoadCaseType Enumeration in ETABS API
    ///</summary>
    //public enum ETABSLoadCaseType
    //{
    //    LinearStatic = 1,
    //    NonlinearStatic = 2,
    //    Modal = 3,
    //    ResponseSpectrum = 4,
    //    LinearHistory = 5,
    //    NonlinearHistory = 6,
    //    LinearDynamic = 7,
    //    NonlinearDynamic = 8,
    //    MovingLoad = 9,
    //    Buckling = 10,
    //    SteadyState = 11,
    //    PowerSpectralDensity = 12,
    //    LinearStaticMultiStep = 13,
    //    HyperStatic = 14,
    //}
}

