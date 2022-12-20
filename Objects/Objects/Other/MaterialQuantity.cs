
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Objects.Other
{
    public class MaterialQuantity : Base
    {
        [DetachProperty]
        public Objects.Other.Material material { get; set; }
        public double volume { get; set; }

        /// <summary>
        /// Area of the material on a element
        /// </summary>
        public double area { get; set; }

        /// <summary>
        /// UnitMeasure of the quantity,e.g meters implies squaremeters for area and cubicmeters for the volume
        /// </summary>
        public string units { get; set; }


        public MaterialQuantity() { }
        
        [Speckle.Core.Kits.SchemaInfo("MaterialQuantity", "Creates the quantity of a material")]
        public MaterialQuantity(Objects.Other.Material m, double volume, double  area, string units)
        {
            material = m;
            this.volume = volume;   
            this.area = area;   
            this.units = units;
        }
    }

}
