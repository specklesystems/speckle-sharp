using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using RevitElement = Autodesk.Revit.DB.Element;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit
  {
    /// <summary>
    /// Adds a new stream to the file. It takes a stream state, gets the Revit elements
    /// the user wants to add using the filter, and writes the StreamState to the file.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override void AddNewStream( StreamState state )
    {
      // add stream and related data to the class
      LocalStateWrapper.StreamStates.Add( state );

      if (state.Filter != null)
        GetSelectionFilterObjects( state.Filter, state.AccountId, state.Stream.id );
    }

    /// <summary>
    /// Update the stream state and adds adds the filtered objects
    /// </summary>
    /// <param name="state"></param>
    public override void UpdateStream( StreamState state )
    {
      var index = LocalStateWrapper.StreamStates.FindIndex( b => b.Stream.id == state.Stream.id );
      LocalStateWrapper.StreamStates[index] = state;

      GetSelectionFilterObjects( state.Filter, state.AccountId, state.Stream.id );
    }

    /// <summary>
    /// Converts the Revit elements that have been added to the stream by the user, sends them to
    /// the Server and the local DB, and creates a commit with the objects.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override async Task<StreamState> SendStream( StreamState state )
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter( Applications.Revit );
      converter.SetContextDocument( CurrentDoc.Document );

      var objsToConvert = state.Placeholders.Union( state.Objects, comparer: new ApplicationObjectComparer() );
      var streamId = state.Stream.id;
      var client = state.Client;


      var convertedObjects = new List<Base>();
      var failedConversions = new List<RevitElement>();

      var units = CurrentDoc.Document.GetUnits().GetFormatOptions( UnitType.UT_Length ).DisplayUnits.ToString()
        .ToLowerInvariant().Replace( "dut_", "" );
      // InjectScaleInKits(GetScale(units)); // this is used for feet to sane units conversion.

      var errorMsg = "";
      var errors = new List<SpeckleException>();

      foreach (var obj in objsToConvert)
      {
        // TODO: why is this one fkin model curve losing its application id???
        RevitElement revitElement = null;
        if (obj.applicationId != null)
          revitElement = CurrentDoc.Document.GetElement( obj.applicationId );

        if (revitElement == null)
        {
          errors.Add( new SpeckleException( message: $"Could not retrieve element: {obj.speckle_type}" ) );
          continue;
        }

        var conversionResult = converter.ConvertToSpeckle( revitElement );
        if (conversionResult == null)
        {
          // TODO what happens to failed conversions?
          failedConversions.Add( revitElement );
          continue;
        }

        convertedObjects.Add( conversionResult );
      }

      if (errors.Any() || converter.ConversionErrors.Any())
      {
        errorMsg = string.Format( "There {0} {1} failed conversion{2} and {3} error{4}",
          converter.ConversionErrors.Count() == 1 ? "is" : "are",
          converter.ConversionErrors.Count(),
          converter.ConversionErrors.Count() == 1 ? "" : "s",
          errors.Count(),
          errors.Count() == 1 ? "" : "s" );
        Log.CaptureException( new SpeckleException( errorMsg ) );
      }

      var transports = new List<ITransport>() { new ServerTransport( client.Account, streamId ) };
      var emptyBase = new Base();
      var @base = new Base { ["@data"] = convertedObjects };
      var objectId = "";
      Execute.PostToUIThread( () => state.Progress.Maximum = (int)@base.GetTotalChildrenCount() );

      if (state.CancellationToken.IsCancellationRequested) return null;

      objectId = await Operations.Send( @base, state.CancellationToken, transports,
        onProgressAction: dict => UpdateProgress( dict, state.Progress ) );

      if (state.CancellationToken.IsCancellationRequested) return null;

      var objByType = convertedObjects.GroupBy( o => o.speckle_type );
      var convertedTypes = objByType.Select(
        grouping => $"{grouping.Count()} {grouping.Key.Split( '.' ).Last()}s" ).ToList();

      var res = await client.CommitCreate( new CommitCreateInput()
      {
        streamId = streamId,
        objectId = objectId,
        branchName = "main",
        message =
          $"Added {convertedObjects.Count()} elements from Revit: {string.Join( ", ", convertedTypes )}. " +
          $"There were {converter.ConversionErrors.Count} failed conversions."
      } );

      // update the state
      state.Objects = convertedObjects;
      state.Placeholders = new List<Base>(); // just clearing doesn't raise prop changed notif
      state.Stream = await client.StreamGet( streamId );

      // Persist state to revit file
      WriteStateToFile();

      RaiseNotification( $"{convertedObjects.Count()} objects sent to Speckle 🚀" );
      return state;
    }

    public override async Task<StreamState> ReceiveStream( StreamState state )
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter( Applications.Revit );
      converter.SetContextDocument( CurrentDoc.Document );

      var transport = new ServerTransport( state.Client.Account, state.Stream.id );
      var newStream = await state.Client.StreamGet( state.Stream.id );
      var commit = newStream.branches.items[0].commits.items[0];
      Base commitObject;

      if (state.CancellationToken.IsCancellationRequested) return null;

      commitObject = await Operations.Receive( commit.referencedObject, state.CancellationToken, transport,
        onProgressAction: dict => UpdateProgress( dict, state.Progress ),
        onTotalChildrenCountKnown: count => Execute.PostToUIThread( () => state.Progress.Maximum = count ) );

      if (state.CancellationToken.IsCancellationRequested) return null;

      var newObjects = FlattenStreamData( commitObject );
      var oldObjects = state.Objects;


      // TODO: edit objects from connector so we don't need to delete and recreate everything
      // var toDelete = oldObjects.Except(newObjects, new BaseObjectComparer()).ToList();
      // var toCreate = newObjects;
      var toDelete = oldObjects;
      var toUpdate = newObjects;

      var revitElements = new List<object>();
      var errors = new List<SpeckleException>();

      // TODO diff stream states

      // delete
      Queue.Add( () =>
       {
         using (var t = new Transaction( CurrentDoc.Document, $"Speckle Delete: ({state.Stream.id})" ))
         {
           t.Start();
           foreach (var oldObj in toDelete)
           {
             var revitElement = CurrentDoc.Document.GetElement( oldObj.applicationId );
             if (revitElement == null)
             {
               errors.Add( new SpeckleException( message: "Could not retrieve element" ) );
               Debug.WriteLine(
                 $"Could not retrieve element (id: {oldObj.applicationId}, type: {oldObj.speckle_type})" );
               continue;
             }

             CurrentDoc.Document.Delete( revitElement.Id );
           }

           t.Commit();
         }
       } );
      Executor.Raise();

      // update or create
      Queue.Add( () =>
       {
         using (var t = new Transaction( CurrentDoc.Document, $"Speckle Receive: ({state.Stream.id})" ))
         {
           // TODO `t.SetFailureHandlingOptions`
           t.Start();
           revitElements = converter.ConvertToNative( toUpdate );
           t.Commit();
         }
       } );
      Executor.Raise();

      if (errors.Any() || converter.ConversionErrors.Any())
      {
        var convErrors = converter.ConversionErrors.Count;
        var err = errors.Count;
        Log.CaptureException( new SpeckleException(
          $"{convErrors} conversion error{Formatting.PluralS( convErrors )} and {err} error{Formatting.PluralS( err )}" ) );
      }

      state.Stream = newStream;
      state.Objects = newObjects;
      WriteStateToFile();
      RaiseNotification( $"Deleting {toDelete.Count} elements and updating {toUpdate.Count} elements..." );

      return state;
    }

    public override void BakeStream( string args )
    {
      throw new NotImplementedException();
    }

    private void UpdateProgress( ConcurrentDictionary<string, int> dict, ProgressReport progress )
    {
      if (progress == null) return;
      Execute.PostToUIThread( () => progress.Value = dict.Values.Last() );
    }

    /// <summary>
    /// Pass selected element ids to UI
    /// </summary>
    /// <param name="args"></param>
    public override List<string> GetSelectedObjects()
    {
      var selectedObjects = CurrentDoc != null
        ? CurrentDoc.Selection.GetElementIds().Select(
          id => CurrentDoc.Document.GetElement( id ).UniqueId ).ToList()
        : new List<string>();

      return selectedObjects;
    }

    public override List<string> GetObjectsInView()
    {
      var collector = new FilteredElementCollector( CurrentDoc.Document, CurrentDoc.Document.ActiveView.Id )
        .WhereElementIsNotElementType();
      var elementIds = collector.ToElements().Select( el => el.UniqueId );

      return new List<string>( elementIds );
    }

    public override void RemoveSelectionFromClient( string args )
    {
      throw new NotImplementedException();
    }

    public override void RemoveStream( string streamId )
    {
      var streamState = LocalStateWrapper.StreamStates.FirstOrDefault(
        cl => cl.Stream.id == streamId
      );
      LocalStateWrapper.StreamStates.Remove( streamState );
      WriteStateToFile();
    }

    #region private methods

    private List<Base> FlattenStreamData( Base commit )
    {
      var list = new List<Base>();
      foreach (var member in commit.GetDynamicMemberNames())
      {
        RecursivelyFlattenStream( commit[member], list );
      }
      return list;
    }

    private void RecursivelyFlattenStream( object @object, List<Base> list )
    {
      if (@object is Base @base)
        list.Add( @base );
      else if (@object is List<object> objects)
      {
        foreach (var o in objects)
        {
          RecursivelyFlattenStream( o, list );
        }
      }
    }

    private Type GetFilterType( string typeString )
    {
      Assembly ass = typeof( ISelectionFilter ).Assembly;
      return ass.GetType( typeString );
    }

    /// <summary>
    /// Given the filter in use by a stream returns the document elements that match it
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="accountId"></param>
    /// <param name="streamId"></param>
    /// <returns></returns>
    private IEnumerable<Base> GetSelectionFilterObjects( ISelectionFilter filter, string accountId, string streamId )
    {
      var doc = CurrentDoc.Document;
      IEnumerable<Base> objects = new List<Base>();

      var selectionIds = new List<string>();

      switch (filter.Name)
      {
        case "Selection":
          {
            var selFilter = filter as ElementsSelectionFilter;
            selectionIds = selFilter.Selection;
            break;
          }
        case "Category":
          {
            var catFilter = filter as ListSelectionFilter;
            var bics = new List<BuiltInCategory>();
            var categories = Globals.GetCategories( doc );
            IList<ElementFilter> elementFilters = new List<ElementFilter>();

            foreach (var cat in catFilter.Selection)
            {
              elementFilters.Add( new ElementCategoryFilter( categories[cat].Id ) );
            }

            var categoryFilter = new LogicalOrFilter( elementFilters );

            selectionIds = new FilteredElementCollector( doc )
              .WhereElementIsNotElementType()
              .WhereElementIsViewIndependent()
              .WherePasses( categoryFilter )
              .Select( x => x.UniqueId ).ToList();
            break;
          }
        case "View":
          {
            var viewFilter = filter as ListSelectionFilter;

            var views = new FilteredElementCollector( doc )
              .WhereElementIsNotElementType()
              .OfClass( typeof( View ) )
              .Where( x => viewFilter.Selection.Contains( x.Name ) );

            foreach (var view in views)
            {
              var ids = new FilteredElementCollector( doc, view.Id )
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .Where( x => x.IsPhysicalElement() )
                .Select( x => x.UniqueId ).ToList();

              selectionIds = selectionIds.Union( ids ).ToList();
            }

            break;
          }
        case "Parameter":
          try
          {
            var propFilter = filter as PropertySelectionFilter;
            var query = new FilteredElementCollector( doc )
              .WhereElementIsNotElementType()
              .WhereElementIsNotElementType()
              .WhereElementIsViewIndependent()
              .Where( x => x.IsPhysicalElement() )
              .Where( fi => fi.LookupParameter( propFilter.PropertyName ) != null );

            propFilter.PropertyValue = propFilter.PropertyValue.ToLowerInvariant();

            switch (propFilter.PropertyOperator)
            {
              case "equals":
                query = query.Where( fi =>
                   GetStringValue( fi.LookupParameter( propFilter.PropertyName ) ) == propFilter.PropertyValue );
                break;
              case "contains":
                query = query.Where( fi =>
                   GetStringValue( fi.LookupParameter( propFilter.PropertyName ) ).Contains( propFilter.PropertyValue ) );
                break;
              case "is greater than":
                query = query.Where( fi => UnitUtils.ConvertFromInternalUnits(
                                             fi.LookupParameter( propFilter.PropertyName ).AsDouble(),
                                             fi.LookupParameter( propFilter.PropertyName ).DisplayUnitType ) >
                                           double.Parse( propFilter.PropertyValue ) );
                break;
              case "is less than":
                query = query.Where( fi => UnitUtils.ConvertFromInternalUnits(
                                             fi.LookupParameter( propFilter.PropertyName ).AsDouble(),
                                             fi.LookupParameter( propFilter.PropertyName ).DisplayUnitType ) <
                                           double.Parse( propFilter.PropertyValue ) );
                break;
            }

            selectionIds = query.Select( x => x.UniqueId ).ToList();
          }
          catch (Exception e)
          {
            Log.CaptureException( e );
          }

          break;
      }

      // LOCAL STATE management
      objects = selectionIds.Select( id =>
       {
         var temp = new Base();
         temp.applicationId = id;
         temp["__type"] = "Placeholder";
         return temp;
       } );

      var streamState = LocalStateWrapper.StreamStates.FirstOrDefault(
        cl => (string)cl.Stream.id == (string)streamId
      );
      streamState.Placeholders.AddRange( objects );

      // Persist state and clients to revit file
      WriteStateToFile();
      var plural = objects.Count() == 1 ? "" : "s";

      if (objects.Any())
        NotifyUi( new RetrievedFilteredObjectsEvent()
        {
          Notification = $"You have added {objects.Count()} object{plural} to this stream.",
          AccountId = accountId,
          Objects = objects
        } );
      RaiseNotification( $"You have added {objects.Count()} object{plural} to this stream." );

      return objects;
    }

    private string GetStringValue( Parameter p )
    {
      string value = "";
      if (!p.HasValue)
        return value;
      if (string.IsNullOrEmpty( p.AsValueString() ) && string.IsNullOrEmpty( p.AsString() ))
        return value;
      if (!string.IsNullOrEmpty( p.AsValueString() ))
        return p.AsValueString().ToLowerInvariant();
      else
        return p.AsString().ToLowerInvariant();
    }

    private class BaseObjectComparer : IEqualityComparer<Base>
    {
      /// <summary>
      /// compares two speckle objects for equality based on both:
      /// matching application id and matching speckle id
      /// </summary>
      /// <param name="obj1"></param>
      /// <param name="obj2"></param>
      /// <returns></returns>
      public bool Equals( Base obj1, Base obj2 )
      {
        if (ReferenceEquals( obj1, obj2 ))
          return true;
        if (obj1 == null || obj2 == null)
          return false;
        return obj1.applicationId == obj2.applicationId && obj1.id == obj2.id;
      }

      public int GetHashCode( Base obj )
      {
        return base.GetHashCode();
      }
    }

    private class ApplicationObjectComparer : IEqualityComparer<Base>
    {
      /// <summary>
      /// compares two speckle objects for equality based on matching applications id.
      /// mainly for comparing objects from the StreamState.Placeholders list (no speckle id)
      /// and the StreamState.Objects list (does have speckle id)
      /// </summary>
      /// <param name="obj1"></param>
      /// <param name="obj2"></param>
      /// <returns></returns>
      public bool Equals( Base obj1, Base obj2 )
      {
        if (ReferenceEquals( obj1, obj2 ))
          return true;
        if (obj1 == null || obj2 == null)
          return false;
        return obj1.applicationId == obj2.applicationId;
      }

      public int GetHashCode( Base obj )
      {
        return base.GetHashCode();
      }
    }

    #endregion
  }
}
