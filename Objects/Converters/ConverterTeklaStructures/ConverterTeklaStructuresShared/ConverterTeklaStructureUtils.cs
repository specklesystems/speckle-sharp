using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Geometry;
//using Tekla.Structures.Model;
using System.Linq;


namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public enum TeklaStructuresConverterSupported
    {
      Node,
      Line,
      Element1D,
      Element2D,
      Model,
    }

    public enum TeklaStructuresAPIUsableTypes
    {

    }
  }
}
