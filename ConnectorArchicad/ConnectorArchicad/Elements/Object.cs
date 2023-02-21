using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;
using Archicad.Model;


namespace Archicad
{
  public class ArchicadObject
  {
    public string applicationId { get; set; }

    public Point pos { get; set; }

    public Archicad.Model.MeshModel model { get; set; }

    public string units { get; set; }

    public ArchicadObject() { }

    [SchemaInfo("ArchicadObject", "Creates an Archicad object.", "Archicad", "Structure")]

    public ArchicadObject(string applicationId, Point basePoint, Archicad.Model.MeshModel model)
    {
      this.applicationId = applicationId;
      this.pos = basePoint;
      this.model = model;
    }
  }
}