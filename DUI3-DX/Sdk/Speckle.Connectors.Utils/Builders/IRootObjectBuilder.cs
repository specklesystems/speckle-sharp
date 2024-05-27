using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.Utils.Builders;

public interface IRootObjectBuilder<in T>
{
  public SendConversionResults Build(
    IReadOnlyList<T> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  );
}
