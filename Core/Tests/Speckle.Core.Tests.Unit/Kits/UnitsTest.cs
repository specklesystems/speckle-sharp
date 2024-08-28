using NUnit.Framework;
using Speckle.Core.Kits;

namespace Speckle.Core.Tests.Unit.Kits;

[TestOf(typeof(Units))]
public class UnitsTest
{
  private const double EPS = 0.00022;

  [Test, Combinatorial]
  [DefaultFloatingPointTolerance(EPS)]
  public void TestUnitConversion(
    [ValueSource(typeof(Units), nameof(Units.SupportedUnits))] string from,
    [ValueSource(typeof(Units), nameof(Units.SupportedUnits))] string to
  )
  {
    var forwards = Units.GetConversionFactor(from, to);
    var backwards = Units.GetConversionFactor(to, from);

    Assert.That(
      backwards * forwards,
      Is.EqualTo(1d),
      $"Behaviour says that 1{from} == {forwards}{to}, and 1{to} == {backwards}{from}"
    );
  }
}
