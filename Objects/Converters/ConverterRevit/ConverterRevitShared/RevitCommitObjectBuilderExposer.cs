using ConverterRevitShared;
using RevitSharedResources.Interfaces;

namespace Objects.Converter.Revit;

public partial class ConverterRevit : IRevitCommitObjectBuilderExposer
{
  public IRevitCommitObjectBuilder commitObjectBuilder { get; } =
    new RevitCommitObjectBuilder(CommitCollectionStrategy.ByCollection);
}
