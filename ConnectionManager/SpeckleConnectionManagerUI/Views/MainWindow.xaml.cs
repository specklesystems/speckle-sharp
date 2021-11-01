using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System.Threading.Tasks;
using SpeckleConnectionManagerUI.ViewModels;

namespace SpeckleConnectionManagerUI.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WhenActivated(d => d(ViewModel!.ShowNewServerWindowInteraction.RegisterHandler(HandleShowNewServerWindowInteraction)));
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private async Task HandleShowNewServerWindowInteraction(InteractionContext<AddConnectionViewModel, AddConnectionViewModel> interaction)
        {
            var loginWindow = new AddNewConnectionWindow() { DataContext = interaction.Input };

            var updatedNewServerWindowViewModel = await loginWindow.ShowDialog<AddConnectionViewModel>(this);
            interaction.SetOutput(updatedNewServerWindowViewModel);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}