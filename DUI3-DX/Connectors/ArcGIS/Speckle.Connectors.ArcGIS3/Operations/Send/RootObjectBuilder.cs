using ArcGIS.Desktop.Mapping;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Operations;
using Speckle.Converters.Common;
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

  public Base Build(
    IReadOnlyList<MapMember> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    if (!objects.Any())
    {
      // POC: not sure if we would want to throw in here?
      throw new InvalidOperationException("No objects were found. Please update your send filter!");
    }

    // POC: does this feel like the right place? I am wondering if this should be called from within send/rcv?
    // begin the unit of work
    using var uow = _unitOfWorkFactory.Resolve<ISpeckleConverterToSpeckle>();
    var converter = uow.Service;

    int count = 0;

    Collection rootObjectCollection = new(); //TODO: Collections

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
      }
      // POC: Exception handling on conversion logic must be revisited after several connectors have working conversions
      catch (SpeckleConversionException e)
      {
        // POC: DO something with the exception
        Console.WriteLine(e);
        continue;
      }
      catch (NotSupportedException e)
      {
        // POC: DO something with the exception
        Console.WriteLine(e);
        continue;
      }

      onOperationProgressed?.Invoke("Converting", (double)++count / objects.Count);
    }

    return rootObjectCollection;
  }
}
