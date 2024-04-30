using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Core.Models;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Connectors.Utils.Extensions;

namespace Speckle.Connectors.Autocad.Operations.Receive;

public class HostObjectBuilder : IHostObjectBuilder
{
  private readonly ISpeckleConverterToHost _converter;
  private readonly AutocadLayerManager _autocadLayerManager;
  private readonly GraphTraversal _traversalFunction;

  public HostObjectBuilder(
    ISpeckleConverterToHost converter,
    AutocadLayerManager autocadLayerManager,
    GraphTraversal traversalFunction
  )
  {
    _converter = converter;
    _autocadLayerManager = autocadLayerManager;
    _traversalFunction = traversalFunction;
  }

  public IEnumerable<string> Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    // Prompt the UI conversion started. Progress bar will swoosh.
    onOperationProgressed?.Invoke("Converting", null);

    // Layer filter for received commit with project and model name
    _autocadLayerManager.CreateLayerFilter(projectName, modelName);
    var traversalGraph = _traversalFunction.Traverse(rootObject).ToArray();

    string baseLayerPrefix = $"SPK-{projectName}-{modelName}-";

    HashSet<string> uniqueLayerNames = new();
    List<string> handleValues = new();
    int count = 0;

    // POC: Will be addressed to move it into AutocadContext!
    using (TransactionContext.StartTransaction(Application.DocumentManager.MdiActiveDocument))
    {
      foreach (TraversalContext tc in traversalGraph)
      {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
          var path = tc.GetCurrentObjectPath();
          var layerFullName = _autocadLayerManager.LayerFullName(baseLayerPrefix, string.Join("-", path));

          if (uniqueLayerNames.Add(layerFullName))
          {
            _autocadLayerManager.CreateLayerOrPurge(layerFullName);
          }

          object converted = _converter.Convert(tc.Current);
          List<object> flattened = Utilities.FlattenToHostConversionResult(converted);

          foreach (Entity conversionResult in flattened.Cast<Entity>())
          {
            if (conversionResult == null)
            {
              // POC: This needed to be double checked why we check null and continue
              continue;
            }

            conversionResult.Append(layerFullName);

            handleValues.Add(conversionResult.Handle.Value.ToString());
          }

          onOperationProgressed?.Invoke("Converting", (double)++count / traversalGraph.Length);
        }
        catch (Exception e) when (!e.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
        {
          // POC: report, etc.
          Debug.WriteLine("conversion error happened.");
        }
      }
    }
    return handleValues;
  }
}
