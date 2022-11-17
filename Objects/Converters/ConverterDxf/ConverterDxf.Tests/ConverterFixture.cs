using System;
using Objects.Converters.DxfConverter;
using Speckle.Core.Models;
using Speckle.netDxf.Units;
using Xunit;
using Dxf = Speckle.netDxf;
using Dxfe = Speckle.netDxf.Entities;

namespace ConverterDxf.Tests
{
    public class ConverterFixture: IDisposable
    {
        public SpeckleDxfConverter Converter;
        public readonly Dxf.DxfDocument Doc;

        public ConverterFixture()
        {
            Converter = new SpeckleDxfConverter();
            Doc = new Dxf.DxfDocument();
            Doc.DrawingVariables.InsUnits = DrawingUnits.Meters;
            Converter.SetContextDocument(Doc);
        }

        public T AssertAndConvertToNative<T>(Base @base)
        {
            Assert.True(Converter.CanConvertToNative(@base));
            var dxfObject = Converter.ConvertToNative(@base);
            Assert.NotNull(dxfObject);
            Assert.IsAssignableFrom<T>(dxfObject);
            return (T)dxfObject;
        }

        public void Dispose()
        {
            Converter = null;
        }
    }
}