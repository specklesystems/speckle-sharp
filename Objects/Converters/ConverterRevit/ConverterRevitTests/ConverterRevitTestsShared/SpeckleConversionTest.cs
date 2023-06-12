using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ConnectorRevit.Storage;
using ConverterRevitTestsShared;
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
    internal async Task NativeToSpeckle(UIDocument doc)
    {
      var converter = new ConverterRevit();
      converter.SetContextDocument(doc.Document);
      var commitSender = new LocalCommitSender(converter, fixture);
      fixture.StreamState.CommitId = await ConnectorBindingsRevit.SendStreamTestable(fixture.StreamState, commitSender, new DesktopUI2.ViewModels.ProgressViewModel(), typeof(ConverterRevit), doc)
        .ConfigureAwait(false);

      var selectedObjects = ConnectorBindingsRevit.GetSelectionFilterObjects(fixture.StreamState.Filter, fixture.StreamState.Settings, doc.Document);
      var convertedObjects = commitSender.GetConvertedObjects().ToList();
      foreach (var selectedObject in selectedObjects)
      {
        AssertUtils.ValidSpeckleElement(
          selectedObject, 
          convertedObjects.FirstOrDefault(b => b.applicationId == selectedObject.UniqueId)
        );
      }
    }

    /// <summary>
    /// Gets elements from the fixture SourceDoc
    /// Converts them to Speckle
    /// Creates a new Doc (or uses the open one if open!)
    /// Converts the speckle objects to Native
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assert"></param>
    internal async Task SpeckleToNative<T>(
      Action<T, T> assert,
      Func<T, T, Task> assertAsync = null
    ) where T : Element
    {
      var convertedObjs = await SpeckleToNative(fixture.SourceDoc, assert, assertAsync).ConfigureAwait(false);
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
    internal async Task<IConvertedObjectsCache<Base,Element>> SpeckleToNative<T>(
      UIDocument doc,
      Action<T, T> assert,
      Func<T, T, Task> assertAsync,
      IConvertedObjectsCache<Base, Element> previouslyConvertedObjectsCache = null
    ) where T : Element
    {
      await NativeToSpeckle(doc).ConfigureAwait(false);
      var receivedObjects = await ConnectorBindingsRevit.ReceiveStreamTestable(
        fixture.StreamState,
        new IntegrationTestCommitReceiver(fixture), 
        new DesktopUI2.ViewModels.ProgressViewModel(), 
        typeof(ConverterRevit), 
        fixture.NewDoc
      ).ConfigureAwait(false);

      var sourceObjects = ConnectorBindingsRevit.GetSelectionFilterObjects(fixture.StreamState.Filter, fixture.StreamState.Settings, doc.Document);

      foreach (var obj in sourceObjects)
      {
        var sourceObject = (T)obj;
        var destObject = (T)receivedObjects.GetCreatedObjectsFromConvertedId(obj.UniqueId).First();

        if (previouslyConvertedObjectsCache != null)
        {
          // make sure the previousObject was updated instead of a new one being created
          var previousObject = previouslyConvertedObjectsCache.GetCreatedObjectsFromConvertedId(obj.UniqueId).First();
          Assert.Equal(previousObject.UniqueId, destObject.UniqueId);
        }
        
        assert?.Invoke(sourceObject, destObject);
        if (assertAsync != null)
        {
          await assertAsync.Invoke(sourceObject, destObject).ConfigureAwait(false);
        }
      }

      return receivedObjects;
    }

    /// <summary>
    /// Runs SpeckleToNative with SourceDoc and UpdatedDoc
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assert"></param>
    internal async Task SpeckleToNativeUpdates<T>(Action<T, T> assert, Func<T, T, Task> assertAsync = null)
      where T : Element
    {
      var initialObjs = await SpeckleToNative(fixture.SourceDoc, assert, assertAsync).ConfigureAwait(false);
      _ = await SpeckleToNative(fixture.UpdatedDoc, assert, assertAsync, initialObjs).ConfigureAwait(false);

      SpeckleUtils.DeleteElement(initialObjs.GetCreatedObjects());
    }

    internal async Task SelectionToNative<T>(Action<T, T> assert, Func<T, T, Task> assertAsync = null)
    {
      ConverterRevit converter = new ConverterRevit();
      converter.SetContextDocument(fixture.SourceDoc);
      var spkElems = fixture.Selection.Select(x => converter.ConvertToSpeckle(x) as Base).ToList();

      converter = new ConverterRevit();
      converter.SetContextDocument(fixture.NewDoc);
      converter.SetContextDocument(new StreamStateCache(new StreamState()));
      var revitEls = new List<object>();

      await SpeckleUtils.RunInTransaction(
        () =>
        {
          //xru.RunInTransaction(() =>
          //{
          foreach (var el in spkElems)
          {
            var res = converter.ConvertToNative(el);
            if (res is List<ApplicationObject> apls)
              revitEls.AddRange(apls);
            else
              revitEls.Add(res);
          }
          //}, fixture.NewDoc).Wait();
        },
        fixture.NewDoc.Document,
        converter
      );

      Assert.Equal(0, converter.Report.ConversionErrorsCount);

      for (var i = 0; i < revitEls.Count; i++)
      {
        var sourceElem = (T)(object)fixture.Selection[i];
        var destElement = (T)((ApplicationObject)revitEls[i]).Converted.FirstOrDefault();
        assert?.Invoke(sourceElem, destElement);
        if (assertAsync != null)
        {
          await assertAsync.Invoke(sourceElem, destElement);
        }
      }
      SpeckleUtils.DeleteElement(revitEls);
    }
  }

  public class UpdateData
  {
    public Document Doc { get; set; }
    public IList<Element> Elements { get; set; }
    public List<ApplicationObject> AppPlaceholders { get; set; }
  }
}
