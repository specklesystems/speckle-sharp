using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Objects.BuiltElements.Revit;
using Objects.Utils;

namespace Objects.Other
{
    /// <summary>
    /// Generic class for materials containing generic parameters
    /// </summary>
    public class Material : Base
    {

        public string name { get; set; }


        public Material() { }

        [SchemaInfo("RevitMaterial", "Creates a Speckle material", "BIM", "Architecture")]
        public Material(string name)
        {
            this.name = name;
        }
    }
}

namespace Objects.Other.Revit
{

    /// <summary>
    /// Material in Revit defininf all revit properties from Autodesk.Revit.DB.Material
    /// </summary>
    public class RevitMaterial : Material
    {
        public string materialCategory { get; set; }
        public string materialClass { get; set; }

        public int shininess { get; set; }
        public int smoothness { get; set; }
        public int transparency { get; set; }

        public Base parameters { get; set; }

        public RevitMaterial() { }

        [SchemaInfo("RevitMaterial", "Creates a Speckle material", "Revit", "Architecture")]
        public RevitMaterial(string name, string category, string materialclass, int shiny, int smooth, int transparent,
      List<Parameter> parameters = null)
        {
            this.parameters = parameters.ToBase();
            this.name = name;
            this.materialCategory = category;
            this.materialClass = materialclass;
            this.shininess = shiny;
            this.smoothness = smooth;
            this.transparency = transparent;
        }
    }
}


