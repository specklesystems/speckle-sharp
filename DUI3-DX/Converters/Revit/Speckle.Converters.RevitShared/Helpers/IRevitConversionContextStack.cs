using Autodesk.Revit.DB;
using Speckle.Converters.Common;

namespace Speckle.Converters.RevitShared.Helpers;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "See base class justification"
)]
// POC: so this should *probably* be Document and NOT UI.UIDocument, the former is Conversion centric
// and the latter is more for connector
public interface IRevitConversionContextStack : IConversionContextStack<Document, ForgeTypeId> { }
