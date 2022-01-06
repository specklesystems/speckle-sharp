using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using System.Collections;

using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Core.Transports;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using DesktopUI2.Models.Filters;
using Speckle.ConnectorMicroStationOpenRoads.Storage;

using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.MstnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;

#if (OPENBUILDINGS)
using Bentley.Building.Api;
#endif

#if (OPENROADS || OPENRAIL)
using Bentley.CifNET.GeometryModel.SDK;
using Bentley.CifNET.LinearGeometry;
using Bentley.CifNET.SDK;
#endif

using Stylet;

namespace Speckle.ConnectorMicroStationOpen.UI
{
  public partial class ConnectorBindingsMicroStationOpen2 : ConnectorBindings
  {
    public DgnFile File => Session.Instance.GetActiveDgnFile();
    public DgnModel Model => Session.Instance.GetActiveDgnModel();
    public string ModelUnits { get; set; }
#if (OPENROADS || OPENRAIL)
    public GeometricModel GeomModel { get; private set; }
    public List<string> civilElementKeys => new List<string> { "Alignment" };
#endif
    
#if (OPENBUILDINGS)
    public bool ExportGridLines { get; set; } = true;
#else
    public bool ExportGridLines = false;
#endif

    // Like the AutoCAD API, the Bentley APIs should only be called on the main thread.
    // As in the AutoCAD/Civil3D connectors, we therefore creating a control in the ConnectorBindings constructor (since it's called on main thread) that allows for invoking worker threads on the main thread - thank you Claire!!
    public System.Windows.Forms.Control Control;
    public ConnectorBindingsMicroStationOpen2() : base()
    {
      Control = new System.Windows.Forms.Control();
      Control.CreateControl();

      ModelUnits = Model.GetModelInfo().GetMasterUnit().GetName(true, true);

#if (OPENROADS || OPENRAIL)
      ConsensusConnection sdkCon = Bentley.CifNET.SDK.Edit.ConsensusConnectionEdit.GetActive();
      GeomModel = sdkCon.GetActiveGeometricModel();
#endif
    }

    #region local streams
    public override void WriteStreamsToFile(List<StreamState> streams)
    {
      StreamStateManager2.WriteStreamStateList(File, streams);
    }

    public override List<StreamState> GetStreamsInFile()
    {
      var streams = new List<StreamState>();
      if (File != null)
        streams = StreamStateManager2.ReadState(File);
      return streams;
    }
    #endregion

    #region boilerplate
    public override string GetHostAppName() => Utils.BentleyAppName;

    public override string GetDocumentId()
    {
      string path = GetDocumentLocation();
      return Core.Models.Utilities.hashString(path + File.GetFileName(), Speckle.Core.Models.Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation() => Path.GetDirectoryName(File.GetFileName());

    public override string GetFileName() => Path.GetFileName(File.GetFileName());

    public override string GetActiveViewName() => "Entire Document";

    public override List<string> GetObjectsInView()
    {
      if (Model == null)
        return new List<string>();

      var graphicElements = Model.GetGraphicElements();

      var objs = new List<string>();
      using (var elementEnumerator = (ModelElementsEnumerator)graphicElements.GetEnumerator())
      {
        objs = graphicElements.Where(el => !el.IsInvisible).Select(el => el.ElementId.ToString()).ToList(); // Note: this returns all graphic objects in the model.
      }

      return objs;
    }

    public override List<string> GetSelectedObjects()
    {
      var objs = new List<string>();

      if (Model == null)
      {
        return objs;
      }

      uint numSelected = SelectionSetManager.NumSelected();
      DgnModelRef modelRef = Session.Instance.GetActiveDgnModelRef();

      for (uint i = 0; i < numSelected; i++)
      {
        Bentley.DgnPlatformNET.Elements.Element el = null;
        SelectionSetManager.GetElement(i, ref el, ref modelRef);
        objs.Add(el.ElementId.ToString());
      }

      return objs;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      //Element Type, Element Class, Element Template, Material, Level, Color, Line Style, Line Weight
      var levels = new List<string>();

      FileLevelCache levelCache = Model.GetFileLevelCache();
      foreach (var level in levelCache.GetHandles())
      {
        levels.Add(level.Name);
      }
      levels.Sort();

      var elementTypes = new List<string> { "Arc", "Ellipse", "Line", "Spline", "Line String", "Complex Chain", "Shape", "Complex Shape", "Mesh" };

      var filterList = new List<ISelectionFilter>();
      filterList.Add(new ListSelectionFilter { Slug = "level", Name = "Levels", Icon = "LayersTriple", Description = "Selects objects based on their level.", Values = levels });
      filterList.Add(new ListSelectionFilter { Slug = "elementType", Name = "Element Types", Icon = "Category", Description = "Selects objects based on their element type.", Values = elementTypes });

#if (OPENROADS || OPENRAIL)
      var civilElementTypes = new List<string> { "Alignment" };
      filterList.Add(new ListSelectionFilter { Slug = "civilElementType", Name = "Civil Features", Icon = "RailroadVariant", Description = "Selects civil features based on their type.", Values = civilElementTypes });
#endif

      filterList.Add(new AllSelectionFilter { Slug = "all", Name = "All", Icon = "CubeScan", Description = "Selects all document objects." });

      return filterList;
    }

    //TODO
    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }
    #endregion

    #region receiving
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Utils.BentleyAppName);

      if (converter == null)
        throw new Exception("Could not find any Kit!");

      if (Control.InvokeRequired)
        Control.Invoke(new SetContextDelegate(converter.SetContextDocument), new object[] { Session.Instance });
      else
        converter.SetContextDocument(Session.Instance);


    }

    // Recurses through the commit object and flattens it. Returns list of Base objects with their bake layers
    private List<Tuple<Base, string>> FlattenCommitObject(object obj, ISpeckleConverter converter, string layer, StreamState state, ref int count, bool foundConvertibleMember = false)
    {
      var objects = new List<Tuple<Base, string>>();

      if (obj is Base @base)
      {
        if (converter.CanConvertToNative(@base))
        {
          objects.Add(new Tuple<Base, string>(@base, layer));
          return objects;
        }
        else
        {
          int totalMembers = @base.GetDynamicMembers().Count();
          foreach (var prop in @base.GetDynamicMembers())
          {
            count++;

            // get bake layer name
            string objLayerName = prop.StartsWith("@") ? prop.Remove(0, 1) : prop;
            string acLayerName = $"{layer}${objLayerName}";

            var nestedObjects = FlattenCommitObject(@base[prop], converter, acLayerName, state, ref count, foundConvertibleMember);
            if (nestedObjects.Count > 0)
            {
              objects.AddRange(nestedObjects);
              foundConvertibleMember = true;
            }
          }
          if (!foundConvertibleMember && count == totalMembers) // this was an unsupported geo
            converter.Report.Log($"Skipped not supported type: { @base.speckle_type }. Object {@base.id} not baked.");
          return objects;
        }
      }

      if (obj is List<object> list)
      {
        count = 0;
        foreach (var listObj in list)
          objects.AddRange(FlattenCommitObject(listObj, converter, layer, state, ref count));
        return objects;
      }

      if (obj is IDictionary dict)
      {
        count = 0;
        foreach (DictionaryEntry kvp in dict)
          objects.AddRange(FlattenCommitObject(kvp.Value, converter, layer, state, ref count));
        return objects;
      }

      return objects;
    }
    #endregion

    public override async Task SendStream(StreamState state, ProgressViewModel progress)
    {
      throw new NotImplementedException();
    }

  }
}