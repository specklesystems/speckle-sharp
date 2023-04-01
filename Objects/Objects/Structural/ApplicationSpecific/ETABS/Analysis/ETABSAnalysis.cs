using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Structural.CSI.Analysis
{
  public class CSIAnalysis : Base
  {
    public ActiveDOFs activeDOFs { get; set; }
    public FloorMeshSettings floorMeshSettings { get; set; }
    public CSIAnalysis() { }
  }

  public class ActiveDOFs : Base
  {
    public bool UX { get; set; }
    public bool UY { get; set; }
    public bool UZ { get; set; }
    public bool RX { get; set; }
    public bool RY { get; set; }
    public bool RZ { get; set; }
    public ActiveDOFs() { }
    public ActiveDOFs(bool UX, bool UY, bool UZ, bool RX, bool RY, bool RZ)
    {
      this.UX = UX;
      this.UY = UY;
      this.UZ = UZ;
      this.RX = RX;
      this.RY = RY;
      this.RZ = RZ;
    }
  }

  public class FloorMeshSettings : Base
  {
    public MeshOption meshOption { get; set; }
    public double maximumMeshSize { get; set; }
    public FloorMeshSettings() { }
    public FloorMeshSettings(MeshOption meshOption, double maximumMeshSize)
    {
      this.meshOption = meshOption;
      this.maximumMeshSize = maximumMeshSize;
    }
  }

  public class WallMeshSettings : Base
  {
    public double maximumMeshSize { get; set; }
    public WallMeshSettings() { }
    public WallMeshSettings(double maximumMeshSize)
    {
      this.maximumMeshSize = maximumMeshSize;
    }
  }

  public class CrackingAnalysisOptions : Base
  {
    public string reinforcementSource { get; set; }
    public double minTensionRatio { get; set; }
    public double minCompressionRatio { get; set; }

    public CrackingAnalysisOptions() { }

    public CrackingAnalysisOptions(string reinforcementSource, double minTensionRatio, double minCompressionRatio)
    {
      this.reinforcementSource = reinforcementSource;
      this.minTensionRatio = minTensionRatio;
      this.minCompressionRatio = minCompressionRatio;
    }
  }

  public class SAPFireOptions : Base
  {
    public SolverOption solverOption { get; set; }
    public AnalysisProcess analysisProcess { get; set; }
    public SAPFireOptions() { }

    public SAPFireOptions(SolverOption solverOption, AnalysisProcess analysisProcess)
    {
      this.solverOption = solverOption;
      this.analysisProcess = analysisProcess;
    }
  }

  public enum MeshOption
  {

  }

  public enum SolverOption
  {

  }
  public enum AnalysisProcess
  {

  }
}
