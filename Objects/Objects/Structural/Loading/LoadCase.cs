using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Loading;

public class LoadCase : Base // or LoadPattern? (per CSI)
{
  public LoadCase() { }

  /// <summary>
  /// A structural load case, a load case gives a way of grouping load effects together
  /// </summary>
  /// <param name="name">The name of the load case (the names of individual loads that are associated with the load case are defined elsewhere, in the loads themselves)</param>
  /// <param name="loadType">The type of the load case</param>
  /// <param name="group">A way of grouping load cases with the similar characteristics (ex. the source/mass source/origin of the loads)</param>
  /// <param name="actionType">The type of action of the load</param>
  /// <param name="description">A description of the load case</param>
  [SchemaInfo("Load Case", "Creates a Speckle structural load case", "Structural", "Loading")]
  public LoadCase(
    string name,
    LoadType loadType,
    string? group = null,
    ActionType actionType = ActionType.None,
    string? description = null
  )
  {
    this.name = name;
    this.loadType = loadType;
    this.group = group;
    this.actionType = actionType;
    this.description = description ?? "";
  }

  public string name { get; set; } //load case title, ex. "Dead load"
  public LoadType loadType { get; set; } //ex. Dead load
  public string? group { get; set; } //or load group, "A"
  public ActionType actionType { get; set; } //ex. Permanent
  public string description { get; set; } = ""; //category as alternative, ex. Offices – Cat.B, assembly area
}
