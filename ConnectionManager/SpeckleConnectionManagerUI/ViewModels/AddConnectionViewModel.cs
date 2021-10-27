using System.Linq;
using System.Diagnostics;
using System.Reactive;
using ReactiveUI;
using SpeckleConnectionManagerUI.Views;
using SpeckleConnectionManagerUI.Services;


namespace SpeckleConnectionManagerUI.ViewModels
{
    public class AddConnectionViewModel : ViewModelBase
    {
        public string newServerUrl = string.Empty;
        public string NewServerUrl
        {
            get => newServerUrl;
            set
            {
                this.RaiseAndSetIfChanged(ref newServerUrl, value);
                EnableInput = true;
            }
        }

        private bool enableInput = false;
        public bool EnableInput
        {
            get => enableInput;
            set => this.RaiseAndSetIfChanged(ref enableInput, value);
        }

        public ReactiveCommand<Unit, AddConnectionViewModel> SubmitCommand { get; set; }

        public AddConnectionViewModel()
        {
            SubmitCommand = ReactiveCommand.Create(() => this);
        }

    }
}
