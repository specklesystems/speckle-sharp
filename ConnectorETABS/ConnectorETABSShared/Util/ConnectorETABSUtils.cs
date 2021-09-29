using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using System.Linq;
using Speckle.ConnectorETABS.UI;
using Objects.Converter.ETABS;
using ETABSv1;

namespace Speckle.ConnectorETABS.Util
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
                case "Area":
                    return GetAllAreaNames(doc);
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
            Area = 4

        }

        /// <summary>
        /// same as ObjectType in ETABS cSelect.GetSelected API function
        /// </summary>
        public enum ETABSViewSelectableTypes
        {
            Point = 1,
            Frame = 2,
            Area = 5
        }
    }

}
