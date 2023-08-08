using System.Linq;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorAutocadDUI3.Bindings;

public class BasicConnectorBindingAutocad : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }

  private static Document Doc => Application.DocumentManager.MdiActiveDocument;
  private readonly AutocadDocumentModelStore _store;
  private static string _previousDocName;
  
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
}
