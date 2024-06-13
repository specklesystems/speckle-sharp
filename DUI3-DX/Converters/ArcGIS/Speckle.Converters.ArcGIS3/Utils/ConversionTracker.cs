using ArcGIS.Desktop.Mapping;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Utils;

/// <summary>
/// Container connecting the received Base object, converted hostApp object, dataset it was written to,
/// URI of the layer mapped from the dataset and, if applicable, feature/row id.
/// </summary>
public struct ObjectConversionTracker
{
  public Base Base { get; set; }
  public string NestedLayerName { get; set; }
  public ACG.Geometry? HostAppGeom { get; set; }
  public MapMember? HostAppMapMember { get; set; }
  public string? DatasetId { get; set; }
  public int? DatasetRow { get; set; }
  public string? MappedLayerURI { get; set; }
  public Exception? Exception { get; set; }

  public void AddException(Exception ex)
  {
    Exception = ex;
    HostAppGeom = null;
    DatasetId = null;
    DatasetRow = null;
    MappedLayerURI = null;
  }

  public void AddDatasetId(string datasetId)
  {
    DatasetId = datasetId;
  }

  public void AddDatasetRow(int datasetRow)
  {
    DatasetRow = datasetRow;
  }

  public void AddConvertedMapMember(MapMember mapMember)
  {
    HostAppMapMember = mapMember;
  }

  public void AddLayerURI(string layerURIstring)
  {
    MappedLayerURI = layerURIstring;
  }

  /// <summary>
  /// Initializes a new instance of <see cref="ObjectConversionTracker"/>.
  /// </summary>
  /// <param name="baseObj">Original received Base object.</param>
  /// <param name="nestedLayerName">String with the full traversed path to the object. Will be used to create nested layer structure in the TOC.</param>
  public ObjectConversionTracker(Base baseObj, string nestedLayerName)
  {
    Base = baseObj;
    NestedLayerName = nestedLayerName;
  }

  /// <summary>
  /// Constructor for received non-GIS geometries.
  /// Initializes a new instance of <see cref="ObjectConversionTracker"/>, accepting converted hostApp geometry.
  /// </summary>
  /// <param name="baseObj">Original received Base object.</param>
  /// <param name="nestedLayerName">String with the full traversed path to the object. Will be used to create nested layer structure in the TOC.</param>
  /// <param name="hostAppGeom">Converted ArcGIS.Core.Geometry.</param>
  public ObjectConversionTracker(Base baseObj, string nestedLayerName, ACG.Geometry hostAppGeom)
  {
    Base = baseObj;
    NestedLayerName = nestedLayerName;
    HostAppGeom = hostAppGeom;
  }

  /// <summary>
  /// Constructor for received native GIS layers.
  /// Initializes a new instance of <see cref="ObjectConversionTracker"/>, accepting datasetID of a coverted Speckle layer.
  /// </summary>
  /// <param name="baseObj">Original received Base object.</param>
  /// <param name="nestedLayerName">String with the full traversed path to the object. Will be used to create nested layer structure in the TOC.</param>
  /// <param name="datasetId">ID of the locally written dataset, created from received Speckle layer.</param>
  public ObjectConversionTracker(Base baseObj, string nestedLayerName, string datasetId)
  {
    Base = baseObj;
    NestedLayerName = nestedLayerName;
    DatasetId = datasetId;
  }
}
