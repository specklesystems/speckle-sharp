using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Properties
{
  /// <summary>
  /// Used to store application-specific properties from the authoring application for higher fidelity intra-application data transfer.
  /// An object sent from a connector will only have one <see cref="ApplicationProperties"/> object which correlates to
  /// the authoring application. However, for advanced usage you could create custom objects with multiple <see cref="ApplicationProperties"/>
  /// for different applications in order to create an object with near-seamless sending and receiving in different applications.
  /// Note that this could cause issues if a property is modified in one host application and the change isn't reflected
  /// across all <see cref="ApplicationProperties"/> objects.
  /// </summary>
  public class ApplicationProperties : Base
  {
    /// <summary>
    /// The name of the authoring application as a string. Use the defined <see cref="HostApplications"/> name
    /// (eg. <c>HostApplications.Revit.Name</c>)
    /// </summary>
    public string app { get; set; }
    
    public string className { get; set; }
    
    public Dictionary<string, object> props { get; set; } = new Dictionary<string, object>();

    public ApplicationProperties()
    {
    }
  }
}