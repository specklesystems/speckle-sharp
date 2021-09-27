using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Materials
{
    public class Material : Base
    {
        public string name { get; set; }
        public string grade { get; set; } //ex. 350W(G40.21 Plate), could be set in name too
        public MaterialType type { get; set; }
        public string designCode { get; set; }
        public string codeYear { get; set; }

        public Material() { }

        [SchemaInfo("Material", "Creates a Speckle structural material", "Structural", "Materials")]
        public Material(string name, MaterialType type, string grade = null)
        {
            this.name = name;
            this.type = type;
            this.grade = grade;
            this.designCode = designCode;
            this.codeYear = codeYear;
        }
    }
}
