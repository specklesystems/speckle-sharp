using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConverterGSA
{
  public class ConverterGSA : ISpeckleConverter
  {
    public string Description => throw new NotImplementedException();

    public string Name => throw new NotImplementedException();

    public string Author => throw new NotImplementedException();

    public string WebsiteOrEmail => throw new NotImplementedException();

    public HashSet<Exception> ConversionErrors => throw new NotImplementedException();

    public bool CanConvertToNative(Base @object)
    {
      throw new NotImplementedException();
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
      throw new NotImplementedException();
    }

    public IEnumerable<string> GetServicedApplications()
    {
      throw new NotImplementedException();
    }

    public void SetContextDocument(object doc)
    {
      throw new NotImplementedException();
    }

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects)
    {
      throw new NotImplementedException();
    }

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects)
    {
      throw new NotImplementedException();
    }
  }
}
