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

    private delegate ToSpeckleResult ToSpeckleMethodDelegate(GsaRecord gsaRecord, GSALayer layer = GSALayer.Both);

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
      { ModelAspect.Elements, new List<Type>() { typeof(GSAAssembly), typeof(Axis), typeof(GSAElement1D), typeof(GSAElement2D), typeof(GSAElement3D), typeof(GSAMember1D), typeof(GSAMember2D) } },
      { ModelAspect.Loads, new List<Type>()
        { typeof(GSAAnalysisCase), typeof(GSATask), typeof(GSALoadCase), typeof(GSALoadBeam), typeof(GSALoadFace), typeof(GSALoadGravity), typeof(GSALoadCase), typeof(GSALoadCombination), typeof(GSALoadNode) } },
      { ModelAspect.Restraints, new List<Type>() { typeof(Objects.Structural.Geometry.Restraint) } },
      { ModelAspect.Properties, new List<Type>()
        { typeof(GSAProperty1D), typeof(GSAProperty2D), typeof(SectionProfile), typeof(PropertyMass), typeof(PropertySpring), typeof(PropertyDamper), typeof(Property3D) } },
      { ModelAspect.Materials, new List<Type>() { typeof(GSAMaterial), typeof(Concrete), typeof(Steel), typeof(Concrete) } }
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

      if (!ConvertToSpeckle(objects.Cast<GsaRecord>(), 
        Instance.GsaModel.StreamLayer, (Instance.GsaModel.StreamSendConfig == StreamContentConfig.ModelAndResults),
        modelInfo, out retList))
      {
        return null;
      }

      return retList;
    }

    private bool ConvertToSpeckle(IEnumerable<GsaRecord> gsaRecords, GSALayer sendLayer, bool sendResults, ModelInfo modelInfo, out List<Base> returnObjects)
    {
      var typeGens = Instance.GsaModel.Proxy.GetTxTypeDependencyGenerations(sendLayer);
      returnObjects = new List<Base>();

      var gsaRecordsByType = gsaRecords.GroupBy(r => r.GetType()).ToDictionary(r => r.Key, r => r.ToList());

      var modelsByLayer = new Dictionary<GSALayer, Model>() { { GSALayer.Design, new Model() { specs = modelInfo, layerDescription = "Design" } } };
      var modelHasData = new Dictionary<GSALayer, bool>() { { GSALayer.Design, false } };
      if (sendLayer == GSALayer.Both)
      {
        modelsByLayer.Add(GSALayer.Analysis, new Model() { specs = modelInfo, layerDescription = "Analysis" });
        modelHasData.Add(GSALayer.Analysis, false);
      }

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
                  if (AssignIntoModel(modelsByLayer[l], l, toSpeckleResult))
                  {
                    modelHasData[l] = true;
                  }
                }
                
                var speckleObjs = toSpeckleResult.ModelObjects; //Don't need to add result objects to the cache since they aren't needed for serialisation
                if (speckleObjs != null && speckleObjs.Count > 0)
                {
                  Instance.GsaModel.Cache.SetSpeckleObjects(nativeObj, speckleObjs.ToDictionary(so => so.applicationId, so => (object)so));
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
      var objs = toSpeckleResult.GetModelObjectsForLayer(layer);
      if (objs == null || objs.Count == 0)
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

      public ToSpeckleResult(IEnumerable<Base> layerAgnosticObjects = null, IEnumerable<Base> designLayerOnlyObjects = null, IEnumerable<Base> analysisLayerOnlyObjects = null, IEnumerable<Base> resultObjects = null)
      {
        if (designLayerOnlyObjects != null)
        {
          this.DesignLayerOnlyObjects = designLayerOnlyObjects.ToList();
        }
        if (analysisLayerOnlyObjects != null)
        {
          this.AnalysisLayerOnlyObjects = analysisLayerOnlyObjects.ToList();
        }
        if (layerAgnosticObjects != null)
        {
          this.LayerAgnosticObjects = layerAgnosticObjects.ToList();
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
          this.DesignLayerOnlyObjects = new List<Base>() { designLayerOnlyObject };
        }
        if (analysisLayerOnlyObject != null)
        {
          this.AnalysisLayerOnlyObjects = new List<Base>() { analysisLayerOnlyObject };
        }
        if (layerAgnosticObject != null)
        {
          this.LayerAgnosticObjects = new List<Base>() { layerAgnosticObject };
        }
        if (resultObjects != null)
        {
          this.ResultObjects = resultObjects.ToList();
        }
      }

      public List<Base> GetModelObjectsForLayer(GSALayer layer)
      {
        if (layer != GSALayer.Design && layer != GSALayer.Analysis)
        {
          return null;
        }
        var objs = new List<Base>();
        if (layer == GSALayer.Design)
        {
          if (DesignLayerOnlyObjects != null)
          {
            objs.AddRange(DesignLayerOnlyObjects);
          }
        }
        else
        {
          if (AnalysisLayerOnlyObjects != null)
          {
            objs.AddRange(AnalysisLayerOnlyObjects);
          }
        }
        if (LayerAgnosticObjects != null)
        {
          objs.AddRange(LayerAgnosticObjects);
        }
        return objs;
      }
    }
    #endregion
  }
}
