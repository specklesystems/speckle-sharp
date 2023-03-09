using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Objects.Converter.Revit;
using Revit.Async;
using Speckle.Core.Models;
using xUnitRevitUtils;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  internal static class SpeckleUtils
  {
    public static bool CloseDoc(DB.Document doc, bool saveChanges = false)
    {
      if (doc == null)
        return false;

      bool result = false;
      xru.UiContext.Send(x => { result = doc.Close(saveChanges); }, null);
      return result;
    }
    internal async static Task<string> RunInTransaction(Action action, DB.Document doc, ConverterRevit converter, string transactionName = "transaction", bool ignoreWarnings = false)
    {
      var tcs = new TaskCompletionSource<string>();

      await RevitTask.RunAsync(() =>
      {
        using var g = new DB.TransactionGroup(doc, transactionName);
        using var transaction = new DB.Transaction(doc, transactionName);

        g.Start();
        transaction.Start();

        converter.SetContextDocument(transaction);

        if (ignoreWarnings)
        {
          var options = transaction.GetFailureHandlingOptions();
          options.SetFailuresPreprocessor(new IgnoreAllWarnings());
          transaction.SetFailureHandlingOptions(options);
        }

        try
        {
          action.Invoke();
          transaction.Commit();
          g.Assimilate();
        }
        catch (Exception exception)
        {
          tcs.TrySetException(exception);
        }

        tcs.TrySetResult("");
      });

      return await tcs.Task;
    }

    internal class IgnoreAllWarnings : Autodesk.Revit.DB.IFailuresPreprocessor
    {
      public DB.FailureProcessingResult PreprocessFailures(Autodesk.Revit.DB.FailuresAccessor failuresAccessor)
      {
        IList<Autodesk.Revit.DB.FailureMessageAccessor> failureMessages = failuresAccessor.GetFailureMessages();
        foreach (Autodesk.Revit.DB.FailureMessageAccessor item in failureMessages)
        {
          failuresAccessor.DeleteWarning(item);
        }

        return DB.FailureProcessingResult.Continue;
      }
    }

    internal static void DeleteElement(object obj)
    {
      switch (obj)
      {
        case IList list:
          foreach (var item in list)
            DeleteElement(item);
          break;
        case ApplicationObject o:
          foreach (var item in o.Converted)
            DeleteElement(item);
          break;
        case DB.Element o:
          try
          {
            xru.RunInTransaction(() =>
            {
              o.Document.Delete(o.Id);
            }, o.Document).Wait();
          }
          // element already deleted, don't worry about it
          catch { }
          break;
        default:
          throw new Exception("It's not an element!?!?!");
      }
    }
  }
}
