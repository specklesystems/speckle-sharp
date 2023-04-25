namespace Objects.Structural.Loading;

public enum LoadType
{
  None,
  Dead,
  SuperDead,
  Soil,
  Live,
  LiveRoof,
  ReducibleLive,
  Wind,
  Snow,
  Rain,
  Thermal,
  Notional,
  Prestress,
  Equivalent,
  Accidental,
  SeismicRSA,
  SeismicAccTorsion,
  SeismicStatic,
  Other
}

public enum ActionType
{
  None,
  Permanent,
  Variable,
  Accidental
}

public enum BeamLoadType
{
  Point,
  Uniform,
  Linear,
  Patch,
  TriLinear
}

public enum FaceLoadType
{
  Constant,
  Variable,
  Point
}

public enum LoadDirection2D
{
  X,
  Y,
  Z
}

public enum LoadDirection
{
  X,
  Y,
  Z,
  XX,
  YY,
  ZZ
}

public enum CombinationType
{
  LinearAdd,
  Envelope,
  AbsoluteAdd,
  SRSS,
  RangeAdd // what's this?
}
