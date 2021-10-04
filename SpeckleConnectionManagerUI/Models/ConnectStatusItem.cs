using ReactiveUI;

namespace SpeckleConnectionManagerUI.Models
{
    public class ConnectStatusItem : ReactiveObject
    {
        public string? ServerName { get; set; }

        private bool _disconnected = true;

        public bool Disconnected
        {
            get => _disconnected;
            set => this.RaiseAndSetIfChanged(ref _disconnected, value);
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