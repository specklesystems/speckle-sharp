using System;
using Objects.Geometry;

namespace Objects.BuiltElements.Revit;

[Obsolete(
  "Networks are no longer used to assemble MEP systems in Revit. See the RevitCommitBuilder for MEP systems conversion."
)]
public class RevitNetworkElement : NetworkElement
{
  public RevitNetworkElement() { }

  /// <summary>
  /// Indicates if this element was constructed from an MEP curve
  /// </summary>
  public bool isCurveBased { get; set; }

  /// <summary>
  /// Indicates if this element needs temporary placeholder objects to be created first when receiving
  /// </summary>
  /// <remarks>
  /// For example, some fittings cannot be created based on connectors, and so will be created similarly to mechanical equipment
  /// </remarks>
  public bool isConnectorBased { get; set; }
}

[Obsolete(
  "Networks are no longer used to assemble MEP systems in Revit. See the RevitCommitBuilder for MEP systems conversion."
)]
public class RevitNetworkLink : NetworkLink
{
  public double height { get; set; }
  public double width { get; set; }
  public double diameter { get; set; }
  public Point origin { get; set; }
  public Vector direction { get; set; }

  /// <summary>
  /// The system category
  /// </summary>
  public string systemName { get; set; }

  public string systemType { get; set; }

  /// <summary>
  /// The connector profile shape of the <see cref="NetworkLink"/>
  /// </summary>
  public string shape { get; set; }

  /// <summary>
  /// The link domain
  /// </summary>
  public string domain { get; set; }

  /// <summary>
  /// The index indicating the position of this link on the connected fitting element, if applicable
  /// </summary>
  /// <remarks>
  /// Revit fitting links are 1-indexed. For example, "T" fittings will have ordered links from index 1-3.
  /// </remarks>
  public int fittingIndex { get; set; }

  /// <summary>
  /// Indicates if this link needs temporary placeholder objects to be created first when receiving
  /// </summary>
  /// <remarks>
  /// Placeholder geometry are curves.
  /// For example, U-bend links need temporary pipes to be created first, if one or more linked pipes have not yet been created in the network.
  /// </remarks>
  public bool needsPlaceholders { get; set; }

  /// <summary>
  /// Indicates if this link has been connected to its elements
  /// </summary>
  public bool isConnected { get; set; }
}
