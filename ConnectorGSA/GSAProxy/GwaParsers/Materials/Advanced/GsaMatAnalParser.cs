using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.MAT_ANAL, GwaSetCommandType.Set, false, false, true)]
  public class GsaMatAnalParser : GwaParser<GsaMatAnal>
  {
    public GsaMatAnalParser(GsaMatAnal gsaMatAnal) : base(gsaMatAnal)  {  }

    public GsaMatAnalParser() : base(new GsaMatAnal()) { }

    private bool Embedded = true;

    public override bool FromGwa(string gwa)
    {
      return FromGwa(gwa, out var _);
    }

    public bool FromGwa(string gwa, out List<string> remainingItems)
    {
      //Process the first part of gwa string
      remainingItems = Split(gwa);
      if (remainingItems[0].StartsWith("set", StringComparison.OrdinalIgnoreCase))
      {
        remainingItems.Remove(remainingItems[0]);
      }
      if (!ParseKeywordVersionSid(remainingItems[0]))
      {
        return false;
      }
      remainingItems = remainingItems.Skip(1).ToList();

      //Detect presence or absense of num (record index) argument based on number of items
      if (int.TryParse(remainingItems[0], out var foundIndex))
      {
        //Not embedded - MAT_ANAL | record.Index | Type | Name | Colour | NumParams | etc...
        record.Index = foundIndex;
        remainingItems = remainingItems.Skip(1).ToList();
      }
      else
      {
        //Embedded - MAT_ANAL | Name | Index | Type | NumParams | etc...
        if (!FromGwaByFuncs(remainingItems, out remainingItems, AddName)) return false;
        if (int.TryParse(remainingItems[0], out foundIndex))
        {
          record.Index = foundIndex; // will be negative
          remainingItems = remainingItems.Skip(1).ToList();
        }
      }

      //Process common items
      //When embedded, name and colour are omitted
      if (!FromGwaByFuncs(remainingItems, out remainingItems, (v) => Enum.TryParse<MatAnalType>(v, true, out record.Type))) return false;
      if (record.Index > 0)
      {
        //Not embedded
        Embedded = false;
        if (!FromGwaByFuncs(remainingItems, out remainingItems, AddName, (v) => AddColour(v, out record.Colour))) return false;
      }
      if (!FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableIntValue(v, out record.NumParams))) return false;

      //Process material specific items
      switch (record.Type)
      {
        case MatAnalType.MAT_ELAS_ISO:
          //MAT_ANAL | num | MAT_ELAS_ISO | name | colour | 6 | E | nu | rho | alpha | G | damp |
          //2 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out record.E), (v) => AddNullableDoubleValue(v, out record.Nu),
            (v) => AddNullableDoubleValue(v, out record.Rho), (v) => AddNullableDoubleValue(v, out record.Alpha), (v) => AddNullableDoubleValue(v, out record.G),
            (v) => AddNullableDoubleValue(v, out record.Damp), null, null);
        case MatAnalType.MAT_DRUCKER_PRAGER:
          //MAT_ANAL | num | MAT_DRUCKER_PRAGER | name | colour | 10 | G | nu | rho | cohesion | phi | psi | record.Eh | scribe | alpha | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out record.G), (v) => AddNullableDoubleValue(v, out record.Nu),
            (v) => AddNullableDoubleValue(v, out record.Rho), (v) => AddNullableDoubleValue(v, out record.Cohesion), (v) => AddNullableDoubleValue(v, out record.Phi),
            (v) => AddNullableDoubleValue(v, out record.Psi), (v) => AddNullableDoubleValue(v, out record.Eh), (v) => AddNullableDoubleValue(v, out record.Scribe),
            (v) => AddNullableDoubleValue(v, out record.Alpha), (v) => AddNullableDoubleValue(v, out record.Damp), null, null);
        case MatAnalType.MAT_ELAS_ORTHO:
          //MAT_ANAL | num | MAT_ELAS_ORTHO | name | colour | 14 | record.Ex | record.Ey | record.Ez | nuxy | nuyz | nuzx | rho | alphax | alphay | alphaz | record.Gxy | record.Gyz | record.Gzx | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out record.Ex), (v) => AddNullableDoubleValue(v, out record.Ey),
            (v) => AddNullableDoubleValue(v, out record.Ez), (v) => AddNullableDoubleValue(v, out record.Nuxy), (v) => AddNullableDoubleValue(v, out record.Nuyz),
            (v) => AddNullableDoubleValue(v, out record.Nuzx), (v) => AddNullableDoubleValue(v, out record.Rho), (v) => AddNullableDoubleValue(v, out record.Alphax),
            (v) => AddNullableDoubleValue(v, out record.Alphay), (v) => AddNullableDoubleValue(v, out record.Alphaz), (v) => AddNullableDoubleValue(v, out record.Gxy),
            (v) => AddNullableDoubleValue(v, out record.Gyz), (v) => AddNullableDoubleValue(v, out record.Gzx), (v) => AddNullableDoubleValue(v, out record.Damp), null, null);
        case MatAnalType.MAT_ELAS_PLAS_ISO:
          //MAT_ANAL | num | MAT_ELAS_PLAS_ISO | name | colour | 9 | E | nu | rho | alpha | yield | ultimate | record.Eh | beta | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out record.E), (v) => AddNullableDoubleValue(v, out record.Nu),
            (v) => AddNullableDoubleValue(v, out record.Rho), (v) => AddNullableDoubleValue(v, out record.Alpha), (v) => AddNullableDoubleValue(v, out record.Yield),
            (v) => AddNullableDoubleValue(v, out record.Ultimate), (v) => AddNullableDoubleValue(v, out record.Eh), (v) => AddNullableDoubleValue(v, out record.Beta),
            (v) => AddNullableDoubleValue(v, out record.Damp), null, null);
        case MatAnalType.MAT_FABRIC:
          //MAT_ANAL | num | MAT_FABRIC | name | colour | 4 | record.Ex | record.Ey | nu | G | 1 | comp
          //"MAT_ANAL.1\t6\tMAT_FABRIC\tMaterial 6\tNO_RGB\t5\t800000\t400000\t0.45\t30000\t0\t1\t\t0"
          //3 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out record.Ex), (v) => AddNullableDoubleValue(v, out record.Ey),
            (v) => AddNullableDoubleValue(v, out record.Nu), (v) => AddNullableDoubleValue(v, out record.G), null, (v) => AddNullableDoubleValue(v, out record.Comp), null, null);
        case MatAnalType.MAT_MOHR_COULOMB:
          //MAT_ANAL | num | MAT_MOHR_COULOMB | name | colour | 9 | G | nu | rho | cohesion | phi | psi | record.Eh | alpha | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out record.G), (v) => AddNullableDoubleValue(v, out record.Nu),
            (v) => AddNullableDoubleValue(v, out record.Rho), (v) => AddNullableDoubleValue(v, out record.Cohesion), (v) => AddNullableDoubleValue(v, out record.Phi),
            (v) => AddNullableDoubleValue(v, out record.Psi), (v) => AddNullableDoubleValue(v, out record.Eh), (v) => AddNullableDoubleValue(v, out record.Alpha),
            (v) => AddNullableDoubleValue(v, out record.Damp), null, null);
        default:
          return false;
      }
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //Process common items
      if (Embedded)
      {
        //Embedded - MAT_ANAL | Name | record.Index | Type | NumParams | etc...
        items.Insert(items.Count - 1, record.Name);
        AddItems(ref items, record.Type.ToString(), record.NumParams);
      }
      else
      {
        //Not embedded - MAT_ANAL | record.Index | Type | Name | Colour | NumParams | etc...
        AddItems(ref items, record.Type.ToString(), record.Name, record.Colour.ToString(), record.NumParams);
      }

      //Process material specific items
      switch (record.Type)
      {
        case MatAnalType.MAT_ELAS_ISO:
          //MAT_ANAL | num | MAT_ELAS_ISO | name | colour | 6 | E | nu | rho | alpha | G | damp |
          //2 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, record.E, record.Nu, record.Rho, record.Alpha, record.G, record.Damp, 0, 0);
          break;
        case MatAnalType.MAT_DRUCKER_PRAGER:
          //MAT_ANAL | num | MAT_DRUCKER_PRAGER | name | colour | 10 | G | nu | rho | cohesion | phi | psi | record.Eh | scribe | alpha | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, record.G, record.Nu, record.Rho, record.Cohesion, record.Phi, record.Psi, record.Eh, record.Scribe, record.Alpha, record.Damp, 0, 0);
          break;
        case MatAnalType.MAT_ELAS_ORTHO:
          //MAT_ANAL | num | MAT_ELAS_ORTHO | name | colour | 14 | record.Ex | record.Ey | record.Ez | nuxy | nuyz | nuzx | rho | alphax | alphay | alphaz | record.Gxy | record.Gyz | record.Gzx | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, record.Ex, record.Ey, record.Ez, record.Nuxy, record.Nuyz, record.Nuzx, record.Rho, record.Alphax, record.Alphay, record.Alphaz, record.Gxy, record.Gyz, record.Gzx, record.Damp, 0, 0);
          break;
        case MatAnalType.MAT_ELAS_PLAS_ISO:
          //MAT_ANAL | num | MAT_ELAS_PLAS_ISO | name | colour | 9 | E | nu | rho | alpha | yield | ultimate | record.Eh | beta | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, record.E, record.Nu, record.Rho, record.Alpha, record.Yield, record.Ultimate, record.Eh, record.Beta, record.Damp, 0, 0);
          break;
        case MatAnalType.MAT_FABRIC:
          //MAT_ANAL | num | MAT_FABRIC | name | colour | 4 | record.Ex | record.Ey | nu | G | 1 | comp
          //3 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, record.Ex, record.Ey, record.Nu, record.G, 0, 1, record.Comp, 0, 0);
          break;
        case MatAnalType.MAT_MOHR_COULOMB:
          //MAT_ANAL | num | MAT_MOHR_COULOMB | name | colour | 9 | G | nu | rho | cohesion | phi | psi | record.Eh | alpha | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, record.G, record.Nu, record.Rho, record.Cohesion, record.Phi, record.Psi, record.Eh, record.Alpha, record.Damp, 0, 0);
          break;
        default:
          break;
      }

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }
  }
}
