using System.Collections.Generic;
using Objects.Other;
using Speckle.Core.Models;

namespace Objects.Organization;

/// <summary>
/// Represents a model from an authoring application and can be used as the root commit object when sending.
/// It contains <see cref="ModelInfo"/> and <see cref="Setting"/> objects
/// </summary>
public class Model : Collection
{
  public Model() { }

  public Model(ModelInfo info, List<Setting>? settings = null)
  {
    this.info = info;
    this.settings = settings;
  }

  /// <summary>
  /// General model-wide information stored in a <see cref="ModelInfo"/> object.
  /// This may include anything from simply a project / file name to specific location information (eg with <see cref="BIMModelInfo"/>)
  /// </summary>
  public ModelInfo info { get; set; }

  [System.Obsolete("These are not being used")]
  public List<Setting>? settings { get; set; }
}

/// <summary>
/// Basic model info class to be attached to the <see cref="Model.info"/> field on a <see cref="Model"/> object.
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
/// Extended <see cref="ModelInfo"/> to be attached to the <see cref="Model.info"/> field on a <see cref="Model"/> object.
/// This contains additional properties applicable to AEC projects.
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

public class Setting : Base
{
  /// <summary>
  /// The name of the setting
  /// </summary>
  public string name { get; set; }

  /// <summary>
  /// The objects selected in the setting
  /// </summary>
  public List<Base> selection { get; set; }
}
