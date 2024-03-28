using ArcGIS.Desktop.Mapping;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Core.Models;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Core.Logging;

namespace Speckle.Connectors.ArcGis.Operations.Send;

/// <summary>
/// Stateless builder object to turn an <see cref="ISendFilter"/> into a <see cref="Base"/> object
/// </summary>
public class RootObjectBuilder
{
  private readonly IScopedFactory<ISpeckleConverterToSpeckle> _converterFactory;

  public RootObjectBuilder(IScopedFactory<ISpeckleConverterToSpeckle> converterFactory)
  {
    _converterFactory = converterFactory;
  }

  public Base Build(
    //ISendFilter sendFilter,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    if (MapView.Active == null)
    {
      throw new SpeckleException("No Map currently active");
    }
    var selectedObjects = GetSelection(MapView.Active.Map);

    // if (selectedObjects.Count == 0)
    // {
    //   throw new InvalidOperationException("No objects were found. Please update your send filter!");
    // }

    Base commitObject = ConvertObjects(selectedObjects, onOperationProgressed, ct);

    return commitObject;
  }

  /// <remarks>
  /// This method must be called on the MCT. Use QueuedTask. Run.
  /// </remarks>
  private Dictionary<MapMember, List<long>> GetSelection(Map map)
  {
    // var allMembers = map.GetMapMembersAsFlattenedList();

    //POC: Right now, we're not using the send filter
    // We're still undecided how we handle MapMember vs objectId when converting
    // + the ArcGIS api breaks some assumptions we've made
    // e.g.
    // - There isn't a single type of ID to uniquely identify objects
    //    - MapMembers are identifiable by uri, but objects on a MapMember have an objectId which is their index in the MapMember
    //    - plus, some MapMembers work differently (raster layers, voxel layers, pointclound layers) etc.
    // - getting selection is an async operation, needs to be done on the main thread, and returns the full object, not just an ID

    var selectedMemberUrls = map.GetSelection().ToDictionary();

    if (selectedMemberUrls.Count == 0)
    {
      throw new SpeckleException("No data to send");
    }

    return selectedMemberUrls;
  }

  //poc: semi dupe
  private Collection ConvertObjects(
    IReadOnlyDictionary<MapMember, List<long>> mapMembers,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken cancellationToken = default
  )
  {
    // var rootObjectCollection = new Collection { name = RhinoDoc.ActiveDoc.Name ?? "Unnamed document" };
    int count = 0;

    Collection rootObjectCollection = new(); //TODO: Collections
    ISpeckleConverterToSpeckle converter = _converterFactory.ResolveScopedInstance();

    foreach ((MapMember mapMember, List<long> objectIds) in mapMembers)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var collectionHost = rootObjectCollection;
      var applicationId = mapMember.ToString();

      try
      {
        Base converted = converter.Convert(mapMember);
        converted.applicationId = applicationId;

        // add to host
        collectionHost.elements.Add(converted);
        onOperationProgressed?.Invoke("Converting", (double)++count / mapMembers.Count);
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
    }

    return rootObjectCollection;
  }
}
