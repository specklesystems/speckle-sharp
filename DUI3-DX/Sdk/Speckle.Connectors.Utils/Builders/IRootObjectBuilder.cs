using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Models;

namespace Speckle.Connectors.Utils.Builders;

public interface IRootObjectBuilder<T>
{
  public Base Build(
    IReadOnlyList<T> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  );
}
