using System.Diagnostics.CodeAnalysis;
using Autodesk.Revit.DB;
using Speckle.ProxyGenerator;
using Speckle.Revit2023.Interfaces;
#pragma warning disable CA1010
#pragma warning disable CA1710

namespace Speckle.Revit2023.Api;

[Proxy(
  typeof(Document),
  ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface,
  ProxyClassAccessibility.Public,
  new[] { "PlanTopology", "PlanTopologies", "TypeOfStorage", "Equals" }
)]
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]
public partial interface IRevitDocumentProxy : IRevitDocument { }

[Proxy(
  typeof(ModelCurveArray),
  ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface,
  ProxyClassAccessibility.Public,
  new[] { "GetEnumerator", "Item" }
)]
public partial interface IRevitModelCurveCollectionProxy : IRevitModelCurveCollection { }

[Proxy(typeof(Curve), ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface)]
public partial interface IRevitCurveProxy : IRevitCurve { }

[Proxy(typeof(Units), ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface)]
public partial interface IRevitUnitsProxy : IRevitUnits { }

[Proxy(
  typeof(ForgeTypeId),
  ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface,
  new[] { "Equals" }
)]
public partial interface IRevitForgeTypeIdProxy : IRevitForgeTypeId { }

[Proxy(
  typeof(FormatOptions),
  ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface
)]
public partial interface IRevitFormatOptionsProxy : IRevitFormatOptions { }
