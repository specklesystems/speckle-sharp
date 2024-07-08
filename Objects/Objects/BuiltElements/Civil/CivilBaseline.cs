using System.Collections.Generic;

namespace Objects.BuiltElements.Civil;

public class CivilBaseline : Baseline<CivilAlignment, CivilProfile>
{
  public CivilBaseline() { }

  public CivilBaseline(
    string name,
    List<CivilBaselineRegion> regions,
    List<double> stations,
    double startStation,
    double endStation,
    CivilAlignment alignment,
    CivilProfile profile
  )
  {
    this.name = name;
    this.regions = regions;
    this.stations = stations;
    this.startStation = startStation;
    this.endStation = endStation;
    this.alignment = alignment;
    this.profile = profile;
    isFeaturelineBased = false;
  }

  public CivilBaseline(
    string name,
    List<CivilBaselineRegion> regions,
    List<double> stations,
    double startStation,
    double endStation,
    Featureline featureline
  )
  {
    this.name = name;
    this.regions = regions;
    this.stations = stations;
    this.startStation = startStation;
    this.endStation = endStation;
    this.featureline = featureline;
    isFeaturelineBased = true;
  }

  public List<CivilBaselineRegion> regions { get; set; }

  public List<double> stations { get; set; }

  public double startStation { get; set; }

  public double endStation { get; set; }
}
