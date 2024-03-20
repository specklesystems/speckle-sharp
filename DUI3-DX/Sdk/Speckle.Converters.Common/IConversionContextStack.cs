namespace Speckle.Converters.Common;

public interface IConversionContextStack<TContext, TDocument, THostUnit>
  where TContext : IConversionContext<TDocument, THostUnit>
  where TDocument : class { }
