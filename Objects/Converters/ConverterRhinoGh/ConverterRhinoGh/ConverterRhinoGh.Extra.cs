using Objects;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    private string _modelUnits;
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
        {
          _modelUnits = Doc.ModelUnitSystem.ToSpeckle();
        }
        return _modelUnits;
      }
    }
    private void SetUnits(IGeometry geom)
    {
      geom.units = ModelUnits;
    }



  }
}
