using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.ConnectorNavisworks.Bindings
{
    public partial class ConnectorBindingsNavisworks
    {
        // Stubbed implementations as navisworks won't be receiving.
        public override bool CanPreviewReceive => false;

        /// <summary>
        /// GetReceiveNodes is required to Init the Avalonia App.
        /// There is no receive mode relevant for the Navisworks Connector at this time. 
        /// </summary>
        public override List<ReceiveMode> GetReceiveModes()
        {
            // No receive modes available
            return new List<ReceiveMode>
            {
                ReceiveMode.Ignore
            };
        }

        /// <summary>
        /// Stored Base objects from commit flattening on receive: key is the Base id
        /// </summary>
        public Dictionary<string, Base> StoredObjects = new Dictionary<string, Base>();

        public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            return null;
        }


        public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            return state;
        }
    }
}