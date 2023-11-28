using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;

namespace Objects.Organization;

public class DataTable : Base
{
  public DataTable() { }

  public int columnCount => columnMetadata.Count;
  public int rowCount => rowMetadata.Count;
  public int headerRowIndex { get; set; }
  public string name { get; set; }
  public List<Base> rowMetadata { get; set; } = new List<Base>();
  public List<Base> columnMetadata { get; set; } = new List<Base>();
  public List<List<object>> data { get; set; } = new List<List<object>>();

  public void AddRow(Base metadata, int index = -1, params object[] objects)
  {
    if (objects.Length != columnCount)
    {
      throw new ArgumentException(
        $"\"AddRow\" method was passed {objects.Length} objects, but the DataTable has {columnCount} columns. Partial and extended table rows are not accepted by the DataTable object."
      );
    }

    if (index < 0 || index >= data.Count)
    {
      data.Add(objects.ToList());
      rowMetadata.Add(metadata);
    }
    else
    {
      data.Insert(index, objects.ToList());
      rowMetadata.Insert(index, metadata);
    }
  }

  public void DefineColumn(Base metadata)
  {
    columnMetadata.Add(metadata);
  }
}
