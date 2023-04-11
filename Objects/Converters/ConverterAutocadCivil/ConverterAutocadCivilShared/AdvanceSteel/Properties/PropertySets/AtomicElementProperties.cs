#if ADVANCESTEEL2023
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.AdvanceSteel.Connection;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Geometry;
using ASObjectId = Autodesk.AdvanceSteel.CADLink.Database.ObjectId;

namespace Objects.Converter.AutocadCivil
{
  public class AtomicElementProperties : ASBaseProperties<AtomicElement>, IASProperties
  {
    public override Dictionary<string, ASProperty> BuildedPropertyList()
    {
      Dictionary<string, ASProperty> dictionary = new Dictionary<string, ASProperty>();

      InsertProperty(dictionary, "volume", nameof(AtomicElement.Volume));
      InsertProperty(dictionary, "used for numbering - assembly", nameof(AtomicElement.AssemblyUsedForNumbering));
      InsertProperty(dictionary, "used for numbering - note", nameof(AtomicElement.NoteUsedForNumbering));
      InsertProperty(dictionary, "used for numbering - role", nameof(AtomicElement.RoleUsedForNumbering));
      InsertProperty(dictionary, "used for BOM - single part", nameof(AtomicElement.SinglePartUsedForBOM));
      InsertProperty(dictionary, "used for BOM - main part", nameof(AtomicElement.MainPartUsedForBOM));
      InsertProperty(dictionary, "used for collision check - single part", nameof(AtomicElement.SinglePartUsedForCollisionCheck));
      InsertProperty(dictionary, "used for collision check - main part", nameof(AtomicElement.MainPartUsedForCollisionCheck));
      InsertProperty(dictionary, "structural member", nameof(AtomicElement.StructuralMember));
      InsertProperty(dictionary, "used for numbering - holes", nameof(AtomicElement.HolesUsedForNumbering));
      InsertProperty(dictionary, "mainPart number", nameof(AtomicElement.MainPartNumber));
      InsertProperty(dictionary, "singlePart number", nameof(AtomicElement.SinglePartNumber));
      InsertProperty(dictionary, "preliminary part prefix", nameof(AtomicElement.PreliminaryPartPrefix));
      InsertProperty(dictionary, "preliminary part number", nameof(AtomicElement.PreliminaryPartNumber));
      InsertProperty(dictionary, "preliminary part position number", nameof(AtomicElement.PreliminaryPartPositionNumber));
      InsertProperty(dictionary, "used for numbering - item number", nameof(AtomicElement.ItemNumberUsedForNumbering));
      InsertProperty(dictionary, "used for numbering - dennotation", nameof(AtomicElement.DennotationUsedForNumbering));
      InsertProperty(dictionary, "used for numbering - coating", nameof(AtomicElement.CoatingUsedForNumbering));
      InsertProperty(dictionary, "used for numbering - material", nameof(AtomicElement.MaterialUsedForNumbering));
      InsertProperty(dictionary, "unwind start factor", nameof(AtomicElement.UnwindStartFactor));
      InsertProperty(dictionary, "denotation", nameof(AtomicElement.Denotation));
      InsertProperty(dictionary, "assembly", nameof(AtomicElement.Assembly));
      InsertProperty(dictionary, "note", nameof(AtomicElement.Note));
      InsertProperty(dictionary, "item number", nameof(AtomicElement.ItemNumber));
      InsertProperty(dictionary, "specific gravity", nameof(AtomicElement.SpecificGravity));
      InsertProperty(dictionary, "coating", nameof(AtomicElement.Coating));
      InsertProperty(dictionary, "number of holes", nameof(AtomicElement.NumberOfHoles));
      InsertProperty(dictionary, "is attached part", nameof(AtomicElement.IsAttachedPart));
      InsertProperty(dictionary, "is main part", nameof(AtomicElement.IsMainPart));
      InsertProperty(dictionary, "main part prefix", nameof(AtomicElement.MainPartPrefix));
      InsertProperty(dictionary, "single part prefix", nameof(AtomicElement.SinglePartPrefix));
      InsertProperty(dictionary, "used for numbering - single part", nameof(AtomicElement.SinglePartUsedForNumbering));
      InsertProperty(dictionary, "used for numbering - main part", nameof(AtomicElement.MainPartUsedForNumbering));
      InsertProperty(dictionary, "explicit quantity", nameof(AtomicElement.ExplicitQuantity));
      InsertProperty(dictionary, "material description", nameof(AtomicElement.MaterialDescription));
      InsertProperty(dictionary, "coating description", nameof(AtomicElement.CoatingDescription));
      InsertProperty(dictionary, "material", nameof(AtomicElement.Material));
      InsertProperty(dictionary, "unwind", nameof(AtomicElement.Unwind));

      //Functions

      InsertCustomProperty(dictionary, "balance point", nameof(AtomicElementProperties.GetBalancePoint), null);
      InsertProperty(dictionary, "mainPart position", nameof(AtomicElement.GetMainPartPositionNumber));
      InsertProperty(dictionary, "model quantity", nameof(AtomicElement.GetQuantityInModel));
      InsertProperty(dictionary, "singlePart position", nameof(AtomicElement.GetSinglePartPositionNumber));
      InsertProperty(dictionary, "features number", nameof(AtomicElement.NumFeatures));
      InsertCustomProperty(dictionary, "holes", nameof(AtomicElementProperties.GetHoles), null);
      InsertCustomProperty(dictionary, "numbering - valid single part", nameof(AtomicElementProperties.HasValidSPNumber), null);
      InsertCustomProperty(dictionary, "numbering - valid main part", nameof(AtomicElementProperties.HasValidMPNumber), null);

      return dictionary;
    }

    private static Point3d GetBalancePoint(AtomicElement atomicElement)
    {
      //it's necessary round the balance point because it has different returns at the last decimals 
      if(!atomicElement.GetBalancepoint(out var point, out var weigth))
      {
        return Point3d.kOrigin;
      }

      return new Point3d(Round(point.x), Round(point.y), Round(point.z));
    }

    private static bool HasValidSPNumber(AtomicElement atomicElement)
    {
      atomicElement.GetNumberingStatus(out bool hasValidSPNumber, out bool hasValidMPNumber);
      return hasValidSPNumber;
    }

    private static bool HasValidMPNumber(AtomicElement atomicElement)
    {
      atomicElement.GetNumberingStatus(out bool hasValidSPNumber, out bool hasValidMPNumber);
      return hasValidMPNumber;
    }

    private static List<Dictionary<object, object>> GetHoles(AtomicElement atomicElement)
    {
      var holes = GetHolesFeatures(atomicElement);

      List<Dictionary<object, object>> listHolesDetails = new List<Dictionary<object, object>>();

      foreach (var hole in holes)
      {
        hole.CS.GetCoordSystem(out var point, out _, out _, out var vectorZ);

        Dictionary<object, object> holeProperties = new Dictionary<object, object>
        {
          { "Diameter", hole.Hole.Diameter },
          { "Center", point },
          { "Normal", vectorZ }
        };

        listHolesDetails.Add(holeProperties);
      }

      return listHolesDetails;
    }

    private static List<ConnectionHoleFeature> GetHolesFeatures(AtomicElement pAtomicElement)
    {
      List<ConnectionHoleFeature> holes = new List<ConnectionHoleFeature>();

      if (pAtomicElement == null)
        return holes;

      var features = pAtomicElement.GetFeatures(true);

      foreach (ASObjectId objectIDASFeature in features)
      {
        FilerObject filerObject = DatabaseManager.Open(objectIDASFeature);
        if (filerObject is ConnectionHoleFeature)
        {
          ConnectionHoleFeature connectionHoleFeature = (ConnectionHoleFeature)filerObject;
          holes.Add(connectionHoleFeature);
        }
      }

      return holes;
    }

  }
}
#endif
