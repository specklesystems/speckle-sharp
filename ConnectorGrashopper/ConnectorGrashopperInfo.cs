using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ConnectorGrashopper
{
  public class ConnectorGrashopperInfo : GH_AssemblyInfo
  {
    public override string Name
    {
      get
      {
        return "Speckle 2";
      }
    }
    public override Bitmap Icon
    {
      get
      {
        //Return a 24x24 pixel bitmap to represent this GHA library.
        return null;
      }
    }
    public override string Description
    {
      get
      {
        //Return a short string describing the purpose of this GHA library.
        return "";
      }
    }
    public override Guid Id
    {
      get
      {
        return new Guid("074ee50d-763d-495a-8be2-6934897dd6b1");
      }
    }
    
    public override string AuthorName
    {
      get
      {
        //Return a string identifying you or your company.
        return "AEC Systems";
      }
    }
    public override string AuthorContact
    {
      get
      {
        //Return a string representing your preferred contact details.
        return "hello@speckle.systems";
      }
    }
  }
}
