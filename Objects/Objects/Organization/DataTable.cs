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
    private DataTable() { }
    public DataTable(int columnCount)
    {
      _columnCount = columnCount;

    }

    public object this[int column, int row]
    {
      get => _rows[row][column];
      set => _rows[row][column] = value;
    }

    private int _rowCount = 0;
    public int RowCount => _rowCount;

    private int _columnCount = 0;
    public int ColumnCount => _columnCount;

    [Chunkable()]
    [DetachProperty]
    private List<DataRow> _rows { get; } = new List<DataRow>();

    public void AddRow(DataRow row)
    {
      _rows.Add(row);
      _rowCount++;
    }

    public void AddRow(params object[] objects)
    {
      if (objects.Length != _columnCount)
        throw new ArgumentException($"\"AddRow\" method was passed {objects.Length} objects, but the DataTable has {_columnCount} column. Partial and extended table rows are not accepted by the DataTable object.");

      var newRow = new DataRow(objects);
      _rows.Add(newRow);
      _rowCount++;
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
          _rows[i].Insert(index, objects[i]);
        }
        else
        {
          _rows[i].Add(objects[i]);
        }
      }
      _columnCount++;
    }
  }

  public class DataRow : Base
  {
    public DataRow() { }
    public DataRow(params object[] objects)
    {
      _data = new List<object>();
      _data.AddRange(objects);
    }

    private readonly List<object> _data;
    public object this[int index]
    {
      get => _data[index];
      set => _data[index] = value;
    }

    internal void Add(object value)
    {
      _data.Add(value);
    }

    internal void Insert(int index, object value)
    {
      _data.Insert(index, value);
    }

    internal void Remove(object value)
    {
      _data.Remove(value);
    }
    internal void RemoveAt(int index)
    {
      _data.RemoveAt(index);
    }
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
