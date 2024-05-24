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
  new[] { "PlanTopology", "PlanTopologies", "TypeOfStorage", "Equals" }
)]
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]
public partial interface IRevitDocumentProxy : IRevitDocument { }

[Proxy(
  typeof(ForgeTypeId),
  ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface,
  new[] { "Equals" }
)]
public partial interface IRevitForgeTypeIdProxy : IRevitForgeTypeId { }
