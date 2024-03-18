﻿using System.Collections.Generic;
using Speckle.Connectors.DUI.Bindings;

namespace Speckle.Connectors.Rhino7.Filters;

public class RhinoEverythingFilter : EverythingSendFilter
{
  public override List<string> GetObjectIds() => new(); // TODO

  public override bool CheckExpiry(string[] changedObjectIds) => true;
}
