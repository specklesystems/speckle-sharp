using System;
using System.Collections.Generic;
using System.Threading;
using Autodesk.Revit.DB;
using Revit.Async;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Services;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Connectors.Revit.Operations.Receive;

public class RevitHostObjectBuilder : IHostObjectBuilder
{
  private readonly ISpeckleConverterToHost _toHostConverter;
  private readonly ITransactionManagementService _transactionManagementService;

  public RevitHostObjectBuilder(
    ISpeckleConverterToHost toHostConverter,
    ITransactionManagementService transactionManagementService
  )
  {
    _toHostConverter = toHostConverter;
    _transactionManagementService = transactionManagementService;
  }

  public IEnumerable<string> Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    // POC : asyncify
    return RevitTask
      .RunAsync(() =>
      {
        _transactionManagementService.StartTransactionManagement($"Loaded data from {projectName}");
        List<string> elementIds = new();
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
            elementIds.Add(element.UniqueId);
          }
          YieldToUiThread();
        }

        _transactionManagementService.FinishTransactionManagement();
        return elementIds;
      })
      .GetAwaiter()
      .GetResult();
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
