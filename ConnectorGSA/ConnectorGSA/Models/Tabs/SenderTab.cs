using System.Collections.Generic;
using Speckle.GSA.API;

namespace ConnectorGSA.Models
{
  public class SenderTab : TabBase
  {
    public StreamContentConfig StreamContentConfig { get; set; } = StreamContentConfig.ModelOnly;

    public string LoadCaseList { get; set; }
    public int AdditionalPositionsFor1dElements { get; set; } = 3;

    public ResultSettings ResultSettings { get; set; } = new ResultSettings();

    public List<StreamState> SenderSidRecords { get => this.sidSpeckleRecords; }

    private string documentTitle = "";

    public SenderTab() : base(GSALayer.Both)
    {

    }


    internal void SetDocumentName(string name)
    {
      documentTitle = name;
    }
  }
}
