using System;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Application = Autodesk.Navisworks.Api.Application;
using Document = Autodesk.Navisworks.Api.Document;

namespace Speckle.ConnectorNavisworks.Bindings
{
    public partial class ConnectorBindingsNavisworks
    {
        public void RegisterAppEvents()
        {
            //// GLOBAL EVENT HANDLERS

            // Navisworks is an SDI (Single Document Interface) and on launch has an initial empty document.
            // Loading a file doesn't trigger the ActiveDocumentChanged event.
            // Instead it amends it in place. We can listen to the filename changing to get the intuitive event.
            Doc.FileNameChanged += DocumentChangedEvent;
            Doc.SelectionSets.Changed += SetsChangedEvent;
        }


        private void SetsChangedEvent(object sender, EventArgs e)
        {
            SavedSets = Doc.SelectionSets;
            UpdateSelectedStream?.Invoke();

            MainViewModel.Home.RaisePropertyChanged($"AvailableFilters");
        }


        // Triggered when the active document name is changed.
        // This will happen automatically if a document is newly created or opened.
        private void DocumentChangedEvent(object sender, EventArgs e)
        {
            Document doc = sender as Document;

            try
            {
                // As ConnectorNavisworks is Send only, There is little use for a new empty document.
                if (doc == null || doc.IsClear) return;

                UpdateSelectedStream?.Invoke();

                var streams = GetStreamsInFile();
                UpdateSavedStreams?.Invoke(streams);

                MainViewModel.GoHome();
            }
            catch
            {
                // ignored
            }
        }
    }
}