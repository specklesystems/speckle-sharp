namespace Objects.Structural;

public enum AxisType
{
  Cartesian,
  Cylindrical,
  Spherical
}

public enum LoadAxisType
{
  Global,
  Local, // local element axes
  DeformedLocal // element local axis that is embedded in the element as it deforms
}
