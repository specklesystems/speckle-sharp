using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.GSA.Loading;
using Objects.Structural.GSA.Properties;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Structural.Results;
using Objects.Structural.Analysis;
using Objects.Structural.GSA.Analysis;
using Objects.Structural.GSA.Materials;
using Objects.Structural.GSA.Bridge;

namespace ConverterGSA
{
  //Container for highest level conversion functionality, for both directions (to-Speckle and to-native)
  public partial class ConverterGSA : ISpeckleConverter
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
    
    public List<string> ConvertedObjectsList { get; set; } = new List<string>();

    private delegate ToSpeckleResult ToSpeckleMethodDelegate(GsaRecord gsaRecord, GSALayer layer = GSALayer.Both);

    //private Dictionary<GSALayer, HashSet<int>> meaningfulNodeIndices = new Dictionary<GSALayer, HashSet<int>>();
    private Dictionary<GSALayer, HashSet<string>> meaningfulNodesAppIds = new Dictionary<GSALayer, HashSet<string>>();

    #region model_group
    private enum ModelAspect
    {
      Nodes,
      Elements,
      Loads,
      Restraints,
      Properties,
      Materials
    }

    private static List<ModelAspect> modelAspectValues = Enum.GetValues(typeof(ModelAspect)).Cast<ModelAspect>().ToList();

    //These are the groupings in the Model class, which are *Speckle* object types
    private readonly Dictionary<ModelAspect, List<Type>> modelGroups = new Dictionary<ModelAspect, List<Type>>()
    {
      { ModelAspect.Nodes, new List<Type>() { typeof(GSANode) } },
      { ModelAspect.Elements, new List<Type>() { typeof(GSAAssembly), typeof(GSAElement1D), typeof(GSAElement2D), typeof(GSAElement3D), typeof(GSAMember1D), typeof(GSAMember2D),
        //CatchAll
        typeof(GSAStage), //Analysis stages
        typeof(Axis), typeof(GSAGridSurface), typeof(GSAGridPlane), typeof(GSAGridLine), typeof(GSAPolyline),  //Geometry
        typeof(GSARigidConstraint), typeof(GSAGeneralisedRestraint), //Constraints
        typeof(GSAAlignment), typeof(GSAInfluenceBeam), typeof(GSAInfluenceNode), typeof(GSAPath), typeof(GSAUserVehicle) } }, //Bridge
      { ModelAspect.Loads, new List<Type>()
        { typeof(GSAAnalysisCase), typeof(GSATask), typeof(GSALoadCase), typeof(GSALoadBeam), typeof(GSALoadFace), typeof(GSALoadGravity), 
        typeof(GSALoadCase), typeof(GSALoadCombination), typeof(GSALoadNode), typeof(GSALoadThermal2d), typeof(GSALoadGridArea), typeof(GSALoadGridLine), 
        typeof(GSALoadGridPoint) } },
      { ModelAspect.Restraints, new List<Type>() { typeof(Objects.Structural.Geometry.Restraint) } },
      { ModelAspect.Properties, new List<Type>()
        { typeof(GSAProperty1D), typeof(GSAProperty2D), typeof(SectionProfile), typeof(PropertyMass), typeof(PropertySpring), typeof(PropertyDamper), typeof(Property3D) } },
      { ModelAspect.Materials, new List<Type>() { typeof(GSAMaterial), typeof(GSAConcrete), typeof(GSASteel) } }
    };
    #endregion

    public ConverterGSA()
    {
      SetupToSpeckleFns();
      SetupToNativeFns();
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

      var retObj = (ToNativeFns.ContainsKey(t)) ? ToNativeFns[t](@object) : null;

      if (Instance.GsaModel.ConversionProgress != null)
      {
        Instance.GsaModel.ConversionProgress.Report(retObj != null);
      }

      return retObj;
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
          var speckleObjects = toSpeckleResult.ObjectsByLayer.SelectMany(kvp => kvp.Value).ToList();
          if (speckleObjects != null && speckleObjects.Count > 0)
          {
            retList.AddRange(speckleObjects.Where(so => so != null));
          }
        }
        return retList;
      }

      var allGsaRecords = objects.Cast<GsaRecord>();
      var modelSettingsNativeTypes = new List<Type> { typeof(GsaUnitData), typeof(GsaTol), typeof(GsaSpecSteelDesign), typeof(GsaSpecConcDesign) };
      var modelSettingsRecords = allGsaRecords.Where(r => modelSettingsNativeTypes.Any(msnt => msnt == r.GetType()));
      var gsaRecords = allGsaRecords.Except(modelSettingsRecords);

      //TO DO - fill in this more
      var modelInfo = new ModelInfo()
      {
        application = "GSA"
      };
      if (ConvertToSpeckle(modelSettingsRecords, out ModelSettings ms))
      {
        modelInfo.settings = ms;
      }

      var sendResults = (Instance.GsaModel.StreamSendConfig == StreamContentConfig.ModelAndResults);
      if (!ConvertToSpeckle(allGsaRecords, Instance.GsaModel.StreamLayer, sendResults, modelInfo, out retList))
      {
        return null;
      }

      return retList;
    }

    private bool ConvertToSpeckle(IEnumerable<GsaRecord> modelSettingsRecords, out ModelSettings ms)
    {
      ms = new ModelSettings() { modelUnits = new ModelUnits() };

      var unitDataRecords = modelSettingsRecords.Where(r => r is GsaUnitData).Cast<GsaUnitData>().ToList();

      foreach (var ud in unitDataRecords)
      {
        switch(ud.Option)
        {
          case UnitDimension.Length: ms.modelUnits.length = ud.Name; break;
          case UnitDimension.Sections: ms.modelUnits.sections = ud.Name; break;
          case UnitDimension.Displacements: ms.modelUnits.displacements = ud.Name; break;
          case UnitDimension.Stress: ms.modelUnits.stress = ud.Name; break;
          case UnitDimension.Force: ms.modelUnits.force = ud.Name; break;
          case UnitDimension.Mass: ms.modelUnits.mass = ud.Name; break;
          case UnitDimension.Time: ms.modelUnits.time = ud.Name; break;
          case UnitDimension.Temperature: ms.modelUnits.temperature = ud.Name; break;
          case UnitDimension.Velocity: ms.modelUnits.velocity = ud.Name; break;
          case UnitDimension.Acceleration: ms.modelUnits.acceleration = ud.Name; break;
          case UnitDimension.Energy: ms.modelUnits.energy = ud.Name; break;
          case UnitDimension.Angle: ms.modelUnits.angle = ud.Name; break;
          case UnitDimension.Strain: ms.modelUnits.strain = ud.Name; break;
        }
      }

      var tol = modelSettingsRecords.FirstOrDefault(r => r is GsaTol);
      if (tol != null)
      {
        ms.coincidenceTolerance = ((GsaTol)tol).Node;
      }
      var specConc = modelSettingsRecords.FirstOrDefault(r => r is GsaSpecConcDesign);
      if (specConc != null)
      {
        ms.concreteCode = ((GsaSpecConcDesign)specConc).Code;
      }
      var specSteel = modelSettingsRecords.FirstOrDefault(r => r is GsaSpecConcDesign);
      if (specSteel != null)
      {
        ms.steelCode = ((GsaSpecConcDesign)specSteel).Code;
      }

      return true;
    }

    private bool ConvertToSpeckle(IEnumerable<GsaRecord> gsaRecords, GSALayer sendLayer, bool sendResults, ModelInfo modelInfo, out List<Base> returnObjects)
    {
      var typeGens = Instance.GsaModel.Proxy.GetTxTypeDependencyGenerations(sendLayer);
      returnObjects = new List<Base>();

      //var nodeDependentSchemaTypesByLayer = new Dictionary<GSALayer, List<Type>>();
      //nodeDependentSchemaTypesByLayer.Add(GSALayer.Design, Instance.GsaModel.Proxy.GetNodeDependentTypes(GSALayer.Design));

      var gsaRecordsByType = gsaRecords.GroupBy(r => r.GetType()).ToDictionary(r => r.Key, r => r.ToList());

      var modelsByLayer = new Dictionary<GSALayer, Model>() { { GSALayer.Design, new Model() { specs = modelInfo, layerDescription = "Design" } } };
      var modelHasData = new Dictionary<GSALayer, bool>() { { GSALayer.Design, false } };
      if (sendLayer == GSALayer.Both)
      {
        modelsByLayer.Add(GSALayer.Analysis, new Model() { specs = modelInfo, layerDescription = "Analysis" });
        modelHasData.Add(GSALayer.Analysis, false);

        //nodeDependentSchemaTypesByLayer.Add(GSALayer.Analysis, Instance.GsaModel.Proxy.GetNodeDependentTypes(GSALayer.Analysis));

        //nodeDependentSchemaTypesByLayer.Add(GSALayer.Both, Instance.GsaModel.Proxy.GetNodeDependentTypes(GSALayer.Both));
      }

      /*
      if (Instance.GsaModel.SendOnlyMeaningfulNodes)
      {//Remove nodes across the layers
        var referencedNodeIndices = new List<int>();
        foreach (var t in nodeDependentSchemaTypesByLayer[sendLayer].Where(ndst => gsaRecordsByType.Keys.Contains(ndst)))
        {
          var ns = gsaRecordsByType[t];
          foreach (var i in ns.n)
          {

          }
        }
      }
      */

      var nodesTemp = new Dictionary<GsaRecord, ToSpeckleResult>();

      var rsa = new ResultSetAll();
      bool resultSetHasData = false;

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
                var toSpeckleResult = ToSpeckle(nativeObj, sendLayer);

                foreach (var l in modelsByLayer.Keys)
                {
                  //Special case for nodes due to the possibility that not all of them should be sent, and the subset that should be sent can vary by layer
                  if (Instance.GsaModel.SendOnlyMeaningfulNodes && t == typeof(GsaNode))
                  {
                    if (!nodesTemp.ContainsKey(nativeObj))
                    {
                      nodesTemp.Add(nativeObj, toSpeckleResult);
                    }
                  }
                  else
                  {
                    if (AssignIntoModel(modelsByLayer[l], l, toSpeckleResult))
                    {
                      modelHasData[l] = true;
                    }
                  }
                }

                foreach (var l in toSpeckleResult.ObjectsByLayer.Keys)
                {
                  var layerObjectsToCache = toSpeckleResult.ObjectsByLayer[l].Where(o => o != null);
                  if (layerObjectsToCache.Count() > 0)
                  {
                    Instance.GsaModel.Cache.SetSpeckleObjects(nativeObj, layerObjectsToCache.ToDictionary(o => o.applicationId, o => (object)o), l);
                  }
                }

                if (AssignIntoResultSet(rsa, toSpeckleResult))
                {
                  resultSetHasData = true;
                }
              }
            }
            catch (Exception ex)
            {

            }
          }
        }
      }

      if (Instance.GsaModel.SendOnlyMeaningfulNodes && nodesTemp != null && nodesTemp.Keys.Count > 0)
      {
        foreach (var l in modelsByLayer.Keys)
        {
          foreach (var n in nodesTemp.Keys)
          {
            if (nodesTemp[n].ObjectsByLayer.ContainsKey(l) && nodesTemp[n].ObjectsByLayer[l] != null)
            {
              foreach (var o in nodesTemp[n].ObjectsByLayer[l])
              {
                if (meaningfulNodesAppIds[l].Contains(o.applicationId))
                {
                  AssignIntoModel(modelsByLayer[l], l, nodesTemp[n]);
                }
              }
            }
            if (nodesTemp[n].ObjectsByLayer.ContainsKey(GSALayer.Both) && nodesTemp[n].ObjectsByLayer[GSALayer.Both] != null)
            {
              foreach (var o in nodesTemp[n].ObjectsByLayer[GSALayer.Both])
              {
                if (meaningfulNodesAppIds[l].Contains(o.applicationId))
                {
                  AssignIntoModel(modelsByLayer[l], l, nodesTemp[n]);
                }
              }
            }
          }
        }
      }

      foreach (var l in modelsByLayer.Keys)
      {
        if (modelHasData[l])
        {
          returnObjects.Add(modelsByLayer[l]);
        }
      }

      if (sendResults && resultSetHasData)
      {
        returnObjects.Add(rsa);
      }

      return true;
    }

    private bool AssignIntoResultSet(ResultSetAll rsa, ToSpeckleResult toSpeckleResult)
    {
      var objs = toSpeckleResult.ResultObjects;
      if (objs == null || objs.Count == 0)
      {
        return false;
      }

      var objsByType = objs.GroupBy(o => o.GetType()).ToDictionary(g => g.Key, g => g.ToList());

      foreach (var t in objsByType.Keys)
      {
        if (objsByType[t] == null || objsByType[t].Count == 0)
        {
          continue;
        }
        if (t == typeof(ResultNode))
        {
          if (rsa.resultsNode == null)
          {
            rsa.resultsNode = new ResultSetNode(objsByType[t].Cast<ResultNode>().ToList());
          }
          else
          {
            rsa.resultsNode.resultsNode.AddRange(objsByType[t].Cast<ResultNode>().ToList());
          }
        }
        if (t == typeof(Result1D))
        {
          if (rsa.results1D == null)
          {
            rsa.results1D = new ResultSet1D(objsByType[t].Cast<Result1D>().ToList());
          }
          else
          {
            rsa.results1D.results1D.AddRange(objsByType[t].Cast<Result1D>().ToList());
          }
        }
        if (t == typeof(Result2D))
        {
          if (rsa.results2D == null)
          {
            rsa.results2D = new ResultSet2D(objsByType[t].Cast<Result2D>().ToList());
          }
          else
          {
            rsa.results2D.results2D.AddRange(objsByType[t].Cast<Result2D>().ToList());
          }
        }
        //Other result types aren't supported yet
      }
      return true;
    }

    private bool AssignIntoModel(Model model, GSALayer layer, ToSpeckleResult toSpeckleResult)
    {
      var objs = new List<Base>();
      if (toSpeckleResult.ObjectsByLayer.ContainsKey(GSALayer.Both))
      {
        objs.AddRange(toSpeckleResult.ObjectsByLayer[GSALayer.Both]);
      }
      if (toSpeckleResult.ObjectsByLayer.ContainsKey(layer))
      {
        objs.AddRange(toSpeckleResult.ObjectsByLayer[layer]);
      }
      if (objs == null || objs.Count() == 0)
      {
        return false;
      }
      var objsByType = objs.GroupBy(o => o.GetType()).ToDictionary(g => g.Key, g => g.ToList());
      int numObjs = 0;
      foreach (var sType in objsByType.Keys)
      {
        if (modelGroups[ModelAspect.Nodes].Contains(sType))
        {
          if (model.nodes == null)
          {
            model.nodes = new List<Base>();
          }
          model.nodes.AddRange(objsByType[sType]);
        }
        if (modelGroups[ModelAspect.Elements].Contains(sType))
        {
          if (model.elements == null)
          {
            model.elements = new List<Base>();
          }
          model.elements.AddRange(objsByType[sType]);
        }
        if (modelGroups[ModelAspect.Loads].Contains(sType))
        {
          if (model.loads == null)
          {
            model.loads = new List<Base>();
          }
          model.loads.AddRange(objsByType[sType]);
        }
        if (modelGroups[ModelAspect.Restraints].Contains(sType))
        {
          if (model.restraints == null)
          {
            model.restraints = new List<Base>();
          }
          model.restraints.AddRange(objsByType[sType]);
        }
        if (modelGroups[ModelAspect.Properties].Contains(sType))
        {
          if (model.properties == null)
          {
            model.properties = new List<Base>();
          }
          model.properties.AddRange(objsByType[sType]);
        }
        if (modelGroups[ModelAspect.Materials].Contains(sType))
        {
          if (model.materials == null)
          {
            model.materials = new List<Base>();
          }
          model.materials.AddRange(objsByType[sType]);
        }
        numObjs += objsByType[sType].Count();
      }
      return (numObjs > 0);
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

    #region private_classes
    internal class ToSpeckleResult
    {
      public bool Success = true;
      public Dictionary<GSALayer, List<Base>> ObjectsByLayer = new Dictionary<GSALayer, List<Base>>();
      public List<Base> ResultObjects;

      public ToSpeckleResult(bool success)  //Used mainly when there is an error
      {
        this.Success = success;
      }

      public ToSpeckleResult(Base layerAgnosticObject)
      {
        ObjectsByLayer.Add(GSALayer.Both, new List<Base> { layerAgnosticObject });
      }

      public ToSpeckleResult(IEnumerable<Base> layerAgnosticObjects = null, IEnumerable<Base> designLayerOnlyObjects = null, IEnumerable<Base> analysisLayerOnlyObjects = null, IEnumerable<Base> resultObjects = null)
      {
        if (designLayerOnlyObjects != null)
        {
          this.ObjectsByLayer.UpsertDictionary(GSALayer.Design, designLayerOnlyObjects);
        }
        if (analysisLayerOnlyObjects != null)
        {
          this.ObjectsByLayer.UpsertDictionary(GSALayer.Analysis, analysisLayerOnlyObjects);
        }
        if (layerAgnosticObjects != null)
        {
          this.ObjectsByLayer.UpsertDictionary(GSALayer.Both, layerAgnosticObjects);
        }
        if (resultObjects != null)
        {
          this.ResultObjects = resultObjects.ToList();
        }
      }

      public ToSpeckleResult(Base layerAgnosticObject = null, Base designLayerOnlyObject = null, Base analysisLayerOnlyObject = null, IEnumerable<Base> resultObjects = null)
      {
        if (designLayerOnlyObject != null)
        {
          this.ObjectsByLayer.UpsertDictionary(GSALayer.Design, designLayerOnlyObject);
        }
        if (analysisLayerOnlyObject != null)
        {
          this.ObjectsByLayer.UpsertDictionary(GSALayer.Analysis, analysisLayerOnlyObject);
        }
        if (layerAgnosticObject != null)
        {
          this.ObjectsByLayer.UpsertDictionary(GSALayer.Both, layerAgnosticObject);
        }
        if (resultObjects != null)
        {
          this.ResultObjects = resultObjects.ToList();
        }
      }
    }
    #endregion
  }
}
