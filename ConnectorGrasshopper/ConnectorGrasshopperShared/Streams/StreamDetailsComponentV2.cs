using System;
using System.Drawing;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Streams;

public class StreamDetailsComponentV2 : GH_SpeckleTaskCapableComponent<Stream>
{
  public StreamDetailsComponentV2()
    : base(
      "Stream Details",
      "sDet",
      "Extracts the details of a given stream, use is limited to 20 streams.",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.STREAMS
    ) { }

  public override Guid ComponentGuid => new("9E29DDE3-E6BF-499D-A7FD-AD2E38B0F6FF");
  protected override Bitmap Icon => Resources.StreamDetails;

  public override GH_Exposure Exposure => GH_Exposure.secondary;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddParameter(
      new SpeckleStreamParam("Stream", "S", "A stream object of the stream to be updated.", GH_ParamAccess.item)
    );
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddTextParameter("Stream ID", "ID", "Unique ID of the stream to be updated.", GH_ParamAccess.item);
    pManager.AddTextParameter("Name", "N", "Name of the stream.", GH_ParamAccess.item);
    pManager.AddTextParameter("Description", "D", "Description of the stream", GH_ParamAccess.item);
    pManager.AddTextParameter("Created At", "C", "Date of creation", GH_ParamAccess.item);
    pManager.AddTextParameter("Updated At", "U", "Date when it was last modified", GH_ParamAccess.item);
    pManager.AddBooleanParameter("Public", "P", "True if the stream is to be publicly available.", GH_ParamAccess.item);
    pManager.AddGenericParameter(
      "Collaborators",
      "Cs",
      "Users that have collaborated in this stream",
      GH_ParamAccess.list
    );
    pManager.AddGenericParameter("Branches", "B", "List of branches for this stream", GH_ParamAccess.list);
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    if (InPreSolve)
    {
      switch (DA.Iteration)
      {
        case 0:
          Tracker.TrackNodeRun();
          break;
        case 20:
          AddRuntimeMessage(
            GH_RuntimeMessageLevel.Warning,
            "Input data has too many items. Only the first 20 streams will be fetched."
          );
          return;
      }

      StreamWrapper sw = null;
      if (!DA.GetData(0, ref sw))
      {
        return;
      }

      TaskList.Add(
        Task.Run(() =>
        {
          var account = sw.GetAccount().Result;
          var client = new Client(account);
          return client.StreamGet(sw.StreamId);
        })
      );

      return;
    }

    if (!GetSolveResults(DA, out var stream))
    {
      return;
    }

    DA.SetData(0, stream.id);
    DA.SetData(1, stream.name);
    DA.SetData(2, stream.description);
    DA.SetData(3, stream.createdAt);
    DA.SetData(4, stream.updatedAt);
    DA.SetData(5, stream.isPublic);
    DA.SetDataList(6, stream.collaborators);
    DA.SetDataList(7, stream.branches.items);
  }
}
