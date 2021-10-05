using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Geometry;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
    public string ModelUnits()
        {   
           
            var units = Model.GetDatabaseUnits();
            if (units != 0)
            {
                string[] unitsCat = units.ToString().Split('_');
                return unitsCat[1];
            }
            else
            {
                return null;
            }
}
    public Restraint Restraint(bool[] releases)
        {
            Restraint restraint = new Restraint();
            restraint.stiffnessX = Convert.ToInt32(!releases[0]);
            restraint.stiffnessY = Convert.ToInt32(!releases[1]);
            restraint.stiffnessZ = Convert.ToInt32(!releases[2]);
            restraint.stiffnessXX = Convert.ToInt32(!releases[3]);
            restraint.stiffnessYY = Convert.ToInt32(!releases[4]);
            restraint.stiffnessZZ = Convert.ToInt32(!releases[5]);
            return restraint;

        }

        public enum ETABSConverterSupported {
        Element1D,
        Element2D
        }

        public enum ETABSAPIUsableTypes
    {
        Point = 1, // cPointObj
        Frame = 2, // cFrameObj
                   //Tendon = 3, 
        Area = 4, // cAreaObj
        LoadPattern = 5,
        Model,
        ColumnResults,
        BeamResults,
        BraceResults,
        PierResults,
        SpandrelResults
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
