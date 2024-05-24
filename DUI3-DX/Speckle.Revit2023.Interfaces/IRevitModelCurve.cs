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
