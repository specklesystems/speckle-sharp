using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2.Models.Interfaces;
using DesktopUI2.Models.Settings;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace ConverterRevitTestsShared
{
  internal class DummyStreamState : IStreamState
  {
    public List<ISetting> Settings => throw new NotImplementedException();

    public Commit LastCommit 
    {
      get { return null; } 
      set { }
    }

    public ReceiveMode ReceiveMode => throw new NotImplementedException();

    public string StreamId => throw new NotImplementedException();

    public List<ApplicationObject> ReceivedObjects { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public string BranchName => throw new NotImplementedException();

    public string CommitId => throw new NotImplementedException();

    public Client Client => throw new NotImplementedException();

    public Task<Commit> GetCommit(CancellationToken cancellationToken = default)
    {
      throw new NotImplementedException();
    }
  }
}
