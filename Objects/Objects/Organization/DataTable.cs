using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

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
      get => _data[row][column];
      set => _data[row][column] = value;
    }

    private int _rowCount = 0;
    public int RowCount => _rowCount;

    private int _columnCount = 0;
    public int ColumnCount => _columnCount;

    [Chunkable()]
    [DetachProperty]
    internal List<List<object>> _data { get; } = new List<List<object>>();
    public List<IDataColumn> Columns { get; } = new List<IDataColumn>();
    public List<DataRow> Rows { get; } = new List<DataRow>();

    public void AddRow(Dictionary<string, object> metadata, params object[] objects)
    {
      if (objects.Length != _columnCount)
        throw new ArgumentException($"\"AddRow\" method was passed {objects.Length} objects, but the DataTable has {_columnCount} column. Partial and extended table rows are not accepted by the DataTable object.");

      var newRow = new List<object>();
      for (var i = 0; i < _columnCount; i++)
      {
        if (objects[i] != null 
          && (objects[i].GetType() != Columns[i].Type 
            || objects[i].GetType().IsSubclassOf(Columns[i].Type)))
          throw new ArgumentException($"Trying to add value \"{objects[i]}\" of type \"{objects[i].GetType().Name}\" to table column at index {i}. This column is expecting a value of type {Columns[i].GetType().Name}");

        newRow.Add(objects[i]);
      }

      _data.Add(newRow);
      Rows.Add(new DataRow(this, metadata));
      _rowCount++;
    }

    public DataRow GetRow(int index)
    {
      return Rows[index];
    }

    public void DefineColumn<T>()
    {
      var newColumn = new DataColumn<T>(this);
      Columns.Add(newColumn);
      _columnCount++;
    }

    public void AddColumn<T>(int index = -1, params T[] objects)
    {
      if (objects.Length == 0)
        throw new ArgumentException("No objects provided. Use \"DefineColumn\" to define an empty column");

      if (objects.Length != _rowCount)
        throw new ArgumentException($"\"AddColumn\" method was passed {objects.Length} objects, but the DataTable has {_rowCount} rows. Partial and extended table columns are not accepted by the DataTable object.");

      if (index > _rowCount || index < -1)
        throw new ArgumentException($"Column index {index} is out of range");

      if (index == -1)
      {
        index = objects.Length - 1;
      }

      DefineColumn<T>();
      for (var i = 0; i < objects.Length; i++ )
      {
        if (index < objects.Length)
        {
          _data[i].Insert(index, objects[i]);
        }
        else
        {
          _data[i].Add(objects[i]);
        }
      }
    }

    public IDataColumn GetColumn(int index)
    {
      return Columns[index];
    }

    private void AddCellValue()
    {

    }
  }

  public class TableData : Base
  {

  }

  public class DataRow : Base
  {
    public int RowIndex => ParentTable.Rows.FindIndex(row => row == this);
    public DataRow() { }
    public DataRow(DataTable table, Dictionary<string, object> metadata)
    {
      ParentTable = table;
      Metadata = metadata;
    }
    //public DataRow(params T[] objects)
    //{
    //  CellData.AddRange(objects);
    //}
    public IEnumerable CellData
    {
      get
      {
        return ParentTable._data[RowIndex];
      }
    }
    public IEnumerator GetEnumerator()
    {
      return CellData.GetEnumerator();
    }
    public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

    [DetachProperty]
    public DataTable ParentTable { get; }

    public object this[int index]
    {
      get => ParentTable._data[RowIndex][index];
      set => ParentTable._data[RowIndex][index] = value;
    }
  }
  public interface IDataColumn : IEnumerable
  {
    public DataTable ParentTable { get; }
    public Type Type { get; }
  }
  
  public class DataColumn<T> : Base, IDataColumn
  {
    [JsonIgnore]
    public Type Type => typeof(T);
    public int ColumnIndex => ParentTable.Columns.FindIndex(col => col as DataColumn<T> == this);
    public DataColumn() { }
    public DataColumn(DataTable table) 
    {
      ParentTable = table;
    }
    //public DataColumn(params T[] objects)
    //{
    //  CellData.AddRange(objects);
    //}
    public IEnumerable<T> CellData 
    { 
      get
      {
        var index = ColumnIndex;
        foreach (var row in ParentTable._data)
        {
          yield return (T)row[index];
        }
      }
    }
    public IEnumerator GetEnumerator()
    {
      return CellData.GetEnumerator();
    }
    public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

    [DetachProperty]
    public DataTable ParentTable { get; }

    public T this[int index]
    {
      get => (T)ParentTable._data[index][ColumnIndex];
      set => ParentTable._data[index][ColumnIndex] = value;
    }

    //internal void Add(T value)
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
}
