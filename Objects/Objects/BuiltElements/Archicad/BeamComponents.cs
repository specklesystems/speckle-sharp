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
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool circleBased { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string modelElemStructureType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double nominalHeight { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double nominalWidth { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool isHomogeneous { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double endWith { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double endHeight { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool isEndWidthAndHeightLinked { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool isWidthAndHeightLinked { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string profileAttrName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string buildingMaterial { get; set; }
    }

    public class Scheme : Base
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string lengthType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double fixedLength { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double lengthProportion { get; set; }
    }

    public class Cut : Base
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string cutType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double customAngle { get; set; }
    }

    public class Hole : Base
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string holeType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool holeContourOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public System.Int32 holeId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double centerx { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double centerz { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double width { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double height { get; set; }
    }
}