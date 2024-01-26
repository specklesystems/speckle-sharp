using Objects.Structural.CSI.Properties;
using Speckle.Core.Kits;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  private string DiaphragmToNative(CSIDiaphragm csiDiaphragm)
  {
    //TODO: test this bad boy, I'm not sure how it would create anything meaningful with just a name and a bool
    var success = Model.Diaphragm.SetDiaphragm(csiDiaphragm.name, csiDiaphragm.SemiRigid);

    if (success != 0)
    {
      throw new ConversionException($"Failed to create/modify diaphragm {csiDiaphragm.name}");
    }

    return csiDiaphragm.name;
  }

  CSIDiaphragm diaphragmToSpeckle(string name)
  {
    bool semiRigid = false;
    Model.Diaphragm.GetDiaphragm(name, ref semiRigid);
    return new CSIDiaphragm(name, semiRigid);
  }
}
