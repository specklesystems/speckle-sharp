#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Geometry;
using Autodesk.AdvanceSteel.Modelling;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using System.Linq;

namespace Objects.Converter.AutocadCivil
{
  public class MainAliasProperties : ASBaseProperties<MainAlias>, IASProperties
  {
    public override Dictionary<string, ASProperty> BuildedPropertyList()
    {
      Dictionary<string, ASProperty> dictionary = new Dictionary<string, ASProperty>();

      InsertProperty(dictionary, "numbering - fabrication station", nameof(MainAlias.FabricationStationUsedForNumbering));
      InsertProperty(dictionary, "load number", nameof(MainAlias.LoadNumber));
      InsertProperty(dictionary, "carrier", nameof(MainAlias.Carrier));
      InsertProperty(dictionary, "fabrication station", nameof(MainAlias.FabricationStation));
      InsertProperty(dictionary, "supplier", nameof(MainAlias.Supplier));
      InsertProperty(dictionary, "PO number", nameof(MainAlias.PONumber));
      InsertProperty(dictionary, "requisition number", nameof(MainAlias.RequisitionNumber));
      InsertProperty(dictionary, "heat number", nameof(MainAlias.HeatNumber));
      InsertProperty(dictionary, "shipped date", nameof(MainAlias.ShippedDate));
      InsertProperty(dictionary, "delivery date", nameof(MainAlias.DeliveryDate));
      InsertProperty(dictionary, "numbering - supplier", nameof(MainAlias.SupplierUsedForNumbering));
      InsertProperty(dictionary, "numbering - requisition number", nameof(MainAlias.RequisitionNumberUsedForNumbering));
      InsertProperty(dictionary, "approval comment", nameof(MainAlias.ApprovalComment));
      InsertProperty(dictionary, "numbering - heat number", nameof(MainAlias.HeatNumberUsedForNumbering));
      InsertProperty(dictionary, "numbering - PO number", nameof(MainAlias.PONumberUsedForNumbering));
      InsertProperty(dictionary, "approval status code", nameof(MainAlias.ApprovalStatusCode));
      InsertProperty(dictionary, "standard weight", nameof(MainAlias.GetStandardWeight));

      return dictionary;
    }

  }
}
#endif
