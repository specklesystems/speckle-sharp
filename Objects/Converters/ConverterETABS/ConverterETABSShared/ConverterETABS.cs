using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ETABSv1;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using OSG = Objects.Structural.Geometry;
using OSEA = Objects.Structural.ETABS.Analysis;
using Objects.Converter.ETABS;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS : ISpeckleConverter
    {
#if ETABSV18
        public static string ETABSAppName = Applications.ETABSv18;
#elif ETABSV19
        public static string ETABSAppName = Applications.ETABSv19;
#endif
        public string Description => "Default Speckle Kit for ETABS";

        public string Name => nameof(ConverterETABS);

        public string Author => "Speckle";

        public string WebsiteOrEmail => "https://speckle.systems";

        public cSapModel Model { get; private set; }

        public void SetContextDocument(object doc)
        {
            Model = (cSapModel)doc;
        }

        public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();

        public bool CanConvertToNative(Base @object)
        {
            throw new NotImplementedException();
            //foreach (var type in Enum.GetNames(typeof(ConverterETABSUtils.ETABSAPIUsableTypes)))
            //{
            //    if (type == (string)@object)
            //    {
            //        return true;
            //    }
            //}
            //return false;
        }

        public bool CanConvertToSpeckle(object @object)
        {
            foreach (var type in Enum.GetNames(typeof(ConverterETABSUtils.ETABSAPIUsableTypes)))
            {
                if (type == @object.ToString())
                {
                    return true;
                }
            }
            return false;
        }

        public object ConvertToNative(Base @object)
        {
            throw new NotImplementedException();
            //switch (@object)
            //{
            //    case  OSG.Node o:
            //        return PointToNative(o);
            //    case Frame o:
            //        return FrameToNative(o);
            //    case Area o:
            //        return AreaToNative(o);
            //    default:
            //        ConversionErrors.Add(new SpeckleException("Unsupported Speckle Object: Can not convert to native", level: Sentry.SentryLevel.Warning));
            //        return null;
            //}
        }

            public List<object> ConvertToNative(List<Base> objects)
        {
            return objects.Select(x => ConvertToNative(x)).ToList();
        }

        public Base ConvertToSpeckle(object @object)
        {
            (string type, string name) = ((string, string))@object;
            Base returnObject = null;
            switch (type)
            {
                case "Point":
                    returnObject = PointToSpeckle(name);
                    break;
                case "Frame":
                    returnObject = FrameToSpeckle(name);
                    break;
                case "@Model":
                    returnObject = ModelToSpeckle();
                    break;
                    //case "Area":
                    //    returnObject = AreaToSpeckle(name);
                    //    break;
                    //case "Link":
                    //    returnObject = LinkToSpeckle(name);
                    //    break;
                    //case "PropMaterial":
                    //    returnObject = PropMaterialToSpeckle(name);
                    //    break;
                    //case "PropFrame":
                    //    returnObject = PropFrameToSpeckle(type, name);
                    //    break;
                    //case "LoadCase":
                    //    returnObject = LoadCaseToSpeckle(name);
                    //    break;
                case "LoadPattern":
                    returnObject = LoadPatternToSpeckle(name);
                    break;
                    //case "GridSys":
                    //    returnObject = GridSysToSpeckle(name);
                    //    break;
                    //case "Combo":
                    //    returnObject = ComboToSpeckle(name);
                    //    break;
                    //case "DesignSteel":
                    //    returnObject = DesignSteelToSpeckle(name);
                    //    break;
                    //case "DeisgnConcrete":
                    //    returnObject = DesignConcreteToSpeckle(name);
                    //    break;
                    //case "Story":
                    //    returnObject = StoryToSpeckle(name);
                    //    break;
                    //case "Diaphragm":
                    //    returnObject = DiaphragmToSpeckle(name);
                    //    break;
                    //case "PierLabel":
                    //    returnObject = PierLabelToSpeckle(name);
                    //    break;
                    //case "PropAreaSpring":
                    //    returnObject = PropAreaSpringToSpeckle(name);
                    //    break;
                    //case "PropLineSpring":
                    //    returnObject = PropLineSpringToSpeckle(name);
                    //    break;
                    //case "PropPointSpring":
                    //    returnObject = PropPointSpringToSpeckle(name);
                    //    break;
                    //case "SpandrelLabel":
                    //    returnObject = SpandrelLabelToSpeckle(name);
                    //    break;
                    //case "PropTendon":
                    //    returnObject = PropTendonToSpeckle(name);
                    //    break;
                    //case "PropLink":
                    //    returnObject = PropLinkToSpeckle(name);
                    //    break;
                    //default:
                    //    ConversionErrors.Add(new SpeckleException($"Skipping not supported type: {type}"));
                    //    returnObject = null;
                    //    break;
            }
            return returnObject;
        }

        public List<Base> ConvertToSpeckle(List<object> objects)
        {
                return objects.Select(x => ConvertToSpeckle(x)).ToList();
        }

        public IEnumerable<string> GetServicedApplications() => new string[] {ETABSAppName };


        public void SetContextObjects(List<ApplicationPlaceholderObject> objects)
        {
            throw new NotImplementedException();
        }

        public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects)
        {
            throw new NotImplementedException();
        }
    }
}
