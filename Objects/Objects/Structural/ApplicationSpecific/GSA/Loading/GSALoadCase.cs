using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Objects.Structural.Loading;

namespace Objects.Structural.GSA.Loading
{
    public class GSALoadCase : LoadCase
    {
        public int nativeId { get; set; }
        public LoadDirection2D direction { get; set; }
        public string include { get; set; }
        public bool bridge { get; set; }
        public GSALoadCase() { }

        [SchemaInfo("GSALoadCase", "Creates a Speckle structural load case for GSA", "GSA", "Loading")]
        public GSALoadCase(int nativeId, string name, LoadType loadType, LoadDirection2D loadDirection, string source = null, ActionType actionType = ActionType.None, string description = null, string include = null, bool bridge = false)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.loadType = loadType;
            this.group = source;
            this.actionType = actionType;
            this.description = description;
            this.direction = loadDirection;
            this.include = include;
            this.bridge = bridge;
        }
    }





}
