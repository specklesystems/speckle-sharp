using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Analysis;

/// <summary>
/// Codes and standards references, model units, design settings, analysis settings, precision and tolerances
/// </summary>
public class ModelSettings : Base
{
  public ModelSettings() { }

  /// <summary>
  /// SchemaBuilder constructor for a model settings object
  /// </summary>
  /// <param name="modelUnits"></param>
  /// <param name="steelCode"></param>
  /// <param name="concreteCode"></param>
  /// <param name="coincidenceTolerance"></param>
  [SchemaInfo(
    "ModelSettings",
    "Creates a Speckle object which describes design and analysis settings for the structural model",
    "Structural",
    "Analysis"
  )]
  public ModelSettings(
    ModelUnits modelUnits = null,
    string steelCode = null,
    string concreteCode = null,
    double coincidenceTolerance = 10
  )
  {
    this.modelUnits = modelUnits == null ? new ModelUnits(UnitsType.Metric) : modelUnits;
    this.steelCode = steelCode;
    this.concreteCode = concreteCode;
    this.coincidenceTolerance = coincidenceTolerance;
  }

  /// <summary>
  /// Units object containing units information for key structural model quantities
  /// </summary>
  [DetachProperty]
  public ModelUnits modelUnits { get; set; }

  public string steelCode { get; set; } //could be enum
  public string concreteCode { get; set; } //could be enum
  public double coincidenceTolerance { get; set; }
}
