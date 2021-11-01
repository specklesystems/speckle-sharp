namespace Speckle.GSA.API.GwaSchema
{
  public enum GwaSetCommandType
  {
    Set = 0,
    SetAt = 1,
    SetNoIndex = 2
  }

  public enum GSAEntity
  {
    NotSet = 0,
    NODE = 1,
    ELEMENT = 2,
    MEMBER = 3,
    LINE = 6,
    AREA = 7,
    REGION = 8
  }


  public enum Restraint
  {
    [StringValue("Free")]
    Free,
    [StringValue("Fixed")]
    Fixed,
    [StringValue("Pinned")]
    Pinned,
    [StringValue("Full rotational")]
    FullRotational,
    [StringValue("Partial rotational")]
    PartialRotational,
    [StringValue("Top flange lateral")]
    TopFlangeLateral
  }

  public enum NodeRestraint
  {
    [StringValue("free")]
    Free,
    [StringValue("pin")]
    Pin,
    [StringValue("fix")]
    Fix,
    Custom
  }

  public enum RestraintCondition
  {
    Free = 0,
    Constrained = 1
  }

  public enum CurveType
  {
    NotSet = 0,
    Lagrange,
    Circular
  }

  public enum PointDefinition
  {
    NotSet = 0,
    Points,
    Spacing,
    Storey,
    Explicit
  }

  public enum GridExpansion
  {
    NotSet = 0,
    Legacy = 1,
    PlaneAspect = 2,
    PlaneSmooth = 3,
    PlaneCorner = 4
  }

  public enum GridSurfaceSpan
  {
    NotSet = 0,
    One = 1,
    Two = 2
  }

  public enum GridLineType
  {
    NotSet = 0,
    Arc,
    Line
  }

  public enum GridSurfaceElementsType
  {
    NotSet = 0,
    OneD = 1,
    TwoD = 2
  }

  //Note: these enum values map to different integers in GWA than is shown here
  public enum GridPlaneAxisRefType
  {
    NotSet = 0,
    Global,
    XElevation,
    YElevation,
    GlobalCylindrical,
    Reference
  }

  public enum GridPlaneType
  {
    NotSet = 0,
    General = 1,
    Storey = 2
  }

  public enum AxisRefType
  {
    NotSet = 0,
    Global,
    Local,
    Reference
  }

  public enum NodeAxisRefType
  {
    NotSet = 0,
    Global,
    XElevation,
    YElevation,
    Vertical,
    Reference
  }

  public enum IncludeOption
  {
    [StringValue("INC_UNDEF")]
    Undefined,
    [StringValue("INC_SUP")]
    Unfavourable,
    [StringValue("INC_INF")]
    Favourable,
    [StringValue("INC_BOTH")]
    Both
  }

  public enum LoadCategory
  {
    [StringValue("~")]
    NotSet,
    [StringValue("A")]
    Residential,
    [StringValue("B")]
    Office,
    [StringValue("C")]
    CongregationArea,
    [StringValue("D")]
    Shop,
    [StringValue("E")]
    Storage,
    [StringValue("F")]
    LightTraffic,
    [StringValue("G")]
    Traffic,
    [StringValue("H")]
    Roofs
  }

  public enum InfType
  {
    NotSet = 0,
    FORCE,
    DISP
  }

  public enum PathType
  {
    NotSet = 0,
    LANE,
    FOOTWAY,
    TRACK,
    VEHICLE,
    CWAY_1WAY,
    CWAY_2WAY
  }

  public enum LoadBeamAxisRefType
  {
    NotSet = 0,
    Global,
    Local,
    Natural,
    Reference
  }

  public enum Property2dType
  {
    NotSet = 0,
    Stress,
    Fabric,
    Plate,
    Shell,
    Curved,
    Load
  }

  public enum Property2dRefSurface
  {
    [StringValue("CENTROID")]
    Centroid,
    [StringValue("TOP_CENTRE")]
    TopCentre,
    [StringValue("BOT_CENTRE")]
    BottomCentre
  }

  public enum Property2dMaterialType
  {
    Generic = 0,  //The default
    Steel,
    Concrete,
    FRP,
    Aluminium,
    Glass,
    Timber,
    Fabric
  }

  public enum AxisDirection3
  {
    NotSet = 0,
    X = 1,
    Y = 2,
    Z = 3,
  }

  public enum AxisDirection6
  {
    NotSet = 0,
    X,
    Y,
    Z,
    XX,
    YY,
    ZZ
  }

  public enum LoadAreaOption
  {
    NotSet = 0,
    Plane,
    PolyRef,
    Polygon
  }

  public enum LoadLineOption
  {
    NotSet = 0,
    PolyRef,
    Polygon
  }

  public enum MemberGeometry
  {
    NotSet = 0,
    LINEAR,
    ARC,
    RAD
  }

  public enum MeshingOption
  {
    NotSet = 0,
    MESH,
    SELF,
    NONE
  }

  //In keeping with the approach to be as close to the inherent GWA schema as possible, this is kept as one list, and not split up into one for 1D and one for 2D
  public enum AnalysisType
  {
    NotSet = 0,
    //1D items - when MEMB is set (using MemberType enum above) to one one of the 1D types, then, and only then, is one of the following 10 is valid
    BEAM,
    BAR,
    STRUT,
    TIE,
    ROD,
    LINK,
    SPRING,
    CABLE,
    SPACER,
    DAMPER,
    //2D items - when MEMB is set to be a 2D type, then, and only then, are one of the following valid
    EL_TYPE,
    LINEAR,
    QUADRATIC,
    RIGID
  }

  //The number values are key here
  public enum FireResistance
  {
    Undefined = 0,
    HalfHour = 30,
    OneHour = 60,
    OneAndAHalfHours = 90,
    TwoHours = 120,
    ThreeHours = 180,
    FourHours = 240
  }

  public enum LoadHeightReferencePoint
  {
    [StringValue("")]
    NotSet = 0,
    [StringValue("SHR_CENTRE")]
    ShearCentre,
    [StringValue("TOP_FLANGE")]
    TopFlange,
    [StringValue("BOT_FLANGE")]
    BottomFlange
  }

  public enum ReleaseCode
  {
    [StringValue("")]
    NotSet = 0,
    [StringValue("F")]
    Fixed,
    [StringValue("R")]
    Released,
    [StringValue("K")]
    Stiff
  }

  //Need StringValues here because the value to be inserted as a string into GWA starts with a number, so the idea of simply using <enum value>.ToString() doesn't work
  //Note: these values differ from the documentation for 10.1, which is incorrect; these values are taken from copying GWA commands from a 10.1 file
  public enum MemberType
  {
    [StringValue("")]
    NotSet = 0,
    //1D
    [StringValue("BEAM")]
    Beam,
    [StringValue("COLUMN")]
    Column,
    [StringValue("1D_GENERIC")]
    Generic1d,
    [StringValue("1D_VOID_CUTTER")]
    Void1d,
    //2D
    [StringValue("SLAB")]
    Slab,
    [StringValue("WALL")]
    Wall,
    [StringValue("2D_GENERIC")]
    Generic2d,
    [StringValue("2D_VOID_CUTTER")]
    Void2d,
    //3D
    [StringValue("3D_GENERIC")]
    Generic3d
  }

  public enum ElementType
  {
    [StringValue("BAR")]
    Bar,
    [StringValue("BEAM")]
    Beam,
    [StringValue("BRICK8")]
    Brick8,
    [StringValue("CABLE")]
    Cable,
    [StringValue("DAMPER")]
    Damper,
    [StringValue("LINK")]
    Link,
    [StringValue("PYRAMID5")]
    Pyramid5,
    [StringValue("QUAD4")]
    Quad4,
    [StringValue("QUAD8")]
    Quad8,
    [StringValue("ROD")]
    Rod,
    [StringValue("SPACER")]
    Spacer,
    [StringValue("SPRING")]
    Spring,
    [StringValue("STRUT")]
    Strut,
    [StringValue("TETRA4")]
    Tetra4,
    [StringValue("TIE")]
    Tie,
    [StringValue("TRI3")]
    Triangle3,
    [StringValue("TRI6")]
    Triangle6,
    [StringValue("WEDGE6")]
    Wedge6
  };

  public enum Section1dType
  {
    [StringValue("1D_GENERIC")]
    Generic,
    [StringValue("BEAM")]
    Beam,
    [StringValue("COLUMN")]
    Column,
    [StringValue("SLAB")]
    Slab,
    [StringValue("RIBSLAB")]
    RibbedSlab,
    [StringValue("COMPOS")]
    CompositeBeam,
    [StringValue("PILE")]
    Pile,
    [StringValue("EXPLICIT")]
    Explicit
  }

  public enum Section1dMaterialType
  {
    GENERIC,
    STEEL,
    CONCRETE,
    FRP,
    ALUMINIUM,
    TIMBER,
    GLASS,
    REBAR
  }

  public enum Load2dThermalType
  {
    [StringValue("CONS")]
    Uniform,
    [StringValue("DZ")]
    Gradient,
    [StringValue("GEN")]
    General
  }

  public enum Load2dFaceType
  {
    [StringValue("CONS")] Uniform,
    [StringValue("GEN")] General,
    [StringValue("POINT")] Point
  }

  public enum RigidConstraintType
  {
    NotSet = 0,
    ALL,
    XY_PLANE,
    YZ_PLANE,
    ZX_PLANE,
    XY_PLATE,
    YZ_PLATE,
    ZX_PLATE,
    PIN,
    XY_PLANE_PIN,
    YZ_PLANE_PIN,
    ZX_PLANE_PIN,
    XY_PLATE_PIN,
    YZ_PLATE_PIN,
    ZX_PLATE_PIN,
    Custom
  }

  public enum Section1dStandardProfileType
  {
    [StringValue("R")] Rectangular,
    [StringValue("RHS")] RectangularHollow,
    [StringValue("C")] Circular,
    [StringValue("CHS")] CircularHollow,
    [StringValue("I")] ISection,
    [StringValue("T")] Tee,
    [StringValue("CH")] Channel,
    [StringValue("A")] Angle,
    [StringValue("D")] AngleDoubled,
    [StringValue("TR")] Taper,
    [StringValue("E")] Ellipse,
    [StringValue("GI")] GeneralI,
    [StringValue("TT")] TaperT,
    [StringValue("TA")] TaperAngle,
    [StringValue("RC")] RectoCircular,
    [StringValue("RE")] RectoEllipse,
    [StringValue("TI")] TaperI,
    [StringValue("SP")] SecantPile,
    [StringValue("SPW")] SecantPileWall,
    [StringValue("OVAL")] Oval,
    [StringValue("X")] Cruciform,
    [StringValue("GZ")] GenericZ,
    [StringValue("GC")] GenericC,
    [StringValue("CUB")] Castellated,
    [StringValue("CB")] Cellular,
    [StringValue("ACB")] AsymmetricCellular,
    [StringValue("SHT")] SheetPile
  }

  public enum Section1dProfileGroup
  {
    [StringValue("STD")] Standard,
    [StringValue("CAT")] Catalogue,
    [StringValue("GEO")] Perimeter,
    [StringValue("EXP")] Explicit
  }

  public enum ComponentReflection
  {
    NONE,
    Y_AXIS,
    Z_AXIS,
    BOTH
  }

  public enum Section1dTaperType
  {
    NONE,
    SIMPLE,
    HOR_TAPER,
    LINEAR,
    HAUNCH,
    FISH_BELLY
  }

  public enum SectionSteelSectionType
  {
    [StringValue("UNDEF")] Undefined,
    [StringValue("ROLLED")] HotRolled,
    [StringValue("WELDED")] Welded,
    [StringValue("FORMED")] ColdFormed,
    [StringValue("LWELDED")] LightlyWelded,
    [StringValue("STRESS")] StressRelieved
  }

  public enum SectionSteelPlateType
  {
    [StringValue("UNDEF")] Undefined,
    [StringValue("FLAMECUT")] FlameCut,
    [StringValue("ASROLLED")] AsRolled
  }

  public enum ReferencePoint
  {
    [StringValue("CENTROID")]
    Centroid,
    [StringValue("TOP_LEFT")]
    TopLeft,
    [StringValue("TOP_CENTRE")]
    TopCentre,
    [StringValue("TOP_RIGHT")]
    TopRight,
    [StringValue("MIDDLE_LEFT")]
    MiddleLeft,
    [StringValue("MIDDLE_RIGHT")]
    MiddleRight,
    [StringValue("BOTTOM_LEFT")]
    BottomLeft,
    [StringValue("BOTTOM_CENTRE")]
    BottomCentre,
    [StringValue("BOTTOM_RIGHT")]
    BottomRight
  }

  public enum ReleaseInclusion
  {
    [StringValue("NO_RLS")]
    NotIncluded,
    [StringValue("RLS")]
    Included,
    [StringValue("STIFF")]  //Uncertain if this is actually part of the GWA as implemented in GSA 10.1, as I've not been able to create this in the UI
    Stiff
  }

  public enum ExposedSurfaces
  {
    NotSet = 0,
    ALL,
    THREE,
    TOP_BOT,
    SIDES,
    ONE,
    NONE
  }

  public enum EffectiveLengthType
  {
    [StringValue("AUTOMATIC")]
    Automatic,
    [StringValue("EFF_LEN")]
    EffectiveLength,
    [StringValue("EXPLICIT")]
    Explicit
  }

  public enum MassModification
  {
    [StringValue("")]
    NotSet = 0,
    [StringValue("MOD")]
    Modified,
    [StringValue("DEF")]
    Defined,
  }

  public enum MatAnalType
  {
    NotSet = 0,
    MAT_DRUCKER_PRAGER,
    MAT_ELAS_ISO,
    MAT_ELAS_ORTHO,
    MAT_ELAS_PLAS_ISO,
    MAT_FABRIC,
    MAT_MOHR_COULOMB
  }

  public enum MatCurveParamType
  {
    UNDEF,            //undefined
    MAT_VOID,         //void material - no strength
    EXPLICIT,         //explicit material curve
    EXPLICIT_ENV,     //explicit material curve envelope
    LINEAR,           //linear material
    ELAS_PLAS,        //elastic-perfectly plastic material
    ELAS_HARD,        //elastic-hardening material
    NO_COMPRESSION,   //no compression
    PRESTRESS,        //prestress tendons
    PROG_YIELD,       //progressive yield
    BS5400_BAR,       //BS 5400 bar curve
    PARK,             //Park model
    ASTM_STRAND,      //ASTM A416 prestress strand
    ELAS_PLAS_TEN,    //elastic-plastic tendon
    RECT_PARABOLA,    //parabola rectangle
    RECTANGLE,        //rectangular compression block
    BILINEAR,         //bilinear stress block
    POPOVICS,         //Popovics
    FIB_SCHEMATIC,    //FIB schematic (BS8110 part2 curve)
    RECT_AISC,        //AISC 360 filled compression tube
    BS8110_PT2,       //part2 curve
    NO_TENSION,       //no tension
    INTERPOLATED,     //interplated between cracked and uncracked
    TR59,             //Technical Report 59 (was ICE technical note 372)
    PD6687,           //PD 6687 variant of Eurocode model
    EC2_CONFINED,     //EC2 confined
    MANDER,           //Mander
    MANDER_CONFINED,  //Mander confined
    MOD_RECTANGLE,    //modified rectangle
    RIGID_PLAS        //rigid-plastic
  }

  public enum MatType
  {
    STEEL,
    CONCRETE,
    FRP,
    ALUMINIUM,
    TIMBER,
    GLASS,
    FABRIC,
    REBAR,
    GENERIC
  }

  public enum MatConcreteType
  {
    [StringValue("CUBE")]
    CUBE,
    [StringValue("CYLINDER")]
    CYLINDER
  }

  public enum MatConcreteCement
  {
    N,
    S,
    R
  }

  public enum Colour
  {
    NotSet = 0,
    NO_RGB,
    BLACK,
    MAROON,
    DARK_RED,
    RED,
    ORANGE_RED,
    DARK_GREEN,
    GREEN,
    OLIVE,
    DARK_ORANGE,
    ORANGE,
    GOLD,
    LAWN_GREEN,
    LIME,
    CHARTREUSE,
    YELLOW,
    DARK_GOLDENROD,
    SADDLE_BROWN,
    CHOCOLATE,
    GOLDENROD,
    FIREBRICK,
    FOREST_GREEN,
    OLIVE_DRAB,
    BROWN,
    SIENNA,
    DARK_OLIVE_GREEN,
    GREEN_YELLOW,
    LIMEGREEN,
    YELLOW_GREEN,
    CRIMSON,
    PERU,
    TOMATO,
    DARK_SLATE_GREY,
    CORAL,
    SEA_GREEN,
    INDIAN_RED,
    SANDY_BROWN,
    DIM_GREY,
    DARK_KHAKI,
    MIDNIGHT_BLUE,
    MEDIUM_SEA_GREEN,
    SALMON,
    DARK_SALMON,
    LIGHT_SALMON,
    SPRING_GREEN,
    NAVY,
    PURPLE,
    TEAL,
    GREY,
    LIGHT_CORAL,
    INDIGO,
    MEDIUM_VIOLET_RED,
    BURLYWOOD,
    DARK_BLUE,
    DARK_MAGENTA,
    DARK_SLATE_BLUE,
    DARK_CYAN,
    TAN,
    KHAKI,
    ROSY_BROWN,
    DARK_SEA_GREEN,
    SLATE_GREY,
    LIGHT_GREEN,
    DEEP_PINK,
    PALE_VIOLET_RED,
    PALE_GREEN,
    LIGHT_SLATE_GREY,
    MEDIUM_SPRING_GREEN,
    CADET_BLUE,
    DARK_GREY,
    LIGHT_SEA_GREEN,
    MEDIUM_AQUAMARINE,
    PALE_GOLDENROD,
    NAVAJO_WHITE,
    WHEAT,
    HOT_PINK,
    STEEL_BLUE,
    MOCCASIN,
    PEACH_PUFF,
    SILVER,
    LIGHT_PINK,
    BISQUE,
    PINK,
    DARK_ORCHID,
    MEDIUM_TURQUOISE,
    MEDIUM_BLUE,
    SLATE_BLUE,
    BLANCHED_ALMOND,
    LEMON_CHIFFON,
    TURQUOISE,
    DARK_TURQUOISE,
    LIGHT_GOLDENROD_YELLOW,
    DARK_VIOLET,
    MEDIUM_ORCHID,
    LIGHT_GREY,
    AQUAMARINE,
    PAPAYA_WHIP,
    ORCHID,
    ANTIQUE_WHITE,
    THISTLE,
    MEDIUM_PURPLE,
    GAINSBORO,
    BEIGE,
    CORNSILK,
    PLUM,
    LIGHT_STEEL_BLUE,
    LIGHT_YELLOW,
    ROYAL_BLUE,
    MISTY_ROSE,
    BLUE_VIOLET,
    LIGHT_BLUE,
    POWDERBLUE,
    LINEN,
    OLDLACE,
    SKYBLUE,
    CORNFLOWER_BLUE,
    MEDIUM_SLATE_BLUE,
    VIOLET,
    PALE_TURQUOISE,
    SEASHELL,
    FLORAL_WHITE,
    HONEYDEW,
    IVORY,
    LAVENDER_BLUSH,
    WHITE_SMOKE,
    LIGHT_SKY_BLUE,
    LAVENDER,
    SNOW,
    MINT_CREAM,
    BLUE,
    MAGENTA,
    DODGER_BLUE,
    DEEP_SKY_BLUE,
    ALICE_BLUE,
    GHOST_WHITE,
    CYAN,
    LIGHT_CYAN,
    AZURE,
    WHITE
  }

  public enum Dimension
  {
    NotSet,
    NULL,
    FORCE,
    LENGTH,
    MASS,
    DISP,
    PROP_LEN,
    TIME,
    TEMPERATURE,
    PRESSURE,
    GRD_ACCEL,
    ENERGY,
    VEL,
    ANGLE,
    STRAIN,
    WORK,
    MOMENT,
    STRESS,
    FORCE_PER_LENGTH,
    MOMENT_PER_LENGTH,
    MOMENT_PER_ROTATION,
    EI,
    WARP_CONST,
    AREA,
    FIRST_MOMENT_AREA,
    SECOND_MOMENT_AREA,
    VOLUME,
    PER_LENGTH,
    PER_AREA,
    PER_VOLUME,
    CS_AREA,
    CS_AREA_PER_LENGTH,
    CURVATURE,
    INERTIA,
    DENSITY,
    AREA_DENSITY,
    WEIGHT_DENSITY,
    MASS_PER_LENGTH,
    PER_MASS,
    ANGULAR_VELOCITY,
    ANGULAR_ACCELERATION,
    FLOW_RATE,
    FREQUENCY,
    ANGULAR_FREQUENCY,
    PERIOD,
    THERMAL_EXPANSION,
    TEMPERATURE_GRADIENT,
    IMPULSE,
    IMPULSE_PER_AREA,
    IMPULSE_PER_LENGTH,
    MOMENTUM,
    ANGULAR_MOMENTUM,
    I_PER_LENGTH,
    VOLUME_PER_AREA,
    DAMPING,
    ENERGY_DENSITY,
    ENERGY_PER_LENGTH,
    COMPLIANCE,
    DEGREE,
    PERCENT
  }

  public enum StructuralLoadCaseType
  {
    NotSet,
    Generic,
    Dead,
    Soil,
    Live,
    Rain,
    Snow,
    Wind,
    Earthquake,
    Thermal
  }

  public enum StructuralSpringPropertyType
  {
    NotSet = 0,
    General,
    Axial,
    Torsional,
    //Matrix, not supported yet
    Compression,
    Tension,
    //Connector, GWA commands using this doesn't seem to work yet
    Lockup,
    Gap,
    Friction
  }

  public enum UnitDimension
  {
    //These are described in dimension_syntax overlapping the values of UNIT_DATA, at https://arup-group.github.io/oasys-combined/GSA_Text.html
    [StringValue("LENGTH")]
    Length,
    [StringValue("SECTION")]
    Sections,
    [StringValue("DISP")]
    Displacements,
    [StringValue("STRESS")]
    Stress,
    [StringValue("FORCE")]
    Force,
    [StringValue("MASS")]
    Mass,
    [StringValue("TIME")]
    Time,
    [StringValue("TEMP")]
    Temperature,
    [StringValue("VEL")]
    Velocity,
    [StringValue("ACCEL")]
    Acceleration,
    [StringValue("ENERGY")]
    Energy,
    [StringValue("ANGLE")]
    Angle,
    [StringValue("STRAIN")]
    Strain
  }
}
