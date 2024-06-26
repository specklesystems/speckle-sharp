using System.Diagnostics;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Caching;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Connectors.Utils.Operations;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Connectors.ArcGis.Operations.Send;

/// <summary>
/// Stateless builder object to turn an ISendFilter into a <see cref="Base"/> object
/// </summary>
public class ArcGISRootObjectBuilder : IRootObjectBuilder<MapMember>
{
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;
  private readonly ISendConversionCache _sendConversionCache;
  private readonly IHostToSpeckleUnitConverter<Unit> _unitConverter;

  public ArcGISRootObjectBuilder(
    IUnitOfWorkFactory unitOfWorkFactory,
    ISendConversionCache sendConversionCache,
    IHostToSpeckleUnitConverter<Unit> unitConverter
  )
  {
    _unitOfWorkFactory = unitOfWorkFactory;
    _sendConversionCache = sendConversionCache;
    _unitConverter = unitConverter;
  }

  public RootObjectBuilderResult Build(
    IReadOnlyList<MapMember> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    // POC: does this feel like the right place? I am wondering if this should be called from within send/rcv?
    // begin the unit of work

    // ADD MAP UNITS CHECK
    try
    {
      _unitConverter.ConvertOrThrow(MapView.Active.Map.SpatialReference.Unit);
    }
    catch (SpeckleException ex) { }

    using var uow = _unitOfWorkFactory.Resolve<IRootToSpeckleConverter>();
    var converter = uow.Service;

    int count = 0;

    Collection rootObjectCollection = new(); //TODO: Collections

    List<SendConversionResult> results = new(objects.Count);
    var cacheHitCount = 0;

    foreach (MapMember mapMember in objects)
    {
      ct.ThrowIfCancellationRequested();
      var collectionHost = rootObjectCollection;
      var applicationId = mapMember.URI;

      try
      {
        Base converted;
        if (_sendConversionCache.TryGetValue(sendInfo.ProjectId, applicationId, out ObjectReference value))
        {
          converted = value;
          cacheHitCount++;
        }
        else
        {
          converted = converter.Convert(mapMember);
          converted.applicationId = applicationId;
        }

        // add to host
        collectionHost.elements.Add(converted);
        results.Add(new(Status.SUCCESS, applicationId, mapMember.GetType().Name, converted));
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        results.Add(new(Status.ERROR, applicationId, mapMember.GetType().Name, null, ex));
        // POC: add logging
      }

      onOperationProgressed?.Invoke("Converting", (double)++count / objects.Count);
    }

    // POC: Log would be nice, or can be removed.
    Debug.WriteLine(
      $"Cache hit count {cacheHitCount} out of {objects.Count} ({(double)cacheHitCount / objects.Count})"
    );

    return new(rootObjectCollection, results);
  }
}
