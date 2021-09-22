using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Objects.Structural.Loading;

namespace Objects.Structural.GSA.Loading
{
    public class LoadCase : Structural.Loading.LoadCase
    {
        public int nativeId { get; set; }
        public LoadCase() { }

        [SchemaInfo("GSALoadCase", "Creates a Speckle structural load case for GSA", "GSA", "Loading")]
        public LoadCase(int nativeId, string name, LoadType loadType, string source = null, ActionType actionType = ActionType.None, string description = null)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.loadType = loadType;
            this.group = source;
            this.actionType = actionType;
            this.description = description;
        }
    }





}
