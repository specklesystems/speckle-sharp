using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConverterGSA
{
  public class ConverterGSA : ISpeckleConverter
  {
    #region ISpeckleConverter props

    public string Description => "Default Speckle Kit for GSA";
    public string Name => nameof(ConverterGSA);
    public string Author => "Arup";
    public string WebsiteOrEmail => "https://www.oasys-software.com/";

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();

    #endregion ISpeckleConverter props

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public Dictionary<Type, Func<GsaRecord_, List<Base>>> ToSpeckleFns;

    public ConverterGSA()
    {
      ToSpeckleFns = new Dictionary<Type, Func<GsaRecord_, List<Base>>>()
        {
          { typeof(GsaNode), GsaNodeToSpeckle }
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
      var native = objects.Where(o => o.GetType().IsSubclassOf(typeof(GsaRecord_)));
      if (native.Count() < objects.Count())
      {
        ConversionErrors.Add(new Exception("Non-native objects: " + (objects.Count() - native.Count())));
        objects = native.ToList();
      }
      return objects.SelectMany(x => ToSpeckle((GsaRecord_)x)).ToList();
    }

    public IEnumerable<string> GetServicedApplications()
    {
      throw new NotImplementedException();
    }

    public void SetContextDocument(object doc)
    {
      throw new NotImplementedException();
    }

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects)
    {
      throw new NotImplementedException();
    }

    private List<Base> ToSpeckle(GsaRecord_ nativeObject)
    {
      var nativeType = nativeObject.GetType();
      return ToSpeckleFns[nativeType](nativeObject);
    }

    public List<Base> GsaNodeToSpeckle(GsaRecord_ nativeObject)
    {
      var gsaNode = (GsaNode)nativeObject;

      return new List<Base>();
    }
  }
}
