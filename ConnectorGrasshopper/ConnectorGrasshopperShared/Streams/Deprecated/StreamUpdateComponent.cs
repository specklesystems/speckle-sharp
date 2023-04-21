using System;
using System.Drawing;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Rhino;
using Speckle.Core.Api;
using Speckle.Core.Models.Extensions;

namespace ConnectorGrasshopper.Streams;

[Obsolete]
public class StreamUpdateComponent : GH_SpeckleComponent
{
  private Exception error;

  private Stream stream;

  public StreamUpdateComponent()
    : base(
      "Stream Update",
      "sUp",
      "Updates a stream with new details",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS
    ) { }

  public override Guid ComponentGuid => new("F83B9956-1A5C-4844-B7F6-87A956105831");

  protected override Bitmap Icon => Resources.StreamUpdate;

  public override GH_Exposure Exposure => GH_Exposure.hidden;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    var stream = pManager.AddParameter(
      new SpeckleStreamParam("Stream", "S", "Unique ID of the stream to be updated.", GH_ParamAccess.item)
    );
    var name = pManager.AddTextParameter("Name", "N", "Name of the stream.", GH_ParamAccess.item);
    var desc = pManager.AddTextParameter("Description", "D", "Description of the stream", GH_ParamAccess.item);
    var isPublic = pManager.AddBooleanParameter(
      "Public",
      "P",
      "True if the stream is to be publicly available.",
      GH_ParamAccess.item
    );
    Params.Input[name].Optional = true;
    Params.Input[desc].Optional = true;
    Params.Input[isPublic].Optional = true;
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddTextParameter("Stream ID", "ID", "Unique ID of the stream to be updated.", GH_ParamAccess.item);
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    DA.DisableGapLogic();
    GH_SpeckleStream ghSpeckleStream = null;
    string name = null;
    string description = null;
    bool isPublic = false;

    if (!DA.GetData(0, ref ghSpeckleStream))
      return;
    DA.GetData(1, ref name);
    DA.GetData(2, ref description);
    DA.GetData(3, ref isPublic);

    var streamWrapper = ghSpeckleStream.Value;
    if (error != null)
    {
      Message = null;
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, error.ToFormattedString());
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
      if (DA.Iteration == 0)
        Tracker.TrackNodeRun();
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

          if (stream.isPublic != isPublic)
            input.isPublic = isPublic;

          await client.StreamUpdate(input);
        }
        catch (Exception e)
        {
          error = e;
        }
        finally
        {
          RhinoApp.InvokeOnUiThread(
            (Action)
              delegate
              {
                ExpireSolution(true);
              }
          );
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
