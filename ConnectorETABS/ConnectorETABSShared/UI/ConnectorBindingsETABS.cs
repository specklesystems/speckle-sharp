using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using ConnectorETABSShared.Util;
using ConnectorETABSShared.Storage;
using System.Linq;
using System.Timers;
using System.IO;

namespace ConnectorETABSShared.UI
{
    class ConnectorBindingsETABS : ConnectorBindings
    {
        public static ConnectorETABSDocument Doc { get; set; } = new ConnectorETABSDocument();
        public Timer SelectionTimer;
        public List<StreamState> DocumentStreams { get; set; } = new List<StreamState>();


        public ConnectorBindingsETABS(ConnectorETABSDocument doc)
        {
            Doc = doc;
            SelectionTimer = new Timer(2000) { AutoReset = true, Enabled = true };
            SelectionTimer.Elapsed += SelectionTimer_Elapsed;
            SelectionTimer.Start();
        }

        private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Doc == null)
            {
                return;
            }

            var selection = GetSelectedObjects();

            NotifyUi(new UpdateSelectionCountEvent() { SelectionCount = selection.Count });
            NotifyUi(new UpdateSelectionEvent() { ObjectIds = selection });
        }


        #region Local stream I/O with local file
        public override void AddNewStream(StreamState state)
        {
            Tracker.TrackPageview(Tracker.STREAM_CREATE);
            var index = DocumentStreams.FindIndex(b => b.Stream.id == state.Stream.id);
            if (index == -1)
            {
                DocumentStreams.Add(state);
                WriteStateToFile();
            }
        }
        private void WriteStateToFile()
        {
            StreamStateManager.WriteStreamStateList(Doc, DocumentStreams);
        }

        public override void RemoveStreamFromFile(string streamId)
        {
            var streamState = DocumentStreams.FirstOrDefault(s => s.Stream.id == streamId);
            if (streamState != null)
            {
                DocumentStreams.Remove(streamState);
                WriteStateToFile();
            }
        }

        public override List<StreamState> GetStreamsInFile()
        {
            if (Doc != null)
                DocumentStreams = StreamStateManager.ReadState(Doc);

            return DocumentStreams;
        }

        public override void PersistAndUpdateStreamInFile(StreamState state)
        {
            var index = DocumentStreams.FindIndex(b => b.Stream.id == state.Stream.id);
            if (index != -1)
            {
                DocumentStreams[index] = state;
                WriteStateToFile();
            }
        }

        #endregion

        #region boilerplate
        public override string GetActiveViewName()
        {
            throw new NotImplementedException();
        }

        public override string GetDocumentId() => GetDocHash(Doc);

        private string GetDocHash(ConnectorETABSDocument doc) => Utilities.hashString(doc.Document.GetModelFilepath() + doc.Document.GetModelFilename(), Utilities.HashingFuctions.MD5);

        public override string GetDocumentLocation() => Doc.Document.GetModelFilepath();

        public override string GetFileName() => Doc.Document.GetModelFilename();

        public override string GetHostAppName() => ConnectorETABSUtils.ETABSAppName;

        public override List<string> GetObjectsInView()
        {
            throw new NotImplementedException();
        }

        public override List<string> GetSelectedObjects()
        {
            var names = new List<string>();
            var util = new ConnectorETABSUtils();
            var typeNameTupleList = ConnectorETABSUtils.SelectedObjects(Doc);
            if (typeNameTupleList == null) return new List<string>() { };
            foreach (var item in typeNameTupleList)
            {
                (string typeName, string name) = item;
                if (ConnectorETABSUtils.IsTypeETABSAPIUsable(typeName))
                {
                    names.Add(string.Concat(typeName, ": ", name));
                }
            }
            if (names.Count == 0)
            {
                return new List<string>() { };
            }
            return names;
        }

        public override List<ISelectionFilter> GetSelectionFilters()
        {
            var objectTypes = new List<string>();
            //var objectIds = new List<string>();
            if (Doc != null)
            {
                ConnectorETABSUtils.GetObjectIDsTypesAndNames(Doc);
                objectTypes = ConnectorETABSUtils.ObjectIDsTypesAndNames
                    .Select(pair => pair.Value.Item1).Distinct().ToList();
                //objectIds = ConnectorETABSUtils.ObjectIDsTypesAndNames.Select(pair => pair.Key).ToList();

            }

            return new List<ISelectionFilter>()
            {
            new ListSelectionFilter {Slug="type", Name = "Cat",
                Icon = "Category", Values = objectTypes,
                Description="Adds all objects belonging to the selected types"},
        //new PropertySelectionFilter{
        //  Slug="param",
        //  Name = "Param",
        //  Description="Adds  all objects satisfying the selected parameter",
        //  Icon = "FilterList",
        //  HasCustomProperty = false,
        //  Values = objectNames,
        //  Operators = new List<string> {"equals", "contains", "is greater than", "is less than"}
        //},
            new AllSelectionFilter {Slug="all",  Name = "All",
                Icon = "CubeScan", Description = "Selects all document objects." }
            };
        }

        public override void SelectClientObjects(string args)
        {
            throw new NotImplementedException();
        }

        private List<string> GetSelectionFilterObjects(ISelectionFilter filter)
        {
            var doc = Doc.Document;

            var selection = new List<string>();

            switch (filter.Slug)
            {
                case "all":
                    if (ConnectorETABSUtils.ObjectIDsTypesAndNames == null)
                    {
                        ConnectorETABSUtils.GetObjectIDsTypesAndNames(Doc);
                    }
                    selection.AddRange(ConnectorETABSUtils.ObjectIDsTypesAndNames
                                .Select(pair => pair.Key).ToList());
                    return selection;


                case "type":
                    var typeFilter = filter as ListSelectionFilter;
                    if (ConnectorETABSUtils.ObjectIDsTypesAndNames == null)
                    {
                        ConnectorETABSUtils.GetObjectIDsTypesAndNames(Doc);
                    }
                    foreach (var type in typeFilter.Selection)
                    {
                        selection.AddRange(ConnectorETABSUtils.ObjectIDsTypesAndNames
                            .Where(pair => pair.Value.Item1 == type)
                            .Select(pair => pair.Key)
                            .ToList());
                    }
                    return selection;


                    /// ETABS doesn't list fields of different objects. 
                    /// For "param" search, maybe search over the name of
                    /// methods of each type?

                    //case "param":
                    //    try
                    //    {
                    //        if (ConnectorETABSUtils.ObjectTypes.Count == 0)
                    //        {
                    //            var _ = ConnectorETABSUtils.GetObjectTypesAndNames(Doc);
                    //        }

                    //        var propFilter = filter as PropertySelectionFilter;
                    //        var query = new FilteredElementCollector(doc)
                    //          .WhereElementIsNotElementType()
                    //          .WhereElementIsNotElementType()
                    //          .WhereElementIsViewIndependent()
                    //          .Where(x => x.IsPhysicalElement())
                    //          .Where(fi => fi.LookupParameter(propFilter.PropertyName) != null);

                    //        propFilter.PropertyValue = propFilter.PropertyValue.ToLowerInvariant();

                    //        switch (propFilter.PropertyOperator)
                    //        {
                    //            case "equals":
                    //                query = query.Where(fi =>
                    //                  GetStringValue(fi.LookupParameter(propFilter.PropertyName)) == propFilter.PropertyValue);
                    //                break;
                    //            case "contains":
                    //                query = query.Where(fi =>
                    //                  GetStringValue(fi.LookupParameter(propFilter.PropertyName)).Contains(propFilter.PropertyValue));
                    //                break;
                    //            case "is greater than":
                    //                query = query.Where(fi => RevitVersionHelper.ConvertFromInternalUnits(
                    //                                            fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                    //                                            fi.LookupParameter(propFilter.PropertyName)) >
                    //                                          double.Parse(propFilter.PropertyValue));
                    //                break;
                    //            case "is less than":
                    //                query = query.Where(fi => RevitVersionHelper.ConvertFromInternalUnits(
                    //                                            fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                    //                                            fi.LookupParameter(propFilter.PropertyName)) <
                    //                                          double.Parse(propFilter.PropertyValue));
                    //                break;
                    //        }

                    //        selection = query.ToList();
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        Log.CaptureException(e);
                    //    }
                    //    return selection;
            }

            return selection;

        }
        #endregion

        #region receiving
        public override Task<StreamState> ReceiveStream(StreamState state)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region sending
        public override async Task<StreamState> SendStream(StreamState state)
        {
            throw new NotImplementedException();
            //var kit = KitManager.GetDefaultKit();
            ////var converter = new ConverterETABS();
            ////var converter = kit.LoadConverter(ConnectorETABSUtils.ETABSAppName);
            ////converter.SetContextDocument(Doc);
            ////Exceptions.Clear();

            //var commitObj = new Base();

            //int objCount = 0;

            //if (state.Filter != null)
            //{
            //    state.SelectedObjectIds = GetSelectionFilterObjects(state.Filter);
            //}

            //var totalObjectCount = state.SelectedObjectIds.Count();

            //if (totalObjectCount == 0)
            //{
            //    RaiseNotification("Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.");
            //    return state;
            //}

            //var conversionProgressDict = new ConcurrentDictionary<string, int>();
            //conversionProgressDict["Conversion"] = 0;
            //Execute.PostToUIThread(() => state.Progress.Maximum = totalObjectCount);


            //foreach (var applicationId in state.SelectedObjectIds)
            //{
            //    if (state.CancellationTokenSource.Token.IsCancellationRequested)
            //    {
            //        return null;
            //    }

            //    Base converted = null;
            //    string containerName = string.Empty;


            //    var selectedObjectType = ConnectorETABSUtils.ObjectIDsTypesAndNames
            //        .Where(pair => pair.Key == applicationId)
            //        .Select(pair => pair.Value.Item1).FirstOrDefault();

            //    if (!converter.CanConvertToSpeckle(selectedObjectType))
            //    {
            //        state.Errors.Add(new Exception($"Objects of type ${selectedObjectType} are not supported"));
            //        continue;
            //    }

            //    Tracker.TrackPageview(Tracker.CONVERT_TOSPECKLE);

            //    var typeAndName = ConnectorETABSUtils.ObjectIDsTypesAndNames
            //        .Where(pair => pair.Key == applicationId)
            //        .Select(pair => pair.Value).FirstOrDefault();

            //    converted = converter.ConvertToSpeckle(typeAndName);

            //    if (converted == null)
            //    {
            //        state.Errors.Add(new Exception($"Failed to convert object ${applicationId} of type ${selectedObjectType}."));
            //        continue;
            //    }


            //    if (converted != null)
            //    {
            //        if (commitObj[selectedObjectType] == null)
            //        {
            //            commitObj[selectedObjectType] = new List<Base>();
            //        }
            //                 ((List<Base>)commitObj[selectedObjectType]).Add(converted);
            //    }
            //    converted.applicationId = applicationId;

            //    objCount++;
            //    conversionProgressDict["Conversion"]++;
            //    UpdateProgress(conversionProgressDict, state.Progress);
            //}

            //if (objCount == 0)
            //{
            //    RaiseNotification("Zero objects converted successfully. Send stopped.");
            //    return state;
            //}

            //if (state.CancellationTokenSource.Token.IsCancellationRequested)
            //{
            //    return null;
            //}

            //Execute.PostToUIThread(() => state.Progress.Maximum = objCount);

            //var streamId = state.Stream.id;
            //var client = state.Client;

            //var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

            //var commitObjId = await Operations.Send(
            //  commitObj,
            //  state.CancellationTokenSource.Token,
            //  transports,
            //  onProgressAction: dict => UpdateProgress(dict, state.Progress),
            //  /* TODO: a wee bit nicer handling here; plus request cancellation! */
            //  onErrorAction: (err, exception) => { Exceptions.Add(exception); }
            //  );

            //if (Exceptions.Count != 0)
            //{
            //    RaiseNotification($"Failed to send: \n {Exceptions.Last().Message}");
            //    return null;
            //}

            //var actualCommit = new CommitCreateInput
            //{
            //    streamId = streamId,
            //    objectId = commitObjId,
            //    branchName = state.Branch.name,
            //    message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {objCount} elements from ETABS.",
            //    sourceApplication = Applications.ETABS18
            //};

            //if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

            //try
            //{
            //    var commitId = await client.CommitCreate(actualCommit);

            //    await state.RefreshStream();
            //    state.PreviousCommitId = commitId;

            //    PersistAndUpdateStreamInFile(state);
            //    RaiseNotification($"{objCount} objects sent to {state.Stream.name}.");
            //}
            //catch (Exception e)
            //{
            //    Globals.Notify($"Failed to create commit.\n{e.Message}");
            //    state.Errors.Add(e);
            //}

            //return state;
        }

        #endregion

    }
}
