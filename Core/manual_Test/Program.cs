
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections.Generic;

namespace foo
{
  public class App
  {
    public static void Main(string[] args)
    {
      var myObject = new Base();
      var ptsList = new List<Point>();
      for (int i = 0; i < 100; i++)
        ptsList.Add(new Point(i, i, i));

      myObject["@Points"] = ptsList;
      var server = new ServerInfo { url = "http://hyperion.local:3000", name = "Docker Server" };
      var firstUserAccount = new Account { token = "feb8e3c5b9989aca88eafe5b08ffff3caa13aad07e", userInfo = new UserInfo { id = "a2f91f2a1a18153849b3ccaa61b10ef9", email = "944ed7e@acme.com" }, serverInfo = server };
      var myServerTransport = new ServerTransport(firstUserAccount, "c77e36bf39");
      var objectId = Operations.Send(myObject, new List<ITransport>() { myServerTransport }, false, disposeTransports: true).Result;

      Console.WriteLine("Done");


    }
  }


 
public class Point : Base
  {
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Point() { }

    public Point(double X, double Y, double Z)
    {
      this.X = X;
      this.Y = Y;
      this.Z = Z;
    }
  }

}
