using System;
using System.Collections.Concurrent;
using Speckle.Core.Api;
using SCT = Speckle.Core.Transports;
using Stylet;
using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.ConnectorETABS.Util;
using System.Linq;
using DesktopUI2.ViewModels;

namespace Speckle.ConnectorETABS.UI
{
    public partial class ConnectorBindingsETABS : ConnectorBindings

    {
        #region sending
        //private void UpdateProgress(ConcurrentDictionary<string, int> dict, ProgressReport progress)
        //{
        //    if (progress == null) return;

        //    Execute.PostToUIThread(() =>
        //    {
        //        progress.ProgressDict = dict;
        //        progress.Value = dict.Values.Last();
        //    });
        //}

        public override async Task SendStream(StreamState state, ProgressViewModel progress)
        {
            //throw new NotImplementedException();
            var kit = KitManager.GetDefaultKit();
            //var converter = new ConverterETABS();
            var converter = kit.LoadConverter(ConnectorETABSUtils.ETABSAppName);
            converter.SetContextDocument(Model);
            Exceptions.Clear();

            var commitObj = new Base();
            int objCount = 0;

            if (state.Filter != null)
            {
                state.SelectedObjectIds = GetSelectionFilterObjects(state.Filter);
            }

            var totalObjectCount = state.SelectedObjectIds.Count();

            if (totalObjectCount == 0)
            {
                //RaiseNotification("Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.");
                //return state;
            }

            var conversionProgressDict = new ConcurrentDictionary<string, int>();
            conversionProgressDict["Conversion"] = 0;
            progress.Update(conversionProgressDict);


            //if( commitObj["@Stories"] == null)
            //{
            //    commitObj["@Stories"] = converter.ConvertToSpeckle(("Stories", "ETABS"));
            //}

            foreach (var applicationId in state.SelectedObjectIds)
            {
                if (progress.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                Base converted = null;
                string containerName = string.Empty;


                var selectedObjectType = ConnectorETABSUtils.ObjectIDsTypesAndNames
                    .Where(pair => pair.Key == applicationId)
                    .Select(pair => pair.Value.Item1).FirstOrDefault();

                if (!converter.CanConvertToSpeckle(selectedObjectType))
                {
                    //state.Errors.Add(new Exception($"Objects of type ${selectedObjectType} are not supported"));
                    continue;
                }

                Tracker.TrackPageview(Tracker.CONVERT_TOSPECKLE);

                var typeAndName = ConnectorETABSUtils.ObjectIDsTypesAndNames
                    .Where(pair => pair.Key == applicationId)
                    .Select(pair => pair.Value).FirstOrDefault();

                converted = converter.ConvertToSpeckle(typeAndName);

                if (converted == null)
                {
                    //state.Errors.Add(new Exception($"Failed to convert object ${applicationId} of type ${selectedObjectType}."));
                    continue;
                }


                //if (converted != null)
                //{
                //    if (commitObj[selectedObjectType] == null)
                //    {
                //        commitObj[selectedObjectType] = new List<Base>();
                //    }
                //             ((List<Base>)commitObj[selectedObjectType]).Add(converted);
                //}
                converted.applicationId = applicationId;

                objCount++;
                conversionProgressDict["Conversion"]++;
                progress.Update(conversionProgressDict);
            }
            if (commitObj["@Model"] == null)
            {
                commitObj["@Model"] = converter.ConvertToSpeckle(("Model", "ETABS"));
            }

            if (objCount == 0)
            {
                return;
                //RaiseNotification("Zero objects converted successfully. Send stopped.");
                //return state;
            }

            if (progress.CancellationTokenSource.Token.IsCancellationRequested)
            {
                return;
            }

            var streamId = state.StreamId;
            var client = state.Client;

            var transports = new List<SCT.ITransport>() { new SCT.ServerTransport(client.Account, streamId) };

            var objectId = await Operations.Send(
                @object: commitObj,
                cancellationToken: progress.CancellationTokenSource.Token,
                transports: transports,
                onProgressAction: dict => progress.Update(dict),
                onErrorAction: (Action<string, Exception>)((s, e) =>
                {
                    this.Exceptions.Add(e); // TODO!
                              //state.Errors.Add(e);
                    progress.CancellationTokenSource.Cancel();
                 }),    
                disposeTransports: true
                );

            //var commitObjId = await Operations.Send(
            //  commitObj,
            //  state.CancellationTokenSource.Token,
            //  transports,
            //  onProgressAction: dict => UpdateProgress(dict, state.Progress),
            //  /* TODO: a wee bit nicer handling here; plus request cancellation! */
            //  onErrorAction: (err, exception) => { Exceptions.Add(exception); }
            //  );

            if (Exceptions.Count != 0)
            {
                //RaiseNotification($"Failed to send: \n {Exceptions.Last().Message}");
                return;
            }

            var actualCommit = new CommitCreateInput
            {
                streamId = streamId,
                objectId = objectId,
                branchName = state.BranchName,
                message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {objCount} elements from ETABS.",
                sourceApplication = ConnectorETABSUtils.ETABSAppName
            };

            if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

            try
            {
                var commitId = await client.CommitCreate(actualCommit);

                //await state.RefreshStream();
                state.PreviousCommitId = commitId;

                //PersistAndUpdateStreamInFile(state);
                //RaiseNotification($"{objCount} objects sent to {state.Stream.name}. 🚀");
            }
            catch (Exception e)
            {
                //Globals.Notify($"Failed to create commit.\n{e.Message}");
                //state.Errors.Add(e);
            }

            //return state;
        }

        #endregion
    }
}