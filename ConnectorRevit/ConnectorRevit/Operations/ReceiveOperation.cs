using System;
using System.Collections.Generic;
using System.Text;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using Autodesk.Revit.DB;
using static Speckle.ConnectorRevit.UI.ConnectorBindingsRevit;
using DesktopUI2.Models;
using Speckle.Core.Kits;
using ConnectorRevit.Storage;

namespace ConnectorRevit.Operations
{
  public class ReceiveOperation
  {
    private ISpeckleConverter speckleConverter;
    public IConvertedObjectsCache<Base, Element> convertedObjectsCache;
    public IReceivedObjectIdMap<Base, Element> receivedObjectIdMap;

    public delegate ReceiveOperation Factory(
      StreamState streamState
    );
    public ReceiveOperation(
      ISpeckleConverter converter,
      IConvertedObjectsCache<Base, Element> convertedObjectsCache,
      StreamStateCache.Factory receivedObjectIdMapFactory,
      StreamState streamState
    )
    {
      this.speckleConverter = converter;
      this.convertedObjectsCache = convertedObjectsCache;
      this.receivedObjectIdMap = receivedObjectIdMapFactory(streamState);
    }
  }
}
