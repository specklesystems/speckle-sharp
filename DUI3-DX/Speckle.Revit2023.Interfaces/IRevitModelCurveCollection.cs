using System.Diagnostics.CodeAnalysis;

namespace Speckle.Revit2023.Interfaces;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public interface IRevitModelCurveCollection : IReadOnlyList<IRevitModelCurve> { }
