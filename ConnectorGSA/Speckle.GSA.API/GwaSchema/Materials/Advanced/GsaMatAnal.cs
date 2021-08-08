using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.MAT_ANAL, GwaSetCommandType.Set, false, false, true)]
  public class GsaMatAnal : GsaRecord
  {
    //The current documentation doesn't align with the GSA 10.1 keyword example. 
    //A bug ticket has been placed with the GSA developers. This class will need to be updated once the documentation is up to date.
    //
    //In most cases, 2 undocuemnted parameters are included in the current GSA 10.1 keywords. These are ignored as they are zero. 
    //When creating a gwa string, 2 additional zeros are added at the end corresponding to the 2 undocuemented parameters.

    public MatAnalType Type;
    public string Name { get => name; set { name = value; } }  //If not embedded
    public Colour Colour = Colour.NO_RGB; //If not embedded
    public int? NumParams;
    #region MAT_ELAS_ISO
    public double? E;
    public double? Nu;
    public double? Rho;
    public double? Alpha;
    public double? G;
    public double? Damp;
    #endregion
    #region MAT_ELAS_ORTHO
    public double? Yield;
    public double? Ultimate;
    public double? Eh;
    public double? Beta;
    #endregion
    #region MAT_DRUCKER_PRAGER
    public double? Cohesion;
    public double? Phi;
    public double? Psi;
    public double? Scribe;
    #endregion
    #region ELAS_ORTHO
    public double? Ex;
    public double? Ey;
    public double? Ez;
    public double? Nuxy;
    public double? Nuyz;
    public double? Nuzx;
    public double? Alphax;
    public double? Alphay;
    public double? Alphaz;
    public double? Gxy;
    public double? Gyz;
    public double? Gzx;
    #endregion
    #region FABRIC
    public double? Comp;
    #endregion
    #region MAT_MOHR_COULOMB
    #endregion

    private bool Embedded = true;

    public GsaMatAnal() : base()
    {
      //Defaults
      Version = 1;
    }

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
        //Not embedded - MAT_ANAL | Index | Type | Name | Colour | NumParams | etc...
        Index = foundIndex;
        remainingItems = remainingItems.Skip(1).ToList();
      }
      else
      {
        //Embedded - MAT_ANAL | Name | Index | Type | NumParams | etc...
        if (!FromGwaByFuncs(remainingItems, out remainingItems, AddName)) return false;
        if (int.TryParse(remainingItems[0], out foundIndex))
        {
          Index = foundIndex; // will be negative
          remainingItems = remainingItems.Skip(1).ToList();
        }
      }

      //Process common items
      //When embedded, name and colour are omitted
      if (!FromGwaByFuncs(remainingItems, out remainingItems, (v) => Enum.TryParse<MatAnalType>(v, true, out Type)))  return false;
      if (Index > 0)
      {
        //Not embedded
        Embedded = false;
        if (!FromGwaByFuncs(remainingItems, out remainingItems, AddName, (v) => AddColour(v, out Colour))) return false;
      }
      if (!FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableIntValue(v, out NumParams))) return false;

      //Process material specific items
      switch (Type)
      {
        case MatAnalType.MAT_ELAS_ISO:
          //MAT_ANAL | num | MAT_ELAS_ISO | name | colour | 6 | E | nu | rho | alpha | G | damp |
          //2 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out E), (v) => AddNullableDoubleValue(v, out Nu), 
            (v) => AddNullableDoubleValue(v, out Rho), (v) => AddNullableDoubleValue(v, out Alpha), (v) => AddNullableDoubleValue(v, out G), 
            (v) => AddNullableDoubleValue(v, out Damp), null, null);
        case MatAnalType.MAT_DRUCKER_PRAGER:
          //MAT_ANAL | num | MAT_DRUCKER_PRAGER | name | colour | 10 | G | nu | rho | cohesion | phi | psi | Eh | scribe | alpha | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out G), (v) => AddNullableDoubleValue(v, out Nu),
            (v) => AddNullableDoubleValue(v, out Rho), (v) => AddNullableDoubleValue(v, out Cohesion), (v) => AddNullableDoubleValue(v, out Phi),
            (v) => AddNullableDoubleValue(v, out Psi), (v) => AddNullableDoubleValue(v, out Eh), (v) => AddNullableDoubleValue(v, out Scribe),
            (v) => AddNullableDoubleValue(v, out Alpha), (v) => AddNullableDoubleValue(v, out Damp), null, null);
        case MatAnalType.MAT_ELAS_ORTHO:
          //MAT_ANAL | num | MAT_ELAS_ORTHO | name | colour | 14 | Ex | Ey | Ez | nuxy | nuyz | nuzx | rho | alphax | alphay | alphaz | Gxy | Gyz | Gzx | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out Ex), (v) => AddNullableDoubleValue(v, out Ey),
            (v) => AddNullableDoubleValue(v, out Ez), (v) => AddNullableDoubleValue(v, out Nuxy), (v) => AddNullableDoubleValue(v, out Nuyz),
            (v) => AddNullableDoubleValue(v, out Nuzx), (v) => AddNullableDoubleValue(v, out Rho), (v) => AddNullableDoubleValue(v, out Alphax),
            (v) => AddNullableDoubleValue(v, out Alphay), (v) => AddNullableDoubleValue(v, out Alphaz), (v) => AddNullableDoubleValue(v, out Gxy),
            (v) => AddNullableDoubleValue(v, out Gyz), (v) => AddNullableDoubleValue(v, out Gzx), (v) => AddNullableDoubleValue(v, out Damp), null, null);
        case MatAnalType.MAT_ELAS_PLAS_ISO:
          //MAT_ANAL | num | MAT_ELAS_PLAS_ISO | name | colour | 9 | E | nu | rho | alpha | yield | ultimate | Eh | beta | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems,  out remainingItems, (v) => AddNullableDoubleValue(v, out E), (v) => AddNullableDoubleValue(v, out Nu),
            (v) => AddNullableDoubleValue(v, out Rho), (v) => AddNullableDoubleValue(v, out Alpha), (v) => AddNullableDoubleValue(v, out Yield),
            (v) => AddNullableDoubleValue(v, out Ultimate), (v) => AddNullableDoubleValue(v, out Eh), (v) => AddNullableDoubleValue(v, out Beta),
            (v) => AddNullableDoubleValue(v, out Damp), null, null);
        case MatAnalType.MAT_FABRIC:
          //MAT_ANAL | num | MAT_FABRIC | name | colour | 4 | Ex | Ey | nu | G | 1 | comp
          //"MAT_ANAL.1\t6\tMAT_FABRIC\tMaterial 6\tNO_RGB\t5\t800000\t400000\t0.45\t30000\t0\t1\t\t0"
          //3 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out Ex), (v) => AddNullableDoubleValue(v, out Ey),
            (v) => AddNullableDoubleValue(v, out Nu), (v) => AddNullableDoubleValue(v, out G), null, (v) => AddNullableDoubleValue(v, out Comp), null, null);
        case MatAnalType.MAT_MOHR_COULOMB:
          //MAT_ANAL | num | MAT_MOHR_COULOMB | name | colour | 9 | G | nu | rho | cohesion | phi | psi | Eh | alpha | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out G), (v) => AddNullableDoubleValue(v, out Nu),
            (v) => AddNullableDoubleValue(v, out Rho), (v) => AddNullableDoubleValue(v, out Cohesion), (v) => AddNullableDoubleValue(v, out Phi),
            (v) => AddNullableDoubleValue(v, out Psi), (v) => AddNullableDoubleValue(v, out Eh), (v) => AddNullableDoubleValue(v, out Alpha),
            (v) => AddNullableDoubleValue(v, out Damp), null, null);
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
        //Embedded - MAT_ANAL | Name | Index | Type | NumParams | etc...
        items.Insert(items.Count - 1, Name);
        AddItems(ref items, Type.ToString(), NumParams); 
      }
      else
      {
        //Not embedded - MAT_ANAL | Index | Type | Name | Colour | NumParams | etc...
        AddItems(ref items, Type.ToString(), Name, Colour.ToString(), NumParams);
      }

      //Process material specific items
      switch (Type)
      {
        case MatAnalType.MAT_ELAS_ISO:
          //MAT_ANAL | num | MAT_ELAS_ISO | name | colour | 6 | E | nu | rho | alpha | G | damp |
          //2 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, E, Nu, Rho, Alpha, G, Damp, 0, 0);
          break;
        case MatAnalType.MAT_DRUCKER_PRAGER:
          //MAT_ANAL | num | MAT_DRUCKER_PRAGER | name | colour | 10 | G | nu | rho | cohesion | phi | psi | Eh | scribe | alpha | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, G, Nu, Rho, Cohesion, Phi, Psi, Eh, Scribe, Alpha, Damp, 0, 0);
          break;
        case MatAnalType.MAT_ELAS_ORTHO:
          //MAT_ANAL | num | MAT_ELAS_ORTHO | name | colour | 14 | Ex | Ey | Ez | nuxy | nuyz | nuzx | rho | alphax | alphay | alphaz | Gxy | Gyz | Gzx | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, Ex, Ey, Ez, Nuxy, Nuyz, Nuzx, Rho, Alphax, Alphay, Alphaz, Gxy, Gyz, Gzx, Damp, 0, 0);
          break;
        case MatAnalType.MAT_ELAS_PLAS_ISO:
          //MAT_ANAL | num | MAT_ELAS_PLAS_ISO | name | colour | 9 | E | nu | rho | alpha | yield | ultimate | Eh | beta | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, E, Nu, Rho, Alpha, Yield, Ultimate, Eh, Beta, Damp, 0, 0);
          break;
        case MatAnalType.MAT_FABRIC:
          //MAT_ANAL | num | MAT_FABRIC | name | colour | 4 | Ex | Ey | nu | G | 1 | comp
          //3 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, Ex, Ey, Nu, G, 0, 1, Comp, 0, 0);
          break;
        case MatAnalType.MAT_MOHR_COULOMB:
          //MAT_ANAL | num | MAT_MOHR_COULOMB | name | colour | 9 | G | nu | rho | cohesion | phi | psi | Eh | alpha | damp
          //2 additional items are undocumented based on GSA 10.1 gwas
          AddItems(ref items, G, Nu, Rho, Cohesion, Phi, Psi, Eh, Alpha, Damp, 0, 0);
          break;
        default:
          break;
      }

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }
  }
}
