using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    /// <summary>
    /// Convert fabrication parts to speckle elements
    /// </summary>
    /// <param name="revitElement">Fabrication part in Revit</param>
    /// <param name="notes">Conversion notes for the exceptional cases</param>
    /// <returns>RevitElement which represents converted fabrication part as speckle object</returns>
    public RevitElement FabricationPartToSpeckle(FabricationPart revitElement, out List<string> notes)
    {
      return RevitElementToSpeckle(revitElement, out notes);
    }
  }
}