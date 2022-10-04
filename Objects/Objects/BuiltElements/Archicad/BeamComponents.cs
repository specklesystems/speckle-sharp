using System;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Other;

namespace Objects.BuiltElements.Archicad
{
    public class Segment : Base
    {
        [JsonProperty("circleBased")]
        public bool circleBased { get; set; }

        [JsonProperty("modelElemStructureType")]
        public string modelElemStructureType { get; set; }

        [JsonProperty("nominalHeight")]
        public double nominalHeight { get; set; }

        [JsonProperty("nominalWidth")]
        public double nominalWidth { get; set; }

        [JsonProperty("isHomogeneous")]
        public bool isHomogeneous { get; set; }

        [JsonProperty("endWith")]
        public double endWith { get; set; }

        [JsonProperty("endHeight")]
        public double endHeight { get; set; }

        [JsonProperty("isEndWidthAndHeightLinked")]
        public bool isEndWidthAndHeightLinked { get; set; }

        [JsonProperty("isWidthAndHeightLinked")]
        public bool isWidthAndHeightLinked { get; set; }

        [JsonProperty("profileAttrName")]
        public string profileAttrName { get; set; }

        [JsonProperty("buildingMaterial")]
        public string buildingMaterial { get; set; }
    }

    public class Scheme : Base
    {
        [JsonProperty("lengthType")]
        public string lengthType { get; set; }

        [JsonProperty("fixedLength")]
        public double fixedLength { get; set; }

        [JsonProperty("lengthProportion")]
        public double lengthProportion { get; set; }
    }

    public class Cut : Base
    {
        [JsonProperty("cutType")]
        public string cutType { get; set; }

        [JsonProperty("customAngle")]
        public double customAngle { get; set; }
    }

    public class Hole : Base
    {
        [JsonProperty("holeType")]
        public string holeType { get; set; }

        [JsonProperty("holeContureOn")]
        public bool holeContureOn { get; set; }

        [JsonProperty("holeID")]
        public System.Int32 holeID { get; set; }

        [JsonProperty("centerx")]
        public double centerx { get; set; }

        [JsonProperty("centerz")]
        public double centerz { get; set; }

        [JsonProperty("width")]
        public double width { get; set; }

        [JsonProperty("height")]
        public double height { get; set; }
    }

}
