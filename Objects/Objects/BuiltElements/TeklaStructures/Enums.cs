namespace Objects.BuiltElements.TeklaStructures;

public enum TeklaBeamType
{
  Beam,
  PolyBeam,
  SpiralBeam
}

public enum TeklaChamferType
{
  none,
  line,
  rounding,
  arc,
  arc_point,
  square,
  square_parallel,
  line_and_arc
}

public enum TeklaWeldType
{
  none,
  edge_flange,
  square_groove_butt,
  bevel_groove_single_v_butt,
  bevel_groove_single_bevel_butt,
  single_v_butt_with_broad_root_face,
  single_bevel_butt_with_broad_root_face,
  u_groove_single_u_butt,
  j_groove_j_butt,
  bevel_backing,
  fillet,
  plug,
  spot,
  seam,
  slot,
  flare_bevel_groove,
  flare_v_groove,
  corner_flange,
  partial_penetration_single_bevel_butt_plus_fillet,
  partial_penetration_square_groove_plus_fillet,
  melt_through,
  steep_flanked_bevel_groove_single_v_butt,
  steep_flanked_bevel_groove_single_bevel_butt,
  edge,
  iso_surfacing,
  fold,
  inclined
}

public enum TeklaWeldIntermittentType
{
  continuous,
  chain_intermittent,
  staggered_intermittent
}

public enum TeklaDepthEnum
{
  middle,
  front,
  behind
}

public enum TeklaPlaneEnum
{
  middle,
  left,
  right
}

public enum TeklaRotationEnum
{
  front,
  top,
  back,
  below
}

public enum TeklaOpeningTypeEnum
{
  beam,
  contour
}
