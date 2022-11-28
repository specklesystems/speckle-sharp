using System;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;

namespace Speckle.ConnectorNavisworks.Bindings
{
    public partial class ConnectorBindingsNavisworks
    {
        // Stubbed implementations as navisworks won't be receiving.
        public override bool CanPreviewReceive => false;

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