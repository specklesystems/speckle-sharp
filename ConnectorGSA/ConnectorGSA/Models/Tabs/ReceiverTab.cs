using Speckle.GSA.API;
using System.Collections.Generic;

namespace ConnectorGSA.Models
{
  public class ReceiverTab : TabBase
  {
    public double CoincidentNodeAllowance { get; set; } = 10;
    public GsaUnit CoincidentNodeUnits { get; set; } = GsaUnit.Millimetres;
    public List<StreamState> ReceiverStreamStates { get => this.StreamStates; }

    public ReceiverTab() : base(GSALayer.Design)
    {

    }
  }
}
