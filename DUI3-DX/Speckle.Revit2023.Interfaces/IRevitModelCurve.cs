using System.Diagnostics.CodeAnalysis;

namespace Speckle.Revit2023.Interfaces;

public interface IRevitModelCurve : IRevitCurveElement { }

public interface IRevitCurveElement
{
  IRevitCurve GeometryCurve { get; }
}

public interface IRevitCurve
{
  IRevitXYZ GetEndPoint(int index);
  double Length { get; }
}

public interface IRevitXYZ
{
  double DistanceTo(IRevitXYZ source);
}


[SuppressMessage("Design", "CA1040:Avoid empty interfaces")]
public interface IRevitLocation
{
}

public interface IRevitLocationPoint : IRevitLocation
{
  IRevitXYZ Point { get; }
}

public interface IRevitLocationCurve : IRevitLocation
{
  IRevitCurve Curve { get; }
}
