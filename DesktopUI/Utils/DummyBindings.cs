using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.DesktopUI.Utils
{
  public class DummyBindings : ConnectorBindings
  {
    public override void AddExistingClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void AddNewClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void AddObjectsToClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void AddSelectionToClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void BakeClient(string args)
    {
      throw new NotImplementedException();
    }

    public override string GetApplicationHostName()
    {
      return "Spockle";
    }

    public override string GetDocumentId()
    {
      throw new NotImplementedException();
    }

    public override string GetDocumentLocation()
    {
      throw new NotImplementedException();
    }

    public override string GetFileClients()
    {
      throw new NotImplementedException();
    }

    public override string GetFileName()
    {
      throw new NotImplementedException();
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      throw new NotImplementedException();
    }

    public override void PushClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveObjectsFromClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveSelectionFromClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    public override void UpdateClient(string args)
    {
      throw new NotImplementedException();
    }
  }
}
