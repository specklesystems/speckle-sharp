﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Logging = Speckle.Core.Logging;

namespace ConnectorGrasshopper.Streams
{
  public class StreamUpdateComponent : GH_Component
  {
    public StreamUpdateComponent() : base("Stream Update", "sUp", "Updates a stream with new details", ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS)
    { }
    public override Guid ComponentGuid => new Guid("F83B9956-1A5C-4844-B7F6-87A956105831");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      var stream = pManager.AddParameter(new SpeckleStreamParam("Stream", "S", "Unique ID of the stream to be updated.", GH_ParamAccess.item));
      var name = pManager.AddTextParameter("Name", "N", "Name of the stream.", GH_ParamAccess.item);
      var desc = pManager.AddTextParameter("Description", "D", "Description of the stream", GH_ParamAccess.item);
      var isPublic = pManager.AddBooleanParameter("Public", "P", "True if the stream is to be publicly available.",
        GH_ParamAccess.item);
      Params.Input[name].Optional = true;
      Params.Input[desc].Optional = true;
      Params.Input[isPublic].Optional = true;
    }

    protected override Bitmap Icon => Properties.Resources.StreamUpdate;

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Stream ID", "ID", "Unique ID of the stream to be updated.", GH_ParamAccess.item);
    }

    private Stream stream;
    Exception error = null;

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      DA.DisableGapLogic();
      GH_SpeckleStream ghSpeckleStream = null;
      string name = null;
      string description = null;
      bool isPublic = false;

      if (DA.Iteration == 0)
        Logging.Tracker.TrackPageview(Logging.Tracker.STREAM_UPDATE);

      if (!DA.GetData(0, ref ghSpeckleStream)) return;
      DA.GetData(1, ref name);
      DA.GetData(2, ref description);
      DA.GetData(3, ref isPublic);

      var streamWrapper = ghSpeckleStream.Value;
      if (error != null)
      {
        Message = null;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, error.Message);
        error = null;
      }
      else if (stream == null)
      {
        if (streamWrapper == null)
        {
          Message = "";
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Not a stream wrapper!");
          return;
        }
        Message = "Fetching";
        Task.Run(async () =>
        {
          try
          {
            var account = streamWrapper.GetAccount().Result;
            var client = new Client(account);
            var input = new StreamUpdateInput();
            stream = await client.StreamGet(streamWrapper.StreamId);
            input.id = streamWrapper.StreamId;

            input.name = name ?? stream.name;
            input.description = description ?? stream.description;

            if (stream.isPublic != isPublic) input.isPublic = isPublic;

            await client.StreamUpdate(input);

            Logging.Analytics.TrackEvent(account, Logging.Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Stream Update" } });
          }
          catch (Exception e)
          {
            error = e.InnerException ?? e;
          }
          finally
          {
            Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
          }

        });
      }
      else
      {
        stream = null;
        Message = "Done";
        DA.SetData(0, streamWrapper.StreamId);
      }
    }
  }
}
