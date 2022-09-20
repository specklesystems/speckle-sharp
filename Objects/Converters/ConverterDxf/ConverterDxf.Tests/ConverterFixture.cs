using System;
using Speckle.Core.Models;
using Xunit;
using Dxf = netDxf;
using Dxfe = netDxf.Entities;

namespace ConverterDxf.Tests
{
    public class ConverterFixture: IDisposable
    {
        public SpeckleDxfConverter Converter;
        public Dxf.DxfDocument Doc;

        public ConverterFixture()
        {
            Converter = new SpeckleDxfConverter();
            Doc = new Dxf.DxfDocument();
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