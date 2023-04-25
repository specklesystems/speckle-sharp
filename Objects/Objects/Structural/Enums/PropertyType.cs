namespace Objects.Structural;

public enum PropertyType2D
{
  Stress,
  Fabric,
  Plate,
  Shell,
  Curved,
  Wall,
  Strain,
  Axi,
  Load
}

public enum ReferenceSurface
{
  Top,
  Middle,
  Bottom
  //TOP_CENTRE, CENTROID,BOT_CENTRE
}

public enum PropertyType3D
{
  Solid,
  Infinite
}

public enum PropertyTypeSpring
{
  Axial,
  Torsional,
  General,
  Matrix,
  TensionOnly,
  CompressionOnly,
  Connector,
  LockUp,
  Gap,
  Friction
  //Translational, //old
  //Rotational //old
}

public enum PropertyTypeDamper
{
  Axial, //translational
  Torsional, //rotational
  General
}

public enum BaseReferencePoint
{
  Centroid,
  TopLeft,
  TopCentre,
  TopRight,
  MidLeft,
  MidRight,
  BotLeft,
  BotCentre,
  BotRight
}

public enum ShapeType
{
  Rectangular,
  Circular,
  I,
  Tee,
  Angle,
  Channel,
  Perimeter,
  Box,
  Catalogue,
  Explicit,
  Undefined
}
