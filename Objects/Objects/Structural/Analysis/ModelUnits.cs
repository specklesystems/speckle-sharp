using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Analysis;

public class ModelUnits : Base
{
  public ModelUnits() { }

  [SchemaInfo(
    "ModelUnits",
    "Creates a Speckle object which specifies the units associated with the model",
    "Structural",
    "Analysis"
  )]
  public ModelUnits([SchemaParamInfo("Select a set of default units based on the unit system")] UnitsType unitsType)
  {
    if (unitsType == UnitsType.Metric)
    {
      length = "m";
      sections = "m";
      displacements = "m";
      stress = "Pa";
      force = "N";
      mass = "kg";
      time = "s";
      temperature = "K";
      velocity = "m/s";
      acceleration = "m/s2";
      energy = "J";
      angle = "deg";
    }
    if (unitsType == UnitsType.Imperial)
    {
      length = "ft";
      sections = "in";
      displacements = "in";
      stress = "kip/in2";
      force = "kip";
      mass = "lb";
      time = "s";
      temperature = "F";
      velocity = "ft/s";
      acceleration = "ft/s2";
      energy = "ft.lbf";
      angle = "deg";
    }
  }

  [SchemaInfo(
    "ModelUnits (custom)",
    "Creates a Speckle object which specifies the units associated with the model",
    "Structural",
    "Analysis"
  )]
  public ModelUnits(
    [SchemaParamInfo("Used for length and length derived units such as area")] string length = "m",
    [SchemaParamInfo("Used for cross-sectional properties")] string sections = "m",
    [SchemaParamInfo("Used for displacements and cross-sectional dimensions")] string displacements = "m",
    [SchemaParamInfo(
      "Used for stress (distinct from force and length) and stress related quantities like the elastic modulus"
    )]
      string stress = "Pa",
    [SchemaParamInfo("Used for force and force derived units such as moment, etc., but not for stress")]
      string force = "N",
    [SchemaParamInfo("Used for mass and mass derived units such as inertia")] string mass = "kg",
    [SchemaParamInfo("Used for time and time derived units, such as frequency")] string time = "s",
    [SchemaParamInfo("Used for temperature and temperature derived units such as coefficients of expansion")]
      string temperature = "K",
    [SchemaParamInfo("Used for velocity and velocity derived units")] string velocity = "m/s",
    [SchemaParamInfo(
      "Used for acceleration and acceleration derived units (considered as distinct from length and time units)"
    )]
      string acceleration = "m/s2",
    [SchemaParamInfo("Used for energy and energy derived units (considered as distinct from force and length units)")]
      string energy = "J",
    [SchemaParamInfo("To allow selection between degrees and radians for angle measures")] string angle = "deg"
  )
  {
    this.length = length;
    this.sections = sections;
    this.displacements = displacements;
    this.stress = stress;
    this.force = force;
    this.mass = mass;
    this.time = time;
    this.temperature = temperature;
    this.velocity = velocity;
    this.acceleration = acceleration;
    this.energy = energy;
  }

  // use enums instead of strings
  public string length { get; set; } // m, cm, mm, ft, in
  public string sections { get; set; } //m, cm, mm, ft, in
  public string displacements { get; set; } // m, cm, mm, ft, in
  public string stress { get; set; } //Pa, kPa, MPa, GPa, N/m², N/mm², kip/in², psi, psf, ksi
  public string force { get; set; } //N, kN, MN, lbf, kip, tf
  public string mass { get; set; } //kg, t, kt, g, lb, Ton, slug, kip.s²/in, kip.s²/ft, lbf.s²/in, lbf.s²/ft, kip
  public string time { get; set; } // s, ms, min, h, d
  public string temperature { get; set; } // °C, K, °F
  public string velocity { get; set; } //m/s, cm/s, mm/s, ft/s, in/s, km/h, mph
  public string acceleration { get; set; } //m/s², cm/s², mm/s², ft/s², in/s², g, %g, milli-g, Gal
  public string energy { get; set; } //J, KJ, MJ, GJ, kWh, in.lbf, ft.lbf, cal, Btu
  public string angle { get; set; } //deg, rad
  public string strain { get; set; } //ε, %ε, mε, με
}
