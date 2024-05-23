using System.Diagnostics.CodeAnalysis;
using Autodesk.Revit.DB;
using Speckle.InterfaceGenerator;
using Speckle.ProxyGenerator;

namespace RevitGenerator;

[Proxy(typeof(Document), new [] {"PlanTopology", "PlanTopologies", "TypeOfStorage", "Equals"})]
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]

public partial interface IRevitDocument
{
}

[GenerateAutoInterface]
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]
[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
public partial class DocumentProxy : IDocumentProxy
{
}
