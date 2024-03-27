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
    ISendFilter sendFilter,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    if (MapView.Active == null)
    {
      throw new SpeckleException("No Map currently open");
    }
    var selectedObjects = GetSelection(sendFilter, MapView.Active.Map);

    if (selectedObjects.Count == 0)
    {
      throw new InvalidOperationException("No objects were found. Please update your send filter!");
    }

    Base commitObject = ConvertObjects(selectedObjects, onOperationProgressed, ct);

    return commitObject;
  }

  private HashSet<MapMember> GetSelection(ISendFilter sendFilter, Map map)
  {
    var allMembers = map.GetMapMembersAsFlattenedList();

    var selectedMemberUrls = sendFilter.GetObjectIds().ToHashSet();
    HashSet<MapMember> selectedMembers = new(selectedMemberUrls.Count);

    foreach (var member in allMembers)
    {
      //if (selectedMembers.Contains(member))
      {
        selectedMembers.Add(member);
      }
    }

    // if (selectedMemberUrls.Count != selectedMembers.Count)
    // {
    //   throw new SpeckleException("Some of the selected objects were not found in the active map");
    // }

    return selectedMembers;

    //
    // return QueuedTask.Run(() =>
    // {
    //   var res = map.GetSelection();
    //   _ = 0;
    // });
    //
    // var objects = sendFilter.GetObjectIds().Select(GisObjectId.FromEncodedString);
    //
    // Dictionary<string, Layer> layersByUri = map.GetLayersAsFlattenedList().ToDictionary(x => x.URI);
    // foreach (GisObjectId objectId in objects)
    // {
    //   Layer layer = layersByUri[objectId.LayerUri];
    //   switch (layer)
    //   {
    //     FeatureLayer b => b.get
    //   }
    // }
  }

  private Collection ConvertObjects(
    IReadOnlyCollection<MapMember> mapMembers,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken cancellationToken = default
  )
  {
    // var rootObjectCollection = new Collection { name = RhinoDoc.ActiveDoc.Name ?? "Unnamed document" };
    int count = 0;

    Collection rootObjectCollection = new(); //TODO: Collections

    foreach (MapMember gisObject in mapMembers)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var collectionHost = rootObjectCollection;
      var applicationId = gisObject.ToString();

      try
      {
        var converter = _converterFactory.ResolveScopedInstance();
        Base converted = converter.Convert(gisObject);
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
