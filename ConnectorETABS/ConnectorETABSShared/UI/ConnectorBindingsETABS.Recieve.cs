using System;
using System.Threading.Tasks;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;


namespace Speckle.ConnectorETABS.UI
{
    public partial class ConnectorBindingsETABS : ConnectorBindings

    {
        #region receiving
        public override Task<StreamState> ReceiveStream(StreamState state)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
