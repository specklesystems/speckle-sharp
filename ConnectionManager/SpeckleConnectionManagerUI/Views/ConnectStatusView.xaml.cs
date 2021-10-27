using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SpeckleConnectionManagerUI.Views
{
    public class ConnectStatusView : UserControl
    {

        public ConnectStatusView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}