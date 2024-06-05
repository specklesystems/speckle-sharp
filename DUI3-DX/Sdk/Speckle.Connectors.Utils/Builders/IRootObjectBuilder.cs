using Speckle.Connectors.Utils.Conversion;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Models;

namespace Speckle.Connectors.Utils.Builders;

public interface IRootObjectBuilder<in T>
{
  public RootObjectBuilderResult Build(
    IReadOnlyList<T> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  );
}

public record RootObjectBuilderResult(Base RootObject, IEnumerable<SendConversionResult> ConversionResults);
