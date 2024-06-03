using System.Diagnostics.CodeAnalysis;
#pragma warning disable CA1040

namespace Speckle.Revit2023.Interfaces;

public interface IRevitUnits
{
  IRevitFormatOptions GetFormatOptions(IRevitForgeTypeId length);
}

[SuppressMessage("Design", "CA1040:Avoid empty interfaces")]
public interface IRevitFormatOptions
{
  IRevitForgeTypeId GetUnitTypeId();
}

public interface IRevitUnitUtils
{
  double ConvertFromInternalUnits(double value, IRevitForgeTypeId forgeTypeId);
}



#pragma warning restore CA1040
