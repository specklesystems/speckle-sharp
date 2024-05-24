using Autodesk.Revit.DB;
using Speckle.ProxyGenerator;
using Speckle.Revit2023.Interfaces;

namespace Speckle.Revit2023.Api;

[Proxy(typeof(Units), ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface)]
public partial interface IRevitUnitsProxy : IRevitUnits { }

[Proxy(
  typeof(FormatOptions),
  ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface
)]
public partial interface IRevitFormatOptionsProxy : IRevitFormatOptions { }
