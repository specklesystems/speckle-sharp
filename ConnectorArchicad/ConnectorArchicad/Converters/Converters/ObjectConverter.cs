using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Archicad.Operations;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public sealed class Object : IConverter
  {
    #region --- Properties ---

    public Type Type => null;

    #endregion

    #region --- Functions ---

    public async Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token)
    {
      var objects = new List<Archicad.ArchicadObject>();
      foreach (var element in elements)
      {
        // base point
        var basePoint = new Point (0, 0, 0);
        var specialKeys = element.GetMembers();

        // todo Speckle schema
        //if (specialKeys.ContainsKey("basePoint"))
        //  basePoint = (Point)element["basePoint"];

        // get the geometry
        List<Mesh> meshes = null;
        if (element is Mesh mesh)
          meshes = new List<Mesh>() { mesh };
        else
        {
          var m = element["displayValue"] ?? element["@displayValue"];
          if (m is List<Mesh>)
            meshes = (List<Mesh>)m;
          else if (m is List<object>)
            meshes = ((List<object>)m).Cast<Mesh>().ToList();
        }

        if (meshes == null)
          continue;
       
        var meshModel = ModelConverter.MeshToNative(meshes);

        var newObject = new Archicad.ArchicadObject(element.applicationId, Utils.ScaleToNative(basePoint), meshModel);
        objects.Add(newObject);
      }

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateObject(objects), token);
      return result is null ? new List<string>() : result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements, CancellationToken token)
    {
      // Objects not stored on the server
      return new List<Base>();
    }

    #endregion
  }
}
