using System.Diagnostics.CodeAnalysis;
#pragma warning disable CA1040

namespace Speckle.Revit2023.Interfaces;

public interface IRevitDocument
{
  string PathName { get; }
  IRevitUnits GetUnits();
}

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public interface IRevitModelCurveCollection : IEnumerable<IRevitModelCurve> { }

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

public interface IRevitUnits
{
  IRevitFormatOptions GetFormatOptions(IRevitForgeTypeId length);
}

public interface IRevitForgeTypeId { }

public interface IRevitFormatOptions { }
