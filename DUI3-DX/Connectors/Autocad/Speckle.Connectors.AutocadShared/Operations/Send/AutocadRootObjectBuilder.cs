using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Operations;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Connectors.Autocad.Operations.Send;

public class AutocadRootObjectBuilder : IRootObjectBuilder<AutocadRootObject>
{
  private readonly IRootToSpeckleConverter _converter;
  private readonly string[] _documentPathSeparator = { "\\" };

  public AutocadRootObjectBuilder(IRootToSpeckleConverter converter)
  {
    _converter = converter;
  }

  public Base Build(
    IReadOnlyList<AutocadRootObject> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    Collection modelWithLayers =
      new()
      {
        name = Application.DocumentManager.CurrentDocument.Name // POC: https://spockle.atlassian.net/browse/CNX-9319
          .Split(_documentPathSeparator, StringSplitOptions.None)
          .Reverse()
          .First(),
        collectionType = "root"
      };

    // Cached dictionary to create Collection for autocad entity layers. We first look if collection exists. If so use it otherwise create new one for that layer.
    Dictionary<string, Collection> collectionCache = new();
    int count = 0;

    foreach (var (root, applicationId) in objects)
    {
      ct.ThrowIfCancellationRequested();

      try
      {
        Base converted;
        if (
          !sendInfo.ChangedObjectIds.Contains(applicationId)
          && sendInfo.ConvertedObjects.TryGetValue(applicationId + sendInfo.ProjectId, out ObjectReference value) // POC: Interface out constructing keys here to use it otherplaces with a same code. -> https://spockle.atlassian.net/browse/CNX-9313
        )
        {
          converted = value;
        }
        else
        {
          converted = _converter.Convert(root);

          if (converted == null)
          {
            continue;
          }

          converted.applicationId = applicationId;
        }

        // Create and add a collection for each layer if not done so already.
        if ((root as Entity)?.Layer is string layer)
        {
          if (!collectionCache.TryGetValue(layer, out Collection? collection))
          {
            collection = new Collection() { name = layer, collectionType = "layer" };
            collectionCache[layer] = collection;
            modelWithLayers.elements.Add(collectionCache[layer]);
          }

          collection.elements.Add(converted);
        }

        onOperationProgressed?.Invoke("Converting", (double)++count / objects.Count);
      }
      catch (SpeckleConversionException e)
      {
        Console.WriteLine(e);
      }
      catch (NotSupportedException e)
      {
        Console.WriteLine(e);
      }
      catch (Exception e) when (!e.IsFatal())
      {
        Debug.WriteLine(e.Message);
      }
    }

    return modelWithLayers;
  }
}
