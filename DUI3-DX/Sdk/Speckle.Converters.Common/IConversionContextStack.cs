using System.Diagnostics.CodeAnalysis;

namespace Speckle.Converters.Common;

// POC: Suppressed naming warning for now, but we should evaluate if we should follow this or disable it.
[SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "Name ends in Stack but it is in fact a Stack, just not inheriting from `System.Collections.Stack`"
)]
public interface IConversionContextStack<TDocument, THostUnit>
  where TDocument : class
{
  ContextWrapper<TDocument, THostUnit> Push(string speckleUnit);
  ContextWrapper<TDocument, THostUnit> Push(THostUnit hostUnit);
  void Pop();
  ConversionContext<TDocument> Current { get; }
}
