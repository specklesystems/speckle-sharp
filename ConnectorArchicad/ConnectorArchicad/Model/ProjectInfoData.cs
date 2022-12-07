namespace Archicad.Model
{
  public sealed class ProjectInfoData
  {
    public string name { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty;
    public LengthUnit lengthUnit { get; set; } = 0;
    public AreaUnit areaUnit { get; set; } = 0;
    public VolumeUnit volumeUnit { get; set; } = 0;
    public AngleUnit angleUnit { get; set; } = 0;
  }

  public enum LengthUnit
  {
    Meter,
    Decimeter,
    Centimeter,
    Millimeter,
    FootFracInch,
    FootDecInch,
    DecFoot,
    FracInch,
    DecInch
  }

  public enum AreaUnit
  {
    SquareMeter,
    SquareCentimeter,
    SquareMillimeter,
    SquareFoot,
    SquareInch
  }

  public enum VolumeUnit
  {
    CubicMeter,
    Liter,
    CubicCentimeter,
    CubicMillimeter,
    CubicFoot,
    CubicInch,
    CubicYard,
    Gallon
  }

  public enum AngleUnit
  {
    DecimalDegree,
    DegreeMinSec,
    Grad,
    Radian,
    Surveyors
  }
}
