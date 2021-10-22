using System;
using System.Threading.Tasks;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
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


namespace Speckle.ConnectorETABS.UI
{
    public partial class ConnectorBindingsETABS : ConnectorBindings

    {
        public List<Exception> ConversionErrors { get; set; } = new List<Exception>();
        public List<Exception> OperationErrors { get; set; } = new List<Exception>();

        #region receiving
        public override async Task<StreamState> ReceiveStream(StreamState state)
        {
            Tracker.TrackPageview(Tracker.RECEIVE);
            ConversionErrors.Clear();
            OperationErrors.Clear();

            var kit = KitManager.GetDefaultKit();
            var converter = kit.LoadConverter(ConnectorETABSUtils.ETABSAppName);

            if (converter == null)
            {
                RaiseNotification($"Could not find any Kit!");
                state.CancellationTokenSource.Cancel();
                return null;
            }

            converter.SetContextDocument(Model);

            Tracker.TrackPageview(Tracker.STREAM_GET);
            var stream = await state.Client.StreamGet(state.Stream.id);

            if (state.CancellationTokenSource.Token.IsCancellationRequested)
            {
                return null;
            }

            var transport = new ServerTransport(state.Client.Account, state.Stream.id);

            Exceptions.Clear();

            string referencedObject = state.Commit.referencedObject;

            var commitId = state.Commit.id;

            if (commitId == "latest")
            {
                var res = await state.Client.BranchGet(state.CancellationTokenSource.Token, state.Stream.id, state.Branch.name, 1);
                var commit = res.commits.items.FirstOrDefault();
                commitId = commit.id;
                referencedObject = commit.referencedObject;
            }

            var commitObject = await Operations.Receive(
              referencedObject,
              state.CancellationTokenSource.Token,
              transport,
              onProgressAction: d => UpdateProgress(d, state.Progress),
              onTotalChildrenCountKnown: num => Execute.PostToUIThread(() => state.Progress.Maximum = num),
              onErrorAction: (message, exception) => { Exceptions.Add(exception); }
              );

            if (Exceptions.Count != 0)
            {
                RaiseNotification($"Encountered some errors: {Exceptions.Last().Message}");
            }


            var conversionProgressDict = new ConcurrentDictionary<string, int>();
            conversionProgressDict["Conversion"] = 0;
            Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

            Action updateProgressAction = () =>
            {
                conversionProgressDict["Conversion"]++;
                UpdateProgress(conversionProgressDict, state.Progress);
            };

            var commitObjs = FlattenCommitObject(commitObject, converter);
            foreach (var commitObj in commitObjs)
            {
                BakeObject(commitObj, state, converter);
                updateProgressAction?.Invoke();
            }

            try
            {
                await state.RefreshStream();
                WriteStateToFile();
            }
            catch (Exception e)
            {
                WriteStateToFile();
                state.Errors.Add(e);
                Globals.Notify($"Receiving done, but failed to update stream from server.\n{e.Message}");
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
                state.Errors.Add(new Exception($"Failed to convert object {obj.id} of type {obj.speckle_type}\n with error\n{e}"));
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