using System;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad;

public class AutocadConverterToHost : ISpeckleConverterToHost
{
  private readonly IFactory<string, ISpeckleObjectToHostConversion> _toHost;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public AutocadConverterToHost(
    IFactory<string, ISpeckleObjectToHostConversion> toHost,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _toHost = toHost;
    _contextStack = contextStack;
  }

  public object Convert(Base target)
  {
    Type type = target.GetType();

    try
    {
      using (var tr = _contextStack.Current.Document.Database.TransactionManager.StartTransaction())
      {
        var objectConverter = _toHost.ResolveInstance(type.Name);

        if (objectConverter == null)
        {
          throw new NotSupportedException($"No conversion found for {target.GetType().Name}");
        }

        var convertedObject = objectConverter.Convert(target);
        tr.Commit();
        return convertedObject;
      }
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // POC: Just rethrowing for now, Logs may be needed here.
    }
  }
}
