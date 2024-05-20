using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.Connectors.Autocad.Operations.Send;

public record AutocadRootObject(DBObject Root, string ApplicationId);
