using Autodesk.Revit.DB.ExtensibleStorage;

namespace Speckle.Connectors.Revit.HostApp;

public interface IStorageSchema
{
  Schema GetSchema();
}
