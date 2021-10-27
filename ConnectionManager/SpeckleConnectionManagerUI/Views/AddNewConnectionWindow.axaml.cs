using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Controls;
using SpeckleConnectionManagerUI.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Formatters;

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
            this.Activated += OnActivated;
        }

        private void OnActivated(object sender, EventArgs e)
        {
            this.Activated -= OnActivated;
            this.FindControl<TextBox>("ServerUrlTextBox").Focus(); 
        }
    }
}
