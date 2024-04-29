using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Common;

// POC: reminder - writing classes and creating interfaces is a bit like organising your space
// if you have a structure for organising things, your interfaces, then finding your stuff, your classes & methods, becomes easy
// having a lack of interfaces or large interfaces is a bit like lacking structure, when all of your stuff, your classes & methods
// clould be anywhere or all in once place - rooting through box 274 for something you need, when said box has a miriad different
// and unrelated items, is no fun. Plus when you need that item, you end up bringing out the whole box/
[NameAndRankValue(nameof(DB.Floor), 0)]
public class FloorConversionToSpeckle : IHostObjectToSpeckleConversion
{
  public Base Convert(object target) => throw new SpeckleConversionException();
}
