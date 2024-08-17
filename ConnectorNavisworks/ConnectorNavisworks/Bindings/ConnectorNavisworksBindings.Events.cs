using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  /// <summary>
  /// Registers global event handlers for the application.
  /// </summary>
  /// <remarks>
  /// This method sets up event handlers for various events related to the Navisworks document.
  /// Navisworks operates as a Single Document Interface (SDI) and initially opens with an empty document.
  /// Unlike other interfaces, loading a file in Navisworks does not trigger an ActiveDocumentChanged event,
  /// but rather modifies the existing document in place. This method listens for the filename change event
  /// to capture the intuitive document change event. It also registers event handlers for changes in selection sets
  /// and changes in the model collection.
  /// </remarks>
  public void RegisterAppEvents()
  {
    // Register event handlers for document, selection sets, and model collection changes
    s_activeDoc.FileNameChanged += DocumentChangedEvent;
    s_activeDoc.SelectionSets.Changed += SetsChangedEvent;
    s_activeDoc.Models.CollectionChanged += ModelsChangedEvent;
  }

  /// <summary>
  /// Handles the event triggered when the selection sets are changed.
  /// </summary>
  /// <param name="sender">The source of the event.</param>
  /// <param name="e">The event data. Not used in this handler.</param>
  private void SetsChangedEvent(object sender, EventArgs e) => UpdateSelectedStream?.Invoke();

  /// <summary>
  /// Handles the event triggered when the active document name changes.
  /// </summary>
  /// <param name="sender">The source of the event, expected to be of type Document.</param>
  /// <param name="e">Event data associated with the document change event.</param>
  /// <remarks>
  /// This method is called automatically when a document is newly created or opened,
  /// resulting in a change in the active document name. It first verifies if the sender
  /// is a valid and non-clear Document using the IsInvalidOrClearDocument method. If the sender
  /// is valid, the method updates the saved streams, navigates the MainViewModel to home,
  /// retrieves streams from the file, updates the selected stream, and sets the context
  /// document in the Navisworks converter, followed by nullifying the commit cache.
  /// </remarks>
  private void DocumentChangedEvent(object sender, EventArgs e)
  {
    if (IsInvalidOrClearDocument(sender))
    {
      return;
    }

    // Safely cast sender to Document now that we know it's not null or clear.
    Document doc = sender as Document;

    UpdateSavedStreams?.Invoke(new List<StreamState>());
    MainViewModel.GoHome();

    var streams = GetStreamsInFile();
    UpdateSavedStreams?.Invoke(streams);
    UpdateSelectedStream?.Invoke();
    MainViewModel.GoHome();

    _navisworksConverter.SetContextDocument(doc);

    NullifyCommitCache();
  }

  /// <summary>
  /// Handles the event triggered when the models change.
  /// </summary>
  /// <param name="sender">The source of the event, expected to be of type Document.</param>
  /// <param name="e">The event data. Not used in this handler.</param>
  /// <remarks>
  /// Any change in the Models collection of the Document will require the recalculation of the context document.
  /// Particularly important in the establishment of a changed up direction which can occur.
  /// </remarks>
  private void ModelsChangedEvent(object sender, EventArgs e)
  {
    if (IsInvalidOrClearDocument(sender))
    {
      return;
    }

    // Safely cast sender to Document now that we know it's not null or clear.
    Document doc = sender as Document;
    _navisworksConverter.SetContextDocument(doc);

    NullifyCommitCache();
  }

  /// <summary>
  /// Evaluates if the sender object from an event handler is a valid Document.
  /// </summary>
  /// <param name="sender">The sender object, expected to be of type Document.</param>
  /// <returns>
  /// Returns true if the sender is either not a Document or is a Document marked as clear.
  /// This indicates that the sender object is an invalid Document and the event handler
  /// should terminate further processing.
  /// Returns false if the sender is a valid, non-clear Document, suggesting that the event
  /// handler should continue its processing.
  /// </returns>
  /// <remarks>
  /// This method checks the sender object to determine if it is a valid Document (not null
  /// and not marked as clear). If the sender fails this check, the method proceeds to invoke
  /// UpdateSavedStreams with a new list of StreamState and navigates the MainViewModel to home,
  /// then returns true, signifying the sender as an invalid Document. Otherwise, it returns false,
  /// allowing the caller to continue processing.
  /// </remarks>
  private static bool IsInvalidOrClearDocument(object sender) => sender is not Document { IsClear: false };

  /// <summary>
  /// Nullifies the commit cache.
  /// </summary>
  /// <remarks>
  /// This method resets the static fields related to the commit cache to null.
  /// Any changes to the host document will invalidate the cache, so it needs to be reset.
  /// </remarks>
  private static void NullifyCommitCache()
  {
    s_cachedCommit = null;
    CachedConvertedElements = null;
    s_cachedState = null;
  }
}
