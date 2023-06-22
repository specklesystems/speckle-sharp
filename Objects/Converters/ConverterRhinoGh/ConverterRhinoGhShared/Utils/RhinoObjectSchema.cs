using System;
using System.Linq;
using Objects.Structural.Analysis;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects.Converter.RhinoGh.Utils;

public sealed class RhinoObjectsSchema : IRhinoObjectsSchema
{
  public string GetSchema(RhinoObject obj, string key, out string[] args)
  {
    args = null;

    // user string has format "DirectShape{[family], [type]}" if it is a directshape conversion
    // user string has format "AdaptiveComponent{[family], [type]}" if it is an adaptive component conversion
    // user string has format "Pipe{[diameter]}" if it is a pipe conversion
    // user string has format "Duct{[width], [height], [diameter]}" if it is a duct conversion
    // otherwise, it is just the schema type name
    string schema = obj.Attributes.GetUserString(key);

    if (schema == null)
      return null;

    string[] parsedSchema = schema.Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
    if (parsedSchema.Length > 2)
    {
      // there is incorrect formatting in the schema string!
      return null;
    }

    if (parsedSchema.Length == 2)
    {
      args = parsedSchema[1].Split(new[] { ',' },
                StringSplitOptions.RemoveEmptyEntries)?.Select(o => o.Trim())?.ToArray();

      // BUG? should we be returning something here?
    }

    return parsedSchema[0].Trim();
  }
}
