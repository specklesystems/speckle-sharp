using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using Autodesk.Revit.DB;
using DesktopUI2.Models;
using Speckle.Core.Kits;
using ConnectorRevit.Storage;

namespace ConnectorRevit.Operations
{
  public class ReceiveOperation
  {
    private ISpeckleConverter speckleConverter;
    private IConvertedObjectsCache<Base, Element> convertedObjectsCache;
    public IReceivedObjectIdMap<Base, Element> receivedObjectIdMap;

    public delegate ReceiveOperation Factory(
      StreamState streamState
    );
    public ReceiveOperation(
      IConvertedObjectsCache<Base, Element> convertedObjectsCache,
      StreamStateCache.Factory receivedObjectIdMapFactory,
      StreamState streamState,
      ISpeckleConverter speckleConverter
    )
    {
      this.speckleConverter = speckleConverter;
      this.convertedObjectsCache = convertedObjectsCache;
      this.receivedObjectIdMap = receivedObjectIdMapFactory(streamState);

      this.speckleConverter.SetContextDocument(null);
    }
  }
}
