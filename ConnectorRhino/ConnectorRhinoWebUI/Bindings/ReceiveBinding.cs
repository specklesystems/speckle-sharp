using System;
using System.Windows.Threading;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Utils;
using Rhino;

namespace ConnectorRhinoWebUI.Bindings
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

    public void CancelReceive(string modelCardId)
    {
      throw new NotImplementedException();
    }

    public void Receive(string modelCardId, string versionId)
    {
      RhinoDoc doc = RhinoDoc.ActiveDoc;
      ReceiverModelCard model = _store.GetModelById(modelCardId) as ReceiverModelCard;
      RhinoApp.WriteLine(string.Format("Model Card Type: {0}", model.TypeDiscriminator));
      RhinoApp.WriteLine(string.Format("Model Card Id: {0}", model.Id));
      RhinoApp.WriteLine(string.Format("Project Id: {0}", model.ProjectId));
      RhinoApp.WriteLine(string.Format("Model Id: {0}", model.ModelId));
      RhinoApp.WriteLine(string.Format("Version Id: {0}", versionId));

      // TODO: Do here real receive

      Dispatcher.CurrentDispatcher.Invoke(() =>
      {
        Progress.ReceiverProgressToBrowser(Parent, modelCardId, 1);
      }, DispatcherPriority.Background);
    }
  }
}
