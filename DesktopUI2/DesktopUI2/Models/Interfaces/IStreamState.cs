using System;
using System.Collections.Generic;
using System.Text;
using DesktopUI2.Models.Settings;
using Sentry;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace DesktopUI2.Models.Interfaces
{
  public interface IStreamState
  {
    public List<ISetting> Settings { get; }
    public Commit LastCommit { get; set; }
    public ReceiveMode ReceiveMode { get; }
    public string StreamId { get; }
    public List<ApplicationObject> ReceivedObjects { get; set; }
    public string BranchName { get; }
    public string CommitId { get; }
    public Client Client { get; }
  }
}
