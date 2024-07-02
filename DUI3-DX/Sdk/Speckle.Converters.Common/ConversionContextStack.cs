using System.Diagnostics.CodeAnalysis;
using Speckle.InterfaceGenerator;

namespace Speckle.Converters.Common;

// POC: Suppressed naming warning for now, but we should evaluate if we should follow this or disable it.
[SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "Name ends in Stack but it is in fact a Stack, just not inheriting from `System.Collections.Stack`"
)]
[GenerateAutoInterface]
public abstract class ConversionContextStack<TDocument, THostUnit> : IConversionContextStack<TDocument, THostUnit>
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

  private readonly Stack<IConversionContext<TDocument>> _stack = new();

  public IConversionContext<TDocument> Current => _stack.Peek();

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
