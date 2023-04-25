using System;
using System.Collections.Generic;

namespace DesktopUI2.Models;

public class MenuItem
{
  public string Header { get; set; }
  public string Icon { get; set; }
  public Action<StreamState> Action { get; set; }
  public IList<MenuItem> Items { get; set; }
}
