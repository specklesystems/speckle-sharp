using ArcGIS.Desktop.Mapping;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Utils;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Operations;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Connectors.ArcGis.Operations.Send;

/// <summary>
/// Stateless builder object to turn an ISendFilter into a <see cref="Base"/> object
/// </summary>
public class RootObjectBuilder : IRootObjectBuilder<MapMember>
{
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;

  public RootObjectBuilder(IUnitOfWorkFactory unitOfWorkFactory)
  {
    _unitOfWorkFactory = unitOfWorkFactory;
  }

  public SendConversionResults Build(
    IReadOnlyList<MapMember> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    // POC: does this feel like the right place? I am wondering if this should be called from within send/rcv?
    // begin the unit of work
    using var uow = _unitOfWorkFactory.Resolve<IRootToSpeckleConverter>();
    var converter = uow.Service;

    int count = 0;

    Collection rootObjectCollection = new(); //TODO: Collections

    List<SendConversionResult> results = new(objects.Count);
    foreach (MapMember mapMember in objects)
    {
      ct.ThrowIfCancellationRequested();
      var collectionHost = rootObjectCollection;
      var applicationId = mapMember.URI;

      try
      {
        Base converted;
        if (
          !sendInfo.ChangedObjectIds.Contains(applicationId)
          && sendInfo.ConvertedObjects.TryGetValue(applicationId + sendInfo.ProjectId, out ObjectReference? value)
        )
        {
          converted = value;
        }
        else
        {
          converted = converter.Convert(mapMember);
          converted.applicationId = applicationId;
        }

        // add to host
        collectionHost.elements.Add(converted);
        results.Add(new(mapMember, mapMember.GetType().Name, applicationId, converted));
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        results.Add(new(mapMember, mapMember.GetType().Name, applicationId, ex));
        // POC: add logging
      }

      onOperationProgressed?.Invoke("Converting", (double)++count / objects.Count);
    }

    return new(results, rootObjectCollection);
  }
}
