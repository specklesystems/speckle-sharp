#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Kits;

#endregion

namespace RevitAutoTest2022
{
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
          a.ControlledApplication.DocumentOpened += (e, args) =>
          {
            Console.WriteLine("Ready!");
            var doc = args.Document;
            var kit = KitManager.GetDefaultKit();
            var converter = kit.LoadConverter(Applications.Revit2022);
            var elements = GetAllModelElements(doc);

            var toSpeckle =
              elements.Select(el => converter.CanConvertToSpeckle(el) ? converter.ConvertToSpeckle(el) : null);
            
            Console.WriteLine("Converted");

            doc.Close();
          };
          return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
        IList<Element> GetAllModelElements( Document doc )
        {
          List<Element> elements = new List<Element>();
 
          FilteredElementCollector collector
            = new FilteredElementCollector( doc )
              .WhereElementIsNotElementType();
 
          foreach( Element e in collector )
          {
            if( null != e.Category
                && e.Category.HasMaterialQuantities )
            {
              elements.Add( e );
            }
          }
          return elements;
        }}
    
}
