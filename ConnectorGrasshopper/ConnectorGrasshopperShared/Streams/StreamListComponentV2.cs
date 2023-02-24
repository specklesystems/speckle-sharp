﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;

namespace ConnectorGrasshopper.Streams
{
  public class StreamListComponentV2 : GH_SpeckleTaskCapableComponent<List<StreamWrapper>>
  {
    public StreamListComponentV2() : base("Stream List", "sList", "Lists all the streams for this account",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS)
    {
    }


    public override Guid ComponentGuid => new Guid("500F0509-C91F-47B5-AE09-8635275979EC");
    protected override Bitmap Icon => Properties.Resources.StreamList;
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      var acc = pManager.AddParameter(new SpeckleAccountParam{Optional = true});
      pManager.AddIntegerParameter("Limit", "L", "Max number of streams to fetch", GH_ParamAccess.item, 10);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleStreamParam("Streams", "S", "List of streams for the provided account.",
        GH_ParamAccess.list));
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        if (DA.Iteration == 0)
        {
          hasInternetTask = Http.UserHasInternet();
          Tracker.TrackNodeRun();
        }
        
        Account account = null;
        var limit = 10;


        DA.GetData(0, ref account);
        DA.GetData(1, ref limit); // Has default value so will never be empty.

        if (account == null)
        {
          account = AccountManager.GetDefaultAccount();
          if (account == null)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not find default account in this machine. Use the Speckle Manager to add an account.");
            return;
          }

          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Using default account: {account}");
          
        }

        if (limit > 50)
        {
          limit = 50;
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Max number of streams retrieved is 50. Limit has been capped.");
        }
        
        TaskList.Add(ListStreams(account, limit));
        return;
      }

      if (!GetSolveResults(DA, out var data)) 
        return;
      
      DA.SetDataList(0, data.Select(item => new GH_SpeckleStream(item)));
    }

    private Task<bool> hasInternetTask;
    private async Task<List<StreamWrapper>> ListStreams(Account account, int limit)
    {
      if (!hasInternetTask.Result)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "You are not connected to the internet");
        return null;
      }
      
      var client = new Client(account);
      return Task.Run(() => client.StreamsGet(limit)
        .Result
        .Select(stream => new StreamWrapper(stream.id, account.userInfo.id, account.serverInfo.url))
        .ToList()).Result;
    }
  }
}
