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
        public Dictionary<string, Base> StoredObjects = null;
        // There is no receive mode relevant for the Navisworks Connector at this time. 

        public override bool CanPreviewReceive => false;
        public override bool CanReceive => false;

        public override List<ReceiveMode> GetReceiveModes()
        {
            return null;
        }

        public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
        {
            await Task.Delay(0);
            return null;
        }

        public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            return state;
        }
    }
}