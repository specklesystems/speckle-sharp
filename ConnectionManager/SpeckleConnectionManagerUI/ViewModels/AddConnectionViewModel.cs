using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using ReactiveUI.Validation.States;
using SpeckleConnectionManagerUI.Views;
using SpeckleConnectionManagerUI.Services;


namespace SpeckleConnectionManagerUI.ViewModels
{
    public class AddConnectionViewModel : ReactiveValidationObject
    {
        public string newServerUrl = string.Empty;
        public string NewServerUrl
        {
            get => newServerUrl;
            set => this.RaiseAndSetIfChanged(ref newServerUrl, value);
        }

        private readonly ObservableAsPropertyHelper<bool> commandEnabled;
        public bool CommandEnabled => commandEnabled.Value;

        public ReactiveCommand<Unit, AddConnectionViewModel> SubmitCommand { get; set; }

        public AddConnectionViewModel()
        {            
            IObservable<IValidationState> urlValidated =
                this.WhenAnyValue(x => x.NewServerUrl)
                    .Throttle(TimeSpan.FromSeconds(0.5), RxApp.TaskpoolScheduler)
                    .SelectMany(ValidateServerUrl)
                    .ObserveOn(RxApp.MainThreadScheduler);

            IObservable<IValidationState> urlDirty =
                this.WhenAnyValue(x => x.NewServerUrl)
                    .Select(name => new ValidationState(false, ""));

            this.ValidationRule(
                            vm => vm.NewServerUrl,
                            urlValidated.Merge(urlDirty));

            commandEnabled = urlValidated
                .Select(message => false)
                .Merge(urlDirty.Select(message => true))
                .ToProperty(this, x => x.CommandEnabled);

            SubmitCommand = ReactiveCommand.Create(() => this, this.IsValid());
        }

        private static async Task<IValidationState> ValidateServerUrl(string url)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.7)).ConfigureAwait(false);
            return CheckServerUrl(url) ? ValidationState.Valid : new ValidationState(false, "You must specify a valid Speckle Server url (ex. https://v2.speckle.arup.com)");
        }

        public static bool CheckServerUrl(string serverUrl)
        {
            Uri baseUri;
            bool result = Uri.TryCreate(serverUrl, UriKind.Absolute, out baseUri)
                && (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps);

            if (!result) return false;

            try
            {
                var _serverUri = baseUri.Scheme + "://" + baseUri.Host;

                if (!baseUri.IsDefaultPort) { _serverUri += ":" + baseUri.Port; }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_serverUri + "/explorer");
                request.Timeout = 1000;
                request.Method = "HEAD";
                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        return response.StatusCode == HttpStatusCode.OK;
                    }
                }
                catch (WebException)
                {
                    return false;
                }

            }
            catch (Exception err)
            {
                return false;
            }
        }

    }
}
