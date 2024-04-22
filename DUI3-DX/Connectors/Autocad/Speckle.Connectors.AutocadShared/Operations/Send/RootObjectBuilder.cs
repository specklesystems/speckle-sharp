using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Operations;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Connectors.Autocad.Operations.Send;

public class RootObjectBuilder : IRootObjectBuilder<(DBObject obj, string applicationId)>
{
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;

  public RootObjectBuilder(IUnitOfWorkFactory unitOfWorkFactory)
  {
    _unitOfWorkFactory = unitOfWorkFactory;
  }

  public Base Build(
    IEnumerable<(DBObject obj, string applicationId)> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    // POC: does this feel like the right place? I am wondering if this should be called from within send/rcv?
    // begin the unit of work
    using var uow = _unitOfWorkFactory.Resolve<ISpeckleConverterToSpeckle>();
    var converter = uow.Service;

    Collection modelWithLayers =
      new()
      {
        name = Application.DocumentManager.CurrentDocument.Name
          .Split(s_separator, StringSplitOptions.None)
          .Reverse()
          .First(),
        collectionType = "root"
      };

    Dictionary<string, Collection> collectionCache = new();
    int count = 0;

    foreach ((DBObject obj, string applicationId) tuple in objects)
    {
      ct.ThrowIfCancellationRequested();

      var dbObject = tuple.obj;
      var applicationId = tuple.applicationId;

      try
      {
        Base converted;
        if (
          !sendInfo.ChangedObjectIds.Contains(applicationId)
          && sendInfo.ConvertedObjects.TryGetValue(applicationId + sendInfo.ProjectId, out ObjectReference value)
        )
        {
          converted = value;
        }
        else
        {
          converted = converter.Convert(dbObject);

          if (converted == null)
          {
            continue;
          }

          converted.applicationId = applicationId;
        }

        // Create and add a collection for each layer if not done so already.
        if ((tuple.obj as Entity)?.Layer is string layer)
        {
          if (!collectionCache.TryGetValue(layer, out Collection? collection))
          {
            collection = new Collection() { name = layer, collectionType = "layer" };
            collectionCache[layer] = collection;
            modelWithLayers.elements.Add(collectionCache[layer]);
          }

          collection.elements.Add(converted);
        }

        onOperationProgressed?.Invoke("Converting", (double)++count / objects.Count());
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

  private static readonly string[] s_separator = new[] { "\\" };
}
