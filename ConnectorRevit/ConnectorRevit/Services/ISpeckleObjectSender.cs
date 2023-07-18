using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Models;

namespace ConnectorRevit.Services
{
  /// <summary>
  /// Responsible for sending the commit object to the desired destination
  /// </summary>
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
