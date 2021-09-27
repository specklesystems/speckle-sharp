using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Loading
{
    public class LoadCase : Base // or LoadPattern? (per CSI)
    {
        public string name { get; set; } //load case title, ex. "Dead load"
        public LoadType loadType { get; set; } //Dead load              
        public string source { get; set; } //or load group, "A"  
        public ActionType actionType { get; set; } //Permanent
        public string description { get; set; } //category as alternative, ex. Offices – Cat.B, assembly area     
        public LoadCase() { }

        [SchemaInfo("LoadCase", "Creates a Speckle structural load case", "Structural", "Loading")]
        public LoadCase(string name, LoadType loadType, string source = null, ActionType actionType = ActionType.None, string description = null) 
        {
            this.name = name;
            this.loadType = loadType;
            this.source = source;
            this.actionType = actionType;
            this.description = description;
        }
    }
}
