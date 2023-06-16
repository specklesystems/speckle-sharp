using Autodesk.Revit.DB;
using Speckle.Core.Models;
using System.Threading.Tasks;

namespace ConverterRevitTestsShared.AssertionClasses
{
  internal interface IAssertEqual
  {
    public Task Handle<T>(T sourceElement, T destElement, Base speckleElement) where T : Element;
  }
}
