namespace Objects.Structural.Geometry;

public enum RestraintType
{
  Free, //Release
  Pinned,
  Fixed,
  Roller
  //Spring //flexible
  //rigid, free, flexible, comp only, tens only, flex comp only, flex tens only, non lin <-- SAF
  //free, fixed, fixed negative, fixed positive, spring, spring negative, spring positive, spring relative, spring relative neg, spring relative pos, non lin, friction, damped, gap <-- BHoM
}

public enum RestraintDescription
{
  none,
  all,
  x,
  y,
  z,
  xy,
  xz,
  yz
}
