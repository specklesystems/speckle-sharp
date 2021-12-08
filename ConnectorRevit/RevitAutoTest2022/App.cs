#region Namespaces

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;

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
        converter.SetContextDocument(doc);
        var elements = GetAllModelElements(doc);

        var toSpeckle =
          elements.Select(el => converter.CanConvertToSpeckle(el) ? converter.ConvertToSpeckle(el) : null);

        var accs = AccountManager.GetAccounts();
        var acc = accs.First();
        var client = new Client(acc);

        var streamId = client.StreamCreate(new StreamCreateInput
        {
          name = "Test stream vX.X.X", 
          description = "The tests for yxsasdfa", 
          isPublic = true
        }).Result;
        
        var transport = new ServerTransport(acc, streamId);
        
        var refObj = new Base();
        refObj[ "convertedObjects" ] = toSpeckle.ToList();

        var objectId = Operations.Send(
          refObj, 
          new List<ITransport> { transport }, 
          false, 
          null, 
          null)
          .Result;
      };
      return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication a)
    {
      return Result.Succeeded;
    }

    IList<Element> GetAllModelElements(Document doc)
    {
      List<Element> elements = new List<Element>();

      FilteredElementCollector collector
        = new FilteredElementCollector(doc)
          .WhereElementIsNotElementType();

      foreach (Element e in collector)
      {
        if (null != e.Category
            && e.Category.HasMaterialQuantities)
        {
          elements.Add(e);
        }
      }

      return elements;
    }
  }
}
