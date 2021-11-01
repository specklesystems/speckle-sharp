using ReactiveUI;

namespace SpeckleConnectionManagerUI.Models
{
    public class ConnectStatusItem : ReactiveObject
    {
        public string? ServerName { get; set; }

        public string? ServerUrl { get; set; }

        private bool _disconnected = true;

        private bool _default = false;

        public string? _defaultServerLabel;

        public bool Disconnected
        {
            get => _disconnected;
            set => this.RaiseAndSetIfChanged(ref _disconnected, value);
        }

        public bool Default
        {
            get => _default;
            set => this.RaiseAndSetIfChanged(ref _default, value);
        }

        public string? DefaultServerLabel
        {
            get => _defaultServerLabel;
            set => this.RaiseAndSetIfChanged(ref _defaultServerLabel, value);
        }

        private string _colour = "Red";

        public string Colour
        {
            get => _colour;
            set => this.RaiseAndSetIfChanged(ref _colour, value);
        }

        public int Identifier { get; set; }
    }
}