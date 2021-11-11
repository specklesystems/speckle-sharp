using NUnit.Framework;
using Speckle.Core.Kits;
//using ETABSv1;

namespace ConverterETABSTests
{
    public class Tests
    {
        //cSapModel Model { get; set; }
        [SetUp]
        public void Setup()
        {
            var kits = KitManager.GetDefaultKit();
            var converter = kits.LoadConverter(Applications.ETABSv18);
            //cOAPI myETABSObject;
            //cHelper myHelper = new Helper();
            //myETABSObject = myHelper.CreateObjectProgID("CSI.ETABS.API.ETABSObject");
            //Model = myETABSObject.SapModel;
            //converter.SetContextDocument(Model);
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}