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
    public string id { get; set; }
    public string applicationId { get; set; }

    public Point pos { get; set; }
    public Objects.Other.Transform transform { get; set; }

    public List<string> modelIds { get; set; }

    public string units { get; set; }

    public ArchicadObject() { }

    [SchemaInfo("ArchicadObject", "Creates an Archicad object.", "Archicad", "Structure")]

    public ArchicadObject(string id, string applicationId, Point basePoint, List<string> modelIds)
    {
      this.id = id;
      this.applicationId = applicationId;
      this.pos = basePoint;
      this.modelIds = modelIds;
    }
  }
}
