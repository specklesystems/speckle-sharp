using System;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;

namespace Speckle.ConnectorNavisworks.Bindings
{
    public partial class ConnectorBindingsNavisworks
    {
        public override bool CanPreviewSend => false;


        // Stub - Preview send is not supported
        public override async void PreviewSend(StreamState state, ProgressViewModel progress)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            // TODO!
        }

        public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            return null;
        }
    }
}