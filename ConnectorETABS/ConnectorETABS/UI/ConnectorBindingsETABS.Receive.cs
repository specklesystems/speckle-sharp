using System;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Stylet;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Speckle.ConnectorETABS.Util;
using DesktopUI2.ViewModels;

namespace Speckle.ConnectorETABS.UI
{
    public partial class ConnectorBindingsETABS : ConnectorBindings

    {



        #region receiving
        public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
        {
            Tracker.TrackPageview(Tracker.RECEIVE);
            Exceptions.Clear();

            var kit = KitManager.GetDefaultKit();
            var converter = kit.LoadConverter(ConnectorETABSUtils.ETABSAppName);
            converter.SetContextDocument(Model);
            //var previouslyRecieveObjects = state.ReceivedObjects;

            if (converter == null)
            {
                throw new Exception("Could not find any Kit!");
                //RaiseNotification($"Could not find any Kit!");
                progress.CancellationTokenSource.Cancel();
                //return null;
            }


            Tracker.TrackPageview(Tracker.STREAM_GET);
            var stream = await state.Client.StreamGet(state.StreamId);

            if (progress.CancellationTokenSource.Token.IsCancellationRequested)
            {
                return null;
            }

            var transport = new ServerTransport(state.Client.Account, state.StreamId);

            Exceptions.Clear();

            string referencedObject = state.ReferencedObject;

            var commitId = state.CommitId;
            var commitMsg = state.CommitMessage;
            if (commitId == "latest")
            {
                var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
                var commit = res.commits.items.FirstOrDefault();
                referencedObject = res.commits.items.FirstOrDefault().referencedObject;
                commitId = commit.id;
                commitMsg = commit.message;
            }

            


            var commitObject = await Operations.Receive(
                referencedObject,
                progress.CancellationTokenSource.Token,
                transport,
                onProgressAction: dict => progress.Update(dict),
                onErrorAction: (Action<string, Exception>)((s, e) =>
                {
                    this.Exceptions.Add(e);
                          //state.Errors.Add(e);
                          progress.CancellationTokenSource.Cancel();
                }),
                //onTotalChildrenCountKnown: count => Execute.PostToUIThread(() => state.Progress.Maximum = count),
                disposeTransports: true
                );

            try
            {
                await state.Client.CommitReceived(new CommitReceivedInput
                {
                    streamId = stream.id,
                    commitId = commitId,
                    message = commitMsg,
                    sourceApplication = ConnectorETABSUtils.ETABSAppName
                });
            }
            catch
            {
                // Do nothing!
            }

            //var commitObject = await Operations.Receive(
            //  referencedObject,
            //  state.CancellationTokenSource.Token,
            //  transport,
            //  onProgressAction: d => UpdateProgress(d, state.Progress),
            //  onTotalChildrenCountKnown: num => Execute.PostToUIThread(() => state.Progress.Maximum = num),
            //  onErrorAction: (message, exception) => { Exceptions.Add(exception); }
            //  );

            if (Exceptions.Count != 0)
            {
                //RaiseNotification($"Encountered some errors: {Exceptions.Last().Message}");
                return state;
            }

            if (progress.CancellationTokenSource.Token.IsCancellationRequested)
            {
                return null;
            }

            var conversionProgressDict = new ConcurrentDictionary<string, int>();
            conversionProgressDict["Conversion"] = 0;
            //Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

            Action updateProgressAction = () =>
            {
                conversionProgressDict["Conversion"]++;
                progress.Update(conversionProgressDict);
            };

            var commitObjs = FlattenCommitObject(commitObject, converter);
            foreach (var commitObj in commitObjs)
            {
                BakeObject(commitObj, state, converter);
                updateProgressAction?.Invoke();
            }


            try
            {
                //await state.RefreshStream();
                WriteStateToFile();
            }
            catch (Exception e)
            {
                Exceptions.Add(e);
                WriteStateToFile();
                //state.Errors.Add(e);
                //Globals.Notify($"Receiving done, but failed to update stream from server.\n{e.Message}");
            }

            return state;
        }






        /// <summary>
        /// conversion to native
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="state"></param>
        /// <param name="converter"></param>
        private void BakeObject(Base obj, StreamState state, ISpeckleConverter converter)
        {
            try
            {
                Tracker.TrackPageview(Tracker.CONVERT_TONATIVE);
                converter.ConvertToNative(obj);
            }
            catch (Exception e)
            {
                Exceptions.Add(e);
                return;
                //state.Errors.Add(new Exception($"Failed to convert object {obj.id} of type {obj.speckle_type}\n with error\n{e}"));
            }
        }

        /// <summary>
        /// Recurses through the commit object and flattens it. 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        private List<Base> FlattenCommitObject(object obj, ISpeckleConverter converter)
        {
            List<Base> objects = new List<Base>();

            if (obj is Base @base)
            {
                if (converter.CanConvertToNative(@base))
                {
                    objects.Add(@base);

                    return objects;
                }
                else
                {
                    foreach (var prop in @base.GetDynamicMembers())
                    {
                        objects.AddRange(FlattenCommitObject(@base[prop], converter));
                    }
                    return objects;
                }
            }

            if (obj is List<object> list)
            {
                foreach (var listObj in list)
                {
                    objects.AddRange(FlattenCommitObject(listObj, converter));
                }
                return objects;
            }

            if (obj is IDictionary dict)
            {
                foreach (DictionaryEntry kvp in dict)
                {
                    objects.AddRange(FlattenCommitObject(kvp.Value, converter));
                }
                return objects;
            }

            return objects;
        }

        #endregion
    }
}