using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using SpeckleConnectionManagerUI.ViewModels;
using ReactiveUI;

namespace SpeckleConnectionManagerUI.Views
{
    public partial class AddNewConnectionWindow : ReactiveWindow<AddConnectionViewModel>
    {
        public AddNewConnectionWindow()
        {
            InitializeComponent();
            this.WhenActivated(d => d(ViewModel!.SubmitCommand.Subscribe(Close)));
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
