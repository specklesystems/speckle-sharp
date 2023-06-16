using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ConnectorRevit.Storage;
using ConverterRevitTestsShared;
using ConverterRevitTestsShared.AssertionClasses;
using DesktopUI2.Models;
using Objects.BuiltElements.Revit;
using Objects.Converter.Revit;
using Revit.Async;
using RevitSharedResources.Interfaces;
using Speckle.ConnectorRevit.UI;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using Xunit;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  public class SpeckleConversionTest
  {
    internal SpeckleConversionFixture fixture;

    public SpeckleConversionTest(SpeckleConversionFixture fixture)
    {
      this.fixture = fixture;
      this.fixture.TestClassName = GetType().Name;
    }

    internal async Task NativeToSpeckle()
    {
      await NativeToSpeckle(fixture.SourceDoc).ConfigureAwait(false);
    }
    internal async Task<List<Base>> NativeToSpeckle(UIDocument doc)
    {
      var converter = new ConverterRevit();
      converter.SetContextDocument(doc.Document);
      var commitSender = new SpeckleObjectLocalSender(converter, fixture);
      fixture.StreamState.CommitId = await ConnectorBindingsRevit.SendStreamTestable(fixture.StreamState, commitSender, new DesktopUI2.ViewModels.ProgressViewModel(), typeof(ConverterRevit), doc)
        .ConfigureAwait(false);

      var selectedObjects = ConnectorBindingsRevit.GetSelectionFilterObjects(converter, fixture.StreamState.Filter, fixture.StreamState.Settings, doc.Document);
      var convertedObjects = commitSender.GetConvertedObjects().ToList();
      var speckleObjectsToReceive = new List<Base>();
      foreach (var selectedObject in selectedObjects)
      {
        var convertedObject = convertedObjects.FirstOrDefault(b => b.applicationId == selectedObject.UniqueId);
        if (convertedObject == null)
        {
          throw new Exception($"could not find converted object corresponding to document element with uniqueId {selectedObject.UniqueId}. The element was either not converted or was converted without setting the applicationId");
        }
        AssertUtils.ValidSpeckleElement(
          selectedObject, 
          convertedObject
        );
        speckleObjectsToReceive.Add( convertedObject );
      }
      return speckleObjectsToReceive;
    }

    /// <summary>
    /// Gets elements from the fixture SourceDoc
    /// Converts them to Speckle
    /// Creates a new Doc (or uses the open one if open!)
    /// Converts the speckle objects to Native
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assert"></param>
    internal async Task SpeckleToNative()
    {
      var convertedObjs = await SpeckleToNative(fixture.SourceDoc).ConfigureAwait(false);
      SpeckleUtils.DeleteElement(convertedObjs.GetCreatedObjects());
    }
    
    /// <summary>
    /// Gets elements from the fixture SourceDoc
    /// Converts them to Speckle
    /// Creates a new Doc (or uses the open one if open!)
    /// Converts the speckle objects to Native
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assert"></param>
    internal async Task<IConvertedObjectsCache<Base,Element>> SpeckleToNative(
      UIDocument doc,
      IConvertedObjectsCache<Base, Element> previouslyConvertedObjectsCache = null
    )
    {
      var speckleObjectsToReceive = await NativeToSpeckle(doc).ConfigureAwait(false);

      var receivedObjects = await ConnectorBindingsRevit.ReceiveStreamTestable(
        fixture.StreamState,
        new SpeckleObjectLocalReceiver(fixture),
        new DesktopUI2.ViewModels.ProgressViewModel(), 
        typeof(ConverterRevit), 
        fixture.NewDoc
      ).ConfigureAwait(false);

      IAssertEqual assertionFactory = new AssertionFactory();

      var converter = new ConverterRevit();
      converter.SetContextDocument(doc.Document);
      foreach (var speckleObj in speckleObjectsToReceive)
      {
        if (!converter.CanConvertToNative(speckleObj))
        {
          continue;
        }

        var sourceObject = doc.Document.GetElement(speckleObj.applicationId);
        var destObject = receivedObjects.GetCreatedObjectsFromConvertedId(speckleObj.applicationId).First();

        if (previouslyConvertedObjectsCache != null)
        {
          // make sure the previousObject was updated instead of a new one being created
          var previousObject = previouslyConvertedObjectsCache.GetCreatedObjectsFromConvertedId(speckleObj.applicationId).First();

          // if previous object is not valid then it was deleted and re-created instead of updated
          if (previousObject.IsValidObject)
          {
            Assert.Equal(previousObject.UniqueId, destObject.UniqueId);
          }
        }

        await assertionFactory.Handle(sourceObject, destObject, speckleObj).ConfigureAwait(false);
      }

      return receivedObjects;
    }

    /// <summary>
    /// Runs SpeckleToNative with SourceDoc and UpdatedDoc
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assert"></param>
    internal async Task SpeckleToNativeUpdates()
    {
      var initialObjs = await SpeckleToNative(fixture.SourceDoc).ConfigureAwait(false);
      _ = await SpeckleToNative(fixture.UpdatedDoc, initialObjs).ConfigureAwait(false);

      SpeckleUtils.DeleteElement(initialObjs.GetCreatedObjects());
    }
  }
}
