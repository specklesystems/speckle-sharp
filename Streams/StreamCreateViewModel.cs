using MaterialDesignThemes.Wpf;
using Speckle.Core.Api.GqlModels;
using Speckle.DesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Speckle.DesktopUI.Streams
{
    class StreamCreateViewModel : BindableBase
    {
        public StreamCreateViewModel()
        {
            SelectedSlide = 0;

            MessageQueue = new SnackbarMessageQueue();
            ChooseSimpleCommand = new RelayCommand<string>(OnChooseSimple);
            ChooseAdvancedCommand = new RelayCommand<string>(OnChooseAdvanced);
            ChangeSlideCommand = new RelayCommand<string>(OnChangeSlide);
            CloseDialogCommand = new RelayCommand<string>(OnCloseDialog);
        }

        private Stream _streamToCreate;
        public Stream StreamToCreate
        {
            get => _streamToCreate;
            set => SetProperty(ref _streamToCreate, value);
        }
        private int _selectedSlide;
        public int SelectedSlide
        {
            get => _selectedSlide;
            set => SetProperty(ref _selectedSlide, value);
        }
        private SnackbarMessageQueue _messageQueue;
        public SnackbarMessageQueue MessageQueue
        {
            get => _messageQueue;
            set => SetProperty(ref _messageQueue, value);
        }
        public RelayCommand<string> ChooseAdvancedCommand { get; set; }
        private void OnChooseAdvanced(string name)
        {
            if(name == "")
            {
                MessageQueue.Enqueue("Please choose a name for your stream!");
                return;
            }
            StreamToCreate = new Stream
            {
                name = name
            };
            SelectedSlide = 2;
        }
        public RelayCommand<string> ChooseSimpleCommand { get; set; }
        private void OnChooseSimple(string name)
        {
            if (name == "")
            {
                MessageQueue.Enqueue("Please choose a name for your stream!");
                return;
            }
            StreamToCreate = new Stream
            {
                name = name
            };
            SelectedSlide = 1;
        }
        public RelayCommand<string> ChangeSlideCommand { get; set; }
        public RelayCommand<string> CloseDialogCommand { get; set; }
        private void OnChangeSlide(string slide)
        {
            int index = -1;
            try
            {
                int.TryParse(slide, out index);
                SelectedSlide = index;
            }
            catch
            {
                return;
            }
        }

        private void OnCloseDialog(string arg)
        {
            DialogHost.CloseDialogCommand.Execute(null, null);
            SelectedSlide = 0;
        }
    }
}
