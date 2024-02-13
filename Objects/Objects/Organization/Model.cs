using System.Collections.Generic;
using Objects.Other;
using Speckle.Core.Models;

namespace Objects.Organization;

/// <summary>
/// Basic model info class
/// It contains general information about the model and can be extended or subclassed to include more application-specific
/// information.
/// </summary>
public class ModelInfo : Base
{
  /// <summary>
  /// The name of the model.
  /// </summary>
  public string name { get; set; }

  /// <summary>
  /// The identifying number of the model.
  /// </summary>
  public string number { get; set; }

  //  TODO: not sure about adding a typed `elements` list here? prob should let ppl add whatever named categories here?
}

//  TODO: not quite sure about this name?
/// <summary>
/// Extended <see cref="ModelInfo"/> to contain additional properties applicable to AEC projects.
/// </summary>
public class BIMModelInfo : ModelInfo
{
  /// <summary>
  /// The name of the client
  /// </summary>
  public string clientName { get; set; }

  /// <summary>
  /// The name of the building
  /// </summary>
  public string buildingName { get; set; }

  /// <summary>
  /// The status or phase of the model.
  /// </summary>
  public string status { get; set; }

  /// <summary>
  /// The address of the model.
  /// </summary>
  public string address { get; set; }

  /// <summary>
  /// The name of the site location as a string.
  /// </summary>
  public string siteName { get; set; }

  /// <summary>
  /// The latitude of the site location in radians.
  /// </summary>
  public double latitude { get; set; }

  /// <summary>
  /// The longitude of the site location in radians.
  /// </summary>
  public double longitude { get; set; }

  /// <summary>
  /// A list of origin locations within this model as a list of <see cref="Transform"/>s
  /// </summary>
  public List<Base> locations { get; set; }
}
