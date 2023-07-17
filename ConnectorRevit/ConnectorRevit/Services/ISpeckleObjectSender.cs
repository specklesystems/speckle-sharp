using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Models;

namespace ConnectorRevit.Services
{
  public interface ISpeckleObjectSender
  {
    string CommitId { get; }
    string ObjectId { get; }
    Task<string> Send(
      string streamId,
      string branchName,
      string commitMessage,
      Base commitObject,
      int convertedCount
    );
  }
}
