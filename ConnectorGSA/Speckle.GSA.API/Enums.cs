using Speckle.GSA.API.GwaSchema;

namespace Speckle.GSA.API
{
  public enum GSALayer
  {
    Design,
    Analysis,
    Both
  }

  public enum MessageLevel
  {
    Debug,
    Information,
    Error,
    Fatal
  }

  public enum MessageIntent
  {
    Display,
    TechnicalLog,
    Telemetry
  }

  public enum StreamContentConfig
  {
    None = 0,
    ModelOnly = 1,
    ModelAndResults = 2
  }

  public enum ResultGroup
  {
    Unknown = 0,
    Node = 1,
    Element1d = 2,
    Element2d = 3,
    Assembly = 4
  }

  public enum ResultType
  {
    NodalDisplacements = 0,
    NodalVelocity = 1,
    NodalAcceleration = 2,
    NodalReaction = 3,
    ConstraintForces = 4,
    Element1dDisplacement = 5,
    Element1dForce = 6,
    Element2dDisplacement = 7,
    Element2dProjectedMoment = 8,
    Element2dProjectedForce = 9,
    Element2dProjectedStressBottom = 10,
    Element2dProjectedStressMiddle = 11,
    Element2dProjectedStressTop = 12,
    AssemblyForcesAndMoments = 13
  }
}
