using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Curve = Objects.Geometry.Curve;
using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;
using Network = Objects.Organization.Network;
using Polyline = Objects.Geometry.Polyline;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject NetworkToNative(Network speckleNetwork)
    {
      foreach (var networkElement in speckleNetwork.elements)
      {
        var element = networkElement.element;
        if (CanConvertToNative(element))
        {
          ConvertToNative(element);
        }
      }
      var appObj = new ApplicationObject(speckleNetwork.id, speckleNetwork.speckle_type) { applicationId = speckleNetwork.applicationId };
      return appObj;
    }

    public Network NetworkToSpeckle(Element mepElement, out List<string> notes)
    {
      notes = new List<string>();
      Network speckleNetwork = new Network() { name = mepElement.Name, elements = new List<Organization.NetworkElement>(), links = new List<Organization.NetworkLink>() };

      GetNetworkElements(speckleNetwork, mepElement, out List<string> connectedNotes);
      if (connectedNotes.Any()) notes.AddRange(connectedNotes);
      return speckleNetwork;
    }
  }
}