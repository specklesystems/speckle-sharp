using System.Collections.Generic;
using System.Linq;

namespace Speckle.GSA.API.GwaSchema
{
  //The term "section component" here is a name applied to both the group as a whole as well as one member of the group, 
  //but the latter is shortened to SectionComp to distinguish them here
  public class SectionComp : GsaSectionComponentBase
  {
    public string Name { get => name; set { name = value; } }
    //The GWA specifies ref (i.e. record index) and name, but when a SECTION_COMP is inside a SECTION command, 
    //the ref is absent and name is blank (empty string) - so they'll be left out here
    public int? MatAnalIndex;
    public Section1dMaterialType MaterialType;
    public int? MaterialIndex;
    public double? OffsetY;
    public double? OffsetZ;
    public double? Rotation;
    public ComponentReflection Reflect;
    public int? Pool;
    //These are mentioned in the docs but only in the parameter glossary, not in the syntax of SECTION_COMP
    public Section1dTaperType TaperType;
    public double? TaperPos;

    public Section1dProfileGroup ProfileGroup;
    public ProfileDetails ProfileDetails;

    public SectionComp() : base()
    {
      Version = 4;
    }
  }

  #region profile_details
  public abstract class ProfileDetails
  {
    public abstract string ToDesc();
    public abstract bool FromDesc(string desc);
    public Section1dProfileGroup Group;

    protected List<string> Split(string v)
    {
      try
      {
        return v.ListSplit(' ').ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    protected double? GetValue(List<double?> values, int index)
    {
      return (index < values.Count() ? values[index] : 0);
    }

    protected bool SetValue(ref List<double?> values, int index, double? value)
    {
      if (values == null)
      {
        values = new List<double?>();
      }
      if (values.Count() == 0)
      { 
        for (var i = 0; i < index; i++)
        {
          values.Add(null);
        }
        values.Add(value);
        return true;
      }
      try
      {
        var lastExistingIndex = values.Count - 1;
        if (index > lastExistingIndex)
        {
          if ((index - lastExistingIndex) > 1)
          {
            //Pad out the list with null values until the requested index
            for (var i = (lastExistingIndex + 1); i < index; i++)
            {
              values.Add(null);
            }
            lastExistingIndex = index - 1;
          }
          if (index <= lastExistingIndex)
          {
            values[index] = value;
          }
          else
          {
            values.Add(value);
          }
        }
      } catch
      {
        return false;
      }
      return true;
    }
  }

  public class ProfileDetailsCatalogue : ProfileDetails
  {
    public string Profile;
    public ProfileDetailsCatalogue()
    {
      Group = Section1dProfileGroup.Catalogue;
    }

    public override bool FromDesc(string desc)
    {
      //Example: desc = CAT A-UB 610UB125 19981201
      Profile = desc;
      return true;
    }

    public override string ToDesc()
    {
      return Profile;
    }
  }

  public class ProfileDetailsPerimeter : ProfileDetails
  {
    public string Type;
    public List<string> Actions;
    public List<double?> Y;
    public List<double?> Z;
    public ProfileDetailsPerimeter()
    {
      Group = Section1dProfileGroup.Perimeter;
    }

    public override bool FromDesc(string desc)
    {
      //Examples: 
      //
      // Perimeter
      //    desc = GEO P M(-50|-50) L(50|-50) L(50|50) L(-50|50) M(-40|-40) L(40|-40) L(40|40) L(-40|40)
      //
      // Line Segment
      //    desc = GEO L(mm) T(5) M(0|0) L(100|0) L(100|100) L(0|100) L(0|0)
      var items = Split(desc);
      Actions = new List<string>();
      Y = new List<double?>();
      Z = new List<double?>();
      Type = items[1];

      for (var i = 2; i < items.Count(); i++)
      {
        Actions.Add(items[i].Split('(')[0]);
        if (Actions.Last() == "T")
        {
          Y.Add(items[i].Split('(')[1].Split(')')[0].ToDouble());
          Z.Add(null);
        }
        else
        {
          Y.Add(items[i].Split('(')[1].Split('|')[0].ToDouble());
          Z.Add(items[i].Split('|')[1].Split(')')[0].ToDouble());
        }
      }

      return true;
    }

    public override string ToDesc()
    {
      var v = "GEO " + Type;
      
      for (var i = 0; i < Actions.Count(); i++)
      {
        v += " " + Actions[i] + "(" + Y[i].ToString();
        if (Actions[i] != "T") v += "|" + Z[i].ToString();
        v += ")";
      }

      return v;
    }
  }

  public class ProfileDetailsExplicit : ProfileDetails
  {
    public double? Area { get => GetValue(values, 0); set => SetValue(ref values, 0, value); }
    public double? Iyy {  get => GetValue(values, 1); set => SetValue(ref values, 1, value); }
    public double? Izz { get => GetValue(values, 2); set => SetValue(ref values, 2, value); }
    public double? J { get => GetValue(values, 3); set => SetValue(ref values, 3, value); }
    public double? Ky { get => GetValue(values, 4); set => SetValue(ref values, 4, value); }
    public double? Kz { get => GetValue(values, 5); set => SetValue(ref values, 5, value); }

    private List<double?> values = new List<double?>();

    public ProfileDetailsExplicit()
    {
      Group = Section1dProfileGroup.Explicit;
    }

    public override bool FromDesc(string desc)
    {
      var items = Split(desc);

      //Assume first is the EXP string
      items = items.Skip(1).ToList();
      for (var i = 0; i < items.Count(); i++)
      {
        values.Add((double.TryParse(items[i], out var dVal)) ? (double?)dVal : 0);
      }
      return true;
    }

    public override string ToDesc()
    {
      var strItems = new List<string>() { Group.GetStringValue() };
      
      strItems.AddRange(values.Select(v => (v.HasValue ? v : 0).ToString()));
      return string.Join(" ", strItems);
    }
  }

  public abstract class ProfileDetailsStandard : ProfileDetails
  {
    public Section1dStandardProfileType ProfileType;

    protected List<double?> values = new List<double?>();

    protected ProfileDetailsStandard()
    {
      Group = Section1dProfileGroup.Standard;
    }

    //Should this be replaced with individual setters for each in the child classes?
    public void SetValues(List<double?> values)
    {
      this.values = values;
    }

    public void SetValues(params double?[] values)
    {
      this.values = values.ToList();
    }

    public override bool FromDesc(string desc)
    {
      var items = Split(desc);

      //Assume first is the STD string and second is the type
      if (!items[1].TryParseStringValue(out ProfileType))
      {
        return false;
      }
      items = items.Skip(2).ToList();
      for (var i = 0; i < items.Count(); i++)
      {
        values.Add((double.TryParse(items[i], out var dVal)) ? (double?)dVal : 0);
      }
      return true;
    }

    public override string ToDesc()
    {
      var strItems = new List<string>() { Group.GetStringValue(), ProfileType.GetStringValue() };
      strItems.AddRange(values.Select(v => (v.HasValue ? v : 0).ToString()));
      return string.Join(" ", strItems);
    }
  }

  public class ProfileDetailsRectangular : ProfileDetailsStandard
  {
    public double? d => GetValue(values, 0);
    public double? b => GetValue(values, 1);
  }

  public class ProfileDetailsTwoThickness : ProfileDetailsStandard 
  {
    public double? d => GetValue(values, 0);
    public double? b => GetValue(values, 1);
    public double? tw => GetValue(values, 2);
    public double? tf => GetValue(values, 3);
  }

  public class ProfileDetailsCircular : ProfileDetailsStandard 
  {
    public double? d => GetValue(values, 0);
  }

  public class ProfileDetailsCircularHollow : ProfileDetailsStandard 
  {
    public double? d => GetValue(values, 0);
    public double? t => GetValue(values, 1); 
  }
  public class ProfileDetailsTaper : ProfileDetailsStandard
  {
    public double? d => GetValue(values, 0);
    public double? bt => GetValue(values, 1); 
    public double? bb => GetValue(values, 2); 
  }
  
  public class ProfileDetailsEllipse : ProfileDetailsStandard 
  {
    public double? d => GetValue(values, 0);
    public double? b => GetValue(values, 1); 
    public double? k => GetValue(values, 2); 
  }
  
  public class ProfileDetailsGeneralI : ProfileDetailsStandard 
  {
    public double? d => GetValue(values, 0);
    public double? bt => GetValue(values, 1); 
    public double? bb => GetValue(values, 2); 
    public double? tw => GetValue(values, 3); 
    public double? tft => GetValue(values, 4); 
    public double? tfb => GetValue(values, 5); 
  }
  
  public class ProfileDetailsTaperTAngle : ProfileDetailsStandard 
  {
    public double? d => GetValue(values, 0);
    public double? b => GetValue(values, 1); 
    public double? twt => GetValue(values, 2); 
    public double? twb => GetValue(values, 3); 
    public double? tf => GetValue(values, 4); 
  }
  
  public class ProfileDetailsRectoEllipse : ProfileDetailsStandard 
  {
    public double? d => GetValue(values, 0);
    public double? b => GetValue(values, 1); 
    public double? df => GetValue(values, 2); 
    public double? bf => GetValue(values, 3); 
    public double? k => GetValue(values, 4);    
  }
  
  public class ProfileDetailsTaperI : ProfileDetailsStandard 
  {
    public double? d => GetValue(values, 0);
    public double? bt => GetValue(values, 1); 
    public double? bb => GetValue(values, 2); 
    public double? twt => GetValue(values, 3); 
    public double? twb => GetValue(values, 4); 
    public double? tft => GetValue(values, 5); 
    public double? tfb => GetValue(values, 6); 
  }
  
  public class ProfileDetailsSecant : ProfileDetailsStandard 
  {
    public double? d => GetValue(values, 0);
    public double? c => GetValue(values, 1); 
    public double? n => GetValue(values, 2); 
  }

  public class ProfileDetailsOval : ProfileDetailsStandard 
  {
    public double? d => GetValue(values, 0);
    public double? b => GetValue(values, 1); 
    public double? t => GetValue(values, 2); 
  }

  public class ProfileDetailsZ: ProfileDetailsStandard
  {
    public double? d => GetValue(values, 0);
    public double? bt => GetValue(values, 1);
    public double? bb => GetValue(values, 2);
    public double? dt => GetValue(values, 3);
    public double? db => GetValue(values, 4);
    public double? t => GetValue(values, 5);
  }

  public class ProfileDetailsC: ProfileDetailsStandard
  {
    public double? d => GetValue(values, 0);
    public double? b => GetValue(values, 1);
    public double? dt => GetValue(values, 2);
    public double? t => GetValue(values, 3);
  }

  public class ProfileDetailsCastellatedCellular : ProfileDetailsStandard
  {
    public double? d => GetValue(values, 0);
    public double? b => GetValue(values, 1);
    public double? tw => GetValue(values, 2);
    public double? tf => GetValue(values, 3);
    public double? ds => GetValue(values, 4);
    public double? p => GetValue(values, 5);
  }

  public class ProfileDetailsAsymmetricCellular : ProfileDetailsStandard
  {
    public double? dt => GetValue(values, 0);
    public double? bt => GetValue(values, 1);
    public double? twt=> GetValue(values, 2);
    public double? tft => GetValue(values, 3);
    public double? db => GetValue(values, 4);
    public double? bb => GetValue(values, 5);
    public double? twb => GetValue(values, 6);
    public double? tfb => GetValue(values, 7);
    public double? ds => GetValue(values, 8);
    public double? p => GetValue(values, 9);
  }

  public class ProfileDetailsSheetPile: ProfileDetailsStandard
  {
    public double? d => GetValue(values, 0);
    public double? b => GetValue(values, 1);
    public double? bt => GetValue(values, 2);
    public double? bb => GetValue(values, 3);
    public double? tf => GetValue(values, 4);
    public double? tw => GetValue(values, 5);
  }
  #endregion
}
