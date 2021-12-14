using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Structural.ETABS.Properties
{
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
  public enum ModelingOption {
  Loads,
  Elements
  }
}
