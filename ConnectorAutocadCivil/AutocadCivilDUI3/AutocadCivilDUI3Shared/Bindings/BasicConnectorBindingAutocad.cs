using System;
using System.Collections.Generic;
using System.Linq;
using AutocadCivilDUI3Shared.Extensions;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Speckle.Core.Credentials;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Speckle.ConnectorAutocadDUI3.Bindings;

public class BasicConnectorBindingAutocad : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }

  private static Document Doc => Application.DocumentManager.MdiActiveDocument;
  private readonly AutocadDocumentModelStore _store;

  public BasicConnectorBindingAutocad(AutocadDocumentModelStore store)
  {
    _store = store;
    _store.DocumentChanged += (_, _) =>
    {
      Parent?.SendToBrowser(BasicConnectorBindingEvents.DocumentChanged);
    };
  }

  public string GetSourceApplicationName()
  {
    return Core.Kits.HostApplications.AutoCAD.Slug;
  }

  public string GetSourceApplicationVersion()
  {
    #if AUTOCAD2023DUI3
    return "2023";
    # endif
    #if AUTOCAD2022DUI3
    return "2022";
    #endif
  }

  public Account[] GetAccounts()
  {
    return AccountManager.GetAccounts().ToArray();
  }

  public DocumentInfo GetDocumentInfo()
  {
    var name = Doc.Name.Split(System.IO.Path.PathSeparator).Reverse().First();
    return new DocumentInfo()
    {
      Name = name,
      Id = Doc.Name,
      Location = Doc.Name
    };
  }

  public DocumentModelStore GetDocumentState()
  {
    return _store;
  }

  public void AddModel(ModelCard model)
  {
    _store.Models.Add(model);
  }

  public void UpdateModel(ModelCard model)
  {
    var idx = _store.Models.FindIndex(m => model.Id == m.Id);
    _store.Models[idx] = model;
  }

  public void RemoveModel(ModelCard model)
  {
    var index = _store.Models.FindIndex(m => m.Id == model.Id);
    _store.Models.RemoveAt(index);
  }

  public void HighlightModel(string modelCardId)
  {
    SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
    List<DBObject> dbObjects = Objects.GetObjectsFromDocument(Doc, model.SendFilter.GetObjectIds());
    Database database = Doc.Database;
    Editor editor = Doc.Editor;
    editor.SetImpliedSelection(Array.Empty<ObjectId>());
    Transaction tr = database.TransactionManager.StartTransaction();

    ObjectId[] objectIds = dbObjects.Select(dbObject => dbObject.Id).ToArray();
    editor.SetImpliedSelection(objectIds);
    editor.UpdateScreen();
    
    Parent.RunOnMainThread(
      () =>
      {
        editor.Zoom(Extends3dExtensions.FromObjectIds(editor, objectIds));
      });

    Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
    tr.Commit();
  }
}
