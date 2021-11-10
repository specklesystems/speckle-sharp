using NUnit.Framework;
using ETABSv1;

namespace ConverterETABSTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            ETABSv1.cOAPI myETABSObject;
            ETABSv1.cHelper myHelper = new ETABSv1.Helper();

            myETABSObject = myHelper.CreateObjectProgID("CSI.ETABS.API.ETABSObject");
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}