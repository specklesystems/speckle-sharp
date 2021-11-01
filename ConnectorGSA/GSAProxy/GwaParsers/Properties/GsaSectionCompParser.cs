using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //The term "section component" here is a name applied to both the group as a whole as well as one member of the group, 
  //but the latter is shortened to SectionComp to distinguish them here
  [GsaType(GwaKeyword.SECTION_COMP, GwaSetCommandType.Set, false, true, true)]
  public class SectionCompParser : GwaParser<SectionComp>, ISectionComponentGwaParser
  {
    //public override Type GsaSchemaType { get => typeof(SectionComp); }

    public SectionCompParser(SectionComp sectionComp) : base(sectionComp) { }

    public SectionCompParser() : base(new SectionComp()) { }
    
    public override bool FromGwa(string gwa)
    {
      //SECTION_COMP | ref | name | matAnal | matType | matRef | desc | offset_y | offset_z | rotn | reflect | pool | taperType | taperPos
      //Note: the ref argument is missing when the GWA was embedded within a SECTION command, so need to detect this case
      //This also means the BasicFromGwa can't be called here because that does assume an index parameter
      var items = Split(gwa);

      if (items[0].StartsWith("set", StringComparison.OrdinalIgnoreCase))
      {
        items.Remove(items[0]);
      }
      if (!ParseKeywordVersionSid(items[0]))
      {
        return false;
      }
      items = items.Skip(1).ToList();

      var record = (SectionComp)this.record;

      //Detect presence or absense of ref (record index) argument based on number of items
      if (int.TryParse(items[0], out var foundIndex))
      {
        record.Index = foundIndex;
        items = items.Skip(1).ToList();
      }

      if (!FromGwaByFuncs(items, out var remainingItems, AddName, (v) => AddNullableIndex(v, out record.MatAnalIndex),
        (v) => Enum.TryParse(v, true, out record.MaterialType), (v) => AddNullableIndex(v, out record.MaterialIndex)))
      {
        return false;
      }
      items = remainingItems;

      if (!ProcessDesc(items[0]))
      {
        return false;
      }
      items = items.Skip(1).ToList();

      return (FromGwaByFuncs(items, out _, (v) => AddNullableDoubleValue(v, out record.OffsetY), (v) => AddNullableDoubleValue(v, out record.OffsetZ),
        (v) => AddNullableDoubleValue(v, out record.Rotation), (v) => Enum.TryParse(v, true, out record.Reflect), (v) => AddNullableIntValue(v, out record.Pool),
        (v) => Enum.TryParse(v, true, out record.TaperType), (v) => AddNullableDoubleValue(v, out record.TaperPos)));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      gwa = (GwaItems(out var items, includeSet) && Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    //Note: the ref argument is missing when the GWA was embedded within a SECTION command, hence the addition of the additional boolean argument
    public bool GwaItems(out List<string> items, bool includeSet = false, bool includeRef = false)
    {
      items = new List<string>();

      if (includeSet)
      {
        items.Add("SET");
      }

      var record = (SectionComp)this.record;

      var sid = FormatSidTags(record.StreamId, record.ApplicationId);
      items.Add(keyword + "." + record.Version + ((string.IsNullOrEmpty(sid)) ? "" : ":" + sid));
      
      if ((bool)GetType().GetAttribute<GsaType>("SelfContained"))
      {
        items.Add(record.Index.ToString());
      }

      //SECTION_COMP | ref | name | matAnal | matType | matRef | desc | offset_y | offset_z | rotn | reflect | pool | taperType | taperPos
      if (includeRef && !AddItems(ref items, record.Index ?? 0))
      {
        return false;
      }
      return AddItems(ref items, record.Name, record.MatAnalIndex ?? 0, record.MaterialType.ToString(), record.MaterialIndex ?? 0,
        (record.ProfileDetails == null) ? "" : record.ProfileDetails.ToDesc(),
        record.OffsetY ?? 0, record.OffsetZ ?? 0, record.Rotation ?? 0, record.Reflect.ToString(), record.Pool ?? 0, record.TaperType.ToString(), record.TaperPos ?? 0);
    }

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      ((SectionComp)record).Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool ProcessDesc(string v)
    {
      var pieces = v.ListSplit(" ");
      if (!pieces[0].TryParseStringValue(out Section1dProfileGroup sectionProfileGroup))
      {
        return false;
      }

      var record = ((SectionComp)this.record);

      if (sectionProfileGroup == Section1dProfileGroup.Explicit)
      {
        record.ProfileDetails = new ProfileDetailsExplicit();
        record.ProfileDetails.FromDesc(v);
        record.ProfileGroup = Section1dProfileGroup.Explicit;
      }
      else if (sectionProfileGroup == Section1dProfileGroup.Perimeter)
      {
        record.ProfileDetails = new ProfileDetailsPerimeter();
        record.ProfileDetails.FromDesc(v);
        record.ProfileGroup = Section1dProfileGroup.Perimeter;
      }
      else if (sectionProfileGroup == Section1dProfileGroup.Catalogue)
      {
        record.ProfileDetails = new ProfileDetailsCatalogue();
        record.ProfileDetails.FromDesc(v);
        record.ProfileGroup = Section1dProfileGroup.Catalogue;
      }
      else
      {
        //Standard
        if (!pieces[1].TryParseStringValue(out Section1dStandardProfileType profileType))
        {
          return false;
        }
        switch (profileType)
        {
          case Section1dStandardProfileType.Rectangular:
          case Section1dStandardProfileType.RectoCircular:
            record.ProfileDetails = new ProfileDetailsRectangular();
            break;

          case Section1dStandardProfileType.Circular:
            record.ProfileDetails = new ProfileDetailsCircular();
            break;

          case Section1dStandardProfileType.CircularHollow:
            record.ProfileDetails = new ProfileDetailsCircularHollow();
            break;

          case Section1dStandardProfileType.Taper:
            record.ProfileDetails = new ProfileDetailsTaper();
            break;

          case Section1dStandardProfileType.Ellipse:
            record.ProfileDetails = new ProfileDetailsEllipse();
            break;

          case Section1dStandardProfileType.GeneralI:
            record.ProfileDetails = new ProfileDetailsGeneralI();
            break;

          case Section1dStandardProfileType.TaperT:
          case Section1dStandardProfileType.TaperAngle:
            record.ProfileDetails = new ProfileDetailsTaperTAngle();
            break;

          case Section1dStandardProfileType.RectoEllipse:
            record.ProfileDetails = new ProfileDetailsRectoEllipse();
            break;

          case Section1dStandardProfileType.TaperI:
            record.ProfileDetails = new ProfileDetailsTaperI();
            break;

          case Section1dStandardProfileType.SecantPile:
          case Section1dStandardProfileType.SecantPileWall:
            record.ProfileDetails = new ProfileDetailsSecant();
            break;

          case Section1dStandardProfileType.Oval:
            record.ProfileDetails = new ProfileDetailsOval();
            break;

          case Section1dStandardProfileType.GenericZ:
            record.ProfileDetails = new ProfileDetailsZ();
            break;

          case Section1dStandardProfileType.Castellated:
          case Section1dStandardProfileType.Cellular:
            record.ProfileDetails = new ProfileDetailsCastellatedCellular();
            break;

          case Section1dStandardProfileType.AsymmetricCellular:
            record.ProfileDetails = new ProfileDetailsAsymmetricCellular();
            break;

          case Section1dStandardProfileType.SheetPile:
            record.ProfileDetails = new ProfileDetailsSheetPile();
            break;

          default:
            record.ProfileDetails = new ProfileDetailsTwoThickness();
            break;
        }
        record.ProfileDetails.FromDesc(v);
        record.ProfileGroup = Section1dProfileGroup.Standard;
      }
      return true;
    }
    #endregion
  }

}