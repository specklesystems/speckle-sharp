using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using static Speckle.ConnectorNavisworks.Utils;

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
    }


    // Triggered when the active document name is changed.
    // This will happen automatically if a document is newly created or opened.
    private void DocumentChangedEvent(object sender, EventArgs e)
    {
      Document doc = sender as Document;

      try
      {
        // As ConnectorNavisworks is Send only, There is little use for a new empty document.
        if (doc == null || doc.IsClear)
        {
          UpdateSavedStreams?.Invoke(new List<StreamState>());
          MainViewModel.GoHome();
          return;
        }

        var streams = GetStreamsInFile();
        UpdateSavedStreams?.Invoke(streams);

        UpdateSelectedStream?.Invoke();

        MainViewModel.GoHome();

        NavisworksConverter.SetContextDocument(doc);
      }
      catch (Exception ex)
      {
        ErrorLog($"Something went wrong: {ex.Message}");
      }
    }
  }
}