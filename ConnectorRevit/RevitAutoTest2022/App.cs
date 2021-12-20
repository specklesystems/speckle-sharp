#region Namespaces

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    private UIControlledApplication _app;
    public Result OnStartup(UIControlledApplication a)
    {
      a.ControlledApplication.DocumentOpened += (e, args) =>
      {
        Task.Run(async delegate {
        var result = new SendResult(false, null);
        var doc = args.Document;

        try
        {
          var config = RevitConfig.LoadConfig(doc.PathName);

          var kit = KitManager.GetDefaultKit();
          var converter = kit.LoadConverter(Applications.Revit2022);
          converter.SetContextDocument(doc);
          var elements = GetAllModelElements(doc);

          var toSpeckle =
            elements.Select(el => converter.CanConvertToSpeckle(el) ? converter.ConvertToSpeckle(el) : null);

          var acc = AccountManager.GetAccounts().First(ac => ac.id == config.SenderId);
          var client = new Client(acc);

          var streamId = config.TargetStream;

          var transport = new ServerTransport(acc, streamId);

          var refObj = new Base();
          refObj["convertedObjects"] = toSpeckle.ToList();
          
          var objectId = await Operations.Send(
            refObj,
            new List<ITransport> { transport },
            false,
            null,
            ((s, exception) => throw exception));
          
          var commitId = await client.CommitCreate(new CommitCreateInput() {
            branchName = config.TargetBranch,
            objectId = objectId,
            streamId = streamId,
            sourceApplication=Applications.Revit2022
          });

          result.Success = true;
          result.Log="DONE";
        }
        catch (Exception ex)
        {
          result.Log=ex.ToString();
        }
        finally
        {
          result.Save(doc.PathName);
        }
      });
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
