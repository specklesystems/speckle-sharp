using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Civil;

public class CivilBaselineRegion : Base
{
  public CivilBaselineRegion() { }

  public CivilBaselineRegion(
    string name,
    double startStation,
    double endStation,
    string assemblyId,
    string? assemblyName,
    List<CivilAppliedAssembly> appliedAssemblies
  )
  {
    this.name = name;
    this.startStation = startStation;
    this.endStation = endStation;
    this.assemblyId = assemblyId;
    this.assemblyName = assemblyName;
    this.appliedAssemblies = appliedAssemblies;
  }

  /// <summary>
  /// The name of the region
  /// </summary>
  public string name { get; set; }

  /// <summary>
  /// The id of the assembly of the region
  /// </summary>
  public string assemblyId { get; set; }

  public string? assemblyName { get; set; }

  public double startStation { get; set; }

  public double endStation { get; set; }

  [DetachProperty]
  public List<CivilAppliedAssembly> appliedAssemblies { get; set; }

  public string units { get; set; }
}
