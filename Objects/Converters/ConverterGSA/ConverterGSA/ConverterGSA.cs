using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.Properties;
using Objects.Structural;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Restraint = Objects.Structural.Geometry.Restraint;
using Objects.Structural.Materials;

namespace ConverterGSA
{
  public class ConverterGSA : ISpeckleConverter
  {
    #region ISpeckleConverter props
    public static string AppName = Applications.GSA;
    public string Description => "Default Speckle Kit for GSA";

    public string Name => nameof(ConverterGSA);

    public string Author => "Arup";

    public string WebsiteOrEmail => "https://www.oasys-software.com/";

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();
    #endregion ISpeckleConverter props

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public Dictionary<Type, Func<GsaRecord, List<Base>>> ToSpeckleFns;

    public ConverterGSA()
    {
      ToSpeckleFns = new Dictionary<Type, Func<GsaRecord, List<Base>>>()
        {
          //Geometry
          { typeof(GsaAxis), GsaAxisToSpeckle },
          { typeof(GsaNode), GsaNodeToSpeckle },
          { typeof(GsaEl), GsaElementToSpeckle },
          //Loading

          //Material
          { typeof(GsaMatSteel), GsaMaterialSteelToSpeckle },
          { typeof(GsaMatConcrete), GsaMaterialConcreteToSpeckle },
          //Property
          { typeof(GsaSection), GsaSectionToSpeckle },
          { typeof(GsaProp2d), GsaProperty2dToSpeckle },
          //{ typeof(GsaProp3d), GsaProperty3dToSpeckle }, not supported yet
          { typeof(GsaPropMass), GsaPropertyMassToSpeckle },
          { typeof(GsaPropSpr), GsaPropertySpringToSpeckle },
          //Result

          //TODO: add methods for other GSA keywords
        };
    }

    public bool CanConvertToNative(Base @object)
    {
      var t = @object.GetType();
      return (t.IsSubclassOf(typeof(GsaRecord)) && ToSpeckleFns.ContainsKey(t));
    }

    public bool CanConvertToSpeckle(object @object)
    {
      throw new NotImplementedException();
    }

    public object ConvertToNative(Base @object)
    {
      throw new NotImplementedException();
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      throw new NotImplementedException();
    }

    public Base ConvertToSpeckle(object @object)
    {
      throw new NotImplementedException();
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      var native = objects.Where(o => o.GetType().IsSubclassOf(typeof(GsaRecord)));
      if (native.Count() < objects.Count())
      {
        ConversionErrors.Add(new Exception("Non-native objects: " + (objects.Count() - native.Count())));
        objects = native.ToList();
      }
      return objects.SelectMany(x => ToSpeckle((GsaRecord)x)).ToList();
    }

    public IEnumerable<string> GetServicedApplications() => new string[] { AppName };

    public void SetContextDocument(object doc)
    {
      throw new NotImplementedException();
    }

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects)
    {
      throw new NotImplementedException();
    }

    #region ToSpeckle
    private List<Base> ToSpeckle(GsaRecord nativeObject)
    {
      var nativeType = nativeObject.GetType();
      return ToSpeckleFns[nativeType](nativeObject);
    }

    #region Geometry
    public List<Base> GsaNodeToSpeckle(GsaRecord nativeObject)
    {
      var node = GsaNodeToSpeckle((GsaNode)nativeObject);
      return new List<Base>() { node };
    }

    public GSANode GsaNodeToSpeckle(GsaNode gsaNode, string units = null)
    {
      //Node specific members
      var speckleNode = new GSANode()
      {
        name = gsaNode.Name,
        basePoint = new Point(gsaNode.X, gsaNode.Y, gsaNode.Z, units),
        constraintAxis = GetConstraintAxis(gsaNode),
        restraint = GetRestraint(gsaNode)
      };
      if (gsaNode.Index.HasValue)
      {
        var nodeKw = GsaRecord.GetKeyword<GsaNode>();
        speckleNode.applicationId = Instance.GsaModel.GetApplicationId(nodeKw, gsaNode.Index.Value);
      }

      //GSANode specific members
      speckleNode.colour = gsaNode.Colour.ToString();

      if (gsaNode.MeshSize.HasValue && gsaNode.MeshSize.Value > 0)
      {
        speckleNode.localElementSize = gsaNode.MeshSize.Value;
      }

      if (gsaNode.SpringPropertyIndex.HasValue && gsaNode.SpringPropertyIndex.Value > 0)
      {
        speckleNode.springPropertyRef = gsaNode.SpringPropertyIndex.Value.ToString();
      }      
      
      if (gsaNode.MassPropertyIndex.HasValue && gsaNode.MassPropertyIndex.Value > 0)
      {
        speckleNode.massPropertyRef = gsaNode.MassPropertyIndex.Value.ToString();
      }

      return speckleNode;
    }

    public List<Base> GsaAxisToSpeckle(GsaRecord nativeObject)
    {
      var axis = GsaAxisToSpeckle((GsaAxis)nativeObject);
      return new List<Base>() { axis };
    }

    public Axis GsaAxisToSpeckle(GsaAxis gsaAxis)
    {
      //Only supporting cartesian coordinate systems at the moment
      var speckleAxis = new Axis()
      {
        name = gsaAxis.Name,
        axisType = AxisType.Cartesian,
      };
      if (gsaAxis.Index.HasValue)
      {
        var axisKw = GsaRecord.GetKeyword<GsaAxis>();
        speckleAxis.applicationId = Instance.GsaModel.GetApplicationId(axisKw, gsaAxis.Index.Value);
      }

      if (gsaAxis.XDirX.HasValue && gsaAxis.XDirY.HasValue && gsaAxis.XDirZ.HasValue && gsaAxis.XYDirX.HasValue && gsaAxis.XYDirY.HasValue && gsaAxis.XYDirZ.HasValue)
      {
        var origin = new Point(gsaAxis.OriginX, gsaAxis.OriginY, gsaAxis.OriginZ);
        var xdir = UnitVector(new Vector(gsaAxis.XDirX.Value, gsaAxis.XDirY.Value, gsaAxis.XDirZ.Value));
        var ydir = UnitVector(new Vector(gsaAxis.XYDirX.Value, gsaAxis.XYDirY.Value, gsaAxis.XYDirZ.Value));
        var normal = UnitVector(CrossProduct(xdir, ydir));
        ydir = UnitVector(CrossProduct(normal, xdir));
        speckleAxis.definition = new Plane(origin, normal, xdir, ydir);
      }
      else
      {
        speckleAxis.definition = GlobalAxis().definition;
      }

      return speckleAxis;
    }

    public List<Base> GsaElementToSpeckle(GsaRecord nativeObject)
    {
      var element = GsaElementToSpeckle((GsaEl)nativeObject);
      return new List<Base>() { element };
    }

    public Base GsaElementToSpeckle(GsaEl gsaEl)
    {
      if (Is3dElement(gsaEl)) //3D element
      {
        return GsaElemenet3dToSpeckle(gsaEl);
      }
      else if (Is2dElement(gsaEl)) // 2D element
      {
        return GsaElemenet2dToSpeckle(gsaEl);
      }
      else //1D element
      {
        return GsaElemenet1dToSpeckle(gsaEl);
      }
    }

    public Element1D GsaElemenet1dToSpeckle(GsaEl gsaEl)
    {
      //TODO
      return new Element1D();
    }

    public Element2D GsaElemenet2dToSpeckle(GsaEl gsaEl)
    {
      var speckleElemenet2d = new Element2D()
      {
        name = gsaEl.Name,
        type = (ElementType2D)Enum.Parse(typeof(ElementType2D), gsaEl.Type.ToString()),
        parent = new Base(),
        displayMesh = new Mesh()
      };

      if (gsaEl.Index.HasValue)
      {
        var elKw = GsaRecord.GetKeyword<GsaEl>();
        speckleElemenet2d.applicationId = Instance.GsaModel.GetApplicationId(elKw, gsaEl.Index.Value);
      }
      if (gsaEl.PropertyIndex.HasValue) speckleElemenet2d.property = GetProperty2dFromIndex(gsaEl.PropertyIndex.Value);
      if (gsaEl.OffsetZ.HasValue) speckleElemenet2d.offset = gsaEl.OffsetZ.Value;
      if (gsaEl.Angle.HasValue) speckleElemenet2d.orientationAngle = gsaEl.Angle.Value;
      speckleElemenet2d.topology = gsaEl.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();

      return speckleElemenet2d;
    }

    public Element3D GsaElemenet3dToSpeckle(GsaEl gsaEl)
    {
      //TODO
      return new Element3D();
    }
    #endregion

    #region Loading
    //TODO: implement conversion code for loading objects
    /* AreaLoad
     * BeamLoad
     * FaceLoad
     * GravityLoad
     * LoadCase
     * LoadCombination
     * NodeLoad
     */
    #endregion

    #region Materials
    public List<Base> GsaMaterialSteelToSpeckle(GsaRecord nativeObject)
    {
      var steel = GsaMaterialSteelToSpeckle((GsaMatSteel)nativeObject);
      return new List<Base>() { steel };
    }

    public Steel GsaMaterialSteelToSpeckle(GsaMatSteel gsaMatSteel)
    {
      //Currently only handles isotropic steel properties.
      //A lot of information in the gsa objects are currently ignored.

      //Gwa keyword SPEC_STEEL_DESIGN is not well documented:
      //
      //SPEC_STEEL_DESIGN | code
      //
      //Description
      //  Steel design code
      //
      //Parameters
      //  code      steel design code
      //
      //Example (GSA 10.1)
      //  SPEC_STEEL_DESIGN.1	AS 4100-1998	YES	15	YES	15	15	YES	NO	NO	NO
      //
      var speckleSteel = new Steel()
      {
        applicationId = gsaMatSteel.ApplicationId,
        name = gsaMatSteel.Name,
        grade = "",                                 //grade can be determined from gsaMatSteel.Mat.Name (assuming the user doesn't change the default value): e.g. "350(AS3678)"
        type = MaterialType.Steel,
        designCode = "",                            //designCode can be determined from SPEC_STEEL_DESIGN gwa keyword
        codeYear = "",                              //codeYear can be determined from SPEC_STEEL_DESIGN gwa keyword
        yieldStrength = gsaMatSteel.Fy.Value,
        ultimateStrength = gsaMatSteel.Fu.Value,
        maxStrain = gsaMatSteel.EpsP.Value
      };

      //the following properties are stored in multiple locations in GSA
      //youngs modulus
      speckleSteel.youngsModulus = GetPropValue<double>(gsaMatSteel.Mat, "E");
      speckleSteel.poissonsRatio = GetPropValue<double>(gsaMatSteel.Mat, "Nu");
      speckleSteel.shearModulus = GetPropValue<double>(gsaMatSteel.Mat, "G");
      speckleSteel.density = GetPropValue<double>(gsaMatSteel.Mat, "Rho");
      speckleSteel.thermalExpansivity = GetPropValue<double>(gsaMatSteel.Mat, "Alpha");

      return speckleSteel;
    }

    public List<Base> GsaMaterialConcreteToSpeckle(GsaRecord nativeObject)
    {
      var concrete = GsaMaterialConcreteToSpeckle((GsaMatConcrete)nativeObject);
      return new List<Base>() { concrete };
    }

    public Concrete GsaMaterialConcreteToSpeckle(GsaMatConcrete gsaMatConcrete)
    {
      //Currently only handles isotropic concrete properties.
      //A lot of information in the gsa objects are currently ignored.

      var speckleConcrete = new Concrete()
      {
        applicationId = gsaMatConcrete.ApplicationId,
        name = gsaMatConcrete.Name,
        grade = "",                                 //grade can be determined from gsaMatConcrete.Mat.Name (assuming the user doesn't change the default value): e.g. "32 MPa"
        type = MaterialType.Concrete,
        designCode = "",                            //designCode can be determined from SPEC_CONCRETE_DESIGN gwa keyword: e.g. "AS3600_18" -> "AS3600"
        codeYear = "",                              //codeYear can be determined from SPEC_CONCRETE_DESIGN gwa keyword: e.g. "AS3600_18" - "2018"
        compressiveStrength = gsaMatConcrete.Fc.Value,
        maxStrain = gsaMatConcrete.EpsU.Value,
        maxAggregateSize = gsaMatConcrete.Agg.Value,
        tensileStrength = gsaMatConcrete.Fcdt.Value,
        flexuralStrength = 0
      };

      //the following properties are stored in multiple locations in GSA
      speckleConcrete.youngsModulus = GetPropValue<double>(gsaMatConcrete.Mat, "E");
      speckleConcrete.poissonsRatio = GetPropValue<double>(gsaMatConcrete.Mat, "Nu");
      speckleConcrete.shearModulus = GetPropValue<double>(gsaMatConcrete.Mat, "G");
      speckleConcrete.density = GetPropValue<double>(gsaMatConcrete.Mat, "Rho");
      speckleConcrete.thermalExpansivity = GetPropValue<double>(gsaMatConcrete.Mat, "Alpha");

      return speckleConcrete;
    }

    //Timber: GSA keyword not supported yet
    #endregion

    #region Property
    public List<Base> GsaSectionToSpeckle(GsaRecord nativeObject)
    {
      var section = GsaSectionToSpeckle((GsaSection)nativeObject);
      return new List<Base>() { section };
    }

    public Property1D GsaSectionToSpeckle(GsaSection gsaSection)
    {
      var speckleProperty1D = new Property1D()
      {
        applicationId = gsaSection.ApplicationId,
        name = gsaSection.Name
      };

      return speckleProperty1D;
    }

    public List<Base> GsaProperty2dToSpeckle(GsaRecord nativeObject)
    {
      var prop2d = GsaProperty2dToSpeckle((GsaProp2d)nativeObject);
      return new List<Base>() { prop2d };
    }

    public Property2D GsaProperty2dToSpeckle(GsaProp2d gsaProp2d)
    {
      var speckleProperty2D = new Property2D()
      {
        name = gsaProp2d.Name,
        colour = gsaProp2d.Colour.ToString(),
        zOffset = gsaProp2d.RefZ,
        grade = "", // GetGrade(gsaProp2d);
        orientationAxis = GetOrientationAxis(gsaProp2d),
        refSurface = GetReferenceSurface(gsaProp2d)
      };

      if (gsaProp2d.Index.HasValue)
      {
        var prop2dKw = GsaRecord.GetKeyword<GsaProp2d>();
        speckleProperty2D.applicationId = Instance.GsaModel.GetApplicationId(prop2dKw, gsaProp2d.Index.Value);
      }
      if (gsaProp2d.Thickness.HasValue) speckleProperty2D.thickness = gsaProp2d.Thickness.Value;
      if (gsaProp2d.GradeIndex.HasValue) speckleProperty2D.material = GetMaterialFromIndex(gsaProp2d.GradeIndex.Value, gsaProp2d.MatType);
      if (gsaProp2d.Type != Property2dType.NotSet) speckleProperty2D.type = (PropertyType2D)Enum.Parse(typeof(PropertyType2D), gsaProp2d.Type.ToString());

      //Currently no way in the speckle object to disinguish between the value and a percentage.
      speckleProperty2D.modifierInPlane = GetModifier(gsaProp2d, "InPlane");
      speckleProperty2D.modifierBending = GetModifier(gsaProp2d, "Bending");
      speckleProperty2D.modifierShear = GetModifier(gsaProp2d, "Shear");
      speckleProperty2D.modifierVolume = GetModifier(gsaProp2d, "Volume");

      return speckleProperty2D;
    }

    //Property3D: GSA keyword not supported yet

    public List<Base> GsaPropertyMassToSpeckle(GsaRecord nativeObject)
    {
      var propMass = GsaPropertyMassToSpeckle((GsaPropMass)nativeObject);
      return new List<Base>() { propMass };
    }

    public PropertyMass GsaPropertyMassToSpeckle(GsaPropMass gsaPropMass)
    {
      var specklePropertyMass = new PropertyMass()
      {
        applicationId = gsaPropMass.ApplicationId,
        name = gsaPropMass.Name,
        mass = gsaPropMass.Mass,
        inertiaXX = gsaPropMass.Ixx,
        inertiaYY = gsaPropMass.Iyy,
        inertiaZZ = gsaPropMass.Izz,
        inertiaXY = gsaPropMass.Ixy,
        inertiaYZ = gsaPropMass.Iyz,
        inertiaZX = gsaPropMass.Izx
      };

      //Mass modifications
      if (gsaPropMass.Mod == MassModification.Modified)
      {
        specklePropertyMass.massModified = true;
        if (gsaPropMass.ModXPercentage.HasValue) specklePropertyMass.massModifierX = gsaPropMass.ModXPercentage.Value;
        if (gsaPropMass.ModYPercentage.HasValue) specklePropertyMass.massModifierY = gsaPropMass.ModYPercentage.Value;
        if (gsaPropMass.ModZPercentage.HasValue) specklePropertyMass.massModifierZ = gsaPropMass.ModZPercentage.Value;
      }

      return specklePropertyMass;
    }

    public List<Base> GsaPropertySpringToSpeckle(GsaRecord nativeObject)
    {
      var propSpring = GsaPropertySpringToSpeckle((GsaPropSpr)nativeObject);
      return new List<Base>() { propSpring };
    }

    public PropertySpring GsaPropertySpringToSpeckle(GsaPropSpr gsaPropSpr)
    {
      //Apply properties common to all spring types
      var specklePropertySpring = new PropertySpring()
      {
        applicationId = gsaPropSpr.ApplicationId,
        name = gsaPropSpr.Name,
        dampingRatio = gsaPropSpr.DampingRatio.Value
      };

      //Dictionary of fns used to apply spring type specific properties. 
      //Functions will pass by reference specklePropertySpring and make the necessary changes to it
      var fns = new Dictionary<StructuralSpringPropertyType, Func<GsaPropSpr, PropertySpring, bool>>
      { { StructuralSpringPropertyType.Axial, SetProprtySpringAxial },
        { StructuralSpringPropertyType.Torsional, SetPropertySpringTorsional },
        { StructuralSpringPropertyType.Compression, SetProprtySpringCompression },
        { StructuralSpringPropertyType.Tension, SetProprtySpringTension },
        { StructuralSpringPropertyType.Lockup, SetProprtySpringLockup },
        { StructuralSpringPropertyType.Gap, SetProprtySpringGap },
        { StructuralSpringPropertyType.Friction, SetProprtySpringFriction },
        { StructuralSpringPropertyType.General, SetProprtySpringGeneral }
        //MATRIX not yet supported
        //CONNECT not yet supported
      };

      //Apply spring type specific properties
      if (fns.ContainsKey(gsaPropSpr.PropertyType))
      {
        fns[gsaPropSpr.PropertyType](gsaPropSpr, specklePropertySpring);
      }

      return specklePropertySpring;
    }

    //PropertyDamper: GSA keyword not supported yet
    #endregion

    #region Results
    //TODO: implement conversion code for result objects
    /* Result1D
     * Result2D
     * Result3D
     * ResultGlobal
     * ResultNode
     */
    #endregion
    #endregion

    #region ToNative
    public GsaNode NodeToNative(GSANode speckleNode)
    {
      var gsaNode = new GsaNode();
      gsaNode.Name = speckleNode.name;
      gsaNode.ApplicationId = speckleNode.applicationId;
      gsaNode.X = speckleNode.basePoint.x;
      gsaNode.Y = speckleNode.basePoint.y;
      gsaNode.Z = speckleNode.basePoint.z;
      //gsaNode.NodeRestraint = NodeRestraint.Pin;
      //gsaNode.Restraints = null;
      //gsaNode.AxisIndex = null;
      //gsaNode.AxisRefType = AxisRefType.Global;
      //gsaNode.Colour = Colour.NO_RGB;
      //gsaNode.MassPropertyIndex = null;
      //gsaNode.SpringPropertyIndex = null;
      //gsaNode.MeshSize = null;

      return gsaNode;
    }
    //TODO: implement conversion code for other objects
    #endregion

    #region Helper
    #region ToSpeckle
    #region Geometry
    #region Node
    private static Restraint GetRestraint(GsaNode gsaNode)
    {
      Restraint restraint;
      switch (gsaNode.NodeRestraint)
      {
        case NodeRestraint.Pin:
          restraint = new Restraint(RestraintType.Pinned);
          break;
        case NodeRestraint.Fix:
          restraint = new Restraint(RestraintType.Fixed);
          break;
        case NodeRestraint.Free:
          restraint = new Restraint(RestraintType.Free);
          break;
        case NodeRestraint.Custom:
          string code = GetCustomRestraintCode(gsaNode);
          restraint = new Restraint(code.ToString());
          break;
        default:
          restraint = new Restraint();
          break;
      }

      //restraint = UpdateSpringStiffness(restraint, gsaNode);

      return restraint;
    }
    private Plane GetConstraintAxis(GsaNode gsaNode)
    {
      Plane axis;
      Point origin;
      Vector xdir, ydir, normal;

      //TO DO: check with oasys that the definitions for X_ELEV and Y_ELEV are correct
      if (gsaNode.AxisRefType == NodeAxisRefType.XElevation)
      {
        origin = new Point(0, 0, 0);
        xdir = new Vector(0, 1, 0);
        ydir = new Vector(0, 0, 1);
        normal = new Vector(1, 0, 0);
        axis = new Plane(origin, normal, xdir, ydir);
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.YElevation)
      {
        origin = new Point(0, 0, 0);
        xdir = new Vector(-1, 0, 0);
        ydir = new Vector(0, 0, 1);
        normal = new Vector(0, 1, 0);
        axis = new Plane(origin, normal, xdir, ydir);
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.Reference && gsaNode.AxisIndex.HasValue && gsaNode.AxisIndex.Value > 0)
      {
        axis = GetAxisFromIndex(gsaNode.AxisIndex.Value).definition;
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        axis = GlobalAxis().definition;
      }

      return axis;
    }
    private static string GetCustomRestraintCode(GsaNode gsaNode)
    {
      var code = "RRRRRR".ToCharArray();
      for (var i = 0; i < gsaNode.Restraints.Count(); i++)
      {
        switch (gsaNode.Restraints[i])
        {
          case AxisDirection6.X:
            code[0] = 'F';
            break;
          case AxisDirection6.Y:
            code[1] = 'F';
            break;
          case AxisDirection6.Z:
            code[2] = 'F';
            break;
          case AxisDirection6.XX:
            code[3] = 'F';
            break;
          case AxisDirection6.YY:
            code[4] = 'F';
            break;
          case AxisDirection6.ZZ:
            code[5] = 'F';
            break;
        }
      }
      return code.ToString();
    }
    private static Restraint UpdateSpringStiffness(Restraint restraint, GsaNode gsaNode)
    {
      //Spring Stiffness
      if (gsaNode.SpringPropertyIndex.HasValue && gsaNode.SpringPropertyIndex.Value > 0)
      {
        var springKw = GsaRecord.GetKeyword<GsaPropSpr>();
        var gsaRecord = Instance.GsaModel.GetNative(springKw, gsaNode.SpringPropertyIndex.Value);
        if (gsaRecord.GetType() != typeof(GsaPropSpr))
        {
          return restraint;
        }
        var gsaSpring = (GsaPropSpr)gsaRecord;

        //Update spring stiffness
        if (gsaSpring.Stiffnesses[AxisDirection6.X] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[0] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessX = gsaSpring.Stiffnesses[AxisDirection6.X];
        }
        if (gsaSpring.Stiffnesses[AxisDirection6.Y] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[1] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessY = gsaSpring.Stiffnesses[AxisDirection6.Y];
        }
        if (gsaSpring.Stiffnesses[AxisDirection6.Z] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[2] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessZ = gsaSpring.Stiffnesses[AxisDirection6.Z];
        }
        if (gsaSpring.Stiffnesses[AxisDirection6.XX] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[3] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessXX = gsaSpring.Stiffnesses[AxisDirection6.XX];
        }
        if (gsaSpring.Stiffnesses[AxisDirection6.YY] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[4] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessYY = gsaSpring.Stiffnesses[AxisDirection6.YY];
        }
        if (gsaSpring.Stiffnesses[AxisDirection6.ZZ] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[5] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessZZ = gsaSpring.Stiffnesses[AxisDirection6.ZZ];
        }
      }
      return restraint;
    }

    private Node GetNodeFromIndex(int index)
    {
      Node speckleNode = null;
      var nodeKw = GsaRecord.GetKeyword<GsaNode>();
      var gsaNode = Instance.GsaModel.GetNative(nodeKw, index);
      if (gsaNode != null) speckleNode = GsaNodeToSpeckle((GsaNode)gsaNode);

      return speckleNode;
    }
    #endregion

    #region Axis
    private Axis GetAxisFromIndex(int index)
    {
      var axisKw = GsaRecord.GetKeyword<GsaAxis>();
      var gsaAxis = Instance.GsaModel.GetNative(axisKw, index);
      if (gsaAxis.GetType() != typeof(GsaAxis))
      {
        return null;
      }
      return GsaAxisToSpeckle((GsaAxis)gsaAxis);
    }

    private static Axis GlobalAxis()
    {
      //Default global coordinates for case: Global or NotSet
      var origin = new Point(0, 0, 0);
      var xdir = new Vector(1, 0, 0);
      var ydir = new Vector(0, 1, 0);
      var normal = new Vector(0, 0, 1);

      var axis = new Axis()
      {
        name = "",
        axisType = AxisType.Cartesian,
        definition = new Plane(origin, normal, xdir, ydir)
      };

      return axis;
    }
    #endregion

    #region Elements
    public bool Is2dElement(GsaEl gsaEl)
    {
      return (gsaEl.Type == ElementType.Triangle3 || gsaEl.Type == ElementType.Triangle6 || gsaEl.Type == ElementType.Quad4 || gsaEl.Type == ElementType.Quad8);
    }
    public bool Is3dElement(GsaEl gsaEl)
    {
      return (gsaEl.Type == ElementType.Brick8 || gsaEl.Type == ElementType.Pyramid5 || gsaEl.Type == ElementType.Tetra4 || gsaEl.Type == ElementType.Wedge6);
    }
    public bool Is1dElement(GsaEl gsaEl)
    {
      return (gsaEl.Type == ElementType.Bar || gsaEl.Type == ElementType.Beam || gsaEl.Type == ElementType.Cable || gsaEl.Type == ElementType.Damper || 
        gsaEl.Type == ElementType.Link || gsaEl.Type == ElementType.Rod || gsaEl.Type == ElementType.Spacer || gsaEl.Type == ElementType.Spring || 
        gsaEl.Type == ElementType.Strut || gsaEl.Type == ElementType.Tie);
    }
    #endregion
    #endregion

    #region Loading
    #endregion

    #region Materials
    //Some material properties are stored in either GsaMat or GsaMatAnal
    //The GetPropValue<T>(GsaMat gsaMat, string name) method will find the value stored in gsaMat."name"
    //if null or default for type T, then will find the value in gsaMat.Prop."name"
    public static object GetPropValue(object obj, string name)
    {
      foreach (string part in name.Split('.'))
      {
        if (obj == null) { return null; }

        var type = obj.GetType();
        var info = type.GetField(part);
        if (info == null) { return null; }

        obj = info.GetValue(obj);
      }
      return obj;
    }
    public static T GetPropValue<T>(GsaMat gsaMat, string name)
    {
      var retval = GetPropValue(gsaMat, name);
      if (IsNullOrDefault((T)retval))
      {
        return GetPropValue<T>(gsaMat.Prop, name);
      }

      // throws InvalidCastException if types are incompatible
      return (T)retval;
    }
    public static T GetPropValue<T>(GsaMatAnal gsaMatAnal, string name)
    {
      object retval = GetPropValue(gsaMatAnal, name);
      if (retval == null) { return default(T); }

      // throws InvalidCastException if types are incompatible
      return (T)retval;
    }
    static bool IsNullOrDefault<T>(T value)
    {
      return object.Equals(value, default(T));
    }
    private Material GetMaterialFromIndex(int index, Property2dMaterialType type)
    {
      //Initialise
      GwaKeyword matKw;
      GsaRecord gsaMat;
      Material speckleMaterial = null;   

      //Get material based on type and gsa index
      //Convert gsa material to speckle material
      if (type == Property2dMaterialType.Steel)
      {
        matKw = GsaRecord.GetKeyword<GsaMatSteel>();
        gsaMat = Instance.GsaModel.GetNative(matKw, index);
        if (gsaMat != null) speckleMaterial = GsaMaterialSteelToSpeckle((GsaMatSteel)gsaMat);
      }
      else if (type == Property2dMaterialType.Concrete)
      {
        matKw = GsaRecord.GetKeyword<GsaMatConcrete>();
        gsaMat = Instance.GsaModel.GetNative(matKw, index);
        if (gsaMat != null) speckleMaterial = GsaMaterialConcreteToSpeckle((GsaMatConcrete)gsaMat);
      }

      return speckleMaterial;
    }
    #endregion

    #region Properties
    #region Spring
    private bool SetProprtySpringAxial(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Axial;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      return true;
    }
    private bool SetPropertySpringTorsional(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Torsional;
      specklePropertySpring.stiffnessXX = gsaPropSpr.Stiffnesses[AxisDirection6.XX];
      return true;
    }
    private bool SetProprtySpringCompression(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.CompressionOnly;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      return true;
    }
    private bool SetProprtySpringTension(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.TensionOnly;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      return true;
    }
    private bool SetProprtySpringLockup(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      //Also for LOCKUP, there are positive and negative parameters, but these aren't supported yet
      specklePropertySpring.springType = PropertyTypeSpring.LockUp;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      specklePropertySpring.positiveLockup = 0;
      specklePropertySpring.negativeLockup = 0;
      return true;
    }
    private bool SetProprtySpringGap(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Gap;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      return true;
    }
    private bool SetProprtySpringFriction(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Friction;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      specklePropertySpring.stiffnessY = gsaPropSpr.Stiffnesses[AxisDirection6.Y];
      specklePropertySpring.stiffnessZ = gsaPropSpr.Stiffnesses[AxisDirection6.Z];
      specklePropertySpring.frictionCoefficient = gsaPropSpr.FrictionCoeff.Value;
      return true;
    }
    private bool SetProprtySpringGeneral(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.General;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      specklePropertySpring.springCurveX = 0;
      specklePropertySpring.stiffnessY = gsaPropSpr.Stiffnesses[AxisDirection6.Y];
      specklePropertySpring.springCurveY = 0;
      specklePropertySpring.stiffnessZ = gsaPropSpr.Stiffnesses[AxisDirection6.Z];
      specklePropertySpring.springCurveZ = 0;
      specklePropertySpring.stiffnessXX = gsaPropSpr.Stiffnesses[AxisDirection6.XX];
      specklePropertySpring.springCurveXX = 0;
      specklePropertySpring.stiffnessYY = gsaPropSpr.Stiffnesses[AxisDirection6.YY];
      specklePropertySpring.springCurveYY = 0;
      specklePropertySpring.stiffnessZZ = gsaPropSpr.Stiffnesses[AxisDirection6.ZZ];
      specklePropertySpring.springCurveZZ = 0;
      return true;
    }
    #endregion

    #region Property2D
    private ReferenceSurface GetReferenceSurface(GsaProp2d gsaProp2d)
    {
      var refenceSurface = ReferenceSurface.Middle; //default

      if (gsaProp2d.RefPt == Property2dRefSurface.BottomCentre)
      {
        refenceSurface = ReferenceSurface.Bottom;
      }
      else if (gsaProp2d.RefPt == Property2dRefSurface.TopCentre)
      {
        refenceSurface = ReferenceSurface.Top;
      }
      return refenceSurface;
    }

    private Axis GetOrientationAxis(GsaProp2d gsaProp2d)
    {
      //Cartesian coordinate system is the only one supported.
      var orientationAxis = new Axis()
      {
        name = "",
        axisType = AxisType.Cartesian,
      };

      if (gsaProp2d.AxisRefType == AxisRefType.Local)
      {
        //TO DO: handle local reference axis case
        //Local would be a different coordinate system for each element that gsaProp2d was assigned to
      }
      else if (gsaProp2d.AxisRefType == AxisRefType.Reference && gsaProp2d.AxisIndex.HasValue && gsaProp2d.AxisIndex.Value > 0)
      {
        orientationAxis = GetAxisFromIndex(gsaProp2d.AxisIndex.Value);
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        orientationAxis = GlobalAxis();
      }

      return orientationAxis;
    }

    private double GetModifier(GsaProp2d gsaProp2d, string name)
    {
      //gsaProp2d has a number of modifiers, e.g. InPlane, InPlaneStiffnessPercentage, Bending, ...
      //either the value or the percentage field stores the modifier, e.g. either "InPlane" or "InPlaneStiffnessPercentage" is null and the other has a value
      //this method returns that value
      var info = gsaProp2d.GetType().GetField(name);
      if (info.GetValue(gsaProp2d) == null)
      {
        if (name == "Volume")
        {
          info = gsaProp2d.GetType().GetField(name + "Percentage");
        }
        else
        {
          info = gsaProp2d.GetType().GetField(name + "StiffnessPercentage");
        }
      }
      return (double)info.GetValue(gsaProp2d);
    }

    private Property2D GetProperty2dFromIndex(int index)
    {
      Property2D speckleProperty2d = null;
      var prop2dKw = GsaRecord.GetKeyword<GsaProp2d>();
      var gsaProp2d = Instance.GsaModel.GetNative(prop2dKw, index);
      if (gsaProp2d != null) speckleProperty2d = GsaProperty2dToSpeckle((GsaProp2d)gsaProp2d);

      return speckleProperty2d;
    }
    #endregion
    #endregion
    #endregion

    #region ToNative
    #endregion

    #region Other
    #region Vector
    private Vector CrossProduct(Vector a, Vector b)
    {
      Vector c = new Vector()
      {
        x = a.y * b.z - a.z * b.y,
        y = a.z * b.x - a.x * b.z,
        z = a.x * b.y - a.y * b.x
      };
      return c;
    }

    private double DotProduct(Vector a, Vector b)
    {
      return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    private Vector UnitVector(Vector a)
    {
      var l = Norm(a);
      Vector b = new Vector()
      {
        x = a.x / l,
        y = a.y / l,
        z = a.z / l
      };
      return b;
    }

    private double Norm(Vector a)
    {
      return Math.Sqrt(DotProduct(a, a));
    }
    #endregion
    #endregion
    #endregion
  }
}
