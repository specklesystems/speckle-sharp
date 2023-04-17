namespace Objects.Structural.CSI.Properties;

public enum DiaphragmOption
{
  Disconnect,
  FromShellObject,
  DefinedDiaphragm
}

public enum NonLinearOptions
{
  Linear,
  CompressionOnly,
  TensionOnly
}

public enum SpringOption
{
  Link,
  SoilProfileFooting
}

public enum ModelingOption
{
  Loads,
  Elements
}

public enum DesignProcedure
{
  ProgramDetermined,
  SteelFrameDesign,
  ConcreteFrameDesign,
  CompositeBeamDesign,
  SteelJoistDesign,
  NoDesign,
  CompositeColumnDesign
}
