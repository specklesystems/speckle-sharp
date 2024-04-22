using ArcGIS.Desktop.Mapping;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Connectors.ArcGis.Operations.Send;

/// <summary>
/// Stateless builder object to turn an ISendFilter into a <see cref="Base"/> object
/// </summary>
public class RootObjectBuilder
{
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;

  public RootObjectBuilder(IUnitOfWorkFactory unitOfWorkFactory)
  {
    _unitOfWorkFactory = unitOfWorkFactory;
  }

  public Base Build(
    ISendFilter sendFilter,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    if (MapView.Active == null)
    {
      throw new SpeckleException("No Map currently active");
    }

    List<string> selectedObjects = sendFilter.GetObjectIds().Where(obj => obj != null).ToList();

    if (selectedObjects.Count == 0)
    {
      throw new InvalidOperationException("No objects were found. Please update your send filter!");
    }

    Base commitObject = ConvertObjects(selectedObjects, onOperationProgressed, ct);
    return commitObject;
  }

  //poc: semi dupe
  private Collection ConvertObjects(
    IReadOnlyList<string> uriList,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken cancellationToken = default
  )
  {
    // POC: does this feel like the right place? I am wondering if this should be called from within send/rcv?
    // begin the unit of work
    using var uow = _unitOfWorkFactory.Resolve<ISpeckleConverterToSpeckle>();
    var converter = uow.Service;

    // var rootObjectCollection = new Collection { name = RhinoDoc.ActiveDoc.Name ?? "Unnamed document" };
    int count = 0;

    Collection rootObjectCollection = new(); //TODO: Collections

    foreach (string uri in uriList)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var collectionHost = rootObjectCollection;
      var applicationId = uri;

      Base converted = new();

      MapMember mapMember = MapView.Active.Map.FindLayer(uri);
      if (mapMember is null)
      {
        mapMember = MapView.Active.Map.FindStandaloneTable(uri);
      }
      if (mapMember is null)
      {
        continue;
      }

      try
      {
        converted = converter.Convert(mapMember);
      }
      // POC: Exception handling on conversion logic must be revisited after several connectors have working conversions
      catch (SpeckleConversionException e)
      {
        // POC: DO something with the exception
        Console.WriteLine(e);
      }
      catch (NotSupportedException e)
      {
        // POC: DO something with the exception
        Console.WriteLine(e);
      }

      converted.applicationId = applicationId;
      // add to host
      collectionHost.elements.Add(converted);
      onOperationProgressed?.Invoke("Converting", (double)++count / uriList.Count);
    }

    return rootObjectCollection;
  }
}
