using System;
using System.Collections.Generic;
using System.Text;
using CSiAPIv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Models;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void FloorPropertyToNative(CSIProperty2D property2D, ref ApplicationObject appObj)
    {
      int? success = null;
      if (property2D.deckType != Structural.CSI.Analysis.DeckType.Null)
      {
        SetDeck(property2D);
        switch (property2D.deckType)
        {
          case Structural.CSI.Analysis.DeckType.Filled:
            var deckFilled = (CSIProperty2D.DeckFilled)property2D;
            success = Model.PropArea.SetDeckFilled(deckFilled.name,
                deckFilled.SlabDepth,
                deckFilled.RibDepth,
                deckFilled.RibWidthTop,
                deckFilled.RibWidthBot,
                deckFilled.RibSpacing,
                deckFilled.ShearThickness,
                deckFilled.UnitWeight,
                deckFilled.ShearStudDia,
                deckFilled.ShearStudHt,
                deckFilled.ShearStudFu);
            break;
          case Structural.CSI.Analysis.DeckType.Unfilled:
            var deckUnfilled = (CSIProperty2D.DeckUnFilled)property2D;
            success = Model.PropArea.SetDeckUnfilled(deckUnfilled.name,
                deckUnfilled.RibDepth,
                deckUnfilled.RibWidthTop,
                deckUnfilled.RibWidthBot,
                deckUnfilled.RibSpacing,
                deckUnfilled.ShearThickness,
                deckUnfilled.UnitWeight);
            break;
          case Structural.CSI.Analysis.DeckType.SolidSlab:
            var deckSlab = (CSIProperty2D.DeckSlab)property2D;
            success = Model.PropArea.SetDeckSolidSlab(deckSlab.name,
                deckSlab.SlabDepth,
                deckSlab.ShearStudDia,
                deckSlab.ShearStudHt,
                deckSlab.ShearStudFu);
            break;
        }
      }
      else
      {
        switch (property2D.slabType)
        {
          case Structural.CSI.Analysis.SlabType.Slab:
            var SolidSlab = property2D;
            var shell = shellType(SolidSlab);
            success = Model.PropArea.SetSlab(SolidSlab.name, eSlabType.Slab, shell, SolidSlab.material.name, SolidSlab.thickness);
            break;
          case Structural.CSI.Analysis.SlabType.Ribbed:
            var slabRibbed = (CSIProperty2D.RibbedSlab)property2D;
            shell = shellType(slabRibbed);
            Model.PropArea.SetSlab(slabRibbed.name, eSlabType.Ribbed, shell, slabRibbed.material.name, slabRibbed.thickness);
            success = Model.PropArea.SetSlabRibbed(slabRibbed.name,
                slabRibbed.OverAllDepth,
                slabRibbed.thickness,
                slabRibbed.StemWidthTop,
                slabRibbed.StemWidthTop,
                slabRibbed.RibSpacing,
                slabRibbed.RibsParallelTo);
            break;
          case Structural.CSI.Analysis.SlabType.Waffle:
            var slabWaffled = (CSIProperty2D.WaffleSlab)property2D;
            shell = shellType(slabWaffled);
            Model.PropArea.SetSlab(slabWaffled.name, eSlabType.Waffle, shell, slabWaffled.material.name, slabWaffled.thickness);
            success = Model.PropArea.SetSlabWaffle(
                slabWaffled.name,
                slabWaffled.OverAllDepth,
                slabWaffled.thickness,
                slabWaffled.StemWidthTop,
                slabWaffled.StemWidthBot,
                slabWaffled.RibSpacingDir1,
                slabWaffled.RibSpacingDir2);
            break;
        }
      }

      if (success == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: $"{property2D.name}");
      else
        appObj.Update(status: ApplicationObject.State.Failed);
    }

    public eShellType shellType(CSIProperty2D property)
    {
      var shellType = eShellType.Layered;
      switch (property.shellType)
      {
        case Structural.CSI.Analysis.ShellType.ShellThin:
          shellType = eShellType.ShellThin;
          break;
        case Structural.CSI.Analysis.ShellType.Layered:
          shellType = eShellType.Layered;
          break;
        case Structural.CSI.Analysis.ShellType.ShellThick:
          shellType = eShellType.ShellThick;
          break;
        case Structural.CSI.Analysis.ShellType.Membrane:
          shellType = eShellType.Membrane;
          break;
      }
      return shellType;
    }
    public void SetDeck(CSIProperty2D deck)
    {
      var deckType = eDeckType.Filled;
      switch (deck.deckType)
      {
        case Structural.CSI.Analysis.DeckType.Filled:
          deckType = eDeckType.Filled;
          break;
        case Structural.CSI.Analysis.DeckType.Unfilled:
          deckType = eDeckType.Unfilled;
          break;
        case Structural.CSI.Analysis.DeckType.SolidSlab:
          deckType = eDeckType.SolidSlab;
          break;
      }
      var shell = shellType(deck);
      Model.PropArea.SetDeck(deck.name, deckType, shell, deck.material.name, deck.thickness);
    }
    public CSIProperty2D FloorPropertyToSpeckle(string property)
    {
      eDeckType deckType = eDeckType.Filled;
      eSlabType slabType = eSlabType.Drop;
      eShellType shellType = eShellType.Layered;
      string matProp = "";
      double thickness = 0;
      int color = 0;
      string notes = "";
      string GUID = "";

      int d = Model.PropArea.GetDeck(property, ref deckType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);
      if (d == 0)
      {

        var speckleProperties2D = new CSIProperty2D();
        double slabDepth = 0;
        double shearStudDia = 0;
        double shearStudFu = 0;
        double shearStudHt = 0;
        double ribDepth = 0;
        double ribWidthTop = 0;
        double ribWidthBot = 0;
        double ribSpacing = 0;
        double shearThickness = 0;
        double unitWeight = 0;
        var speckleShellType = ConvertShellType(shellType);
        if (deckType == eDeckType.Filled)
        {
          var speckleProperty2D = new CSIProperty2D.DeckFilled();
          Model.PropArea.GetDeckFilled(property,
              ref slabDepth,
              ref ribDepth,
              ref ribWidthTop,
              ref ribWidthBot,
              ref ribSpacing,
              ref shearThickness,
              ref unitWeight,
              ref shearStudDia,
              ref shearStudHt,
              ref shearStudFu);
          speckleProperty2D.SlabDepth = slabDepth;
          speckleProperty2D.ShearStudDia = shearStudDia;
          speckleProperty2D.ShearStudFu = shearStudFu;
          speckleProperty2D.RibDepth = ribDepth;
          speckleProperty2D.RibWidthTop = ribWidthTop;
          speckleProperty2D.RibWidthBot = ribWidthBot;
          speckleProperty2D.RibSpacing = ribSpacing;
          speckleProperty2D.ShearThickness = shearThickness;
          speckleProperty2D.UnitWeight = unitWeight;
          speckleProperty2D.ShearStudHt = shearStudHt;
          speckleProperty2D.deckType = Structural.CSI.Analysis.DeckType.Filled;
          setProperties(speckleProperty2D, matProp, thickness, property);
          speckleProperty2D.type2D = Structural.CSI.Analysis.CSIPropertyType2D.Deck;
          speckleProperty2D.shellType = speckleShellType;
          speckleProperty2D.applicationId = GUID;
          return speckleProperty2D;
        }
        else if (deckType == eDeckType.Unfilled)
        {
          var speckleProperty2D = new CSIProperty2D.DeckUnFilled();
          Model.PropArea.GetDeckUnfilled(property,
              ref ribDepth,
              ref ribWidthTop,
              ref ribWidthBot,
              ref ribSpacing,
              ref shearThickness,
              ref unitWeight);
          speckleProperty2D.RibDepth = ribDepth;
          speckleProperty2D.RibWidthTop = ribWidthTop;
          speckleProperty2D.RibWidthBot = ribWidthBot;
          speckleProperty2D.RibSpacing = ribSpacing;
          speckleProperty2D.ShearThickness = shearThickness;
          speckleProperty2D.UnitWeight = unitWeight;
          speckleProperty2D.deckType = Structural.CSI.Analysis.DeckType.Filled;
          setProperties(speckleProperty2D, matProp, thickness, property);
          speckleProperty2D.type2D = Structural.CSI.Analysis.CSIPropertyType2D.Deck;
          speckleProperty2D.shellType = speckleShellType;
          speckleProperty2D.applicationId = GUID;
          return speckleProperty2D;

        }
        else if (deckType == eDeckType.SolidSlab)
        {
          var speckleProperty2D = new CSIProperty2D.DeckSlab();
          Model.PropArea.GetDeckSolidSlab(property, ref slabDepth, ref shearStudDia, ref shearStudDia, ref shearStudFu);
          speckleProperty2D.SlabDepth = slabDepth;
          speckleProperty2D.ShearStudDia = shearStudDia;
          speckleProperty2D.ShearStudFu = shearStudFu;
          speckleProperty2D.ShearStudHt = shearStudHt;
          speckleProperty2D.deckType = Structural.CSI.Analysis.DeckType.SolidSlab;
          setProperties(speckleProperty2D, matProp, thickness, property);
          speckleProperty2D.type2D = Structural.CSI.Analysis.CSIPropertyType2D.Deck;
          speckleProperty2D.shellType = speckleShellType;
          speckleProperty2D.applicationId = GUID;
          return speckleProperty2D;
        }
      }
      int s = Model.PropArea.GetSlab(property, ref slabType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);
      if (s == 0)
      {
        var specklePropery2DSlab = new CSIProperty2D();
        setProperties(specklePropery2DSlab, matProp, thickness, property);
        specklePropery2DSlab.type2D = Structural.CSI.Analysis.CSIPropertyType2D.Slab;
        double overallDepth = 0;
        double slabThickness = 0;
        double stemWidthTop = 0;
        double stemWidthBot = 0;
        double ribSpacingDir1 = 0;
        double ribSpacingDir2 = 0;
        double ribSpacing = 0;
        int ribParrallelTo = 0;
        var speckleShellType = ConvertShellType(shellType);
        if (slabType == eSlabType.Waffle)
        {
          var speckleProperty2D = new CSIProperty2D.WaffleSlab();
          Model.PropArea.GetSlabWaffle(property, ref overallDepth, ref slabThickness, ref stemWidthTop, ref stemWidthBot, ref ribSpacingDir1, ref ribSpacingDir2);
          speckleProperty2D.OverAllDepth = overallDepth;
          speckleProperty2D.StemWidthBot = stemWidthBot;
          speckleProperty2D.StemWidthTop = stemWidthTop;
          speckleProperty2D.RibSpacingDir1 = ribSpacingDir1;
          speckleProperty2D.RibSpacingDir2 = ribSpacingDir2;
          speckleProperty2D.slabType = Structural.CSI.Analysis.SlabType.Waffle;
          speckleProperty2D.deckType = Structural.CSI.Analysis.DeckType.Null;
          setProperties(speckleProperty2D, matProp, thickness, property);
          speckleProperty2D.shellType = speckleShellType;
          speckleProperty2D.applicationId = GUID;
          return speckleProperty2D;
        }
        else if (slabType == eSlabType.Ribbed)
        {
          var speckleProperty2D = new CSIProperty2D.RibbedSlab();
          Model.PropArea.GetSlabRibbed(property, ref overallDepth, ref slabThickness, ref stemWidthTop, ref stemWidthBot, ref ribSpacing, ref ribParrallelTo);
          speckleProperty2D.OverAllDepth = overallDepth;
          speckleProperty2D.StemWidthBot = stemWidthBot;
          speckleProperty2D.StemWidthTop = stemWidthTop;
          speckleProperty2D.RibSpacing = ribSpacing;
          speckleProperty2D.RibsParallelTo = ribParrallelTo;
          speckleProperty2D.slabType = Structural.CSI.Analysis.SlabType.Ribbed;
          speckleProperty2D.deckType = Structural.CSI.Analysis.DeckType.Null;
          setProperties(speckleProperty2D, matProp, thickness, property);
          speckleProperty2D.shellType = speckleShellType;
          speckleProperty2D.applicationId = GUID;
          return speckleProperty2D;

        }
        else
        {
          switch (slabType)
          {
            case eSlabType.Slab:
              specklePropery2DSlab.slabType = Structural.CSI.Analysis.SlabType.Slab;
              break;
            case eSlabType.Drop:
              specklePropery2DSlab.slabType = Structural.CSI.Analysis.SlabType.Drop;
              break;
            case eSlabType.Mat:
              specklePropery2DSlab.slabType = Structural.CSI.Analysis.SlabType.Mat;
              break;
            case eSlabType.Footing:
              specklePropery2DSlab.slabType = Structural.CSI.Analysis.SlabType.Footing;
              break;
            default:
              specklePropery2DSlab.slabType = Structural.CSI.Analysis.SlabType.Null;
              break;
          }
          specklePropery2DSlab.deckType = Structural.CSI.Analysis.DeckType.Null;
          specklePropery2DSlab.shellType = speckleShellType;
          specklePropery2DSlab.applicationId = GUID;
          return specklePropery2DSlab;
        }
      }
      return null;
    }
  }
}