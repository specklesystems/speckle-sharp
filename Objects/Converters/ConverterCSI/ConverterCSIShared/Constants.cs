namespace ConverterCSIShared;

internal static class Constants
{
  // these three slugs were previously used by the connector and now only serve to maintain backwards
  // compatibility with the slugs that may be saved to a user's existing stream card
  public const string LEGACY_SEND_NODE_RESULTS = "sendNodeResults";
  public const string LEGACY_SEND_1D_RESULTS = "send1DResults";
  public const string LEGACY_SEND_2D_RESULTS = "send2DResults";

  public const string RESULTS_NODE_SLUG = "node-results";
  public const string RESULTS_1D_SLUG = "1d-results";
  public const string RESULTS_2D_SLUG = "2d-results";
  public const string RESULTS_LOAD_CASES_SLUG = "load-cases";

  public const string BEAM_FORCES = "Beam Forces";
  public const string BRACE_FORCES = "Brace Forces";
  public const string COLUMN_FORCES = "Column Forces";
  public const string OTHER_FORCES = "Other Forces";
  public const string FORCES = "Forces";
  public const string STRESSES = "Stresses";
  public const string DISPLACEMENTS = "Displacements";
  public const string VELOCITIES = "Velocities";
  public const string ACCELERATIONS = "Accelerations";
}
