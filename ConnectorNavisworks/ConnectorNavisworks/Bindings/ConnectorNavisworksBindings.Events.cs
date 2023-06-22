using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  public void RegisterAppEvents()
  {
    //// GLOBAL EVENT HANDLERS

    // Navisworks is an SDI (Single Document Interface) and on launch has an initial empty document.
    // Loading a file doesn't trigger the ActiveDocumentChanged event.
    // Instead it amends it in place. We can listen to the filename changing to get the intuitive event.
    _doc.FileNameChanged += DocumentChangedEvent;
    _doc.SelectionSets.Changed += SetsChangedEvent;
  }

  private void SetsChangedEvent(object sender, EventArgs e)
  {
    UpdateSelectedStream?.Invoke();
  }

  // Triggered when the active document name is changed.
  // This will happen automatically if a document is newly created or opened.
  private void DocumentChangedEvent(object sender, EventArgs e)
  {
    // As ConnectorNavisworks is Send only, There is little use for a new empty document.
    if (sender is not Document doc || doc.IsClear)
    {
      UpdateSavedStreams?.Invoke(new List<StreamState>());
      MainViewModel.GoHome();
      return;
    }

    var streams = GetStreamsInFile();
    UpdateSavedStreams?.Invoke(streams);

    UpdateSelectedStream?.Invoke();

    MainViewModel.GoHome();

    _navisworksConverter.SetContextDocument(doc);

    // Nullify any cached commit and conversions
    _cachedCommit = null;
    CachedConvertedElements = null;
    _cachedState = null;
  }
}
