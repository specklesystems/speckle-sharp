using Objects.Geometry;
using Objects.Structural;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using static Objects.Structural.Properties.Profiles.SectionProfile;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.GSA.Loading;
using Objects.Structural.GSA.Properties;
using Objects.Structural.GSA.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using Restraint = Objects.Structural.Geometry.Restraint;
using MemberType = Objects.Structural.Geometry.MemberType;
using GwaMemberType = Speckle.GSA.API.GwaSchema.MemberType;
using AxisDirection6 = Objects.Structural.GSA.Other.AxisDirection6;
using GwaAxisDirection6 = Speckle.GSA.API.GwaSchema.AxisDirection6;
using GwaAxisDirection3 = Speckle.GSA.API.GwaSchema.AxisDirection3;
using System.Runtime.InteropServices;
using System.CodeDom;
using Objects.Structural.Results;
using Speckle.GSA.API.CsvSchema;
using Speckle.Core.Api;
using Objects.Structural.Analysis;
using Objects.Structural.GSA.Analysis;
using Objects.Structural.GSA.Materials;
using System.Collections;
using System.Security.AccessControl;

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

    private delegate ToSpeckleResult ToSpeckleMethodDelegate(GsaRecord gsaRecord, GSALayer layer = GSALayer.Both);

    private Dictionary<Type, ToSpeckleMethodDelegate> ToSpeckleFns;
    private Dictionary<Type, Func<Base, List<GsaRecord>>> ToNativeFns;

    #region model_group
    private enum ModelGroup
    {
      Nodes,
      Elements,
      Loads,
      Restraints,
      Properties,
      Materials
    }

    //These are the groupings in the Model class, which are *Speckle* object types
    private readonly Dictionary<ModelGroup, List<Type>> modelGroups = new Dictionary<ModelGroup, List<Type>>()
    {
      { ModelGroup.Nodes, new List<Type>() { typeof(GSANode) } },
      { ModelGroup.Elements, new List<Type>() { typeof(GSAAssembly), typeof(Axis), typeof(GSAElement1D), typeof(GSAElement2D), typeof(GSAElement3D), typeof(GSAMember1D), typeof(GSAMember2D) } },
      { ModelGroup.Loads, new List<Type>()
        { typeof(Case), typeof(Task), typeof(GSALoadCase), typeof(GSABeamLoad), typeof(GSAFaceLoad), typeof(GSAGravityLoad), typeof(GSALoadCase), typeof(GSALoadCombination), typeof(GSANodeLoad) } },
      { ModelGroup.Restraints, new List<Type>() { typeof(Restraint) } },
      { ModelGroup.Properties, new List<Type>()
        { typeof(GSAProperty1D), typeof(GSAProperty2D), typeof(SectionProfile), typeof(PropertyMass), typeof(PropertySpring), typeof(PropertyDamper), typeof(Property3D) } },
      { ModelGroup.Materials, new List<Type>() { typeof(GSAMaterial), typeof(Concrete), typeof(Steel), typeof(Concrete) } }
    };
    #endregion

    public ConverterGSA()
    {
      ToSpeckleFns = new Dictionary<Type, ToSpeckleMethodDelegate>()
      {
        //Geometry
        { typeof(GsaAssembly), GsaAssemblyToSpeckle },
        { typeof(GsaAxis), GsaAxisToSpeckle },
        { typeof(GsaNode), GsaNodeToSpeckle },
        { typeof(GsaEl), GsaElementToSpeckle },
        { typeof(GsaMemb), GsaMemberToSpeckle },
        { typeof(GsaGridLine), GsaGridLineToSpeckle },
        { typeof(GsaGridPlane), GsaGridPlaneToSpeckle },
        { typeof(GsaGridSurface), GsaGridSurfaceToSpeckle },
        { typeof(GsaPolyline), GsaPolylineToSpeckle },
        //Loading
        { typeof(GsaLoadCase), GsaLoadCaseToSpeckle },
        { typeof(GsaLoad2dFace), GsaFaceLoadToSpeckle },
        { typeof(GsaLoadBeamPoint), GsaBeamLoadToSpeckle },
        { typeof(GsaLoadBeamUdl), GsaBeamLoadToSpeckle },
        { typeof(GsaLoadBeamLine), GsaBeamLoadToSpeckle },
        { typeof(GsaLoadBeamPatch), GsaBeamLoadToSpeckle },
        { typeof(GsaLoadBeamTrilin), GsaBeamLoadToSpeckle },
        { typeof(GsaLoadNode), GsaNodeLoadToSpeckle },
        { typeof(GsaLoadGravity), GsaGravityLoadToSpeckle },
        { typeof(GsaCombination), GsaLoadCombinationToSpeckle },
        { typeof(GsaLoad2dThermal), GsaThermal2dLoadToSpeckle },
        { typeof(GsaLoadGridArea), GsaLoadGridAreaToSpeckle },
        { typeof(GsaLoadGridLine), GsaLoadGridLineToSpeckle },
        { typeof(GsaLoadGridPoint), GsaLoadGridPointToSpeckle },
        //Material
        { typeof(GsaMatSteel), GsaMaterialSteelToSpeckle },
        { typeof(GsaMatConcrete), GsaMaterialConcreteToSpeckle },
        //Property
        { typeof(GsaSection), GsaSectionToSpeckle },
        { typeof(GsaProp2d), GsaProperty2dToSpeckle },
        //{ typeof(GsaProp3d), GsaProperty3dToSpeckle }, not supported yet
        { typeof(GsaPropMass), GsaPropertyMassToSpeckle },
        { typeof(GsaPropSpr), GsaPropertySpringToSpeckle },
        //Constraints
        { typeof(GsaRigid), GsaRigidToSpeckle },
        { typeof(GsaGenRest), GsaGenRestToSpeckle },
        //Analysis Stage
        { typeof(GsaAnalStage), GsaStageToSpeckle },
        //TODO: add methods for other GSA keywords
      };

      ToNativeFns = new Dictionary<Type, Func<Base, List<GsaRecord>>>()
      {
        {  typeof(Axis), AxisToNative }
      };
    }

    public bool CanConvertToNative(Base @object)
    {
      var t = @object.GetType();
      return ToNativeFns.ContainsKey(t);
    }

    public bool CanConvertToSpeckle(object @object)
    {
      var t = @object.GetType();
      return (t.IsSubclassOf(typeof(GsaRecord)) && ToSpeckleFns.ContainsKey(t));
    }

    public object ConvertToNative(Base @object)
    {
      var t = @object.GetType();
      return ToNativeFns[t](@object);
    }

    //Assume that this is called when a Model object is desired, rather than loose Speckle objects.
    //The one thing that is needed here is an order, a set of generations ... 
    public List<object> ConvertToNative(List<Base> objects)
    {
      var retList = new List<object>();
      foreach (var obj in objects)
      {
        var natives = ConvertToNative(obj);
        if (natives != null)
        {
          if (natives is List<GsaRecord>)
          {
            retList.AddRange(((List<GsaRecord>)natives).Cast<object>());
          }
        }
      }
      return retList;
    }

    public Base ConvertToSpeckle(object @object)
    {
      if (@object is List<GsaRecord>)
      {
        //by calling this method with List<GsaRecord>, it is assumed that either:
        //- the caller doesn't care about retrieving any Speckle objects, since a conversion could result in multiple and this method only gives back the first
        //- the caller expects the conversion to only result in one Speckle object anyway
        var objects = ConvertToSpeckle(((List<GsaRecord>)@object).Cast<object>().ToList());
        return objects.First();
      }
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

      //Assume that if only one object is passed in, then a standard ToSpeckle conversion on just that single object (which in GSA world, could result in multiple Base objects)
      //If multiple objects are passed in, then assume a whole model is requested

      var retList = new List<Base>();
      if (objects.Count == 1)
      {
        foreach (var x in objects)
        {
          var toSpeckleResult = ToSpeckle((GsaRecord)x);
          var speckleObjects = toSpeckleResult.ModelObjects;
          if (speckleObjects != null && speckleObjects.Count > 0)
          {
            retList.AddRange(speckleObjects.Where(so => so != null));
          }
        }
        return retList;
      }

      //TO DO - fill in this more
      var modelInfo = new ModelInfo()
      {
        application = "GSA",
        settings = new ModelSettings()
        {
          coincidenceTolerance = 0.01
        }
      };

      //Both layer options include sending of the design layer model (if present in GSA)
      //TO DO - fill in this more
      var model = new Model()
      {
        layerDescription = "Design Layer",
        specs = modelInfo
      };

      if (ConvertToModel(objects.Cast<GsaRecord>().ToList(), GSALayer.Design, modelInfo, out Model designModel, out _))
      {
        retList.Add(designModel);
      }

      if (Instance.GsaModel.StreamLayer == GSALayer.Both)
      {
        if (ConvertToModel(objects.Cast<GsaRecord>().ToList(), GSALayer.Analysis, modelInfo, out Model analysisModel, out List<Base> resultObjects))
        {
          retList.Add(analysisModel);
        }
        if (Instance.GsaModel.StreamSendConfig == StreamContentConfig.ModelAndResults && resultObjects != null && resultObjects.Count > 0)
        {
          retList.AddRange(resultObjects);
        }
      }

      return retList;
    }

    private bool ConvertToModel(List<GsaRecord> gsaRecords, GSALayer layer, ModelInfo modelInfo, out Model retModel, out List<Base> resultObjects)
    {
      var typeGens = Instance.GsaModel.Proxy.GetTxTypeDependencyGenerations(layer);

      var gsaRecordsByType = gsaRecords.GroupBy(r => r.GetType()).ToDictionary(r => r.Key, r => r.ToList());
      var speckleObjsBucket = new List<Base>();

      foreach (var gen in typeGens)
      {
        var genNativeObjsByType = new Dictionary<Type, List<GsaRecord>>();
        foreach (var t in gen)
        {
          if (gsaRecordsByType.ContainsKey(t))
          {
            genNativeObjsByType.Add(t, gsaRecordsByType[t]);
          }
        }

        foreach (var t in genNativeObjsByType.Keys)
        {
          foreach (var nativeObj in genNativeObjsByType[t])
          {
            try
            {
              if (CanConvertToSpeckle(nativeObj))
              {
                var toSpeckleResult = ToSpeckle(nativeObj);
                var speckleObjs = toSpeckleResult.ModelObjects;
                if (speckleObjs != null && speckleObjs.Count > 0)
                {
                  speckleObjsBucket.AddRange(speckleObjs);
                  Instance.GsaModel.Cache.SetSpeckleObjects(nativeObj, speckleObjs.ToDictionary(so => so.applicationId, so => (object)so));
                }
              }
            }
            catch (Exception ex)
            {

            }
          }
        }
      }

      if (speckleObjsBucket.Count > 0)
      {
        var resultType = typeof(Result);
        var allObjsByType = speckleObjsBucket.GroupBy(o => o.GetType()).ToDictionary(g => g.Key, g => g.ToList());
        var foundResultTypes = allObjsByType.Keys.Where(o => o.GetType().IsAssignableFrom(resultType)).ToList();
        var containsResults = (foundResultTypes != null && foundResultTypes.Count > 0);

        var modelObjsByType = containsResults
          ? allObjsByType.Keys.Except(foundResultTypes).ToDictionary(k => k, k => allObjsByType[k])
          : allObjsByType;

        resultObjects = containsResults ? foundResultTypes.SelectMany(t => allObjsByType[t]).ToList() : null;

        var objectsByModelGroup = new Dictionary<ModelGroup, List<Base>>();

        foreach (ModelGroup mg in Enum.GetValues(typeof(ModelGroup)))
        {
          objectsByModelGroup.Add(mg, null);
          foreach (var sType in modelGroups[mg])
          {
            if (modelObjsByType.ContainsKey(sType))
            {
              if (objectsByModelGroup[mg] == null)
              {
                objectsByModelGroup[mg] = new List<Base>();
              }
              objectsByModelGroup[mg].AddRange(modelObjsByType[sType]);
            }
          }
        }

        retModel = new Model(modelInfo, objectsByModelGroup[ModelGroup.Nodes], objectsByModelGroup[ModelGroup.Elements],
          objectsByModelGroup[ModelGroup.Loads], objectsByModelGroup[ModelGroup.Restraints],
          objectsByModelGroup[ModelGroup.Properties], objectsByModelGroup[ModelGroup.Materials]);
      }
      else
      {
        retModel = null;
        resultObjects = null;
      }
      return (retModel != null);
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
    private ToSpeckleResult ToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var nativeType = nativeObject.GetType();
      return ToSpeckleFns.ContainsKey(nativeType) ? ToSpeckleFns[nativeType](nativeObject, layer) : null;
    }

    #region Geometry
    private ToSpeckleResult GsaAssemblyToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaAssembly = (GsaAssembly)nativeObject;
      var speckleAssembly = new GSAAssembly()
      {
        //-- GSA specific --

        //TO DO: to be removed
        //group = 0,
        //colour = "",
        //action = "",
        //isDummy = false,

        //TO DO: to be added
        //name = gsaAssembly.Name,
        //intTopo = 
        //sizeY = gsaAssembly.SizeY,
        //sizeZ = gsaAssembly.SizeZ,
        //curveType = gsaAssembly.CurveType.ToString(),
        //curveOrder = gsaAssembly.CurveOrder.Value,
        //pointDefintion = gsaAssembly.PointDefn,
      };

      if (gsaAssembly.Index.IsIndex()) speckleAssembly.applicationId = Instance.GsaModel.GetApplicationId<GsaAssembly>(gsaAssembly.Index.Value);
      if (gsaAssembly.Index.IsIndex()) speckleAssembly.nativeId = gsaAssembly.Index.Value;
      if (gsaAssembly.Topo1.IsIndex()) speckleAssembly.end1Node = (GSANode)GetNodeFromIndex(gsaAssembly.Topo1.Value);
      if (gsaAssembly.Topo1.IsIndex()) speckleAssembly.end2Node = (GSANode)GetNodeFromIndex(gsaAssembly.Topo2.Value);
      if (gsaAssembly.OrientNode.IsIndex()) speckleAssembly.orientationNode = (GSANode)GetNodeFromIndex(gsaAssembly.OrientNode.Value);
      if (gsaAssembly.Type == GSAEntity.ELEMENT) speckleAssembly.entities = gsaAssembly.ElementIndices.Select(i => (Base)GetElement2DFromIndex(i)).ToList();

      if (gsaAssembly.PointDefn == PointDefinition.Points && gsaAssembly.NumberOfPoints.IsIndex())
      {
        //speckleAssembly.numberOfPoints = gsaAssembly.NumberOfPoints.Value;
      }
      else if (gsaAssembly.PointDefn == PointDefinition.Spacing && gsaAssembly.Spacing.IsPositive())
      {
        //speckleAssembly.spacing = gsaAssembly.Spacing.Value;
      }
      else if (gsaAssembly.PointDefn == PointDefinition.Storey)
      {
        //speckleAssembly.stories = gsaAssembly.StoreyIndices.Select(i => GetStoriesFromIndex(i)).ToList();
      }
      else if (gsaAssembly.PointDefn == PointDefinition.Explicit)
      {
        //speckleAssembly.explicitPositions = gsaAssembly.ExplicitPositions;
      }

      return new ToSpeckleResult(speckleAssembly);
    }

    private ToSpeckleResult GsaNodeToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaNode = (GsaNode)nativeObject;
      var speckleNode = new GSANode()
      {
        //-- App agnostic --
        name = gsaNode.Name,
        basePoint = new Point(gsaNode.X, gsaNode.Y, gsaNode.Z),
        constraintAxis = GetConstraintAxis(gsaNode),
        restraint = GetRestraint(gsaNode),

        //-- GSA specific --
        colour = gsaNode.Colour.ToString(),
      };

      //-- App agnostic --
      if (gsaNode.Index.IsIndex()) speckleNode.applicationId = Instance.GsaModel.GetApplicationId<GsaNode>(gsaNode.Index.Value);
      if (gsaNode.MassPropertyIndex.IsIndex()) speckleNode.massProperty = GetPropertyMassFromIndex(gsaNode.MassPropertyIndex.Value);
      if (gsaNode.SpringPropertyIndex.IsIndex()) speckleNode.springProperty = GetPropertySpringFromIndex(gsaNode.SpringPropertyIndex.Value);

      //-- GSA specific --
      if (gsaNode.Index.IsIndex()) speckleNode.nativeId = gsaNode.Index.Value;
      if (gsaNode.MeshSize.IsPositive()) speckleNode.localElementSize = gsaNode.MeshSize.Value;

      if (GsaNodeResultToSpeckle(gsaNode.Index.Value, speckleNode, out var speckleResults))
      {
        return new ToSpeckleResult(new List<Base>() { speckleNode }, null, null, speckleResults.Select(i => (Base)i).ToList());
      }
      else
      {
        return new ToSpeckleResult(speckleNode);
      }
    }

    private ToSpeckleResult GsaAxisToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaAxis = (GsaAxis)nativeObject;
      //Only supporting cartesian coordinate systems at the moment
      var speckleAxis = new Axis()
      {
        name = gsaAxis.Name,
        axisType = AxisType.Cartesian,
      };
      if (gsaAxis.Index.IsIndex()) speckleAxis.applicationId = Instance.GsaModel.GetApplicationId<GsaAxis>(gsaAxis.Index.Value);
      if (gsaAxis.XDirX.HasValue && gsaAxis.XDirY.HasValue && gsaAxis.XDirZ.HasValue && gsaAxis.XYDirX.HasValue && gsaAxis.XYDirY.HasValue && gsaAxis.XYDirZ.HasValue)
      {
        var origin = new Point(gsaAxis.OriginX, gsaAxis.OriginY, gsaAxis.OriginZ);
        var xdir = (new Vector(gsaAxis.XDirX.Value, gsaAxis.XDirY.Value, gsaAxis.XDirZ.Value)).UnitVector();
        var ydir = (new Vector(gsaAxis.XYDirX.Value, gsaAxis.XYDirY.Value, gsaAxis.XYDirZ.Value)).UnitVector();
        var normal = (xdir * ydir).UnitVector();
        ydir = -(xdir * normal).UnitVector();
        speckleAxis.definition = new Plane(origin, normal, xdir, ydir);
      }
      else
      {
        speckleAxis.definition = GlobalAxis().definition;
      }

      return new ToSpeckleResult(speckleAxis);
    }

    private ToSpeckleResult GsaElementToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaEl = (GsaEl)nativeObject;

      if (gsaEl.Index.IsIndex())
      {
        if (gsaEl.Is3dElement()) //3D element
        {
          var speckleElement3d = GsaElement3dToSpeckle(gsaEl);
          if (GsaElement3dResultToSpeckle(gsaEl.Index.Value, speckleElement3d, out var speckleResults))
          {
            return new ToSpeckleResult(new List<Base>() { speckleElement3d }, null, null, speckleResults.Select(i => (Base)i).ToList());
          }
          else
          {
            return new ToSpeckleResult(speckleElement3d);
          }
        }
        else if (gsaEl.Is2dElement()) // 2D element
        {
          var speckleElement2d = GsaElement2dToSpeckle(gsaEl);
          if (GsaElement2dResultToSpeckle(gsaEl.Index.Value, speckleElement2d, out var speckleResults))
          {
            return new ToSpeckleResult(new List<Base>() { speckleElement2d }, null, null, speckleResults.Select(i => (Base)i).ToList());
          }
          else
          {
            return new ToSpeckleResult(speckleElement2d);
          }
        }
        else //1D element
        {
          var speckleElement1d = GsaElement1dToSpeckle(gsaEl);
          if (GsaElement1dResultToSpeckle(gsaEl.Index.Value, speckleElement1d, out var speckleResults))
          {
            return new ToSpeckleResult(new List<Base>() { speckleElement1d }, null, null, speckleResults.Select(i => (Base)i).ToList());
          }
          else
          {
            return new ToSpeckleResult(speckleElement1d);
          }
        }
      }
      return new ToSpeckleResult(false);
    }

    private GSAElement1D GsaElement1dToSpeckle(GsaEl gsaEl)
    {
      var speckleElement1d = new GSAElement1D()
      {
        //-- App agnostic --
        name = gsaEl.Name,
        type = GetElement1dType(gsaEl.Type),
        end1Releases = GetRestraint(gsaEl.Releases1, gsaEl.Stiffnesses1),
        end2Releases = GetRestraint(gsaEl.Releases2, gsaEl.Stiffnesses1),
        end1Offset = new Vector(),
        end2Offset = new Vector(),
        orientationAngle = 0, //default
        parent = new Base(), //TO DO: add parent
        end1Node = GetNodeFromIndex(gsaEl.NodeIndices[0]),
        end2Node = GetNodeFromIndex(gsaEl.NodeIndices[1]),
        topology = gsaEl.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList(),
        //displayMesh = new Mesh(), //TO DO: add display mesh

        //-- GSA specific --
        colour = gsaEl.Colour.ToString(),
        isDummy = gsaEl.Dummy,
        //action; TO DO: what is this meant to be used for?
      };

      //-- App agnostic --
      if (gsaEl.Index.IsIndex()) speckleElement1d.applicationId = Instance.GsaModel.GetApplicationId<GsaEl>(gsaEl.Index.Value);
      if (gsaEl.PropertyIndex.IsIndex()) speckleElement1d.property = GetProperty1dFromIndex(gsaEl.PropertyIndex.Value);
      if (gsaEl.Angle.HasValue) speckleElement1d.orientationAngle = gsaEl.Angle.Value;
      if (gsaEl.OrientationNodeIndex.IsIndex()) speckleElement1d.orientationNode = GetNodeFromIndex(gsaEl.OrientationNodeIndex.Value);
      speckleElement1d.localAxis = GetLocalAxis(speckleElement1d.end1Node, speckleElement1d.end2Node, speckleElement1d.orientationNode, speckleElement1d.orientationAngle.Radians());
      if (gsaEl.End1OffsetX.HasValue) speckleElement1d.end1Offset.x = gsaEl.End1OffsetX.Value;
      if (gsaEl.OffsetY.HasValue) speckleElement1d.end1Offset.y = gsaEl.OffsetY.Value;
      if (gsaEl.OffsetZ.HasValue) speckleElement1d.end1Offset.z = gsaEl.OffsetZ.Value;
      if (gsaEl.End2OffsetX.HasValue) speckleElement1d.end2Offset.x = gsaEl.End2OffsetX.Value;
      if (gsaEl.OffsetY.HasValue) speckleElement1d.end2Offset.y = gsaEl.OffsetY.Value;
      if (gsaEl.OffsetZ.HasValue) speckleElement1d.end2Offset.z = gsaEl.OffsetZ.Value;

      //-- GSA specific --
      if (gsaEl.Index.IsIndex()) speckleElement1d.nativeId = gsaEl.Index.Value;
      if (gsaEl.Group.IsIndex()) speckleElement1d.group = gsaEl.Group.Value;

      return speckleElement1d;
    }

    private GSAElement2D GsaElement2dToSpeckle(GsaEl gsaEl)
    {
      var speckleElement2d = new GSAElement2D()
      {
        //-- App agnostic --
        name = gsaEl.Name,
        type = (ElementType2D)Enum.Parse(typeof(ElementType2D), gsaEl.Type.ToString()),
        parent = new Base(), //TO DO: add parent
        displayMesh = DisplayMeshElement2d(gsaEl), //TO DO: add display mesh
        baseMesh = new Mesh(), //TO DO: add base mesh
        orientationAngle = 0, //default
        topology = gsaEl.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList(),

        //-- GSA specific --
        colour = gsaEl.Colour.ToString(),
        isDummy = gsaEl.Dummy,
      };

      //-- App agnostic --
      if (gsaEl.Index.IsIndex()) speckleElement2d.applicationId = Instance.GsaModel.GetApplicationId<GsaEl>(gsaEl.Index.Value);
      if (gsaEl.PropertyIndex.IsIndex()) speckleElement2d.property = GetProperty2dFromIndex(gsaEl.PropertyIndex.Value);
      if (gsaEl.OffsetZ.HasValue) speckleElement2d.offset = gsaEl.OffsetZ.Value;
      if (gsaEl.Angle.HasValue) speckleElement2d.orientationAngle = gsaEl.Angle.Value;

      //-- GSA specific --
      if (gsaEl.Index.IsIndex()) speckleElement2d.nativeId = gsaEl.Index.Value;
      if (gsaEl.Group.IsIndex()) speckleElement2d.group = gsaEl.Group.Value;

      return speckleElement2d;
    }

    private GSAElement3D GsaElement3dToSpeckle(GsaEl gsaEl)
    {
      //TODO
      return new GSAElement3D();
    }

    private ToSpeckleResult GsaMemberToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaMemb = (GsaMemb)nativeObject;

      if (gsaMemb.Index.IsIndex())
      {
        if (gsaMemb.Is1dMember()) //1D element
        {
          return new ToSpeckleResult(GsaMember1dToSpeckle(gsaMemb));
        }
        else if (gsaMemb.Is2dMember()) // 2D element
        {
          return new ToSpeckleResult(GsaMember2dToSpeckle(gsaMemb));
        }
        else //3D element
        {
          //TO DO: implement once 3D elements are supported
        }
      }
      return new ToSpeckleResult(false);
    }

    private GSAMember1D GsaMember1dToSpeckle(GsaMemb gsaMemb)
    {
      var speckleMember1d = new GSAMember1D()
      {
        //-- App agnostic --
        name = gsaMemb.Name,
        baseLine = null,
        type = GetMember1dType(gsaMemb.Type),
        end1Releases = GetRestraint(gsaMemb.Releases1, gsaMemb.Stiffnesses1),
        end2Releases = GetRestraint(gsaMemb.Releases2, gsaMemb.Stiffnesses2),
        end1Offset = new Vector(),
        end2Offset = new Vector(),
        orientationAngle = 0, //default
        parent = new Base(),
        end1Node = GetNodeFromIndex(gsaMemb.NodeIndices[0]),
        end2Node = GetNodeFromIndex(gsaMemb.NodeIndices[1]),
        topology = gsaMemb.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList(),
        units = "",

        //-- GSA specific --
        colour = gsaMemb.Colour.ToString(),
        isDummy = gsaMemb.Dummy,
        intersectsWithOthers = gsaMemb.IsIntersector,
      };

      //-- App agnostic --
      if (gsaMemb.Index.IsIndex()) speckleMember1d.applicationId = Instance.GsaModel.GetApplicationId<GsaMemb>(gsaMemb.Index.Value);
      if (gsaMemb.PropertyIndex.IsIndex()) speckleMember1d.property = GetProperty1dFromIndex(gsaMemb.PropertyIndex.Value);
      if (gsaMemb.OrientationNodeIndex.IsIndex()) speckleMember1d.orientationNode = GetNodeFromIndex(gsaMemb.OrientationNodeIndex.Value);
      if (gsaMemb.Angle.HasValue) speckleMember1d.orientationAngle = gsaMemb.Angle.Value;
      speckleMember1d.localAxis = GetLocalAxis(speckleMember1d.end1Node, speckleMember1d.end2Node, speckleMember1d.orientationNode, speckleMember1d.orientationAngle.Radians());
      if (gsaMemb.End1OffsetX.HasValue) speckleMember1d.end1Offset.x = gsaMemb.End1OffsetX.Value;
      if (gsaMemb.OffsetY.HasValue) speckleMember1d.end1Offset.y = gsaMemb.OffsetY.Value;
      if (gsaMemb.OffsetZ.HasValue) speckleMember1d.end1Offset.z = gsaMemb.OffsetZ.Value;
      if (gsaMemb.End2OffsetX.HasValue) speckleMember1d.end2Offset.x = gsaMemb.End2OffsetX.Value;
      if (gsaMemb.OffsetY.HasValue) speckleMember1d.end2Offset.y = gsaMemb.OffsetY.Value;
      if (gsaMemb.OffsetZ.HasValue) speckleMember1d.end2Offset.z = gsaMemb.OffsetZ.Value;

      //-- GSA specific --
      if (gsaMemb.Index.IsIndex()) speckleMember1d.nativeId = gsaMemb.Index.Value;
      if (gsaMemb.Group.IsIndex()) speckleMember1d.group = gsaMemb.Group.Value;
      if (gsaMemb.MeshSize.IsPositive()) speckleMember1d.targetMeshSize = gsaMemb.MeshSize.Value;

      //Unsupported interim schema members
      //gsaMemb.Exposure
      //gsaMemb.Voids
      //gsaMemb.PointNodeIndices
      //gsaMemb.Polylines
      //gsaMemb.AdditionalAreas
      //gsaMemb.AnalysisType
      //gsaMemb.Fire
      //gsaMemb.LimitingTemperature
      //gsaMemb.CreationFromStartDays
      //gsaMemb.StartOfDryingDays
      //gsaMemb.AgeAtLoadingDays
      //gsaMemb.RemovedAtDays
      //gsaMemb.RestraintEnd1
      //gsaMemb.RestraintEnd2
      //gsaMemb.EffectiveLengthType
      //gsaMemb.LoadHeight
      //gsaMemb.LoadHeightReferencePoint
      //gsaMemb.MemberHasOffsets
      //gsaMemb.End1AutomaticOffset
      //gsaMemb.End2AutomaticOffset
      //gsaMemb.EffectiveLengthYY
      //gsaMemb.PercentageYY
      //gsaMemb.EffectiveLengthZZ
      //gsaMemb.PercentageZZ
      //gsaMemb.EffectiveLengthLateralTorsional
      //gsaMemb.FractionLateralTorsional
      //gsaMemb.SpanRestraints
      //gsaMemb.PointRestraints

      return speckleMember1d;
    }

    private GSAMember2D GsaMember2dToSpeckle(GsaMemb gsaMemb)
    {
      var speckleMember2d = new GSAMember2D()
      {
        //-- App agnostic --
        name = gsaMemb.Name,
        type = GetMember2dType(gsaMemb.Type),
        parent = new Base(), //TO DO: add parent
        displayMesh = new Mesh(), //TO DO: add display mesh
        baseMesh = new Mesh(), //TO DO: add base mesh
        orientationAngle = 0, //default
        topology = gsaMemb.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList(),
        units = "",

        //-- GSA specific --
        colour = gsaMemb.Colour.ToString(),
        isDummy = gsaMemb.Dummy,
        intersectsWithOthers = gsaMemb.IsIntersector,
      };

      //-- App agnostic --
      if (gsaMemb.Index.IsIndex()) speckleMember2d.applicationId = Instance.GsaModel.GetApplicationId<GsaMemb>(gsaMemb.Index.Value);
      if (gsaMemb.PropertyIndex.IsIndex()) speckleMember2d.property = GetProperty2dFromIndex(gsaMemb.PropertyIndex.Value);
      if (gsaMemb.Offset2dZ.HasValue) speckleMember2d.offset = gsaMemb.Offset2dZ.Value;
      if (gsaMemb.Angle.HasValue) speckleMember2d.orientationAngle = gsaMemb.Angle.Value;

      //-- GSA specific --
      if (gsaMemb.Index.IsIndex()) speckleMember2d.nativeId = gsaMemb.Index.Value;
      if (gsaMemb.Group.IsIndex()) speckleMember2d.group = gsaMemb.Group.Value;
      if (gsaMemb.MeshSize.IsPositive()) speckleMember2d.targetMeshSize = gsaMemb.MeshSize.Value;

      //Unsupported interim schema members
      //gsaMemb.Exposure
      //gsaMemb.Voids
      //gsaMemb.PointNodeIndices
      //gsaMemb.Polylines
      //gsaMemb.AdditionalAreas
      //gsaMemb.AnalysisType
      //gsaMemb.Fire
      //gsaMemb.LimitingTemperature
      //gsaMemb.CreationFromStartDays
      //gsaMemb.StartOfDryingDays
      //gsaMemb.AgeAtLoadingDays
      //gsaMemb.RemovedAtDays
      //gsaMemb.OffsetAutomaticInternal

      return speckleMember2d;
    }

    private ToSpeckleResult GsaGridLineToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaGridLine = (GsaGridLine)nativeObject;
      var speckleGridLine = new GSAGridLine()
      {
        name = gsaGridLine.Name,
      };
      if (gsaGridLine.Index.IsIndex())
      {
        speckleGridLine.applicationId = Instance.GsaModel.GetApplicationId<GsaGridLine>(gsaGridLine.Index.Value);
        speckleGridLine.nativeId = gsaGridLine.Index.Value;
      }
      if (gsaGridLine.Type == GridLineType.Line) speckleGridLine.line = GetLine(gsaGridLine);
      else if (gsaGridLine.Type == GridLineType.Arc) speckleGridLine.line = GetArc(gsaGridLine);
      return new ToSpeckleResult(speckleGridLine);
    }

    private ToSpeckleResult GsaGridPlaneToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaGridPlane = (GsaGridPlane)nativeObject;
      var speckleGridPlane = new GSAGridPlane()
      {
        name = gsaGridPlane.Name,
        axis = GetGridPlaneAxis(gsaGridPlane.AxisRefType, gsaGridPlane.AxisIndex),
        storeyToleranceBelow = GetStoreyTolerance(gsaGridPlane.StoreyToleranceBelow, gsaGridPlane.StoreyToleranceBelowAuto, gsaGridPlane.Type),
        storeyToleranceAbove = GetStoreyTolerance(gsaGridPlane.StoreyToleranceAbove, gsaGridPlane.StoreyToleranceAboveAuto, gsaGridPlane.Type),
      };
      if (gsaGridPlane.Index.IsIndex())
      {
        speckleGridPlane.applicationId = Instance.GsaModel.GetApplicationId<GsaGridPlane>(gsaGridPlane.Index.Value);
        speckleGridPlane.nativeId = gsaGridPlane.Index.Value;
      }
      if (gsaGridPlane.Elevation.HasValue) speckleGridPlane.elevation = gsaGridPlane.Elevation.Value;

      return new ToSpeckleResult(speckleGridPlane);
    }

    private ToSpeckleResult GsaGridSurfaceToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaGridSurface = (GsaGridSurface)nativeObject;
      var speckleGridSurface = new GSAGridSurface()
      {
        name = gsaGridSurface.Name,
        gridPlane = GetGridPlane(gsaGridSurface.PlaneRefType, gsaGridSurface.PlaneIndex),
        loadExpansion = GetLoadExpansionType(gsaGridSurface.Expansion),
        span = GetSpanType(gsaGridSurface.Span)
      };
      if (gsaGridSurface.Index.IsIndex())
      {
        speckleGridSurface.applicationId = Instance.GsaModel.GetApplicationId<GsaGridSurface>(gsaGridSurface.Index.Value);
        speckleGridSurface.nativeId = gsaGridSurface.Index.Value;
      }
      if (gsaGridSurface.Tolerance.IsPositive()) speckleGridSurface.tolerance = gsaGridSurface.Tolerance.Value;
      if (gsaGridSurface.Angle.HasValue) speckleGridSurface.spanDirection = gsaGridSurface.Angle.Value;
      if (gsaGridSurface.Type == GridSurfaceElementsType.OneD)
      {
        speckleGridSurface.elements = gsaGridSurface.ElementIndices.Select(i => (Base)GetElement1DFromIndex(i)).ToList();
      }
      else if (gsaGridSurface.Type == GridSurfaceElementsType.TwoD)
      {
        speckleGridSurface.elements = gsaGridSurface.ElementIndices.Select(i => (Base)GetElement2DFromIndex(i)).ToList();
      }
      return new ToSpeckleResult(speckleGridSurface);
    }

    private ToSpeckleResult GsaPolylineToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaPolyline = (GsaPolyline)nativeObject;
      var specklePolyline = new GSAPolyline()
      {
        name = gsaPolyline.Name,
        colour = gsaPolyline.Colour.ToString(),
        value = gsaPolyline.Values,
        units = gsaPolyline.Units,
      };
      if (gsaPolyline.Index.IsIndex())
      {
        specklePolyline.nativeId = gsaPolyline.Index.Value;
        specklePolyline.applicationId = Instance.GsaModel.GetApplicationId<GsaPolyline>(gsaPolyline.Index.Value);
      }
      if (gsaPolyline.GridPlaneIndex.IsIndex()) specklePolyline.gridPlane = GetGridPlaneFromIndex(gsaPolyline.GridPlaneIndex.Value);

      return new ToSpeckleResult(specklePolyline); 
    }
    #endregion

    #region Loading
    private ToSpeckleResult GsaLoadCaseToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoadCase = (GsaLoadCase)nativeObject;
      //TO DO: update once GsaLoadCase has been updated
      var speckleLoadCase = new GSALoadCase()
      {
        //-- App agnostic --
        name = gsaLoadCase.Title,
        loadType = GetLoadType(gsaLoadCase.CaseType),
        source = "",
        actionType = ActionType.None,
        description = ""
      };

      //-- App agnostic --
      if (gsaLoadCase.Index.IsIndex()) speckleLoadCase.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadCase>(gsaLoadCase.Index.Value);

      //-- GSA specific --
      if (gsaLoadCase.Index.IsIndex()) speckleLoadCase.nativeId = gsaLoadCase.Index.Value;

      return new ToSpeckleResult(speckleLoadCase);
    }

    private ToSpeckleResult GsaFaceLoadToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoad2dFace = (GsaLoad2dFace)nativeObject;
      var speckleFaceLoad = new GSAFaceLoad()
      {
        //-- App agnostic --
        name = gsaLoad2dFace.Name,
        elements = gsaLoad2dFace.ElementIndices.Select(i => (Base)GetElement2DFromIndex(i)).ToList(),
        loadType = GetAreaLoadType(gsaLoad2dFace.Type),
        direction = GetDirection(gsaLoad2dFace.LoadDirection),
        loadAxisType = GetLoadAxisType(gsaLoad2dFace.AxisRefType),
        isProjected = gsaLoad2dFace.Projected,
        values = gsaLoad2dFace.Values,
      };

      //-- App agnostic --
      if (gsaLoad2dFace.Index.IsIndex()) speckleFaceLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoad2dFace>(gsaLoad2dFace.Index.Value);
      if (gsaLoad2dFace.LoadCaseIndex.IsIndex()) speckleFaceLoad.loadCase = GetLoadCaseFromIndex(gsaLoad2dFace.LoadCaseIndex.Value);
      if (gsaLoad2dFace.AxisRefType == AxisRefType.Reference && gsaLoad2dFace.AxisIndex.IsIndex())
      {
        speckleFaceLoad.loadAxis = GetAxisFromIndex(gsaLoad2dFace.AxisIndex.Value);
      }
      if (gsaLoad2dFace.Type == Load2dFaceType.Point && gsaLoad2dFace.R.HasValue && gsaLoad2dFace.S.HasValue)
      {
        speckleFaceLoad.positions = new List<double>() { gsaLoad2dFace.R.Value, gsaLoad2dFace.S.Value };
      }

      //-- GSA specific --
      if (gsaLoad2dFace.Index.IsIndex()) speckleFaceLoad.nativeId = gsaLoad2dFace.Index.Value;

      return new ToSpeckleResult(speckleFaceLoad);
    }

    private ToSpeckleResult GsaBeamLoadToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoadBeam = (GsaLoadBeam)nativeObject;
      var type = gsaLoadBeam.GetType();
      var speckleBeamLoad = new GSABeamLoad()
      {
        //-- App agnostic --
        name = gsaLoadBeam.Name,
        elements = gsaLoadBeam.ElementIndices.Select(i => (Base)GetElement1DFromIndex(i)).ToList(),
        loadType = GetBeamLoadType(type),
        direction = GetDirection(gsaLoadBeam.LoadDirection),
        loadAxisType = GetLoadAxisType(gsaLoadBeam.AxisRefType),
        isProjected = gsaLoadBeam.Projected,
      };

      //-- App agnostic --
      if (gsaLoadBeam.Index.IsIndex()) speckleBeamLoad.applicationId = Instance.GsaModel.Cache.GetApplicationId(type, gsaLoadBeam.Index.Value);
      if (gsaLoadBeam.LoadCaseIndex.IsIndex()) speckleBeamLoad.loadCase = GetLoadCaseFromIndex(gsaLoadBeam.LoadCaseIndex.Value);
      if (gsaLoadBeam.AxisRefType == LoadBeamAxisRefType.Reference && gsaLoadBeam.AxisIndex.IsIndex())
      {
        speckleBeamLoad.loadAxis = GetAxisFromIndex(gsaLoadBeam.AxisIndex.Value);
      }
      if (GetLoadBeamValues(gsaLoadBeam, out var v)) speckleBeamLoad.values = v;
      if (GetLoadBeamPositions(gsaLoadBeam, out var p)) speckleBeamLoad.positions = p;

      //-- GSA specific --
      if (gsaLoadBeam.Index.IsIndex()) speckleBeamLoad.nativeId = gsaLoadBeam.Index.Value;

      return new ToSpeckleResult(speckleBeamLoad);
    }

    private ToSpeckleResult GsaNodeLoadToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoadNode = (GsaLoadNode)nativeObject;
      var speckleNodeLoad = new GSANodeLoad()
      {
        //-- App agnostic --
        name = gsaLoadNode.Name,
        direction = GetDirection(gsaLoadNode.LoadDirection),
        nodes = gsaLoadNode.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList(),
      };

      //-- App agnostic --
      if (gsaLoadNode.Index.IsIndex()) speckleNodeLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadNode>(gsaLoadNode.Index.Value);
      if (gsaLoadNode.LoadCaseIndex.IsIndex()) speckleNodeLoad.loadCase = GetLoadCaseFromIndex(gsaLoadNode.LoadCaseIndex.Value);
      if (gsaLoadNode.Value.HasValue) speckleNodeLoad.value = gsaLoadNode.Value.Value;
      if (gsaLoadNode.GlobalAxis)
      {
        speckleNodeLoad.loadAxis = GlobalAxis();
      }
      else if (gsaLoadNode.AxisIndex.IsIndex())
      {
        speckleNodeLoad.loadAxis = GetAxisFromIndex(gsaLoadNode.AxisIndex.Value);
      }

      //-- GSA specific --
      if (gsaLoadNode.Index.IsIndex()) speckleNodeLoad.nativeId = gsaLoadNode.Index.Value;

      return new ToSpeckleResult(speckleNodeLoad);
    }

    private ToSpeckleResult GsaGravityLoadToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoadGravity = (GsaLoadGravity)nativeObject;
      var speckleGravityLoad = new GSAGravityLoad()
      {
        //-- App agnostic --
        name = gsaLoadGravity.Name,
        elements = gsaLoadGravity.ElementIndices.Select(i => GetElementFromIndex(i)).ToList(),
        nodes = gsaLoadGravity.Nodes.Select(i => (Base)GetNodeFromIndex(i)).ToList(),
        gravityFactors = GetGravityFactors(gsaLoadGravity),
      };

      //-- App agnostic --
      if (gsaLoadGravity.Index.IsIndex()) speckleGravityLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadGravity>(gsaLoadGravity.Index.Value);
      if (gsaLoadGravity.LoadCaseIndex.IsIndex()) speckleGravityLoad.loadCase = GetLoadCaseFromIndex(gsaLoadGravity.LoadCaseIndex.Value);

      //-- GSA specific --
      if (gsaLoadGravity.Index.IsIndex()) speckleGravityLoad.nativeId = gsaLoadGravity.Index.Value;

      return new ToSpeckleResult(speckleGravityLoad);
    }

    private ToSpeckleResult GsaLoadCombinationToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaCombination = (GsaCombination)nativeObject;
      var speckleLoadCombination = new GSALoadCombination()
      {
        //-- App agnostic --
        name = gsaCombination.Name,
        caseFactors = GetLoadCombinationFactors(gsaCombination.Desc),
        combinationType = GetCombinationType(gsaCombination.Desc)
      };

      //-- App agnostic --
      if (gsaCombination.Index.IsIndex()) speckleLoadCombination.applicationId = Instance.GsaModel.GetApplicationId<GsaCombination>(gsaCombination.Index.Value);

      //-- GSA specific --
      if (gsaCombination.Index.IsIndex()) speckleLoadCombination.nativeId = gsaCombination.Index.Value;

      return new ToSpeckleResult(speckleLoadCombination);
    }

    private ToSpeckleResult GsaThermal2dLoadToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoad2dThermal = (GsaLoad2dThermal)nativeObject;
      var speckleLoad = new GSAThermal2dLoad()
      {
        name = gsaLoad2dThermal.Name,
        elements = gsaLoad2dThermal.ElementIndices.Select(i => GetElement2DFromIndex(i)).ToList(),
        type = GetLoad2dThermalType(gsaLoad2dThermal.Type),
        values = gsaLoad2dThermal.Values,
      };
      if (gsaLoad2dThermal.Index.IsIndex())
      {
        speckleLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoad2dThermal>(gsaLoad2dThermal.Index.Value);
        speckleLoad.nativeId = gsaLoad2dThermal.Index.Value;
      }
      if (gsaLoad2dThermal.LoadCaseIndex.IsIndex()) speckleLoad.loadCase = GetLoadCaseFromIndex(gsaLoad2dThermal.LoadCaseIndex.Value);
      return new ToSpeckleResult(speckleLoad);
    }

    private ToSpeckleResult GsaLoadGridAreaToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoad = (GsaLoadGridArea)nativeObject;
      var speckleLoad = new GSAGridAreaLoad()
      {
        name = gsaLoad.Name,
        loadAxis = GetAxis(gsaLoad.AxisRefType, gsaLoad.AxisIndex),
        isProjected = gsaLoad.Projected,
        direction = GetDirection(gsaLoad.LoadDirection),
        polyline = GetPolyline(gsaLoad.Area, gsaLoad.Polygon, gsaLoad.PolygonIndex),
      };
      if (gsaLoad.Index.IsIndex())
      {
        speckleLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadGridArea>(gsaLoad.Index.Value);
        speckleLoad.nativeId = gsaLoad.Index.Value;
      }
      if (gsaLoad.GridSurfaceIndex.IsIndex()) speckleLoad.gridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) speckleLoad.loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.Value.HasValue) speckleLoad.value = gsaLoad.Value.Value;
      return new ToSpeckleResult(speckleLoad);
    }

    private ToSpeckleResult GsaLoadGridLineToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoad = (GsaLoadGridLine)nativeObject;
      var speckleLoad = new GSAGridLineLoad()
      {
        name = gsaLoad.Name,
        loadAxis = GetAxis(gsaLoad.AxisRefType, gsaLoad.AxisIndex),
        isProjected = gsaLoad.Projected,
        direction = GetDirection(gsaLoad.LoadDirection),
        polyline = GetPolyline(gsaLoad.Line, gsaLoad.Polygon, gsaLoad.PolygonIndex),
      };
      if (gsaLoad.Index.IsIndex())
      {
        speckleLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadGridLine>(gsaLoad.Index.Value);
        speckleLoad.nativeId = gsaLoad.Index.Value;
      }
      if (gsaLoad.GridSurfaceIndex.IsIndex()) speckleLoad.gridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) speckleLoad.loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.Value1.HasValue && gsaLoad.Value2.HasValue) speckleLoad.values = new List<double>() { gsaLoad.Value1.Value, gsaLoad.Value2.Value };
      return new ToSpeckleResult(speckleLoad);
    }

    private ToSpeckleResult GsaLoadGridPointToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaLoad = (GsaLoadGridPoint)nativeObject;
      var speckleLoad = new GSAGridPointLoad()
      {
        name = gsaLoad.Name,
        loadAxis = GetAxis(gsaLoad.AxisRefType, gsaLoad.AxisIndex),
        direction = GetDirection(gsaLoad.LoadDirection),
      };
      if (gsaLoad.Index.IsIndex())
      {
        speckleLoad.applicationId = Instance.GsaModel.GetApplicationId<GsaLoadGridPoint>(gsaLoad.Index.Value);
        speckleLoad.nativeId = gsaLoad.Index.Value;
      }
      if (gsaLoad.GridSurfaceIndex.IsIndex()) speckleLoad.gridSurface = GetGridSurfaceFromIndex(gsaLoad.GridSurfaceIndex.Value);
      if (gsaLoad.LoadCaseIndex.IsIndex()) speckleLoad.loadCase = GetLoadCaseFromIndex(gsaLoad.LoadCaseIndex.Value);
      if (gsaLoad.Value.HasValue) speckleLoad.value = gsaLoad.Value.Value;
      if (gsaLoad.X.HasValue && gsaLoad.Y.HasValue) speckleLoad.position = new Point(gsaLoad.X.Value, gsaLoad.Y.Value, 0);
      return new ToSpeckleResult(speckleLoad);
    }
    #endregion

    #region Materials

    private ToSpeckleResult GsaMaterialSteelToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
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
      var gsaSteel = (GsaMatSteel)nativeObject;
      var speckleSteel = new Steel()
      {
        name = gsaSteel.Name,
        grade = "",                                 //grade can be determined from gsaMatSteel.Mat.Name (assuming the user doesn't change the default value): e.g. "350(AS3678)"
        type = MaterialType.Steel,
        designCode = "",                            //designCode can be determined from SPEC_STEEL_DESIGN gwa keyword
        codeYear = "",                              //codeYear can be determined from SPEC_STEEL_DESIGN gwa keyword
        yieldStrength = gsaSteel.Fy.Value,
        ultimateStrength = gsaSteel.Fu.Value,
        maxStrain = gsaSteel.EpsP.Value
      };
      if (gsaSteel.Index.IsIndex()) speckleSteel.applicationId = Instance.GsaModel.GetApplicationId<GsaMatSteel>(gsaSteel.Index.Value);

      //the following properties are stored in multiple locations in GSA
      if (Choose(gsaSteel.Mat.E, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.E, out var E)) speckleSteel.youngsModulus = E;
      if (Choose(gsaSteel.Mat.Nu, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.Nu, out var Nu)) speckleSteel.poissonsRatio = Nu;
      if (Choose(gsaSteel.Mat.G, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.G, out var G)) speckleSteel.shearModulus = G;
      if (Choose(gsaSteel.Mat.Rho, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.Rho, out var Rho)) speckleSteel.density = Rho;
      if (Choose(gsaSteel.Mat.Alpha, gsaSteel.Mat.Prop == null ? null : gsaSteel.Mat.Prop.Alpha, out var Alpha)) speckleSteel.thermalExpansivity = Alpha;

      return new ToSpeckleResult(speckleSteel);

      /*public string Name { get => name; set { name = value; } }
    public GsaMat Mat;
    public double? Fy;
    public double? Fu;
    public double? EpsP;
    public double? Eh;*/
    }

    private ToSpeckleResult GsaMaterialConcreteToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      //Currently only handles isotropic concrete properties.
      //A lot of information in the gsa objects are currently ignored.

      var gsaConcrete = (GsaMatConcrete)nativeObject;
      var speckleConcrete = new Concrete()
      {
        name = gsaConcrete.Name,
        grade = "",                                 //grade can be determined from gsaMatConcrete.Mat.Name (assuming the user doesn't change the default value): e.g. "32 MPa"
        type = MaterialType.Concrete,
        designCode = "",                            //designCode can be determined from SPEC_CONCRETE_DESIGN gwa keyword: e.g. "AS3600_18" -> "AS3600"
        codeYear = "",                              //codeYear can be determined from SPEC_CONCRETE_DESIGN gwa keyword: e.g. "AS3600_18" - "2018"
        flexuralStrength = 0
      };
      if (gsaConcrete.Index.IsIndex()) speckleConcrete.applicationId = Instance.GsaModel.GetApplicationId<GsaMatConcrete>(gsaConcrete.Index.Value);

      //the following properties might be null
      if (gsaConcrete.Fc.HasValue) speckleConcrete.compressiveStrength = gsaConcrete.Fc.Value;
      if (gsaConcrete.EpsU.HasValue) speckleConcrete.maxStrain = gsaConcrete.EpsU.Value;
      if (gsaConcrete.Agg.HasValue) speckleConcrete.maxAggregateSize = gsaConcrete.Agg.Value;
      if (gsaConcrete.Fcdt.HasValue) speckleConcrete.tensileStrength = gsaConcrete.Fcdt.Value;

      //the following properties are stored in multiple locations in GSA
      if (Choose(gsaConcrete.Mat.E, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.E, out var E)) speckleConcrete.youngsModulus = E;
      if (Choose(gsaConcrete.Mat.Nu, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.Nu, out var Nu)) speckleConcrete.poissonsRatio = Nu;
      if (Choose(gsaConcrete.Mat.G, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.G, out var G)) speckleConcrete.shearModulus = G;
      if (Choose(gsaConcrete.Mat.Rho, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.Rho, out var Rho)) speckleConcrete.density = Rho;
      if (Choose(gsaConcrete.Mat.Alpha, gsaConcrete.Mat.Prop == null ? null : gsaConcrete.Mat.Prop.Alpha, out var Alpha)) speckleConcrete.thermalExpansivity = Alpha;

      return new ToSpeckleResult(speckleConcrete);
    }

    //Timber: GSA keyword not supported yet
    #endregion

    #region Property
    private ToSpeckleResult GsaSectionToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaSection = (GsaSection)nativeObject;
      //TO DO: update code to handle modifiers once SECTION_MOD (or SECTION_ANAL) keyword is supported
      var speckleProperty1D = new GSAProperty1D()
      {
        //-- App agnostic --
        name = gsaSection.Name,
        memberType = MemberType.Generic1D,
        referencePoint = GetReferencePoint(gsaSection.ReferencePoint),

        //-- GSA specific --
        colour = gsaSection.Colour.ToString(),
        //designMaterial = new Material(), // what is this used for? how is it different to material?
      };

      //-- App agnostic --
      if (gsaSection.Index.IsIndex()) speckleProperty1D.applicationId = Instance.GsaModel.GetApplicationId<GsaSection>(gsaSection.Index.Value);
      if (gsaSection.RefY.HasValue) speckleProperty1D.offsetY = gsaSection.RefY.Value;
      if (gsaSection.RefZ.HasValue) speckleProperty1D.offsetZ = gsaSection.RefZ.Value;
      var gsaSectionComp = (SectionComp)gsaSection.Components.Find(x => x.GetType() == typeof(SectionComp));
      if (gsaSectionComp.MaterialIndex.IsIndex()) speckleProperty1D.material = GetMaterialFromIndex(gsaSectionComp.MaterialIndex.Value, gsaSectionComp.MaterialType);
      var fns = new Dictionary<Section1dProfileGroup, Func<ProfileDetails, SectionProfile>>
      { { Section1dProfileGroup.Catalogue, GetProfileCatalogue },
        { Section1dProfileGroup.Explicit, GetProfileExplicit },
        { Section1dProfileGroup.Perimeter, GetProfilePerimeter },
        { Section1dProfileGroup.Standard, GetProfileStandard }
      };
      if (fns.ContainsKey(gsaSectionComp.ProfileGroup)) speckleProperty1D.profile = fns[gsaSectionComp.ProfileGroup](gsaSectionComp.ProfileDetails);

      //-- GSA specific --
      if (gsaSection.Index.IsIndex()) speckleProperty1D.nativeId = gsaSection.Index.Value;
      if (gsaSection.Mass.HasValue) speckleProperty1D.additionalMass = gsaSection.Mass.Value;
      if (gsaSection.Cost.HasValue) speckleProperty1D.cost = gsaSection.Cost.Value;
      if (gsaSection.PoolIndex.IsIndex()) speckleProperty1D.poolRef = gsaSection.PoolIndex.Value;

      return new ToSpeckleResult(speckleProperty1D);
    }

    private ToSpeckleResult GsaProperty2dToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaProp2d = (GsaProp2d)nativeObject;
      var speckleProperty2d = new GSAProperty2D()
      {
        //-- App agnostic --
        name = gsaProp2d.Name,
        colour = gsaProp2d.Colour.ToString(),
        zOffset = gsaProp2d.RefZ,
        orientationAxis = GetOrientationAxis(gsaProp2d),
        refSurface = GetReferenceSurface(gsaProp2d),

        //-- GSA specific --
        additionalMass = gsaProp2d.Mass,
        concreteSlabProp = gsaProp2d.Profile,
        //designMaterial = new Material(), // what is this used for? how is it different to material?
        //cost = 0, //cost is not part of the GWA
      };

      //-- App agnostic --
      if (gsaProp2d.Index.IsIndex()) speckleProperty2d.applicationId = Instance.GsaModel.GetApplicationId<GsaProp2d>(gsaProp2d.Index.Value);
      if (gsaProp2d.Thickness.IsPositive()) speckleProperty2d.thickness = gsaProp2d.Thickness.Value;
      if (gsaProp2d.GradeIndex.IsIndex()) speckleProperty2d.material = GetMaterialFromIndex(gsaProp2d.GradeIndex.Value, gsaProp2d.MatType);
      if (gsaProp2d.Type != Property2dType.NotSet) speckleProperty2d.type = (PropertyType2D)Enum.Parse(typeof(PropertyType2D), gsaProp2d.Type.ToString());
      if (gsaProp2d.InPlaneStiffnessPercentage.HasValue) speckleProperty2d.modifierInPlane = gsaProp2d.InPlaneStiffnessPercentage.Value;  //Only supporting Percentage modifiers
      if (gsaProp2d.BendingStiffnessPercentage.HasValue) speckleProperty2d.modifierBending = gsaProp2d.BendingStiffnessPercentage.Value;
      if (gsaProp2d.ShearStiffnessPercentage.HasValue) speckleProperty2d.modifierShear = gsaProp2d.ShearStiffnessPercentage.Value;
      if (gsaProp2d.VolumePercentage.HasValue) speckleProperty2d.modifierVolume = gsaProp2d.VolumePercentage.Value;

      //-- GSA specific --
      if (gsaProp2d.Index.IsIndex()) speckleProperty2d.nativeId = gsaProp2d.Index.Value;

      return new ToSpeckleResult(speckleProperty2d);
    }

    //Property3D: GSA keyword not supported yet

    private ToSpeckleResult GsaPropertyMassToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaPropMass = (GsaPropMass)nativeObject;
      var specklePropertyMass = new PropertyMass()
      {
        name = gsaPropMass.Name,
        mass = gsaPropMass.Mass,
        inertiaXX = gsaPropMass.Ixx,
        inertiaYY = gsaPropMass.Iyy,
        inertiaZZ = gsaPropMass.Izz,
        inertiaXY = gsaPropMass.Ixy,
        inertiaYZ = gsaPropMass.Iyz,
        inertiaZX = gsaPropMass.Izx
      };
      if (gsaPropMass.Index.IsIndex()) specklePropertyMass.applicationId = Instance.GsaModel.GetApplicationId<GsaPropMass>(gsaPropMass.Index.Value);

      //Mass modifications
      if (gsaPropMass.Mod == MassModification.Modified)
      {
        specklePropertyMass.massModified = true;
        if (gsaPropMass.ModXPercentage.IsPositive()) specklePropertyMass.massModifierX = gsaPropMass.ModXPercentage.Value;
        if (gsaPropMass.ModYPercentage.IsPositive()) specklePropertyMass.massModifierY = gsaPropMass.ModYPercentage.Value;
        if (gsaPropMass.ModZPercentage.IsPositive()) specklePropertyMass.massModifierZ = gsaPropMass.ModZPercentage.Value;
      }
      else
      {
        specklePropertyMass.massModified = false;
      }

      return new ToSpeckleResult(specklePropertyMass);
    }

    private ToSpeckleResult GsaPropertySpringToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaPropSpr = (GsaPropSpr)nativeObject;
      //Apply properties common to all spring types
      var specklePropertySpring = new PropertySpring()
      {
        name = gsaPropSpr.Name,
        dampingRatio = gsaPropSpr.DampingRatio.Value
      };
      if (gsaPropSpr.Index.IsIndex()) specklePropertySpring.applicationId = Instance.GsaModel.GetApplicationId<GsaPropSpr>(gsaPropSpr.Index.Value);

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
        //CONNECT not yet supported
      };

      //Apply spring type specific properties
      if (fns.ContainsKey(gsaPropSpr.PropertyType)) fns[gsaPropSpr.PropertyType](gsaPropSpr, specklePropertySpring);

      return new ToSpeckleResult(specklePropertySpring);
    }

    //PropertyDamper: GSA keyword not supported yet
    #endregion

    #region Results
    private bool GsaNodeResultToSpeckle(int gsaNodeIndex, Node speckleNode, out List<ResultNode> speckleResults)
    {
      speckleResults = null;
      if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Node, gsaNodeIndex, out var csvRecords))
      {
        speckleResults = new List<ResultNode>();
        var gsaNodeResults = csvRecords.FindAll(so => so is CsvNode).Select(so => (CsvNode)so).ToList();
        foreach (var gsaResult in gsaNodeResults)
        {
          var result = new ResultNode()
          {
            description = "", //???
            permutation = "", //???
            node = speckleNode,
          };

          var gsaCaseIndex = Convert.ToInt32(gsaResult.CaseId.Substring(1));
          if (gsaResult.CaseId[0] == 'A')
          {
            result.resultCase = GetLoadCaseFromIndex(gsaCaseIndex);
          }
          else if (gsaResult.CaseId[0] == 'C')
          {
            result.resultCase = GetLoadCombinationFromIndex(gsaCaseIndex);
          }
          result.applicationId = speckleNode.applicationId + "_" + result.resultCase.applicationId;

          //displacements / rotations
          if (gsaResult.Ux.HasValue) result.dispX = gsaResult.Ux.Value;
          if (gsaResult.Uy.HasValue) result.dispY = gsaResult.Uy.Value;
          if (gsaResult.Uz.HasValue) result.dispZ = gsaResult.Uz.Value;
          if (gsaResult.Rxx.HasValue) result.rotXX = gsaResult.Rxx.Value;
          if (gsaResult.Ryy.HasValue) result.rotYY = gsaResult.Ryy.Value;
          if (gsaResult.Rzz.HasValue) result.rotZZ = gsaResult.Rzz.Value;

          //velocities
          if (gsaResult.Vx.HasValue) result.velX = gsaResult.Vx.Value;
          if (gsaResult.Vy.HasValue) result.velY = gsaResult.Vy.Value;
          if (gsaResult.Vz.HasValue) result.velZ = gsaResult.Vz.Value;
          if (gsaResult.Vxx.HasValue) result.velXX = gsaResult.Vxx.Value;
          if (gsaResult.Vyy.HasValue) result.velYY = gsaResult.Vyy.Value;
          if (gsaResult.Vzz.HasValue) result.velZZ = gsaResult.Vzz.Value;

          //accelerations
          if (gsaResult.Ax.HasValue) result.accX = gsaResult.Ax.Value;
          if (gsaResult.Ay.HasValue) result.accY = gsaResult.Ay.Value;
          if (gsaResult.Az.HasValue) result.accZ = gsaResult.Az.Value;
          if (gsaResult.Axx.HasValue) result.accXX = gsaResult.Axx.Value;
          if (gsaResult.Ayy.HasValue) result.accYY = gsaResult.Ayy.Value;
          if (gsaResult.Azz.HasValue) result.accZZ = gsaResult.Azz.Value;

          //reactions forces/moments
          if (gsaResult.Fx_Reac.HasValue) result.reactionX = gsaResult.Fx_Reac.Value;
          if (gsaResult.Fy_Reac.HasValue) result.reactionY = gsaResult.Fy_Reac.Value;
          if (gsaResult.Fz_Reac.HasValue) result.reactionZ = gsaResult.Fz_Reac.Value;
          if (gsaResult.Mxx_Reac.HasValue) result.reactionXX = gsaResult.Mxx_Reac.Value;
          if (gsaResult.Myy_Reac.HasValue) result.reactionYY = gsaResult.Myy_Reac.Value;
          if (gsaResult.Mzz_Reac.HasValue) result.reactionZZ = gsaResult.Mzz_Reac.Value;

          //constraint forces/moments
          if (gsaResult.Fx_Cons.HasValue) result.constraintX = gsaResult.Fx_Cons.Value;
          if (gsaResult.Fy_Cons.HasValue) result.constraintY = gsaResult.Fy_Cons.Value;
          if (gsaResult.Fz_Cons.HasValue) result.constraintZ = gsaResult.Fz_Cons.Value;
          if (gsaResult.Mxx_Cons.HasValue) result.constraintXX = gsaResult.Mxx_Cons.Value;
          if (gsaResult.Myy_Cons.HasValue) result.constraintYY = gsaResult.Myy_Cons.Value;
          if (gsaResult.Mzz_Cons.HasValue) result.constraintZZ = gsaResult.Mzz_Cons.Value;

          speckleResults.Add(result);
        }
        return true;
      }
      return false;
    }

    public bool GsaElement1dResultToSpeckle(int gsaElementIndex, Element1D speckleElement, out List<Result1D> speckleResults)
    {
      speckleResults = null;
      //Instance.GsaModel.Result1DNumPosition = 2;
      if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Element1d, gsaElementIndex, out var csvRecord))
      {
        speckleResults = new List<Result1D>();
        var gsaElement1dResults = csvRecord.FindAll(so => so is CsvElem1d).Select(so => (CsvElem1d)so).ToList();
        foreach (var gsaResult in gsaElement1dResults)
        {
          var result = new Result1D()
          {
            description = "", //???
            permutation = "", //???
            element = speckleElement,
            position = gsaResult.PosR.ToDouble(),
          };

          var gsaCaseIndex = Convert.ToInt32(gsaResult.CaseId.Substring(1));
          if (gsaResult.CaseId[0] == 'A')
          {
            result.resultCase = GetLoadCaseFromIndex(gsaCaseIndex);
          }
          else if (gsaResult.CaseId[0] == 'C')
          {
            result.resultCase = GetLoadCombinationFromIndex(gsaCaseIndex);
          }
          result.applicationId = speckleElement.applicationId + "_" + result.resultCase.applicationId + "_" + result.position.ToString();

          //displacements
          if (gsaResult.Ux.HasValue) result.dispX = gsaResult.Ux.Value;
          if (gsaResult.Uy.HasValue) result.dispY = gsaResult.Uy.Value;
          if (gsaResult.Uz.HasValue) result.dispZ = gsaResult.Uz.Value;

          //forces
          if (gsaResult.Fx.HasValue) result.forceX = gsaResult.Fx.Value;
          if (gsaResult.Fy.HasValue) result.forceY = gsaResult.Fy.Value;
          if (gsaResult.Fz.HasValue) result.forceZ = gsaResult.Fz.Value;

          //moments
          if (gsaResult.Mxx.HasValue) result.momentXX = gsaResult.Mxx.Value;
          if (gsaResult.Myy.HasValue) result.momentYY = gsaResult.Myy.Value;
          if (gsaResult.Mzz.HasValue) result.momentZZ = gsaResult.Mzz.Value;

          speckleResults.Add(result);
        }
        return true;
      }
      return false;
    }

    public bool GsaElement2dResultToSpeckle(int gsaElementIndex, Element2D speckleElement, out List<Result2D> speckleResults)
    {
      speckleResults = null;
      if (Instance.GsaModel.Proxy.GetResultRecords(ResultGroup.Element2d, gsaElementIndex, out var csvRecord))
      {
        speckleResults = new List<Result2D>();
        var gsaElement2dResults = csvRecord.FindAll(so => so is CsvElem2d).Select(so => (CsvElem2d)so).ToList();
        foreach (var gsaResult in gsaElement2dResults)
        {
          var result = new Result2D()
          {
            description = "", //???
            permutation = "", //???
            element = speckleElement,
          };

          if (gsaResult.PosR.HasValue && gsaResult.PosS.HasValue) result.position = new List<double>() { gsaResult.PosR.Value, gsaResult.PosS.Value };

          var gsaCaseIndex = Convert.ToInt32(gsaResult.CaseId.Substring(1));
          if (gsaResult.CaseId[0] == 'A')
          {
            result.resultCase = GetLoadCaseFromIndex(gsaCaseIndex);
          }
          else if (gsaResult.CaseId[0] == 'C')
          {
            result.resultCase = GetLoadCombinationFromIndex(gsaCaseIndex);
          }
          result.applicationId = speckleElement.applicationId + "_" + result.resultCase.applicationId + "_" + result.position[0].ToString() + "_" + result.position[1].ToString();

          //displacements
          if (gsaResult.Ux.HasValue) result.dispX = gsaResult.Ux.Value;
          if (gsaResult.Uy.HasValue) result.dispY = gsaResult.Uy.Value;
          if (gsaResult.Uz.HasValue) result.dispZ = gsaResult.Uz.Value;

          //forces
          if (gsaResult.Nx.HasValue) result.forceXX = gsaResult.Nx.Value;
          if (gsaResult.Ny.HasValue) result.forceYY = gsaResult.Ny.Value;
          if (gsaResult.Nxy.HasValue) result.forceXY = gsaResult.Nxy.Value;
          if (gsaResult.Qx.HasValue) result.shearX = gsaResult.Qx.Value;
          if (gsaResult.Qy.HasValue) result.shearY = gsaResult.Qy.Value;

          //moments
          if (gsaResult.Mx.HasValue) result.momentXX = gsaResult.Mx.Value;
          if (gsaResult.My.HasValue) result.momentYY = gsaResult.My.Value;
          if (gsaResult.Mxy.HasValue) result.momentXY = gsaResult.Mxy.Value;

          //stresses
          if (gsaResult.Xx_b.HasValue) result.stressBotXX = gsaResult.Xx_b.Value;
          if (gsaResult.Yy_b.HasValue) result.stressBotYY = gsaResult.Yy_b.Value;
          if (gsaResult.Zz_b.HasValue) result.stressBotZZ = gsaResult.Zz_b.Value;
          if (gsaResult.Xy_b.HasValue) result.stressBotXY = gsaResult.Xy_b.Value;
          if (gsaResult.Yz_b.HasValue) result.stressBotYZ = gsaResult.Yz_b.Value;
          if (gsaResult.Zx_b.HasValue) result.stressBotZX = gsaResult.Zx_b.Value;
          if (gsaResult.Xx_m.HasValue) result.stressMidXX = gsaResult.Xx_m.Value;
          if (gsaResult.Yy_m.HasValue) result.stressMidYY = gsaResult.Yy_m.Value;
          if (gsaResult.Zz_m.HasValue) result.stressMidZZ = gsaResult.Zz_m.Value;
          if (gsaResult.Xy_m.HasValue) result.stressMidXY = gsaResult.Xy_m.Value;
          if (gsaResult.Yz_m.HasValue) result.stressMidYZ = gsaResult.Yz_m.Value;
          if (gsaResult.Zx_m.HasValue) result.stressMidZX = gsaResult.Zx_m.Value;
          if (gsaResult.Xx_t.HasValue) result.stressTopXX = gsaResult.Xx_t.Value;
          if (gsaResult.Yy_t.HasValue) result.stressTopYY = gsaResult.Yy_t.Value;
          if (gsaResult.Zz_t.HasValue) result.stressTopZZ = gsaResult.Zz_t.Value;
          if (gsaResult.Xy_t.HasValue) result.stressTopXY = gsaResult.Xy_t.Value;
          if (gsaResult.Yz_t.HasValue) result.stressTopYZ = gsaResult.Yz_t.Value;
          if (gsaResult.Zx_t.HasValue) result.stressTopZX = gsaResult.Zx_t.Value;

          speckleResults.Add(result);
        }
        return true;
      }
      return false;
    }

    public bool GsaElement3dResultToSpeckle(int gsaElementIndex, Element3D speckleElement, out List<Result3D> speckleResults)
    {
      //TO DO: update when 3D elements are supported
      speckleResults = null;
      return false;
    }

    //TODO: implement conversion code for result objects
    /* ResultGlobal
     */
    #endregion

    #region Constraints
    private ToSpeckleResult GsaRigidToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaRigid = (GsaRigid)nativeObject;
      var speckleRigid = new GSARigid()
      {
        name = gsaRigid.Name,
        constrainedNodes = gsaRigid.ConstrainedNodes.Select(i => GetNodeFromIndex(i)).ToList(),
        stages = gsaRigid.Stage.Select(i => GetStageFromIndex(i)).ToList(),
        type = GetRigidConstraintType(gsaRigid.Type)
      };
      if (gsaRigid.Index.IsIndex())
      {
        speckleRigid.nativeId = gsaRigid.Index.Value;
        speckleRigid.applicationId = Instance.GsaModel.GetApplicationId<GsaRigid>(gsaRigid.Index.Value);
      }
      if (gsaRigid.PrimaryNode.IsIndex()) speckleRigid.primaryNode = GetNodeFromIndex(gsaRigid.PrimaryNode.Value);
      if (gsaRigid.Type == RigidConstraintType.Custom) speckleRigid.link = GetRigidConstraint(gsaRigid.Link);
      return new ToSpeckleResult(speckleRigid); 
    }

    private ToSpeckleResult GsaGenRestToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaGenRest = (GsaGenRest)nativeObject;
      var speckleGenRest = new GSAGenRest()
      {
        name = gsaGenRest.Name,
        restraint = GetRestraint(gsaGenRest),
        nodes = gsaGenRest.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList(),
        stages = gsaGenRest.StageIndices.Select(i => GetStageFromIndex(i)).ToList(),
      };
      if (gsaGenRest.Index.IsIndex())
      {
        speckleGenRest.nativeId = gsaGenRest.Index.Value;
        speckleGenRest.applicationId = Instance.GsaModel.GetApplicationId<GsaGenRest>(gsaGenRest.Index.Value);
      }
      return new ToSpeckleResult(speckleGenRest); 
    }
    #endregion

    #region Analysis Stage
    private ToSpeckleResult GsaStageToSpeckle(GsaRecord nativeObject, GSALayer layer = GSALayer.Both)
    {
      var gsaStage = (GsaAnalStage)nativeObject;
      var speckleStage = new GSAStage()
      {
        name = gsaStage.Name,
        colour = gsaStage.Colour.ToString(),
        elements = gsaStage.ElementIndices.Select(i => GetElementFromIndex(i)).ToList(),
        lockedElements = gsaStage.LockElementIndices.Select(i => GetElementFromIndex(i)).ToList(),
      };
      if (gsaStage.Index.IsIndex())
      {
        speckleStage.nativeId = gsaStage.Index.Value;
        speckleStage.applicationId = Instance.GsaModel.GetApplicationId<GsaAnalStage>(gsaStage.Index.Value);
      }
      if (gsaStage.Phi.IsPositive()) speckleStage.creepFactor = gsaStage.Phi.Value;
      if (gsaStage.Days.IsIndex()) speckleStage.stageTime = gsaStage.Days.Value;

      return new ToSpeckleResult(speckleStage); 
    }
    #endregion
    #endregion

    #region ToNative
    //TO DO: implement conversion code for ToNative

    private List<GsaRecord> AxisToNative(Base @object)
    {
      var axis = (Axis)@object;

      var index = Instance.GsaModel.Cache.ResolveIndex<GsaAxis>(axis.applicationId);

      return new List<GsaRecord>
      {
        new GsaAxis()
        {
          ApplicationId = axis.applicationId,
          Name = axis.name,
          Index = index,
          OriginX = axis.definition.origin.x,
          OriginY = axis.definition.origin.y,
          OriginZ = axis.definition.origin.z
        }
      };
    }

    #endregion

    #region Helper
    #region ToSpeckle
    #region Geometry
    #region Node
    /// <summary>
    /// Conversion of node restraint from GSA to Speckle
    /// </summary>
    /// <param name="gsaNode">GsaNode object with the restraint definition to be converted</param>
    /// <returns></returns>
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

    /// <summary>
    /// Conversion of 1D element end releases from GSA to Speckle restraint
    /// </summary>
    /// <param name="release">Dictionary of release codes</param>
    /// <returns></returns>
    private static Restraint GetRestraint(Dictionary<GwaAxisDirection6, ReleaseCode> gsaRelease, List<double> gsaStiffness)
    {
      var code = new List<string>() { "F", "F", "F", "F", "F", "F" }; //Default
      int index = 0;
      if (gsaRelease != null)
      {
        foreach (var k in gsaRelease.Keys.ToList())
        {
          switch (k)
          {
            case GwaAxisDirection6.X:
              code[0] = gsaRelease[k].GetStringValue();
              break;
            case GwaAxisDirection6.Y:
              code[1] = gsaRelease[k].GetStringValue();
              break;
            case GwaAxisDirection6.Z:
              code[2] = gsaRelease[k].GetStringValue();
              break;
            case GwaAxisDirection6.XX:
              code[3] = gsaRelease[k].GetStringValue();
              break;
            case GwaAxisDirection6.YY:
              code[4] = gsaRelease[k].GetStringValue();
              break;
            case GwaAxisDirection6.ZZ:
              code[5] = gsaRelease[k].GetStringValue();
              break;
          }
        }
      }

      var speckleRelease = new Restraint(string.Join("", code));

      //Add stiffnesses
      if (code[0] == "K") speckleRelease.stiffnessX = gsaStiffness[index++];
      if (code[1] == "K") speckleRelease.stiffnessY = gsaStiffness[index++];
      if (code[2] == "K") speckleRelease.stiffnessZ = gsaStiffness[index++];
      if (code[3] == "K") speckleRelease.stiffnessXX = gsaStiffness[index++];
      if (code[4] == "K") speckleRelease.stiffnessYY = gsaStiffness[index++];
      if (code[5] == "K") speckleRelease.stiffnessZZ = gsaStiffness[index++];

      return speckleRelease;
    }

    private Restraint GetRestraint(GsaGenRest gsaGenRest)
    {
      var code = new List<string>() { "F", "F", "F", "F", "F", "F" }; //Default
      if (gsaGenRest.X == RestraintCondition.Constrained) code[0] = "R";
      if (gsaGenRest.Y == RestraintCondition.Constrained) code[1] = "R";
      if (gsaGenRest.Z == RestraintCondition.Constrained) code[2] = "R";
      if (gsaGenRest.XX == RestraintCondition.Constrained) code[3] = "R";
      if (gsaGenRest.YY == RestraintCondition.Constrained) code[4] = "R";
      if (gsaGenRest.ZZ == RestraintCondition.Constrained) code[5] = "R";
      return new Restraint(string.Join("", code));
    }

    /// <summary>
    /// Conversion of node constraint axis from GSA to Speckle
    /// </summary>
    /// <param name="gsaNode">GsaNode object with the constraint axis definition to be converted</param>
    /// <returns></returns>
    private Plane GetConstraintAxis(GsaNode gsaNode)
    {
      Plane speckleAxis;
      Point origin;
      Vector xdir, ydir, normal;

      if (gsaNode.AxisRefType == NodeAxisRefType.XElevation)
      {
        origin = new Point(0, 0, 0);
        xdir = new Vector(0, -1, 0);
        ydir = new Vector(0, 0, 1);
        normal = new Vector(-1, 0, 0);
        speckleAxis = new Plane(origin, normal, xdir, ydir);
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.YElevation)
      {
        origin = new Point(0, 0, 0);
        xdir = new Vector(1, 0, 0);
        ydir = new Vector(0, 0, 1);
        normal = new Vector(0, -1, 0);
        speckleAxis = new Plane(origin, normal, xdir, ydir);
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.Vertical)
      {
        origin = new Point(0, 0, 0);
        xdir = new Vector(0, 0, 1);
        ydir = new Vector(1, 0, 0);
        normal = new Vector(0, 1, 0);
        speckleAxis = new Plane(origin, normal, xdir, ydir);
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.Reference && gsaNode.AxisIndex.IsIndex())
      {
        speckleAxis = GetAxisFromIndex(gsaNode.AxisIndex.Value).definition;
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        speckleAxis = GlobalAxis().definition;
      }

      return speckleAxis;
    }

    /// <summary>
    /// Speckle structural schema restraint code
    /// </summary>
    /// <param name="gsaNode">GsaNode object with the restraint definition to be converted</param>
    /// <returns></returns>
    private static string GetCustomRestraintCode(GsaNode gsaNode)
    {
      var code = "RRRRRR".ToCharArray();
      for (var i = 0; i < gsaNode.Restraints.Count(); i++)
      {
        switch (gsaNode.Restraints[i])
        {
          case GwaAxisDirection6.X:
            code[0] = 'F';
            break;
          case GwaAxisDirection6.Y:
            code[1] = 'F';
            break;
          case GwaAxisDirection6.Z:
            code[2] = 'F';
            break;
          case GwaAxisDirection6.XX:
            code[3] = 'F';
            break;
          case GwaAxisDirection6.YY:
            code[4] = 'F';
            break;
          case GwaAxisDirection6.ZZ:
            code[5] = 'F';
            break;
        }
      }
      return code.ToString();
    }

    /// <summary>
    /// Add GSA spring stiffness definition to Speckle restraint definition.
    /// Deprecated: Using GSANode instead of Node, so spring stiffness no longer stored in Restraint
    /// </summary>
    /// <param name="restraint">Restraint speckle object to be updated</param>
    /// <param name="gsaNode">GsaNode object with spring stiffness definition</param>
    /// <returns></returns>
    private Restraint UpdateSpringStiffness(Restraint restraint, GsaNode gsaNode)
    {
      //Spring Stiffness
      if (gsaNode.SpringPropertyIndex.IsIndex())
      {
        var gsaRecord = Instance.GsaModel.GetNative<GsaPropSpr>(gsaNode.SpringPropertyIndex.Value);
        if (gsaRecord.GetType() != typeof(GsaPropSpr))
        {
          return restraint;
        }
        var gsaSpring = (GsaPropSpr)gsaRecord;

        //Update spring stiffness
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.X] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[0] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessX = gsaSpring.Stiffnesses[GwaAxisDirection6.X];
        }
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.Y] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[1] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessY = gsaSpring.Stiffnesses[GwaAxisDirection6.Y];
        }
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.Z] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[2] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessZ = gsaSpring.Stiffnesses[GwaAxisDirection6.Z];
        }
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.XX] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[3] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessXX = gsaSpring.Stiffnesses[GwaAxisDirection6.XX];
        }
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.YY] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[4] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessYY = gsaSpring.Stiffnesses[GwaAxisDirection6.YY];
        }
        if (gsaSpring.Stiffnesses[GwaAxisDirection6.ZZ] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[5] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessZZ = gsaSpring.Stiffnesses[GwaAxisDirection6.ZZ];
        }
      }
      return restraint;
    }

    /// <summary>
    /// Get Speckle node object from GSA node index
    /// </summary>
    /// <param name="index">GSA node index</param>
    /// <returns></returns>
    private Node GetNodeFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaNode, Node>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }
    #endregion

    #region Axis
    /// <summary>
    /// Get Speckle axis object from GSA axis index
    /// </summary>
    /// <param name="index">GSA axis index</param>
    /// <returns></returns>
    private Axis GetAxisFromIndex(int index)
    {
      /*
      var gsaAxis = Instance.GsaModel.GetNative<GsaAxis>(index);
      if (gsaAxis == null || gsaAxis.GetType() != typeof(GsaAxis))
      {
        return null;
      }
      return (Axis)GsaAxisToSpeckle((GsaAxis)gsaAxis).LayerAgnosticObjects.FirstOrDefault();
      */
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaAxis, Axis>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }

    /// <summary>
    /// Speckle global axis definition
    /// </summary>
    /// <returns></returns>
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
    private Base GetElementFromIndex(int index)
    {
      var gsaEl = (GsaEl)Instance.GsaModel.Cache.GetNative<GsaEl>(index);
      if(gsaEl.Is1dElement())
      {
        return GetElement1DFromIndex(index);
      }
      if(gsaEl.Is2dElement())
      {
        return GetElement2DFromIndex(index);
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Get Speckle Element2D object from GSA element index
    /// </summary>
    /// <param name="index">GSA element index</param>
    /// <returns></returns>
    private Element2D GetElement2DFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaEl, Element2D>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    /// <summary>
    /// Get Speckle Element1D object from GSA element index
    /// </summary>
    /// <param name="index">GSA element index</param>
    /// <returns></returns>
    private Element1D GetElement1DFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaEl, Element1D>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    private ElementType1D GetElement1dType(ElementType gsaType)
    {
      ElementType1D speckleType;

      switch (gsaType)
      {
        case ElementType.Bar:
          speckleType = ElementType1D.Bar;
          break;
        case ElementType.Cable:
          speckleType = ElementType1D.Cable;
          break;
        case ElementType.Damper:
          speckleType = ElementType1D.Damper;
          break;
        case ElementType.Link:
          speckleType = ElementType1D.Link;
          break;
        case ElementType.Rod:
          speckleType = ElementType1D.Rod;
          break;
        case ElementType.Spacer:
          speckleType = ElementType1D.Spacer;
          break;
        case ElementType.Spring:
          speckleType = ElementType1D.Spring;
          break;
        case ElementType.Strut:
          speckleType = ElementType1D.Strut;
          break;
        case ElementType.Tie:
          speckleType = ElementType1D.Tie;
          break;
        default:
          speckleType = ElementType1D.Beam;
          break;
      }

      return speckleType;
    }

    /// <summary>
    /// Get the local axis for a 1D element
    /// </summary>
    /// <param name="n1">end1Node</param>
    /// <param name="n2">end2Node</param>
    /// <param name="n3">orientationNode</param>
    /// <param name="angle">orientationAngle in radians</param>
    /// <returns></returns>
    private Plane GetLocalAxis(Node n1, Node n2, Node n3, double angle)
    {
      var normal = new Vector(0, 0, 1); //default

      var p1 = n1.basePoint;
      var p2 = n2.basePoint;
      var origin = new Point(p1.x, p1.y, p1.z);
      var xdir = (new Vector(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z)).UnitVector();

      //Update normal if orientation node exists
      if (n3 != null)
      {
        var p3 = n3.basePoint;
        normal = (new Vector(p3.x - p1.x, p3.y - p1.y, p3.z - p1.z)).UnitVector();
      }

      //Apply rotation angle
      if (angle != 0) normal = normal.Rotate(xdir, angle).UnitVector();

      //xdir and normal define a plane:
      // *ensure normal is perpendicular to xdir on that plane
      // *ensure ydir is normal to the plane
      var ydir = -(xdir * normal).UnitVector();
      normal = (xdir * ydir).UnitVector();

      return new Plane(origin, normal, xdir, ydir);
    }

    private Mesh DisplayMeshElement2d(GsaEl gsaEl, string units = null)
    {
      //TO DO: check if this actually creates a real mesh
      var vertices = new List<double>();
      var faces = new List<int[]>();

      var topology = gsaEl.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();

      foreach (var node in topology)
      {
        vertices.Add(node.basePoint.x);
        vertices.Add(node.basePoint.y);
        vertices.Add(node.basePoint.z);
      }

      if (gsaEl.NodeIndices.Count == 4)
      {
        faces.Add(new int[] { 1, 1, 2, 3, 4 });
      }
      else
      {
        faces.Add(new int[] { 0, 1, 2, 3 });
      }

      var speckleMesh = new Mesh(vertices.ToArray(), faces.SelectMany(o => o).ToArray(), null, null, units);

      return speckleMesh;
    }
    #endregion

    #region Member
    private ElementType1D GetMember1dType(GwaMemberType gsaMemberType)
    {
      return ElementType1D.Beam;
    }

    private ElementType2D GetMember2dType(GwaMemberType gsaMemberType)
    {
      return ElementType2D.Quad4;
    }
    #endregion

    #region Grids
    private Axis GetGridPlaneAxis(GridPlaneAxisRefType gsaAxisType, int? gsaAxisIndex)
    {
      var speckleAxis = new Axis()
      {
        axisType = AxisType.Cartesian
      };

      if (gsaAxisType == GridPlaneAxisRefType.XElevation)
      {
        speckleAxis.name = "xElevation";
        speckleAxis.definition = new Plane()
        {
          origin = new Point(0, 0, 0), 
          normal = new Vector(-1, 0, 0),
          xdir = new Vector(0, -1, 0), 
          ydir = new Vector(0, 0, 1)
        };
      }
      else if (gsaAxisType == GridPlaneAxisRefType.YElevation)
      {
        speckleAxis.name = "yElevation";
        speckleAxis.definition = new Plane()
        {
          origin = new Point(0, 0, 0),
          normal = new Vector(0, -1, 0),
          xdir = new Vector(1, 0, 0),
          ydir = new Vector(0, 0, 1)
        };
      }
      else if (gsaAxisType == GridPlaneAxisRefType.Reference && gsaAxisIndex.IsIndex())
      {
        speckleAxis = GetAxisFromIndex(gsaAxisIndex.Value);
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        speckleAxis = GlobalAxis();
      }

      return speckleAxis;
    }

    private GSAGridPlane GetGridPlane(GridPlaneAxisRefType gsaAxisType, int? gsaAxisIndex)
    {
      var speckleGridPlane = new GSAGridPlane()
      {
        name = "",
        axis = new Axis()
        {
          axisType = AxisType.Cartesian
        },
        elevation = 0,
        storeyToleranceBelow = "",
        storeyToleranceAbove = "",
      };

      if (gsaAxisType == GridPlaneAxisRefType.XElevation)
      {
        speckleGridPlane.name = "X Elevation Grid";
        speckleGridPlane.axis.name = "x Elevation";
        speckleGridPlane.axis.definition = new Plane()
        {
          origin = new Point(0, 0, 0),
          normal = new Vector(-1, 0, 0),
          xdir = new Vector(0, -1, 0),
          ydir = new Vector(0, 0, 1)
        };
      }
      else if (gsaAxisType == GridPlaneAxisRefType.YElevation)
      {
        speckleGridPlane.name = "Y Elevation Grid";
        speckleGridPlane.axis.name = "Y Elevation";
        speckleGridPlane.axis.definition = new Plane()
        {
          origin = new Point(0, 0, 0),
          normal = new Vector(0, -1, 0),
          xdir = new Vector(1, 0, 0),
          ydir = new Vector(0, 0, 1)
        };
      }
      else if (gsaAxisType == GridPlaneAxisRefType.Reference && gsaAxisIndex.IsIndex())
      {
        speckleGridPlane = GetGridPlaneFromIndex(gsaAxisIndex.Value);
      }
      else
      {
        speckleGridPlane.name = "Global Grid";
        speckleGridPlane.axis.name = "Global";
        speckleGridPlane.axis.definition = new Plane()
        {
          origin = new Point(0, 0, 0),
          normal = new Vector(0, 0, 1),
          xdir = new Vector(1, 0, 0),
          ydir = new Vector(0, 1, 0)
        };
      }

      return speckleGridPlane;
    }

    /// <summary>
    /// Get Speckle grid plane object from GSA grid plane index
    /// </summary>
    /// <param name="index">GSA axis index</param>
    /// <returns></returns>
    private GSAGridPlane GetGridPlaneFromIndex(int index)
    {
      /*
      var gsGridPlane = Instance.GsaModel.GetNative<GsaGridPlane>(index);
      if (gsGridPlane == null || gsGridPlane.GetType() != typeof(GsaGridPlane))
      {
        return null;
      }
      return GsaGridPlaneToSpeckle((GsaGridPlane)gsGridPlane);
      */
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaGridPlane, GSAGridPlane>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }

    private string GetStoreyTolerance(double? gsaStoreyTolerance, bool gsaStoreyToleranceAuto, GridPlaneType gsaType)
    {
      var speckleStoreyTolerance = ""; //default

      if (gsaType == GridPlaneType.Storey)
      {
        if (gsaStoreyToleranceAuto)
        {
          speckleStoreyTolerance = "auto";
        }
        else if (gsaStoreyTolerance.HasValue)
        {
          speckleStoreyTolerance = gsaStoreyTolerance.Value.ToString();
        }
      }
      return speckleStoreyTolerance;
    }

    private GSAGridSurface GetGridSurfaceFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaGridSurface, GSAGridSurface>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    private GridSurfaceSpanType GetSpanType(GridSurfaceSpan gsaGridSurfaceSpan)
    {
      if (gsaGridSurfaceSpan == GridSurfaceSpan.One)
      {
        return GridSurfaceSpanType.OneWay;
      }
      else if (gsaGridSurfaceSpan == GridSurfaceSpan.Two)
      {
        return GridSurfaceSpanType.TwoWay;
      }
      else
      {
        return GridSurfaceSpanType.NotSet;
      }
    }

    private LoadExpansion GetLoadExpansionType(GridExpansion gsaExpansion)
    {
      if (gsaExpansion == GridExpansion.Legacy)
      {
        return LoadExpansion.Legacy;
      }
      else if (gsaExpansion == GridExpansion.PlaneAspect)
      {
        return LoadExpansion.PlaneAspect;
      }
      else if (gsaExpansion == GridExpansion.PlaneCorner)
      {
        return LoadExpansion.PlaneCorner;
      }
      else if (gsaExpansion == GridExpansion.PlaneSmooth)
      {
        return LoadExpansion.PlaneSmooth;
      }
      else
      {
        return LoadExpansion.NotSet;
      }
    }
    #endregion

    #region Polyline
    public Polyline GetPolyline(LoadLineOption gsaType, string gsaPolygon, int? gsaPolygonIndex)
    {
      var specklePolyline = new Polyline();
      if (gsaType == LoadLineOption.Polygon)
      {
        specklePolyline = GetPolygonFromString(gsaPolygon);
      }
      else if (gsaType == LoadLineOption.PolyRef && gsaPolygonIndex.HasValue)
      {
        specklePolyline = GetPolygonFromIndex(gsaPolygonIndex.Value);
      }
      else
      {
        specklePolyline = null;
      }
      return specklePolyline;
    }

    public Polyline GetPolyline(LoadAreaOption gsaType, string gsaPolygon, int? gsaPolygonIndex)
    {
      var specklePolyline = new Polyline();
      if (gsaType == LoadAreaOption.Polygon)
      {
        specklePolyline = GetPolygonFromString(gsaPolygon);
      }
      else if (gsaType == LoadAreaOption.PolyRef && gsaPolygonIndex.HasValue)
      {
        specklePolyline = GetPolygonFromIndex(gsaPolygonIndex.Value);
      }
      else
      {
        specklePolyline = null;
      }
      return specklePolyline;
    }

    public GSAPolyline GetPolygonFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaPolyline, GSAPolyline>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    public Polyline GetPolygonFromString(string gsaPolygon)
    {
      //process gsaPolygon string
      //e.g. (0,0) (1,0) (1,1) (0,1)(m)
      var specklePolyline = new Polyline()
      {
        value = new List<double>()
      };
      foreach (var item in gsaPolygon.Split(' '))
      {
        var point = item.Split('(', ')')[1];
        if (point.Split(',').Count() == 2) point += ",0"; //ensure each point has x,y,z value
        specklePolyline.value.AddRange(point.Split(',').Select(p => p.ToDouble()).ToList());
      }
      return specklePolyline;
    }
    #endregion
    #endregion

    #region Loading
    private LoadType GetLoadType(StructuralLoadCaseType gsaLoadType)
    {
      switch (gsaLoadType)
      {
        case StructuralLoadCaseType.Dead:
          return LoadType.Dead;
        case StructuralLoadCaseType.Earthquake:
          return LoadType.SeismicStatic;
        case StructuralLoadCaseType.Live:
          return LoadType.Live;
        case StructuralLoadCaseType.Rain:
          return LoadType.Rain;
        case StructuralLoadCaseType.Snow:
          return LoadType.Snow;
        case StructuralLoadCaseType.Soil:
          return LoadType.Soil;
        case StructuralLoadCaseType.Thermal:
          return LoadType.Thermal;
        case StructuralLoadCaseType.Wind:
          return LoadType.Wind;
        default:
          return LoadType.None;
      }
    }

    private AreaLoadType GetAreaLoadType(Load2dFaceType gsaType)
    {
      switch (gsaType)
      {
        case Load2dFaceType.General:
          return AreaLoadType.Variable;
        case Load2dFaceType.Point:
          return AreaLoadType.Point;
        default:
          return AreaLoadType.Constant;
      }
    }

    private Thermal2dLoadType GetLoad2dThermalType(Load2dThermalType gsaType)
    {
      switch (gsaType)
      {
        case Load2dThermalType.Uniform:
          return Thermal2dLoadType.Uniform;
        case Load2dThermalType.Gradient:
          return Thermal2dLoadType.Gradient;
        case Load2dThermalType.General:
          return Thermal2dLoadType.General;
        default:
          return Thermal2dLoadType.NotSet;
      }
    }

    private LoadDirection GetDirection(GwaAxisDirection3 gsaDirection)
    {
      switch (gsaDirection)
      {
        case GwaAxisDirection3.X:
          return LoadDirection.X;
        case GwaAxisDirection3.Y:
          return LoadDirection.Y;
        case GwaAxisDirection3.Z:
        default:
          return LoadDirection.Z;
      }
    }

    private LoadDirection GetDirection(GwaAxisDirection6 gsaDirection)
    {
      switch (gsaDirection)
      {
        case GwaAxisDirection6.X:
          return LoadDirection.X;
        case GwaAxisDirection6.Y:
          return LoadDirection.Y;
        case GwaAxisDirection6.Z:
          return LoadDirection.Z;
        case GwaAxisDirection6.XX:
          return LoadDirection.XX;
        case GwaAxisDirection6.YY:
          return LoadDirection.YY;
        case GwaAxisDirection6.ZZ:
        default:
          return LoadDirection.ZZ;
      }
    }

    private LoadAxisType GetLoadAxisType(AxisRefType gsaType)
    {
      //TO DO: update when there are more options for LoadAxisType
      switch (gsaType)
      {
        case AxisRefType.Local:
          return LoadAxisType.Local;
        case AxisRefType.Reference:
        case AxisRefType.NotSet:
        case AxisRefType.Global:
        default:
          return LoadAxisType.Global;
      }
    }

    private LoadAxisType GetLoadAxisType(LoadBeamAxisRefType gsaType)
    {
      //TO DO: update when there are more options for LoadAxisType
      switch (gsaType)
      {
        case LoadBeamAxisRefType.Local:
          return LoadAxisType.Local;
        case LoadBeamAxisRefType.Reference:
        case LoadBeamAxisRefType.Natural:
        case LoadBeamAxisRefType.NotSet:
        case LoadBeamAxisRefType.Global:
        default:
          return LoadAxisType.Global;
      }
    }

    private LoadCase GetLoadCaseFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaLoadCase, LoadCase>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    private BeamLoadType GetBeamLoadType(Type t)
    {
      if (t == typeof(GsaLoadBeamPoint))
      {
        return BeamLoadType.Point;
      }
      else if (t == typeof(GsaLoadBeamLine))
      {
        return BeamLoadType.Linear;
      }
      else if (t == typeof(GsaLoadBeamPatch))
      {
        return BeamLoadType.Patch;
      }
      else if (t == typeof(GsaLoadBeamTrilin))
      {
        return BeamLoadType.TriLinear;
      }
      else
      {
        return BeamLoadType.Uniform;
      }
    }

    private bool GetLoadBeamPositions(GsaLoadBeam gsaLoadBeam, out List<double> positions)
    {
      positions = new List<double>();
      var type = gsaLoadBeam.GetType();
      if (type == typeof(GsaLoadBeamPoint))
      {
        positions.Add(((GsaLoadBeamPoint)gsaLoadBeam).Position);
        return true;
      }
      else if (type == typeof(GsaLoadBeamPatch) || type == typeof(GsaLoadBeamPatchTrilin))
      {
        var lb = (GsaLoadBeamPatchTrilin)gsaLoadBeam;
        positions.Add(lb.Position1);
        positions.Add(lb.Position2Percent);
        return true;
      }
      return false;
    }

    private bool GetLoadBeamValues(GsaLoadBeam gsaLoadBeam, out List<double> values)
    {
      values = new List<double>();
      double? v;
      var type = gsaLoadBeam.GetType();
      if (type == typeof(GsaLoadBeamPoint))
      {
        v = ((GsaLoadBeamPoint)gsaLoadBeam).Load;
        if (v.HasValue)
        { 
          values.Add(v.Value);
          return true;
        }
      }
      else if (type == typeof(GsaLoadBeamUdl))
      {
        v = ((GsaLoadBeamUdl)gsaLoadBeam).Load;
        if (v.HasValue)
        {
          values.Add(v.Value);
          return true;
        }
      }
      else if (type == typeof(GsaLoadBeamLine))
      {
        var lb = (GsaLoadBeamLine)gsaLoadBeam;
        if (lb.Load1.HasValue && lb.Load2.HasValue)
        {
          values.Add(lb.Load1.Value);
          values.Add(lb.Load2.Value);
          return true;
        }
      }
      else if (type == typeof(GsaLoadBeamPatch) || type == typeof(GsaLoadBeamPatchTrilin))
      {
        var lb = (GsaLoadBeamPatchTrilin)gsaLoadBeam;
        if (lb.Load1.HasValue && lb.Load2.HasValue)
        {
          values.Add(lb.Load1.Value);
          values.Add(lb.Load2.Value);
          return true;
        }
      }
      return false;
    }

    private Vector GetGravityFactors(GsaLoadGravity gsaLoadGravity)
    {
      var speckleGravityFactors =  new Vector(0, 0, 0);
      if (gsaLoadGravity.X.HasValue) speckleGravityFactors.x = gsaLoadGravity.X.Value;
      if (gsaLoadGravity.Y.HasValue) speckleGravityFactors.y = gsaLoadGravity.Y.Value;
      if (gsaLoadGravity.Z.HasValue) speckleGravityFactors.z = gsaLoadGravity.Z.Value;

      return speckleGravityFactors;
    }

    private Dictionary<string,double> GetLoadCombinationFactors(string desc)
    {
      var speckleCaseFactors = new Dictionary<string, double>();
      int gsaIndex;
      LoadCase speckleLoadCase;
      LoadCombination speckleLoadCombination;

      var gsaCaseFactors = ParseLoadDescription(desc);

      foreach (var key in gsaCaseFactors.Keys)
      {
        gsaIndex = Convert.ToInt32(key.Substring(1));
        if (key[0] == 'A')
        {
          speckleLoadCase = GetLoadCaseFromIndex(gsaIndex);
          speckleCaseFactors.Add(speckleLoadCase.name, gsaCaseFactors[key]);
        }
        else if (key[0] == 'C')
        {
          speckleLoadCombination = GetLoadCombinationFromIndex(gsaIndex);
          speckleCaseFactors.Add(speckleLoadCombination.name, gsaCaseFactors[key]);
        }
      }

      return speckleCaseFactors;
    }

    private LoadCombination GetLoadCombinationFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaCombination, LoadCombination>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }

    /// <summary>
    /// Seperates the load description into a dictionary of the case/task/combo identifier and their factors.
    /// </summary>
    /// <param name="list">Load description.</param>
    /// <param name="currentMultiplier">Factor to multiply the entire list by.</param>
    /// <returns></returns>
    public static Dictionary<string, double> ParseLoadDescription(string list, double currentMultiplier = 1)
    {
      var ret = new Dictionary<string, double>();

      list = list.Replace(" ", "");

      double multiplier = 1;
      var negative = false;

      for (var pos = 0; pos < list.Count(); pos++)
      {
        var currChar = list[pos];

        if (currChar >= '0' && currChar <= '9') //multiplier
        {
          var mult = "";
          mult += currChar.ToString();

          pos++;
          while (pos < list.Count() && ((list[pos] >= '0' && list[pos] <= '9') || list[pos] == '.'))
            mult += list[pos++].ToString();
          pos--;

          multiplier = mult.ToDouble();
        }
        else if (currChar >= 'A' && currChar <= 'Z') //GSA load case or load combination identifier
        {
          var loadDesc = "";
          loadDesc += currChar.ToString();

          pos++;
          while (pos < list.Count() && list[pos] >= '0' && list[pos] <= '9')
            loadDesc += list[pos++].ToString();
          pos--;

          var actualFactor = multiplier == 0 ? 1 : multiplier;
          actualFactor *= currentMultiplier;
          actualFactor = negative ? -1 * actualFactor : actualFactor;

          ret.Add(loadDesc, actualFactor);

          multiplier = 0;
          negative = false;
        }
        else if (currChar == '-') //negative operator
          negative = !negative;
        else if (currChar == 't') //to operator (i.e. add all load cases between the first and second identifier
        {
          if (list[++pos] == 'o')
          {
            var prevDesc = ret.Last();

            var type = prevDesc.Key[0].ToString();
            var start = Convert.ToInt32(prevDesc.Key.Substring(1)) + 1;

            var endDesc = "";

            pos++;
            pos++;
            while (pos < list.Count() && list[pos] >= '0' && list[pos] <= '9')
              endDesc += list[pos++].ToString();
            pos--;

            var end = Convert.ToInt32(endDesc);

            for (var i = start; i <= end; i++)
              ret.Add(type + i.ToString(), prevDesc.Value);
          }
        }
        else if (currChar == '(') //process part inside brackets
        {
          var actualFactor = multiplier == 0 ? 1 : multiplier;
          actualFactor *= currentMultiplier;
          actualFactor = negative ? -1 * actualFactor : actualFactor;

          ret.AddRange(ParseLoadDescription(string.Join("", list.Skip(pos + 1)), actualFactor));

          pos++;
          while (pos < list.Count() && list[pos] != ')')
            pos++;

          multiplier = 0;
          negative = false;
        }
        else if (currChar == ')')
          return ret;
      }

      return ret;
    }

    private CombinationType GetCombinationType(string desc)
    {
      //TO DO: Use desc to deside combination type
      return CombinationType.LinearAdd;
    }

    private Axis GetAxis(AxisRefType gsaAxisType, int? gsaAxisIndex)
    {
      //Cartesian coordinate system is the only one supported.
      var speckleAxis = new Axis()
      {
        name = "",
        axisType = AxisType.Cartesian,
      };

      if (gsaAxisType == AxisRefType.Local)
      {
        //TO DO: handle local reference axis case
      }
      else if (gsaAxisType == AxisRefType.Reference && gsaAxisIndex.IsIndex())
      {
        speckleAxis = GetAxisFromIndex(gsaAxisIndex.Value);
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        speckleAxis = GlobalAxis();
      }

      return speckleAxis;
    }

    private Line GetLine(GsaGridLine gsaGridLine)
    {
      var speckleLine = new Line();
      if (gsaGridLine.XCoordinate.HasValue && gsaGridLine.YCoordinate.HasValue && gsaGridLine.Length.HasValue && gsaGridLine.Theta1.HasValue)
      {
        speckleLine.start = new Point(gsaGridLine.XCoordinate.Value, gsaGridLine.YCoordinate.Value, 0);
        speckleLine.end = new Point(gsaGridLine.XCoordinate.Value + gsaGridLine.Length.Value * Math.Cos(gsaGridLine.Theta1.Value.Radians()),
          gsaGridLine.YCoordinate.Value + gsaGridLine.Length.Value * Math.Sin(gsaGridLine.Theta1.Value.Radians()), 0);
      }
      return speckleLine;
    }

    private Arc GetArc(GsaGridLine gsaGridLine)
    {
      var speckleArc = new Arc();
      if (gsaGridLine.XCoordinate.HasValue && gsaGridLine.YCoordinate.HasValue && gsaGridLine.Length.HasValue && gsaGridLine.Theta1.HasValue && gsaGridLine.Theta1.HasValue)
      {
        speckleArc.radius = gsaGridLine.Length.Value;
        speckleArc.startAngle = gsaGridLine.Theta1.Value.Radians();
        speckleArc.endAngle = gsaGridLine.Theta2.Value.Radians();
        speckleArc.startPoint = new Point(gsaGridLine.XCoordinate.Value + gsaGridLine.Length.Value * Math.Cos(gsaGridLine.Theta1.Value.Radians()),
          gsaGridLine.YCoordinate.Value + gsaGridLine.Length.Value * Math.Sin(gsaGridLine.Theta1.Value.Radians()), 0);
        speckleArc.endPoint = new Point(gsaGridLine.XCoordinate.Value + gsaGridLine.Length.Value * Math.Cos(gsaGridLine.Theta2.Value.Radians()),
          gsaGridLine.YCoordinate.Value + gsaGridLine.Length.Value * Math.Sin(gsaGridLine.Theta2.Value.Radians()), 0);
        //speckleArc.angleRadians;
        speckleArc.plane = GlobalAxis().definition;
      }
      return speckleArc;
    }
    #endregion

    #region Materials
    //Some material properties are stored in either GsaMat or GsaMatAnal

    /// <summary>
    /// Return true if either v1 or v2 has a value.
    /// </summary>
    /// <param name="v1">value to take precidence if not null</param>
    /// <param name="v2">value to take if v1 is null</param>
    /// <param name="v">returned value</param>
    /// <returns></returns>
    public bool Choose(double? v1, double? v2, out double v)
    {
      if (v1.HasValue)
      {
        v = v1.Value;
        return true;
      }
      else if (v2.HasValue)
      {
        v = v2.Value;
        return true;
      }
      else
      {
        v = 0;
        return false;
      }
    }

    /// <summary>
    /// Get Speckle material object from GSA material index
    /// </summary>
    /// <param name="index">GSA material index</param>
    /// <param name="type">GSA material type</param>
    /// <returns></returns>
    private Material GetMaterialFromIndex(int index, Property2dMaterialType type)
    {
      //Initialise
      GsaRecord gsaMat;
      Material speckleMaterial = null;   

      //Get material based on type and gsa index
      //Convert gsa material to speckle material
      if (type == Property2dMaterialType.Steel)
      {
        //gsaMat = Instance.GsaModel.GetNative<GsaMatSteel>(index);
        //if (gsaMat != null) speckleMaterial = GsaMaterialSteelToSpeckle((GsaMatSteel)gsaMat);
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMatSteel, Steel>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
      }
      else if (type == Property2dMaterialType.Concrete)
      {
        //gsaMat = Instance.GsaModel.GetNative<GsaMatConcrete>(index);
        //if (gsaMat != null) speckleMaterial = GsaMaterialConcreteToSpeckle((GsaMatConcrete)gsaMat);
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMatConcrete, Concrete>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
      }

      return speckleMaterial;
    }

    /// <summary>
    /// Get Speckle material object from GSA material index
    /// </summary>
    /// <param name="index">GSA material index</param>
    /// <param name="type">GSA material type</param>
    /// <returns></returns>
    private Material GetMaterialFromIndex(int index, Section1dMaterialType type)
    {
      //Initialise
      GsaRecord gsaMat;
      Material speckleMaterial = null;

      //Get material based on type and gsa index
      //Convert gsa material to speckle material
      if (type == Section1dMaterialType.STEEL)
      {
        //gsaMat = Instance.GsaModel.GetNative<GsaMatSteel>(index);
        //if (gsaMat != null) speckleMaterial = GsaMaterialSteelToSpeckle((GsaMatSteel)gsaMat);
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMatSteel, Steel>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
      }
      else if (type == Section1dMaterialType.CONCRETE)
      {
        //gsaMat = Instance.GsaModel.GetNative<GsaMatConcrete>(index);
        //if (gsaMat != null) speckleMaterial = GsaMaterialConcreteToSpeckle((GsaMatConcrete)gsaMat);
        return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaMatConcrete, Concrete>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
      }

      return speckleMaterial;
    }
    #endregion

    #region Properties
    #region Spring
    /// <summary>
    /// Get Speckle PropertySpring object from GSA property spring index
    /// </summary>
    /// <param name="index">GSA property spring index</param>
    /// <returns></returns>
    private PropertySpring GetPropertySpringFromIndex(int index)
    {
      /*
      PropertySpring specklePropertySpring = null;
      var gsaPropSpr = Instance.GsaModel.GetNative<GsaPropSpr>(index);
      if (gsaPropSpr != null) specklePropertySpring = GsaPropertySpringToSpeckle((GsaPropSpr)gsaPropSpr);

      return specklePropertySpring;
      */
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaPropSpr, PropertySpring>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }

    /// <summary>
    /// Set properties for an axial spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringAxial(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Axial;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a torsional spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetPropertySpringTorsional(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Torsional;
      specklePropertySpring.stiffnessXX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.XX];
      return true;
    }

    /// <summary>
    /// Set properties for a compression only spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringCompression(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.CompressionOnly;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a tension only spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringTension(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.TensionOnly;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a lockup spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringLockup(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      //Also for LOCKUP, there are positive and negative parameters, but these aren't supported yet
      specklePropertySpring.springType = PropertyTypeSpring.LockUp;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      specklePropertySpring.positiveLockup = 0;
      specklePropertySpring.negativeLockup = 0;
      return true;
    }

    /// <summary>
    /// Set properties for a gap spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringGap(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Gap;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a friction spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringFriction(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Friction;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      specklePropertySpring.stiffnessY = gsaPropSpr.Stiffnesses[GwaAxisDirection6.Y];
      specklePropertySpring.stiffnessZ = gsaPropSpr.Stiffnesses[GwaAxisDirection6.Z];
      specklePropertySpring.frictionCoefficient = gsaPropSpr.FrictionCoeff.Value;
      return true;
    }

    /// <summary>
    /// Set properties for a general spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringGeneral(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.General;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.X];
      specklePropertySpring.springCurveX = 0;
      specklePropertySpring.stiffnessY = gsaPropSpr.Stiffnesses[GwaAxisDirection6.Y];
      specklePropertySpring.springCurveY = 0;
      specklePropertySpring.stiffnessZ = gsaPropSpr.Stiffnesses[GwaAxisDirection6.Z];
      specklePropertySpring.springCurveZ = 0;
      specklePropertySpring.stiffnessXX = gsaPropSpr.Stiffnesses[GwaAxisDirection6.XX];
      specklePropertySpring.springCurveXX = 0;
      specklePropertySpring.stiffnessYY = gsaPropSpr.Stiffnesses[GwaAxisDirection6.YY];
      specklePropertySpring.springCurveYY = 0;
      specklePropertySpring.stiffnessZZ = gsaPropSpr.Stiffnesses[GwaAxisDirection6.ZZ];
      specklePropertySpring.springCurveZZ = 0;
      return true;
    }
    #endregion

    #region Mass
    /// <summary>
    /// Get Speckle PropertyMass object from GSA property mass index
    /// </summary>
    /// <param name="index">GSA property mass index</param>
    /// <returns></returns>
    private PropertyMass GetPropertyMassFromIndex(int index)
    {
      /*
      PropertyMass specklePropertyMass = null;
      var gsaPropMass = Instance.GsaModel.GetNative<GsaPropMass>(index);
      if (gsaPropMass != null) specklePropertyMass = GsaPropertyMassToSpeckle((GsaPropMass)gsaPropMass);

      return specklePropertyMass;
      */
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaPropMass, PropertyMass>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }
    #endregion

    #region Property1D
    private BaseReferencePoint GetReferencePoint(ReferencePoint gsaReferencePoint)
    {
      switch(gsaReferencePoint)
      {
        case ReferencePoint.BottomCentre:
          return BaseReferencePoint.BotCentre;
        case ReferencePoint.BottomLeft:
          return BaseReferencePoint.BotLeft;
        default:
          return BaseReferencePoint.Centroid;
      }
    }

    /// <summary>
    /// Get Speckle Property1D object from GSA property 1D index
    /// </summary>
    /// <param name="index">GSA property 1D index</param>
    /// <returns></returns>
    private Property1D GetProperty1dFromIndex(int index)
    {
      /*
      Property1D speckleProperty1d = null;
      var gsaSection = Instance.GsaModel.GetNative<GsaSection>(index);
      if (gsaSection != null) speckleProperty1d = GsaSectionToSpeckle((GsaSection)gsaSection);

      return speckleProperty1d;
      */
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaSection, Property1D>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }

    #region Profiles
    private SectionProfile GetProfileCatalogue(ProfileDetails gsaProfile)
    {
      var p = (ProfileDetailsCatalogue)gsaProfile;
      var items = p.Profile.Split(' ');
      var speckleProfile = new Catalogue()
      {
        shapeType = ShapeType.Catalogue,
        description = p.Profile,
        catalogueName = items[1].Split('-')[0],
        sectionType = items[1].Split('-')[1],
        sectionName = items[2],
      };
      return speckleProfile;
    }
    private SectionProfile GetProfileExplicit(ProfileDetails gsaProfile)
    {
      var p = (ProfileDetailsExplicit)gsaProfile;

      var speckleProfile = new Explicit()
      {
        shapeType = ShapeType.Explicit,
      };

      if (p.Area.HasValue) speckleProfile.area = p.Area.Value;
      if (p.Iyy.HasValue) speckleProfile.Iyy = p.Iyy.Value;
      if (p.Izz.HasValue) speckleProfile.Izz = p.Izz.Value;
      if (p.J.HasValue) speckleProfile.J = p.J.Value;
      if (p.Ky.HasValue) speckleProfile.Ky = p.Ky.Value;
      if (p.Kz.HasValue) speckleProfile.Kz = p.Kz.Value;

      return speckleProfile;
    }
    private SectionProfile GetProfilePerimeter(ProfileDetails gsaProfile)
    {
      var gsaProfilePerimeter = (ProfileDetailsPerimeter)gsaProfile;
      var speckleProfile = new Perimeter()
      {
        shapeType = ShapeType.Perimeter,
        voids = new List<Objects.ICurve>(),
      };

      if (gsaProfilePerimeter.Type[0] == 'P') //Perimeter
      {
        var isVoid = false;
        var outline = new List<double>();
        var voids = new List<List<double>>();
        for (var i = 0; i < gsaProfilePerimeter.Actions.Count(); i++)
        {
          if (gsaProfilePerimeter.Actions[i] == "M" && i != 0) isVoid = true;
          if (gsaProfilePerimeter.Actions[i] == "M" && isVoid) voids.Add(new List<double>());

          if (gsaProfilePerimeter.Y[i].HasValue && gsaProfilePerimeter.Z[i].HasValue)
          {
            if (!isVoid)
            {
              outline.Add(0);
              outline.Add(gsaProfilePerimeter.Y[i].Value);
              outline.Add(gsaProfilePerimeter.Z[i].Value);
            }
            else
            {
              voids.Last().Add(0);
              voids.Last().Add(gsaProfilePerimeter.Y[i].Value);
              voids.Last().Add(gsaProfilePerimeter.Z[i].Value);
            }
          }
        }
        speckleProfile.outline = new Curve() { points = outline };
        foreach (var v in voids) speckleProfile.voids.Add(new Curve() { points = v });
      }
      else if (gsaProfilePerimeter.Type[0] == 'L') //Line Segment
      {
        //TO DO:
      }

      return speckleProfile;
    }
    private SectionProfile GetProfileStandard(ProfileDetails gsaProfile)
    {
      var p = (ProfileDetailsStandard)gsaProfile;
      var speckleProfile = new SectionProfile();
      var fns = new Dictionary<Section1dStandardProfileType, Func<ProfileDetailsStandard, SectionProfile>>
      { { Section1dStandardProfileType.Rectangular, GetProfileStandardRectangluar },
        { Section1dStandardProfileType.RectangularHollow, GetProfileStandardRHS },
        { Section1dStandardProfileType.Circular, GetProfileStandardCircular },
        { Section1dStandardProfileType.CircularHollow, GetProfileStandardCHS },
        { Section1dStandardProfileType.ISection, GetProfileStandardISection },
        { Section1dStandardProfileType.Tee, GetProfileStandardTee },
        { Section1dStandardProfileType.Angle, GetProfileStandardAngle },
        { Section1dStandardProfileType.Channel, GetProfileStandardChannel },
        //TO DO: implement other standard sections as more sections are supported
      };
      if (fns.ContainsKey(p.ProfileType)) speckleProfile = fns[p.ProfileType](p);

      return speckleProfile;
    }
    private SectionProfile GetProfileStandardRectangluar(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsRectangular)gsaProfile;
      var speckleProfile = new Rectangular()
      {
        name = "",
        shapeType = ShapeType.Rectangular,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      return speckleProfile;
    }
    private SectionProfile GetProfileStandardRHS(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsTwoThickness)gsaProfile;
      var speckleProfile = new Rectangular()
      {
        name = "",
        shapeType = ShapeType.Rectangular,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      if (p.tw.HasValue) speckleProfile.webThickness = p.tw.Value;
      if (p.tf.HasValue) speckleProfile.flangeThickness = p.tf.Value;
      return speckleProfile;
    }
    private SectionProfile GetProfileStandardCircular(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsCircular)gsaProfile;
      var speckleProfile = new Circular()
      {
        name = "",
        shapeType = ShapeType.Circular,
      };
      if (p.d.HasValue) speckleProfile.radius = p.d.Value / 2;
      return speckleProfile;
    }
    private SectionProfile GetProfileStandardCHS(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsCircularHollow)gsaProfile;
      var speckleProfile = new Circular()
      {
        name = "",
        shapeType = ShapeType.Circular,
      };
      if (p.d.HasValue) speckleProfile.radius = p.d.Value / 2;
      if (p.t.HasValue) speckleProfile.wallThickness = p.t.Value;
      return speckleProfile;
    }
    private SectionProfile GetProfileStandardISection(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsTwoThickness)gsaProfile;
      var speckleProfile = new ISection()
      {
        name = "",
        shapeType = ShapeType.I,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      if (p.tw.HasValue) speckleProfile.webThickness = p.tw.Value;
      if (p.tf.HasValue) speckleProfile.flangeThickness = p.tf.Value;
      return speckleProfile;
    }
    private SectionProfile GetProfileStandardTee(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsTwoThickness)gsaProfile;
      var speckleProfile = new Tee()
      {
        name = "",
        shapeType = ShapeType.Tee,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      if (p.tw.HasValue) speckleProfile.webThickness = p.tw.Value;
      if (p.tf.HasValue) speckleProfile.flangeThickness = p.tf.Value;
      return speckleProfile;
    }
    private SectionProfile GetProfileStandardAngle(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsTwoThickness)gsaProfile;
      var speckleProfile = new Angle()
      {
        name = "",
        shapeType = ShapeType.Angle,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      if (p.tw.HasValue) speckleProfile.webThickness = p.tw.Value;
      if (p.tf.HasValue) speckleProfile.flangeThickness = p.tf.Value;
      return speckleProfile;
    }
    private SectionProfile GetProfileStandardChannel(ProfileDetailsStandard gsaProfile)
    {
      var p = (ProfileDetailsTwoThickness)gsaProfile;
      var speckleProfile = new Channel()
      {
        name = "",
        shapeType = ShapeType.Channel,
      };
      if (p.b.HasValue) speckleProfile.width = p.b.Value;
      if (p.d.HasValue) speckleProfile.depth = p.d.Value;
      if (p.tw.HasValue) speckleProfile.webThickness = p.tw.Value;
      if (p.tf.HasValue) speckleProfile.flangeThickness = p.tf.Value;
      return speckleProfile;
    }
    #endregion
    #endregion

    #region Property2D
    /// <summary>
    /// Converts the GsaProp2d reference surface to Speckle
    /// </summary>
    /// <param name="gsaProp2d">GsaProp2d object with reference surface definition</param>
    /// <returns></returns>
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

    /// <summary>
    /// Convert GSA 2D element reference axis to Speckle
    /// </summary>
    /// <param name="gsaProp2d">GsaProp2d object with reference axis definition</param>
    /// <returns></returns>
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
      else if (gsaProp2d.AxisRefType == AxisRefType.Reference && gsaProp2d.AxisIndex.IsIndex())
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

    /// <summary>
    /// Get Speckle Property2D object from GSA property 2D index
    /// </summary>
    /// <param name="index">GSA property 2D index</param>
    /// <returns></returns>
    private Property2D GetProperty2dFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaProp2d, Property2D>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }
    #endregion
    #endregion

    #region Constraint
    private RigidConstraint GetRigidConstraintType(RigidConstraintType gsaType)
    {
      switch(gsaType)
      {
        case RigidConstraintType.ALL:
          return RigidConstraint.ALL;
        case RigidConstraintType.XY_PLANE:
          return RigidConstraint.XY_PLANE;
        case RigidConstraintType.YZ_PLANE:
          return RigidConstraint.YZ_PLANE;
        case RigidConstraintType.ZX_PLANE:
          return RigidConstraint.ZX_PLANE;
        case RigidConstraintType.XY_PLATE:
          return RigidConstraint.XY_PLATE;
        case RigidConstraintType.YZ_PLATE:
          return RigidConstraint.YZ_PLATE;
        case RigidConstraintType.ZX_PLATE:
          return RigidConstraint.ZX_PLATE;
        case RigidConstraintType.PIN:
          return RigidConstraint.PIN;
        case RigidConstraintType.XY_PLANE_PIN:
          return RigidConstraint.XY_PLANE_PIN;
        case RigidConstraintType.YZ_PLANE_PIN:
          return RigidConstraint.YZ_PLANE_PIN;
        case RigidConstraintType.ZX_PLANE_PIN:
          return RigidConstraint.ZX_PLANE_PIN;
        case RigidConstraintType.XY_PLATE_PIN:
          return RigidConstraint.XY_PLATE_PIN;
        case RigidConstraintType.YZ_PLATE_PIN:
          return RigidConstraint.YZ_PLATE_PIN;
        case RigidConstraintType.ZX_PLATE_PIN:
          return RigidConstraint.ZX_PLATE_PIN;
        case RigidConstraintType.Custom:
          return RigidConstraint.Custom;
        default:
          return RigidConstraint.NotSet;
      }
    }

    private Dictionary<AxisDirection6,List<AxisDirection6>> GetRigidConstraint(Dictionary<GwaAxisDirection6,List<GwaAxisDirection6>> gsaLink)
    {
      var speckleConstraint = new Dictionary<AxisDirection6, List<AxisDirection6>>();
      foreach (var key in gsaLink.Keys)
      {
        var speckleKey = GetSpeckleAxisDirection6(key);
        speckleConstraint[speckleKey] = new List<AxisDirection6>();
        foreach (var val in gsaLink[key])
        {
          speckleConstraint[speckleKey].Add(GetSpeckleAxisDirection6(val));
        }
      }
      return speckleConstraint;
    }

    private AxisDirection6 GetSpeckleAxisDirection6(GwaAxisDirection6 gsa)
    {
      switch (gsa)
      {
        case GwaAxisDirection6.X:
          return AxisDirection6.X;
        case GwaAxisDirection6.Y:
          return AxisDirection6.Y;
        case GwaAxisDirection6.Z:
          return AxisDirection6.Z;
        case GwaAxisDirection6.XX:
          return AxisDirection6.XX;
        case GwaAxisDirection6.YY:
          return AxisDirection6.YY;
        case GwaAxisDirection6.ZZ:
          return AxisDirection6.ZZ;
        default:
          return AxisDirection6.NotSet;
      }
    }
    #endregion

    #region Analysis Stage
    private GSAStage GetStageFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaAnalStage, GSAStage>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0)
        ? speckleObjects.First() : null;
    }
    #endregion
    #endregion

    #region ToNative
    #endregion

    #endregion

    #region private_classes
    internal class ToSpeckleResult
    {
      public bool Success = true;
      public List<Base> LayerAgnosticObjects;
      public List<Base> DesignLayerOnlyObjects;
      public List<Base> AnalysisLayerOnlyObjects;
      public List<Base> ResultObjects;
      public List<Base> ModelObjects
      {
        get
        {
          var objects = new List<Base>();
          if (LayerAgnosticObjects != null)
          {
            objects.AddRange(LayerAgnosticObjects);
          }
          if (DesignLayerOnlyObjects != null)
          {
            objects.AddRange(DesignLayerOnlyObjects);
          }
          if (AnalysisLayerOnlyObjects != null)
          {
            objects.AddRange(AnalysisLayerOnlyObjects);
          }
          return objects;
        }
      }

      public ToSpeckleResult(bool success)  //Used mainly when there is an error
      {
        this.Success = success;
      }

      public ToSpeckleResult(Base layerAgnosticObject)
      {
        this.LayerAgnosticObjects = new List<Base>() { layerAgnosticObject };
      }

      public ToSpeckleResult(List<Base> layerAgnosticObjects, List<Base> designLayerOnlyObjects = null, List<Base> analysisLayerOnlyObjects = null, List<Base> resultObjects = null)
      {
        this.DesignLayerOnlyObjects = designLayerOnlyObjects;
        this.AnalysisLayerOnlyObjects = analysisLayerOnlyObjects;
        this.LayerAgnosticObjects = layerAgnosticObjects;
        this.ResultObjects = resultObjects;
      }
    }
    #endregion
  }
}
