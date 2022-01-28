using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Geometry;
using Tekla.Structures.Model;
using System.Linq;
using Objects.Geometry;
using System.Collections;
using Tekla.Structures.Solid;
using Tekla.Structures.Catalogs;
using StructuralUtilities.PolygonMesher;
using Speckle.Core.Models;
using Objects.Structural.Properties.Profiles;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public Mesh GetMeshFromSolid(Solid solid){
      List<double> MyList = new List<double> { };
      ArrayList MyFaceNormalList = new ArrayList();
      List<int> facesList = new List<int> { };

      FaceEnumerator MyFaceEnum = solid.GetFaceEnumerator();

      var counter = 0;
      while (MyFaceEnum.MoveNext())
      {
        var mesher = new PolygonMesher();

        Face MyFace = MyFaceEnum.Current as Face;
        if (MyFace != null)
        {
          List<double> TempList = new List<double> { };
          LoopEnumerator MyLoopEnum = MyFace.GetLoopEnumerator();
          while (MyLoopEnum.MoveNext())
          {
            Loop MyLoop = MyLoopEnum.Current as Loop;
            if (MyLoop != null)
            {

              VertexEnumerator MyVertexEnum = MyLoop.GetVertexEnumerator() as VertexEnumerator;
              while (MyVertexEnum.MoveNext())
              {

                Tekla.Structures.Geometry3d.Point MyVertex = MyVertexEnum.Current as Tekla.Structures.Geometry3d.Point;
                if (MyVertex != null)
                {
                  TempList.Add(MyVertex.X);
                  TempList.Add(MyVertex.Y);
                  TempList.Add(MyVertex.Z);

                }


                //speckleBeam.displayMesh = beam.Profile.
              }
              counter++;
            }
          }

          mesher.Init(TempList);
          var faces = mesher.Faces();
          var vertices = mesher.Coordinates;
          var verticesList = vertices.ToList();
          MyList.AddRange(verticesList);
          var largestVertixCount = 0;
          if (facesList.Count == 0)
          {
            largestVertixCount = 0;
          }
          else
          {
            largestVertixCount = facesList.Max() + 1;
          }
          for (int i = 0; i < faces.Length; i++)
          {
            if (i % 4 == 0)
            {
              continue;
            }
            else
            {
              faces[i] += largestVertixCount;
            }
          }
          facesList.AddRange(faces.ToList());
        }
      }
      return new Mesh(MyList, facesList);

    }
        /// <summary>
        /// Get all user properties defined for this object in Tekla
        /// </summary>
        /// <param name="speckleElement"></param>
        /// <param name="teklaObject"></param>
        /// <param name="exclusions">List of BuiltInParameters or GUIDs used to indicate what parameters NOT to get,
        /// we exclude all params already defined on the top level object to avoid duplication and 
        /// potential conflicts when setting them back on the element</param>
        public void GetAllUserProperties(Base speckleElement, ModelObject teklaObject, List<string> exclusions = null)
        {
            Hashtable propertyHashtable = new Hashtable();
            teklaObject.GetAllUserProperties(ref propertyHashtable);

            // sort by key
            var sortedproperties = propertyHashtable.Cast<DictionaryEntry>().OrderBy(x => x.Key).ToDictionary(d => (string)d.Key, d => d.Value);
            
            Base paramBase = new Base();
            foreach (var kv in sortedproperties)
            {
                try
                {
                    paramBase[kv.Key] = kv.Value;
                }
                catch
                {
                    //ignore
                }
            }

            if (paramBase.GetDynamicMembers().Any())
                speckleElement["parameters"] = paramBase;
            //speckleElement["elementId"] = revitElement.Id.ToString();
            speckleElement.applicationId = teklaObject.Identifier.GUID.ToString();
            //speckleElement["units"] = ModelUnits;
        }

        public Structural.Properties.Profiles.SectionProfile GetProfile(string teklaProfileString)
        {
            SectionProfile profile = null;
            ProfileItem profileItem = null;

            LibraryProfileItem libItem = new LibraryProfileItem();
            ParametricProfileItem paramItem = new ParametricProfileItem();
            if (libItem.Select(teklaProfileString))
            {
                profileItem = libItem;
            }
            else if (paramItem.Select(teklaProfileString))
            {
                profileItem = paramItem;
            }

            if (profileItem != null)
            {              
                switch (profileItem.ProfileItemType)
                {
                    case ProfileItem.ProfileItemTypeEnum.PROFILE_I:
                        profile = GetIProfile(profileItem);
                        break;
                    case ProfileItem.ProfileItemTypeEnum.PROFILE_L:
                        profile = GetAngleProfile(profileItem);
                        break;
                    case ProfileItem.ProfileItemTypeEnum.PROFILE_PL:
                        profile = GetRectangularProfile(profileItem);
                        break;
                    case ProfileItem.ProfileItemTypeEnum.PROFILE_U:
                        profile = GetChannelProfile(profileItem);
                        break;
                    default:
                        profile = new SectionProfile();
                        break;
                }
            }
            return profile;
        }

        #region Profile type getters
        private Structural.Properties.Profiles.ISection GetIProfile(ProfileItem profileItem)
        {
            // Set profile name depending on type
            var name = profileItem is LibraryProfileItem ? ((LibraryProfileItem)profileItem).ProfileName : ((ParametricProfileItem)profileItem).CreateProfileString();
            
            // Get properties from catalog
            var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();

            var depth = properties.FirstOrDefault(p => p.Property == "HEIGHT")?.Value;
            var width = properties.FirstOrDefault(p => p.Property == "WIDTH")?.Value;
            var tf = properties.FirstOrDefault(p => p.Property == "FLANGE_THICKNESS")?.Value;
            var tw = properties.FirstOrDefault(p => p.Property == "WEB_THICKNESS")?.Value;

            var speckleProfile = new ISection(name, depth.GetValueOrDefault(), width.GetValueOrDefault(), tw.GetValueOrDefault(), tf.GetValueOrDefault());
            return speckleProfile;
        }
        private Structural.Properties.Profiles.Angle GetAngleProfile(ProfileItem profileItem)
        {
            var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();
            string name = profileItem is LibraryProfileItem ? ((LibraryProfileItem)profileItem).ProfileName : ((ParametricProfileItem)profileItem).CreateProfileString();

            var depth = properties.FirstOrDefault(p => p.Property == "HEIGHT")?.Value;
            var width = properties.FirstOrDefault(p => p.Property == "WIDTH")?.Value;
            var tf1 = properties.FirstOrDefault(p => p.Property == "FLANGE_THICKNESS_1")?.Value;
            var tf2 = properties.FirstOrDefault(p => p.Property == "FLANGE_THICKNESS_2")?.Value;

            var speckleProfile = new Structural.Properties.Profiles.Angle(name, depth.GetValueOrDefault(), width.GetValueOrDefault(), tf1.GetValueOrDefault(), tf2.GetValueOrDefault());

            return speckleProfile;
        }

        private Structural.Properties.Profiles.Rectangular GetRectangularProfile(ProfileItem profileItem)
        {
            var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();
            string name = profileItem is LibraryProfileItem ? ((LibraryProfileItem)profileItem).ProfileName : ((ParametricProfileItem)profileItem).CreateProfileString();

            var depth = properties.FirstOrDefault(p => p.Property == "HEIGHT")?.Value;
            var width = properties.FirstOrDefault(p => p.Property == "WIDTH")?.Value;
            var speckleProfile = new Structural.Properties.Profiles.Rectangular(name, depth.GetValueOrDefault(), width.GetValueOrDefault());

            return speckleProfile;
        } 
        private Structural.Properties.Profiles.Channel GetChannelProfile(ProfileItem profileItem)
        {
            var properties = profileItem.aProfileItemParameters.Cast<ProfileItemParameter>().ToList();
            string name = profileItem is LibraryProfileItem ? ((LibraryProfileItem)profileItem).ProfileName : ((ParametricProfileItem)profileItem).CreateProfileString();

            var depth = properties.FirstOrDefault(p => p.Property == "HEIGHT")?.Value;
            var width = properties.FirstOrDefault(p => p.Property == "WIDTH")?.Value;
            var tf = properties.FirstOrDefault(p => p.Property == "FLANGE_THICKNESS")?.Value;
            var tw = properties.FirstOrDefault(p => p.Property == "WEB_THICKNESS")?.Value;

            var speckleProfile = new Channel(name, depth.GetValueOrDefault(), width.GetValueOrDefault(), tw.GetValueOrDefault(), tf.GetValueOrDefault());
            return speckleProfile;
        }
        #endregion

        public Structural.Materials.Material GetMaterial(string teklaMaterialString)
        {
            Structural.Materials.Material speckleMaterial = null;

            MaterialItem materialItem = new MaterialItem();
            if (materialItem.Select(teklaMaterialString))
            {
                switch (materialItem.Type)
                {
                    case MaterialItem.MaterialItemTypeEnum.MATERIAL_STEEL:
                        speckleMaterial = new Structural.Materials.Steel();
                        break;
                    case MaterialItem.MaterialItemTypeEnum.MATERIAL_CONCRETE:
                        speckleMaterial = new Structural.Materials.Concrete();
                        break;
                    default:
                        speckleMaterial = new Structural.Materials.Material();
                        break;
                }
                speckleMaterial.name = materialItem.MaterialName;
                speckleMaterial.density = materialItem.ProfileDensity;
                speckleMaterial.elasticModulus = materialItem.ModulusOfElasticity;
                speckleMaterial.poissonsRatio = materialItem.PoissonsRatio;
                speckleMaterial.grade = materialItem.MaterialName;
                speckleMaterial.thermalExpansivity = materialItem.ThermalDilatation;
            }
            return speckleMaterial;

        }
        //public static bool IsElementSupported(this ModelObject e)
        //{

        //  if (SupportedBuiltInCategories.Contains(e)
        //    return true;
        //  return false;
        //}

        ////list of currently supported Categories (for sending only)
        ////exact copy of the one in the Speckle.ConnectorRevit.ConnectorRevitUtils
        ////until issue https://github.com/specklesystems/speckle-sharp/issues/392 is resolved
        //private static List<ModelObject.ModelObjectEnum> SupportedBuiltInCategories = new List<ModelObject.ModelObjectEnum>{

        //ModelObject.ModelObjectEnum.BEAM,

    }
}
