using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Logging = Speckle.Core.Logging;

namespace ConnectorGrasshopper.Streams
{
  public class StreamDetailsComponent : GH_Component
  {
    public StreamDetailsComponent() : base("Stream Details", "sDet", "Extracts the details of a given stream, use is limited to 20 streams.",
      ComponentCategories.PRIMARY_RIBBON, ComponentCategories.STREAMS)
    {
    }

    public override Guid ComponentGuid => new Guid("B47CAD66-187C-4D1F-AC77-9CA03BF82A0E");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleStreamParam("Stream", "S",
        "A stream object of the stream to be updated.", GH_ParamAccess.tree));
    }

    protected override Bitmap Icon => Properties.Resources.StreamDetails;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Stream ID", "ID", "Unique ID of the stream to be updated.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Name", "N", "Name of the stream.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Description", "D", "Description of the stream", GH_ParamAccess.tree);
      pManager.AddTextParameter("Created At", "C", "Date of creation", GH_ParamAccess.tree);
      pManager.AddTextParameter("Updated At", "U", "Date when it was last modified", GH_ParamAccess.tree);
      pManager.AddBooleanParameter("Public", "P", "True if the stream is to be publicly available.",
        GH_ParamAccess.tree);
      pManager.AddGenericParameter("Collaborators", "Cs", "Users that have collaborated in this stream",
        GH_ParamAccess.tree);
      pManager.AddGenericParameter("Branches", "B", "List of branches for this stream", GH_ParamAccess.tree);
    }

    private Exception error;
    private Dictionary<GH_Path, Stream> streams;
    private bool tooManyItems;
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (error != null)
      {
        Message = null;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, error.Message);
        error = null;
        streams = null;
      }
      else if (streams == null)
      {
        if (!DA.GetDataTree(0, out GH_Structure<GH_SpeckleStream> ghStreamTree))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not convert object to Stream.");
          Message = null;
          return;
        }



        Message = "Fetching";

        if (ghStreamTree.DataCount == 0)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input S failed to collect data.");
          return;
        }

        if (ghStreamTree.DataCount >= 20)
        {
          tooManyItems = true;
        }

        Task.Run(async () =>
        {
          try
          {
            int count = 0;
            var tasks = new Dictionary<GH_Path, Task<Stream>>();

            ghStreamTree.Paths.ToList().ForEach(path =>
            {
              if (count >= 20) return;
              var branch = ghStreamTree[path];
              var itemCount = 0;
              branch.ForEach(item =>
              {
                if (item == null || count >= 20)
                {
                  itemCount++;
                  return;
                }

                Account account = null;
                try
                {
                  account = item.Value.GetAccount().Result;
                }
                catch (Exception e)
                {
                  error = e.InnerException ?? e;
                  return;
                }

                Logging.Analytics.TrackEvent(account, Logging.Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Stream Details" } });

                var client = new Client(account);

                var task = client.StreamGet(item.Value?.StreamId);
                tasks[path.AppendElement(itemCount)] = task;
                count++;
                itemCount++;
              });
            });

            var values = await Task.WhenAll(tasks.Values);
            var fetchedStreams = new Dictionary<GH_Path, Stream>();

            for (int i = 0; i < tasks.Keys.ToList().Count; i++)
            {
              var key = tasks.Keys.ToList()[i];
              fetchedStreams[key] = values[i];
            }

            streams = fetchedStreams;

          }
          catch (Exception e)
          {
            error = e;
          }
          finally
          {
            Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
          }
        });
      }
      else
      {
        if (tooManyItems)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            "Input data has too many items. Only the first 20 streams will be fetched.");
          tooManyItems = false;
        }
        var id = new GH_Structure<IGH_Goo>();
        var name = new GH_Structure<IGH_Goo>();
        var description = new GH_Structure<IGH_Goo>();
        var createdAt = new GH_Structure<IGH_Goo>();
        var updatedAt = new GH_Structure<IGH_Goo>();
        var isPublic = new GH_Structure<GH_Boolean>();
        var collaborators = new GH_Structure<IGH_Goo>();
        var branches = new GH_Structure<IGH_Goo>();

        streams.AsEnumerable()?.ToList().ForEach(pair =>
        {
          id.Append(GH_Convert.ToGoo(pair.Value.id), pair.Key);
          name.Append(GH_Convert.ToGoo(pair.Value.name), pair.Key);
          description.Append(GH_Convert.ToGoo(pair.Value.description), pair.Key);
          createdAt.Append(GH_Convert.ToGoo(pair.Value.createdAt), pair.Key);
          updatedAt.Append(GH_Convert.ToGoo(pair.Value.updatedAt), pair.Key);
          isPublic.Append(new GH_Boolean(pair.Value.isPublic), pair.Key);
          collaborators.AppendRange(pair.Value.collaborators.Select(GH_Convert.ToGoo).ToList(), pair.Key);
          branches.AppendRange(pair.Value.branches.items.Select(GH_Convert.ToGoo), pair.Key);
        });

        Message = "Done";
        DA.SetDataTree(0, id);
        DA.SetDataTree(1, name);
        DA.SetDataTree(2, description);
        DA.SetDataTree(3, createdAt);
        DA.SetDataTree(4, updatedAt);
        DA.SetDataTree(5, isPublic);
        DA.SetDataTree(6, collaborators);
        DA.SetDataTree(7, branches);
        streams = null;
      }
    }
  }
}
