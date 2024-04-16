using System;
using System.Collections.Generic;
using System.Threading;
using Speckle.Core.Models;

namespace Speckle.Connectors.Utils.Builders;

// POC: We might consider to put also IRootObjectBuilder interface here in same folder and create concrete classes from it in per connector.
public interface IHostObjectBuilder
{
  /// <summary>
  /// Build host application objects from root commit object.
  /// </summary>
  /// <param name="rootObject">Commit object that received from server.</param>
  /// <param name="projectName">Project of the model.</param>
  /// <param name="modelName">Name of the model.</param>
  /// <param name="onOperationProgressed"> Action to update UI progress bar.</param>
  /// <param name="cancellationToken">Cancellation token that passed from top -> ReceiveBinding.</param>
  /// <returns> List of application ids.</returns> // POC: Where we will return these ids will matter later when we target to also cache received application ids.
  /// <remarks>Project and model name are needed for now to construct host app objects into related layers or filters.
  /// POC: we might consider later to have HostObjectBuilderContext? that might hold all possible data we will need.</remarks>
  IEnumerable<string> Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  );
}
