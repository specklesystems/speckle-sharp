using Objects.Structural.Properties.Profiles;
using CSiAPIv1;
using System.Linq;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public SectionProfile SectionToSpeckle(string property)
  {
    var speckleSectionProfile = new SectionProfile();
    eFramePropType propType = eFramePropType.I;
    string catalogue = "";
    string matProp = "";
    string sectionPropertyName = "";

    int s = Model.PropFrame.GetNameInPropFile(
      property,
      ref sectionPropertyName,
      ref catalogue,
      ref matProp,
      ref propType
    );

    GetSectionProfile(property, matProp, propType, ref speckleSectionProfile);

    if (!string.IsNullOrEmpty(catalogue))
    {
      string[] arrayCatPath = catalogue.Split('\\');
      catalogue = arrayCatPath.Last();
      arrayCatPath = catalogue.Split('.');
      catalogue = arrayCatPath[0];

      var catalogueSectionProfile = new Catalogue(property, catalogue, propType.ToString(), sectionPropertyName);

      catalogueSectionProfile.applicationId = speckleSectionProfile.applicationId;

      return catalogueSectionProfile;
    }

    return speckleSectionProfile;

    #region deprecated
    //double T3 = 0;
    //double T2 = 0;
    //double Tf = 0;
    //double TwF = 0;
    //double Twt = 0;
    //double Tw = 0;
    //bool mirrorAbout3 = false;
    //int color = 0;
    //string notes = "";
    //string GUID = "";
    //string FileName = "";

    //s = Model.PropFrame.GetRectangle(property, ref FileName, ref matProp, ref T3, ref T2, ref color, ref notes, ref GUID);
    //if (s == 0)
    //{
    //  speckleSectionProfile = new Rectangular(property, T3, T2);
    //  GetSectionProperties(property, speckleSectionProfile);
    //  return speckleSectionProfile;
    //}

    //s = Model.PropFrame.GetConcreteTee(property, ref FileName, ref matProp, ref T3, ref T2, ref Tf, ref TwF, ref Twt, ref mirrorAbout3, ref color, ref notes, ref GUID);
    //if (s == 0)
    //{
    //  speckleSectionProfile = new Tee(property, T3, T2, TwF, Tf);
    //  GetSectionProperties(property, speckleSectionProfile);
    //  return speckleSectionProfile;
    //}

    //s = Model.PropFrame.GetCircle(property, ref FileName, ref matProp, ref T3, ref color, ref notes, ref GUID);
    //if (s == 0)
    //{
    //  speckleSectionProfile = new Circular(property, T3 / 2);
    //  GetSectionProperties(property, speckleSectionProfile);
    //  return speckleSectionProfile;
    //}

    //s = Model.PropFrame.GetAngle(property, ref FileName, ref matProp, ref T3, ref T2, ref Tf, ref Tw, ref color, ref notes, ref GUID);
    //if (s == 0)
    //{
    //  speckleSectionProfile = new Angle(property, T3, T2, Tw, Tf);
    //  GetSectionProperties(property, speckleSectionProfile);
    //  return speckleSectionProfile;
    //}

    //s = Model.PropFrame.GetChannel(property, ref FileName, ref matProp, ref T3, ref T2, ref Tf, ref Tw, ref color, ref notes, ref GUID);
    //if (s == 0)
    //{
    //  speckleSectionProfile = new Channel(property, T3, T2, Tw, Tf);
    //  GetSectionProperties(property, speckleSectionProfile);
    //  return speckleSectionProfile;
    //}

    //s = Model.PropFrame.GetTube(property, ref FileName, ref matProp, ref T3, ref T2, ref Tf, ref Tw, ref color, ref notes, ref GUID);
    //if (s == 0)
    //{
    //  speckleSectionProfile = new Rectangular(property, T3, T2, Tw, Tf);
    //  GetSectionProperties(property, speckleSectionProfile);
    //  return speckleSectionProfile;
    //}

    //s = Model.PropFrame.GetPipe(property, ref FileName, ref matProp, ref T3, ref Tw, ref color, ref notes, ref GUID);
    //if (s == 0)
    //{
    //  speckleSectionProfile = new Circular(property, T3, Tw);
    //  GetSectionProperties(property, speckleSectionProfile);
    //  return speckleSectionProfile;
    //}
    //return null;
    #endregion
  }

  public void GetSectionProfile(
    string property,
    string matProp,
    eFramePropType propType,
    ref SectionProfile speckleSectionProfile
  )
  {
    double T3 = 0;
    double T2 = 0;
    double Tf = 0;
    double TwF = 0;
    double Twt = 0;
    double Tw = 0;
    double T2b = 0;
    double Tfb = 0;
    bool mirrorAbout3 = false;
    int color = 0;
    string notes = "";
    string GUID = "";
    string FileName = "";

    switch (propType)
    {
      case eFramePropType.I:
        Model.PropFrame.GetISection(
          property,
          ref FileName,
          ref matProp,
          ref T3,
          ref T2,
          ref Tf,
          ref Tw,
          ref T2b,
          ref Tfb,
          ref color,
          ref notes,
          ref GUID
        );
        speckleSectionProfile = new ISection(property, T3, T2, Tw, Tf);
        break;
      case eFramePropType.Rectangular:
        Model.PropFrame.GetRectangle(
          property,
          ref FileName,
          ref matProp,
          ref T3,
          ref T2,
          ref color,
          ref notes,
          ref GUID
        );
        speckleSectionProfile = new Rectangular(property, T3, T2);
        break;
      case eFramePropType.ConcreteTee:
        Model.PropFrame.GetConcreteTee(
          property,
          ref FileName,
          ref matProp,
          ref T3,
          ref T2,
          ref Tf,
          ref TwF,
          ref Twt,
          ref mirrorAbout3,
          ref color,
          ref notes,
          ref GUID
        );
        speckleSectionProfile = new Tee(property, T3, T2, TwF, Tf);
        break;
      case eFramePropType.Circle:
        Model.PropFrame.GetCircle(property, ref FileName, ref matProp, ref T3, ref color, ref notes, ref GUID);
        speckleSectionProfile = new Circular(property, T3 / 2);
        break;
      case eFramePropType.Angle:
        Model.PropFrame.GetAngle(
          property,
          ref FileName,
          ref matProp,
          ref T3,
          ref T2,
          ref Tf,
          ref Tw,
          ref color,
          ref notes,
          ref GUID
        );
        speckleSectionProfile = new Angle(property, T3, T2, Tw, Tf);
        break;
      case eFramePropType.Channel:
        Model.PropFrame.GetChannel(
          property,
          ref FileName,
          ref matProp,
          ref T3,
          ref T2,
          ref Tf,
          ref Tw,
          ref color,
          ref notes,
          ref GUID
        );
        speckleSectionProfile = new Channel(property, T3, T2, Tw, Tf);
        break;
      case eFramePropType.Box:
        Model.PropFrame.GetTube(
          property,
          ref FileName,
          ref matProp,
          ref T3,
          ref T2,
          ref Tf,
          ref Tw,
          ref color,
          ref notes,
          ref GUID
        );
        speckleSectionProfile = new Rectangular(property, T3, T2, Tw, Tf);
        break;
      case eFramePropType.Pipe:
        Model.PropFrame.GetPipe(property, ref FileName, ref matProp, ref T3, ref Tw, ref color, ref notes, ref GUID);
        speckleSectionProfile = new Circular(property, T3, Tw);
        break;
      default:
        break;
    }

    GetSectionProperties(property, ref speckleSectionProfile);
    speckleSectionProfile.applicationId = GUID;
  }

  public void GetSectionProperties(string property, ref SectionProfile sectionProfile)
  {
    double Area = 0;
    double As2 = 0;
    double As3 = 0;
    double Torsion = 0;
    double I22 = 0;
    double I33 = 0;
    double S22 = 0;
    double S33 = 0;
    double Z22 = 0;
    double Z33 = 0;
    double R22 = 0;
    double R33 = 0;
    int Color = 0;
    string Notes = "";
    string GUID = "";
    // var s = Model.PropFrame.GetGeneral(property,ref FileName,ref MatProp,ref T3,ref T2,ref Area,ref As2,ref As3,ref Torsion,ref I22,ref I33,ref S22,ref S33,ref Z22,ref Z33,ref R22,ref R33,ref Color,ref Notes,ref GUID);
    // if(s == 0){
    //   sectionProfile.name = property;
    //   sectionProfile.area = Area;
    //   sectionProfile.Ky = As3 / Area;
    //   sectionProfile.Kz = As2 / Area;
    //   sectionProfile.Iyy = I33;
    //   sectionProfile.Izz = I22;
    //   sectionProfile.J = Torsion;
    //   sectionProfile.applicationId = GUID;
    // }

    var k = Model.PropFrame.GetSectProps(
      property,
      ref Area,
      ref As2,
      ref As3,
      ref Torsion,
      ref I22,
      ref I33,
      ref S22,
      ref S33,
      ref Z22,
      ref Z33,
      ref R22,
      ref R33
    );
    if (k == 0)
    {
      sectionProfile.name = property;
      sectionProfile.area = Area;
      sectionProfile.Ky = (Area == 0 || As3 == 0) ? 0 : As3 / Area;
      sectionProfile.Kz = (Area == 0 || As2 == 0) ? 0 : As2 / Area;
      sectionProfile.Iyy = I33;
      sectionProfile.Izz = I22;
      sectionProfile.J = Torsion;
    }

    //to be discussed whether radius of gyration and section mod/plastic mod is needed ~ i personally think so
  }
}
