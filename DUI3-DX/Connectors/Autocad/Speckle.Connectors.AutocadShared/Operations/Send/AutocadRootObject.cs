using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.Connectors.Autocad.Operations.Send;

// Note: naming is a bit confusing, Root is similar to base commit object, or root commit object, etc. It might be just in my head (dim)
public record AutocadRootObject(DBObject Root, string ApplicationId);
