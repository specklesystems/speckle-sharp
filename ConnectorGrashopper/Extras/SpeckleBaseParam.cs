using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ConnectorGrashopper.Extras
{
    public class SpeckleBaseParam: GH_Param<GH_SpeckleBase>
    {
        public override Guid ComponentGuid => new Guid("55F13720-45C1-4B43-892A-25AE4D95EFF2");
        protected override Bitmap Icon => Properties.Resources.speckle_logo;
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public SpeckleBaseParam(string name, string nickname, string description, GH_ParamAccess access): 
            this(name,nickname,description,"Speckle 2", "Params",access){}
        private SpeckleBaseParam(string name, string nickname, string description, string category, string subcategory, GH_ParamAccess access) : base(name, nickname, description, category, subcategory, access)
        {
        }
    }
}