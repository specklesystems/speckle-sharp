using System.Collections.Generic;

namespace Speckle.Converters.Common;

public class ConversionContextStack<TDocument, THostUnit> : IConversionContextStack<TDocument, THostUnit>
  where TDocument : class
{
  private readonly IHostToSpeckleUnitConverter<THostUnit> _unitConverter;
  private readonly TDocument _document;

  protected ConversionContextStack(
    TDocument document,
    THostUnit hostUnit,
    IHostToSpeckleUnitConverter<THostUnit> unitConverter
  )
  {
    _document = document;
    _unitConverter = unitConverter;

    _stack.Push(new ConversionContext<TDocument>(_document, _unitConverter.ConvertOrThrow(hostUnit)));
  }

  private readonly Stack<ConversionContext<TDocument>> _stack = new();

  public ConversionContext<TDocument> Current => _stack.Peek();

  public ContextWrapper<TDocument, THostUnit> Push(string speckleUnit)
  {
    var context = new ConversionContext<TDocument>(_document, speckleUnit);
    _stack.Push(context);
    return new ContextWrapper<TDocument, THostUnit>(this);
  }

  public ContextWrapper<TDocument, THostUnit> Push(THostUnit hostUnit)
  {
    return Push(_unitConverter.ConvertOrThrow(hostUnit));
  }

  public void Pop() => _stack.Pop();
}
