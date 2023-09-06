using System.Linq;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace ConnectorAutocadDUI3.Bindings
{
  public class ReceiveBinding : IReceiveBinding
  {
    public string Name { get; set; } = "receiveBinding";
    public IBridge Parent { get; set; }
    
    private DocumentModelStore _store;

    public ReceiveBinding(DocumentModelStore store)
    {
      _store = store;
    }
    
    public async void Receive(string modelCardId, string versionId)
    {
      Base commitObject = await DUI3.Utils.Receive.GetCommitBase(Parent, _store, modelCardId, versionId);

      Document doc = Application.DocumentManager.MdiActiveDocument;
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.VersionedAppName);
      converter.SetContextDocument(doc);
      
      
    }

    public void CancelReceive(string modelCardId)
    {
      throw new System.NotImplementedException();
    }
  }
}
