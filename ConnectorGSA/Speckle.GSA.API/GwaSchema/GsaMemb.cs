using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  public class GsaMemb : GsaRecord
  {
    //Not supporting: 3D members, or 2D reinforcement
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public MemberType Type;

    #region members_for_1D_2D

    public ExposedSurfaces Exposure;
    public int? PropertyIndex;
    public int? Group;
    public List<int> NodeIndices;           //Perimeter/edge topology
    public List<List<int>> Voids;           //Void topologies corresponding to the V sections in the GWA string, like in "41 42 43 44 V(45 46 47 48)"
    public List<int> PointNodeIndices;      //Points to include, corresponding to the P list in the GWA string, like in "41 42 43 44 P(50 55)"
    public List<List<int>> Polylines;       //Polyline topologies correspoding to the L sections in the GWA string, like in "41 42 43 44 L(71 72 73)"
    public List<List<int>> AdditionalAreas; //Additional solid area topologies corresponding to the A sections in the GWA string, like in "41 42 43 44 A(45 46 47 48)"
    public int? OrientationNodeIndex;
    public double? Angle;
    public double? MeshSize;
    public bool IsIntersector;
    public AnalysisType AnalysisType;
    public FireResistance Fire;
    public double? LimitingTemperature;
    public int CreationFromStartDays;
    public int StartOfDryingDays;
    public int AgeAtLoadingDays;
    public int RemovedAtDays;
    public bool Dummy;

    #region members_1D
    public Dictionary<AxisDirection6, ReleaseCode> Releases1;
    public List<double> Stiffnesses1;
    public Dictionary<AxisDirection6, ReleaseCode> Releases2;
    public List<double> Stiffnesses2;
    public Restraint RestraintEnd1;
    public Restraint RestraintEnd2;
    public EffectiveLengthType EffectiveLengthType;
    public double? LoadHeight;
    public LoadHeightReferencePoint LoadHeightReferencePoint;
    public bool MemberHasOffsets;
    public bool End1AutomaticOffset;
    public bool End2AutomaticOffset;
    public double? End1OffsetX;
    public double? End2OffsetX;
    public double? OffsetY;
    public double? OffsetZ;

    #region members_1D_eff_len
    //Only one of each set of EffectiveLength__ and Fraction__ values will be filled.  This could be reviewed and refactored accordingly
    public double? EffectiveLengthYY;
    public double? PercentageYY;  //Range: 0-100%
    public double? EffectiveLengthZZ;
    public double? PercentageZZ;  //Range: 0-100%
    public double? EffectiveLengthLateralTorsional;
    public double? FractionLateralTorsional;  //Range: 0-100%
    #endregion

    #region members_1D_explicit
    //Supporting "shortcuts" only, not restraint definitions down to the level of rotational (F1, F2, XX, YY, ZZ) and translational (F1, F2, Z, Y) restraints
    public List<RestraintDefinition> SpanRestraints;
    public List<RestraintDefinition> PointRestraints;
    #endregion

    #endregion

    #region members_2D
    public double? Offset2dZ;
    public bool OffsetAutomaticInternal;

    //Rebar not supported yet - more members to be added here in the future to store rebar-related data
    #endregion

    #endregion

    public GsaMemb() : base()
    {
      Version = 8;
    }

  }

  public struct RestraintDefinition
  {
    public bool All;
    public int? Index;
    public Restraint Restraint;
  }

}
