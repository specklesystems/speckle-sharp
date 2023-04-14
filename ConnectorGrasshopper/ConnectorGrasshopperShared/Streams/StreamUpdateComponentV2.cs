using System;
using System.Drawing;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Streams;

public class StreamUpdateComponentV2 : GH_SpeckleTaskCapableComponent<bool>
{
  public StreamUpdateComponentV2()
    : base(
      "Stream Update",
      "sUp",
      "Updates a stream with new details",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS
    ) { }

  public override Guid ComponentGuid => new("3FE4CDCE-725A-4C4D-B580-ED6E6C7826DA");
  protected override Bitmap Icon => Resources.StreamUpdate;
  public override GH_Exposure Exposure => GH_Exposure.tertiary;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddParameter(
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
    pManager.AddTextParameter("Success", "S", "Indicates if the operation succeeded or not", GH_ParamAccess.item);
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    if (InPreSolve)
    {
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

      if (streamWrapper == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Not a stream wrapper!");
        return;
      }

      if (DA.Iteration == 0)
        Tracker.TrackNodeRun();

      TaskList.Add(Task.Run(() => UpdateStream(streamWrapper, name, description, isPublic), CancelToken));
    }

    if (!GetSolveResults(DA, out var success))
      return;

    DA.SetData(0, success);
  }

  private async Task<bool> UpdateStream(StreamWrapper streamWrapper, string name, string description, bool isPublic)
  {
    var account = streamWrapper.GetAccount().Result;
    var client = new Client(account);
    var input = new StreamUpdateInput();
    var stream = client.StreamGet(streamWrapper.StreamId).Result;
    input.id = streamWrapper.StreamId;

    input.name = name ?? stream.name;
    input.description = description ?? stream.description;
    if (stream.isPublic != isPublic)
      input.isPublic = isPublic;

    return await client.StreamUpdate(input);
  }
}
