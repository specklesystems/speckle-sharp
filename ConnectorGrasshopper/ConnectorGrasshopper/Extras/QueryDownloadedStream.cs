using System;
using System.Collections.Generic;
using System.Reflection;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace ConnectorGrasshopper.Extras
{
  public class QueryDownloadedStream : GH_Component
  {
    
    public QueryDownloadedStream():
      base("Query", "Q", "Query a downloaded stream. This is essentially doing some filtering to keep your canvas a " +
                         "cleaner. It works well for very nested data structures, where you can avoid using the " +
                         "expand component multiple times", 
        ComponentCategories.SECONDARY_RIBBON, 
        ComponentCategories.OBJECTS){}

    public override Guid ComponentGuid => new Guid("34F7F1BD-2F24-4257-8857-FDC06D517464");
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      // pManager.AddGenericParameter("Stream", "S", "The Speckle Stream to receive data from. You can also input the " +
      //                                             "Stream ID or it's URL as text.", GH_ParamAccess.item);
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O",
        "Speckle object to deconstruct into it's properties.", GH_ParamAccess.item));
      
      pManager.AddTextParameter("Query", "Q", "Querying string. This is the path of the variable you want to access." +
                                              "Created by using the names of the variables followed by a `.`. e.g." +
                                              "Building.Wall.Height", GH_ParamAccess.item);
      
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "The resulted data point", GH_ParamAccess.tree);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var Query = "";
      if (!DA.GetData(1, ref Query))
        return;
      GH_SpeckleBase speckleObj = null;
      if (!DA.GetData(0, ref speckleObj))
        return;

      var qs = Query.Split('.');
      
      var objs = new DataTree<object>();
      objs.Add(speckleObj.Value);
      var newObjs = new DataTree<object>();
      
      for (int i = 0; i < qs.Length; i++)
      {
        newObjs = new DataTree<object>();

        for (int j = 0; j < objs.Paths.Count; j++)
        {
          var path = objs.Paths[j];
          var branch = objs.Branch(path);
          for (int k = 0; k < branch.Count; k++)
          {
            var obj = branch[k];
            //var obj = objs[j];
            if(obj != null)
            {
              var o = GetDynamically(qs[i], obj);
              if(o != null)
              {
                var p = new GH_Path();
                if(i == qs.Length - 1)
                  p = path.PrependElement(this.RunCount);
                else
                  p = path.AppendElement(k);

                if(o is IEnumerable<object>)
                {
                  var os = o as IEnumerable<object>;
                  newObjs.AddRange(os, p);
                }
                else
                  newObjs.Add(o, p);
              }
            }
            else
            {
              AddRuntimeMessage(
                GH_RuntimeMessageLevel.Error,
                "Couldn't find property: " + qs[i - 1]);
              return;
            }
          }
        }

        objs = newObjs;
      }

      DA.SetDataTree(0, objs);
    }
    
    public object GetDynamically(string name, object obj)
    {
      //Print("=====" + obj.GetType());
      foreach (var method in obj.GetType().GetMethods())
      {
        //Print(method.ToString());
      }

      var valueMethod = obj.GetType().GetMethod("get_Value");

      if(valueMethod != null)
      {
        obj = valueMethod.Invoke(obj, null);
      }

      var memberMethod = obj.GetType().GetMethod("GetMembers");
      if(memberMethod == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Couldn't find method GetMembers");
        return null;
      }

      var members = memberMethod.Invoke(obj, null);


      if(members is Dictionary<string, object>)
      {
        var dictionary = members as Dictionary<string, object>;

        if(dictionary.ContainsKey(name))
        {
          return dictionary[name];
        }
        else if(dictionary.ContainsKey("@" + name))
        {
          return dictionary["@" + name];
        }


      }
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Couldn't find Key: {name} on {obj}");
      return null;
    }

    public object GetProperty(string name, object obj)
    {
      //Print("=====");

      foreach (var prop in obj.GetType().GetProperties())
      {
        //Print(prop.Name.ToString());
        if(name == prop.Name)
        {
          return prop.GetValue(obj);
        }
      }
      return null;
    }
    
  }
}
