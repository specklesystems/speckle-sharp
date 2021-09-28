using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABSUtils
    {
    public enum ETABSAPIUsableTypes
    {
        Point = 1, // cPointObj
        Frame = 2, // cFrameObj
                   //Tendon = 3, 
        Area = 4, // cAreaObj
        //Link = 5, // cLinkObj
        //PropMaterial = 6, // cPropFrame which is material property
        //                  //PropRebar = 7, // cPropRebar doesn't have set methods
        //PropFrame = 8, // cPropFrame which is Frame section property
        //LoadCase = 9, // cLoadCases
        //LoadPattern = 10, // cLoadPatterns
        //                  //Group = 11, // cGroup
        //GridSys = 12, // cGridSys
        //Combo = 13, // cCombo
        //            //Constraint = 14, // cConstraint; api manual says use diaphragm instead
        //DesignSteel = 15, // cDesignSteel
        //DesignConcrete = 16, // cDesignConcrete
        //Story = 17, /// cStory
        //Diaphragm = 18, // cDiaphragm
        //                // Line = 19, // cLineElm
        //PierLabel = 20, // cPierLabel
        //PropAreaSpring = 21, // cPropAreaSpring 
        //PropLineSpring = 22, // cPropLineSpring
        //PropPointSpring = 23, // cPropPointSpring
        //SpandrelLabel = 24, // cSpandrelLabel
        //                    //Tower = 25, // cTower
        //                    // Cable = 26,
        //                    // Solid = 27,
        //                    // DesignProcedure = 28,
        //                    // DesignStrip = 29,
        //PropTendon = 30,
        //PropLink = 31
    }
}
}
