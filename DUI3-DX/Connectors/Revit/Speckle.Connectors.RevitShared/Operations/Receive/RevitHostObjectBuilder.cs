using System;
using System.Collections.Generic;
using System.Threading;
using Autodesk.Revit.DB;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Connectors.Revit.Operations.Receive;

public class RevitHostObjectBuilder : IHostObjectBuilder
{
  private readonly ISpeckleConverterToHost _toHostConverter;

  public RevitHostObjectBuilder(ISpeckleConverterToHost toHostConverter)
  {
    _toHostConverter = toHostConverter;
  }

  public IEnumerable<string> Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    // POC : obviously not the traversal we want.
    // I'm waiting for jedd to magically make all my traversal issues go away again
    foreach (var obj in rootObject.Traverse(b => false))
    {
      object? conversionResult = null;
      try
      {
        conversionResult = _toHostConverter.Convert(obj);
      }
      catch (SpeckleConversionException ex)
      {
        // POC : logging
      }
      if (conversionResult is Element element)
      {
        yield return element.UniqueId;
      }
      YieldToUiThread();
    }
  }

  private DateTime _timerStarted = DateTime.MinValue;

  private void YieldToUiThread()
  {
    var currentTime = DateTime.UtcNow;

    if (currentTime.Subtract(_timerStarted) < TimeSpan.FromSeconds(.15))
    {
      return;
    }

    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
      () => { },
      System.Windows.Threading.DispatcherPriority.Background
    );

    _timerStarted = currentTime;
  }
}
