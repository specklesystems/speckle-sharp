using System.Diagnostics.CodeAnalysis;
using Speckle.Converters.Common;

namespace Speckle.Converters.Civil3d;

// POC: Suppressed naming warning for now, but we should evaluate if we should follow this or disable it.
[SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "Name ends in Stack but it is in fact a Stack, just not inheriting from `System.Collections.Stack`"
)]
public class Civil3dConversionContextStack : ConversionContextStack<Document, AAEC.BuiltInUnit>
{
  public Civil3dConversionContextStack(IHostToSpeckleUnitConverter<AAEC.BuiltInUnit> unitConverter)
    : base(
      Application.DocumentManager.CurrentDocument,
      GetDocBuiltInUnit(Application.DocumentManager.CurrentDocument),
      unitConverter
    ) { }

  private static AAEC.BuiltInUnit GetDocBuiltInUnit(Document doc)
  {
    AAEC.BuiltInUnit unit = AAEC.BuiltInUnit.Dimensionless;

    using (ADB.Transaction tr = doc.Database.TransactionManager.StartTransaction())
    {
      ADB.ObjectId id = AAEC.ApplicationServices.DrawingSetupVariables.GetInstance(doc.Database, false);
      if (tr.GetObject(id, ADB.OpenMode.ForRead) is AAEC.ApplicationServices.DrawingSetupVariables setupVariables)
      {
        unit = setupVariables.LinearUnit;
      }

      tr.Commit();
    }

    return unit;
  }
}
