using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Autodesk.Revit.UI;
using Speckle.Core.Models;
using Xunit;
using xUnitRevitUtils;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  internal static class SpeckleUtils
  {
    public static SemaphoreSlim Throttler = new SemaphoreSlim(1,1);

    public static UIDocument OpenUIDoc(string filePath)
    {
      Assert.NotNull(xru.Uiapp);
      UIDocument doc = null;
      xru.UiContext.Send(delegate
      {
        doc = xru.Uiapp.OpenAndActivateDocument(filePath);
      }, null);
      Assert.NotNull(doc);
      return doc;
    }

    internal static void DeleteElement(object obj)
    {
      switch (obj)
      {
        case IEnumerable enumerable:
          foreach (var item in enumerable)
            DeleteElement(item);
          break;
        case ApplicationObject o:
          foreach (var item in o.Converted)
            DeleteElement(item);
          break;
        case DB.ViewSchedule _:
          // don't delete a view schedule since we didn't create it in the first place
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

    internal static void CustomAssertions(DB.Element element, Base @base)
    {
      var parameters = element.Parameters.Cast<DB.Parameter>()
        .Where(el => el.Definition.Name.StartsWith("ToSpeckle"));

      foreach (var param in parameters)
      {
        var parts = param.Definition.Name.Split('-');
        if (parts.Length != 3) continue;

        var assertionType = parts[1];
        var prop = parts[2];

        switch (param.StorageType)
        {
          case DB.StorageType.String:
            var baseString = GetBaseValue<string>(@base, prop);
            var stringAssertionMethod = GetAssertionMethod<string>(assertionType);
            try
            {
              stringAssertionMethod(param.AsValueString(), baseString);
            }
            catch (Exception ex)
            {
              stringAssertionMethod(param.AsString(), baseString);
            }
            break;
          case DB.StorageType.Integer:
            var baseInt = GetBaseValue<int>(@base, prop);
            var intAssertionMethod = GetAssertionMethod<int>(assertionType);
            intAssertionMethod(param.AsInteger(), baseInt);
            break;
          case DB.StorageType.Double:
            var baseDouble = GetBaseValue<double>(@base, prop);
            var doubleAssertionMethod = GetAssertionMethod<double>(assertionType);
            doubleAssertionMethod(param.AsDouble(), baseDouble);
            break;
        }
      }
    }

    private static T GetBaseValue<T>(Base @base, string prop)
    {
      var path = prop.Split('.');
      dynamic value = @base;
      foreach (var part in path)
      {
        value = value[part];
      }
      return (T)value;
    }

    private static Action<T, T> GetAssertionMethod<T>(string assertionType)
    {
      return assertionType switch
      {
        "AE" => Assert.Equal,
        _ => throw new Exception($"Assertion type of \"{assertionType}\" is not recognized")
      };
    }
  }
}
