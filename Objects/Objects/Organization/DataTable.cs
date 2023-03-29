using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Core.Models;

namespace Objects.Organization
{
  public class DataTable : Base
  {
    public DataTable() { }
    public DataTable(int columnCount)
    {
      _columnCount = columnCount;
    }

    public object this[int column, int row]
    {
      get => Rows[row][column];
      set => Rows[row][column] = value;
    }

    private int _rowCount = 0;
    public int RowCount => _rowCount;

    private int? _columnCount = null;
    public int? ColumnCount => _columnCount;

    [Chunkable()]
    [DetachProperty]
    public List<DataRow> Rows { get; } = new List<DataRow>();

    public void AddRow(DataRow row)
    {
      _columnCount ??= row.CellData.Count;
      if (row.CellData.Count != _columnCount)
        throw new ArgumentException($"\"AddRow\" method was passed {row.CellData.Count} objects, but the DataTable has {_columnCount} column. Partial and extended table rows are not accepted by the DataTable object.");

      Rows.Add(row);
      _rowCount++;
    }

    public void AddRow(params object[] objects)
    {
      var newRow = new DataRow(objects);
      AddRow(newRow);
    }

    public void AddColumn(int index = -1, params object[] objects)
    {
      if (objects.Length != _rowCount)
        throw new ArgumentException($"\"AddColumn\" method was passed {objects.Length} objects, but the DataTable has {_rowCount} rows. Partial and extended table columns are not accepted by the DataTable object.");

      if (index > _rowCount || index < -1)
        throw new ArgumentException($"Column index {index} is out of range");

      if (index == -1)
      {
        index = objects.Length > 0 ? objects.Length - 1 : 0;
      }

      for (var i = 0; i < objects.Length; i++)
      {
        if (index < objects.Length)
        {
          Rows[i].CellData.Insert(index, objects[i]);
        }
        else
        {
          Rows[i].CellData.Add(objects[i]);
        }
      }
      _columnCount++;
    }
  }

  public class TableData : Base
  {

  }

  public class DataRow : Base
  {
    public DataRow() { }
    public DataRow(params object[] objects)
    {
      CellData.AddRange(objects);
    }

    public List<object> CellData { get; } = new List<object> { };
    public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();
    public object this[int index]
    {
      get => CellData[index];
      set => CellData[index] = value;
    }

    //internal void AddCell(object value)
    //{
    //  CellData.Add(value);
    //}

    //internal void InsertCell(int index, object value)
    //{
    //  CellData.Insert(index, value);
    //}

    //internal void AddMetadata(string key, object value)
    //{
    //  Metadata.Add(key, value);
    //}

    //internal void RemoveCell(object value)
    //{
    //  CellData.Remove(value);
    //}
    //internal void RemoveCellAt(int index)
    //{
    //  CellData.RemoveAt(index);
    //}
  }

  //public class DataColumn<T> : Base
  //{
  //  public DataColumn() { }
  //  public DataColumn(params T[] objects)
  //  {
  //    _data = new List<T>(objects);
  //  }

  //  private readonly List<T> _data;
  //  public T this[int index]
  //  {
  //    get => _data[index];
  //    set => _data[index] = value;
  //  }

  //  internal void Add(T value)
  //  {
  //    _data.Add(value);
  //  }

  //  internal void Remove(T value)
  //  {
  //    _data.Remove(value);
  //  }
  //  internal void RemoveAt(int index)
  //  {
  //    _data.RemoveAt(index);
  //  }
  //}
}
