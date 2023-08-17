using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  public interface IRevitCommitObjectBuilder
  {
    void BuildCommitObject(Base rootCommitObject);
    void IncludeObject(Base conversionResult, Element nativeElement);
  }
}
