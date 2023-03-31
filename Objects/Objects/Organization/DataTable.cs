using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Organization
{
  public class DataTable : Base
  {
    public DataTable() { }

    [JsonIgnore]
    public object this[int column, int row]
    {
      get => DataStorage.Data[row][column];
      set => DataStorage.Data[row][column] = value;
    }
    public DataStorage DataStorage { get; set; } = new DataStorage();

    #region Rows
    public List<DataRow> Rows { get; set; } = new List<DataRow>();
    public int RowCount => Rows.Count;
    public void DefineRow(out DataRow dataRow, Dictionary<string, object> metadata = null, int index = -1)
    {
      if (index < -1 || index > RowCount)
        throw new ArgumentException($"Index of value {index} is outside of the acceptable range, -1:{RowCount}");

      dataRow = new DataRow(this, metadata);

      if (index == -1)
      {
        index = Rows.Count;
      }

      if (index < Rows.Count)
      {
        Rows.Insert(index, dataRow);
        DataStorage.Data.Insert(index, new List<object>());
      }
      else
      {
        Rows.Add(dataRow);
        DataStorage.Data.Add(new List<object>());
      }
    }
    public void AddRow(Dictionary<string, object> metadata = null, int index = -1, params object[] objects)
    {
      #region validation
      if (objects.Length != Columns.Count)
        throw new ArgumentException($"\"AddRow\" method was passed {objects.Length} objects, but the DataTable has {Columns.Count} column. Partial and extended table rows are not accepted by the DataTable object.");

      var newRow = new List<object>();
      for (var i = 0; i < Columns.Count; i++)
      {
        if (objects[i] != null 
          && (objects[i].GetType() != Columns[i].DataType
            || objects[i].GetType().IsSubclassOf(Columns[i].DataType)))
          throw new ArgumentException($"Trying to add value \"{objects[i]}\" of type \"{objects[i].GetType().Name}\" to table column at index {i}. This column is expecting a value of type {Columns[i].GetType().Name}");

        newRow.Add(objects[i]);
      }
      #endregion

      DefineRow(out var dataRow, metadata, index);
      dataRow.PopulateRow(objects);
    }

    public DataRow GetRow(int index)
    {
      return Rows[index];
    }
    #endregion

    #region Columns
    public List<IDataColumn> Columns { get; set; } = new List<IDataColumn>();
    public int ColumnCount => Columns.Count;
    public void DefineColumn<T>(out DataColumn newColumn, Dictionary<string, object> metadata = null, int index = -1)
    {
      if (index < -1 || index > RowCount)
        throw new ArgumentException($"Index of value {index} is outside of the acceptable range, -1:{RowCount}");

      newColumn = new DataColumn(this, typeof(T), metadata);

      if (index == -1)
      {
        index = Columns.Count;
      }

      if (index < Columns.Count)
        Columns.Insert(index, newColumn);
      else
        Columns.Add(newColumn);
    }

    public void AddColumn<T>(Dictionary<string, object> metadata = null, int index = -1, params object[] objects)
    {
      if (objects.Length == 0)
        throw new ArgumentException("No objects provided. Use \"DefineColumn\" to define an empty column");

      if (objects.Length != Rows.Count)
        throw new ArgumentException($"\"AddColumn\" method was passed {objects.Length} objects, but the DataTable has {Rows.Count} rows. Partial and extended table columns are not accepted by the DataTable object.");

      DefineColumn<T>(out var newColumn, metadata, index);
      newColumn.Populate(objects);
    }

    public IDataColumn GetColumn(int index)
    {
      return Columns[index];
    }

    #endregion
  }

  public class DataStorage : Base
  {
    [Chunkable()]
    [DetachProperty]
    public List<List<object>> Data { get; set; } = new List<List<object>>();
  }

  public class DataRow : Base
  {
    public int RowIndex => ParentTable.Rows.FindIndex(row => row == this);
    public DataRow() { }
    public DataRow(DataTable table, Dictionary<string, object> metadata = null)
    {
      ParentTable = table;

      if (metadata != null)
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
        return ParentTable.DataStorage.Data[RowIndex];
      }
    }
    public IEnumerator GetEnumerator()
    {
      return CellData.GetEnumerator();
    }
    public Dictionary<string, object> Metadata { get; set;  } = new Dictionary<string, object>();

    [DetachProperty]
    public DataTable ParentTable { get; set; }

    [JsonIgnore]
    public object this[int index]
    {
      get
      {
        VerifyDataTableExists();
        return ParentTable.DataStorage.Data[RowIndex][index];
      }
      set
      {
        VerifyDataTableExists();
        ParentTable.DataStorage.Data[RowIndex][index] = value;
      }
    }

    private void VerifyDataTableExists()
    {
      if (ParentTable == null)
        throw new Exception("DataRow must be added to a DataTable using the \"DataTable.AddRow()\" method before data can be added to the DataRow");
    }

    internal void PopulateRow(object[] objects)
    {
      ParentTable.DataStorage.Data[RowIndex] = new List<object>(objects);
    }
  }
  public interface IDataColumn : IEnumerable
  {
    public DataTable ParentTable { get; set; }
    public Type DataType { get; set; }
  }

  public class DataColumn : Base, IDataColumn
  {
    [JsonIgnore]
    public Type DataType { get; set; } = typeof(object);
    public int ColumnIndex => ParentTable.Columns.FindIndex(col => col == this);
    public DataColumn() { }
    public DataColumn(DataTable table, Type type, Dictionary<string, object> metadata = null)
    {
      ParentTable = table;
      DataType = type;
      if (metadata != null)
        Metadata = metadata;
    }
    public IEnumerable CellData
    {
      get
      {
        var index = ColumnIndex;
        foreach (var row in ParentTable.DataStorage.Data)
        {
          yield return row[index];
        }
      }
    }
    public IEnumerator GetEnumerator()
    {
      return CellData.GetEnumerator();
    }

    internal void Populate(object[] objects)
    {
      var index = ColumnIndex;
      for (var i = 0; i < ParentTable.Rows.Count; i++)
      {
        if (index < ParentTable.Rows.Count)
        {
          ParentTable.DataStorage.Data[i].Insert(index, objects[i]);
        }
        else
        {
          ParentTable.DataStorage.Data[i].Add(objects[i]);
        }
      }
    }

    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    [DetachProperty]
    public DataTable ParentTable { get; set; }

    [JsonIgnore]
    public object this[int index]
    {
      get => ParentTable.DataStorage.Data[index][ColumnIndex];
      set => ParentTable.DataStorage.Data[index][ColumnIndex] = value;
    }
  }

  //public class DataColumn<T> : Base, IDataColumn
  //{
  //  [JsonIgnore]
  //  public Type DataType => typeof(T);
  //  public int ColumnIndex => ParentTable.Columns.FindIndex(col => col as DataColumn<T> == this);
  //  public DataColumn() { }
  //  public DataColumn(DataTable table, Dictionary<string, object> metadata = null) 
  //  {
  //    ParentTable = table;
  //    if (metadata != null)
  //      Metadata = metadata;
  //  }
  //  //public DataColumn(params T[] objects)
  //  //{
  //  //  CellData.AddRange(objects);
  //  //}
  //  public IEnumerable<T> CellData 
  //  { 
  //    get
  //    {
  //      var index = ColumnIndex;
  //      foreach (var row in ParentTable.DataStorage.Data)
  //      {
  //        yield return (T)row[index];
  //      }
  //    }
  //  }
  //  public IEnumerator GetEnumerator()
  //  {
  //    return CellData.GetEnumerator();
  //  }

  //  internal void Populate(T[] objects)
  //  {
  //    var index = ColumnIndex;
  //    for (var i = 0; i < ParentTable.Rows.Count; i++)
  //    {
  //      if (index < ParentTable.Rows.Count)
  //      {
  //        ParentTable.DataStorage.Data[i].Insert(index, objects[i]);
  //      }
  //      else
  //      {
  //        ParentTable.DataStorage.Data[i].Add(objects[i]);
  //      }
  //    }
  //  }

  //  public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

  //  [DetachProperty]
  //  public DataTable ParentTable { get; }

  //  [JsonIgnore]
  //  public T this[int index]
  //  {
  //    get => (T)ParentTable.DataStorage.Data[index][ColumnIndex];
  //    set => ParentTable.DataStorage.Data[index][ColumnIndex] = value;
  //  }
  //}
}
