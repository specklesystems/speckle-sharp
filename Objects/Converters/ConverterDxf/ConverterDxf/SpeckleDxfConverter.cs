using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Dxf = Speckle.netDxf;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Point = Objects.Geometry.Point;

namespace Objects.Converters.DxfConverter
{
    public partial class SpeckleDxfConverter
    {
        public Dxf.DxfDocument Doc;
        public string Description => "The Objects DXF Converter";
        public string Name => "Speckle DXF Converter";
        public string Author => "Speckle Systems";
        public string WebsiteOrEmail => "https://speckle.systems";

        public ProgressReport Report { get; } = new();
        public ConverterDxfSettings Settings = new();
        public ReceiveMode ReceiveMode { get; set; } = ReceiveMode.Create;

        // TODO: Convert to Speckle is currently not supported.
        public List<Base> ConvertToSpeckle(List<object> objects) => throw new NotImplementedException();
        public bool CanConvertToSpeckle(object @object) => throw new NotImplementedException();
        public Base ConvertToSpeckle(object @object) => throw new NotImplementedException();

        
        public bool CanConvertToNative(Base @base)
        {
            switch (@base)
            {
                case Vector _:
                case Point _:
                case Line _:
                case Mesh _:
                case Brep _:
                    return true;
                default:
                    return false;
            }
        }
        public object ConvertToNative(Base @base)
        {
            switch (@base)
            {
                case Point pt:
                    return PointToNative(pt);
                case Vector vector:
                    return VectorToNative(vector);
                case Line line:
                    return LineToNative(line);
                case Mesh mesh:
                    if (Settings.PrettyMeshes)
                        return MeshToNativePretty(mesh);
                    return MeshToNative(mesh);
                case Brep brep:
                    return BrepToNative(brep);
                default:
                    return null!;
            }
        }
        
        public List<object> ConvertToNative(List<Base> objects) => objects.Select(ConvertToNative).ToList();

        public IEnumerable<string> GetServicedApplications() => new[] { VersionedHostApplications.Dxf };
    }
}