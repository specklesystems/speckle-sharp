using NUnit.Framework;
using Objects.Converter.ETABS;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Speckle.Core.Kits;
using ETABSv1;
using Microsoft.VisualBasic;
using System.Collections.Generic;

namespace ConverterETABSTests
{
    public partial class Tests
    {
        cSapModel Model { get; set; }
        ConverterETABS converter = new ConverterETABS();
        [SetUp]
        public void Setup()
        {
            var kits = KitManager.GetDefaultKit();
            converter = (ConverterETABS)kits.LoadConverter(Applications.ETABSv18);
            cOAPI myETABSObject;
            //Can't figure out how to start the program automatically get a port problem ~ so have to start at least one program
            myETABSObject = (ETABSv1.cOAPI)Interaction.GetObject(Class: "CSI.ETABS.API.ETABSObject");
            //cHelper myHelper = new Helper();
            //myETABSObject = myHelper.CreateObjectProgID("CSI.ETABS.API.ETABSObject");
            //myETABSObject.ApplicationStart();
            Model = myETABSObject.SapModel;
            converter.SetContextDocument(Model);
        }

        [Test]
        public void PointToNative()
        {
            Point point = new Point(0, 1, 2);
            Node node = new Node(point,"1");
            string name = null;
            converter.PointToNative(node);
            double x, y, z;
            x = y = z = 0;
            Model.PointObj.GetCoordCartesian("1", ref x, ref y, ref z);
            if(x==0 && y== 1 && z == 2)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LineToNative()
        {
            Point point1 = new Point(0, 0, 0);
            Point point2 = new Point(1000, 0, 0);
            Line line = new Line(point1, point2);
            converter.LineToNative(line);
            int numberlines = 0;
            string[] linesName = null;
            Model.FrameObj.GetNameList(ref numberlines,ref linesName);
            if(numberlines == 1)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }

        [Test]
        public void AreaToNative()
        {
            Point point1 = new Point(0, 0, 0);
            Point point2 = new Point(1000, 0, 0);
            Point point3 = new Point(0, 1000, 0);
            Point point4 = new Point(1000, 1000, 0);
            Node pt1 = new Node(point1);
            Node pt2 = new Node(point2);
            Node pt3 = new Node(point3);
            Node pt4 = new Node(point4);
            List<Node> listTop = new List<Node> { };
            listTop.Add(pt1);
            listTop.Add(pt2);
            listTop.Add(pt3);
            listTop.Add(pt4);
            Element2D element2D = new Element2D(listTop);
            converter.AreaToNative(element2D);
            int numberArea = 0;
            string[] AreasName = null;
            Model.AreaObj.GetNameList(ref numberArea, ref AreasName);
            if(numberArea == 1)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }

        }
    }
}