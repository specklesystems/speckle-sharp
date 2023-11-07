using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace ConverterCSIShared
{
  internal static class Constants
  {
    // these three slugs were previously used by the connector and now only serve to maintain backwards
    // compatibility with the slugs that may be saved to a user's existing stream card
    public const string LegacySendNodeResults = "sendNodeResults";
    public const string LegacySend1DResults = "send1DResults";
    public const string LegacySend2DResults = "send2DResults";

    public const string ResultsNodeSlug = "node-results";
    public const string Results1dSlug = "1d-results";
    public const string Results2dSlug = "2d-results";

    public const string BeamForces = "Beam Forces";
    public const string BraceForces = "Brace Forces";
    public const string ColumnForces = "Column Forces";
    public const string OtherForces = "Other Forces";
    public const string Forces = "Forces";
    public const string Stresses = "Stresses";
    public const string Displacements = "Displacements";
    public const string Velocities = "Velocities";
    public const string Accelerations = "Accelerations";
  }
}
