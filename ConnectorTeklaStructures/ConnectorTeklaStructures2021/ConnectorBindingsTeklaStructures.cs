using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorTeklaStructures
{
  public class ConnectorBindingsTeklaStructures : ConnectorBindings
  {
    public override string GetActiveViewName()
    {
      throw new NotImplementedException();
    }

    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      throw new NotImplementedException();
    }

    public override string GetDocumentId()
    {
      throw new NotImplementedException();
    }

    public override string GetDocumentLocation()
    {
      throw new NotImplementedException();
    }

    public override string GetFileName()
    {
      throw new NotImplementedException();
    }

    public override string GetHostAppName()
    {
      throw new NotImplementedException();
    }

    public override List<string> GetObjectsInView()
    {
      throw new NotImplementedException();
    }

    public override List<string> GetSelectedObjects()
    {
      throw new NotImplementedException();
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      throw new NotImplementedException();
    }

    public override List<StreamState> GetStreamsInFile()
    {
      throw new NotImplementedException();
    }

    public override Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      throw new NotImplementedException();
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    public override Task SendStream(StreamState state, ProgressViewModel progress)
    {
      throw new NotImplementedException();
    }

    public override void WriteStreamsToFile(List<StreamState> streams)
    {
      throw new NotImplementedException();
    }
  }
}
