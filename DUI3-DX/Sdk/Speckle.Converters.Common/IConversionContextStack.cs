using System;
using System.Reflection.Metadata;

namespace Speckle.Converters.Common;

public interface IConversionContextStack<TDocument, THostUnit>
  where TDocument : class
{
  ContextWrapper<TDocument, THostUnit> Push(string speckleUnit);
  ContextWrapper<TDocument, THostUnit> Push(THostUnit hostUnit);
  void Pop();
  ConversionContext<TDocument> Current { get; }
}
