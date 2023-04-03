using System;
using System.Collections.Generic;
using System.Linq;
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
    [DetachProperty]
    public DataStorage DataStorage { get; set; } = new DataStorage();
    public int RowCount => Rows?.Count ?? 0;
    private List<DataRow> _rows = null;
    public IReadOnlyList<DataRow> Rows
    {
      get => _rows;
      set
      {
        // don't let the user set this list! this is only for deserialization
        // seems a bit hacky
        if (_rows != null) 
          return;

        _rows = value as List<DataRow>;
      }
    }

    public int ColumnCount => Columns?.Count ?? 0;
    private List<DataColumn> _columns = null;
    public IReadOnlyList<DataColumn> Columns
    {
      get => _columns;
      set
      {
        // don't let the user set this list! this is only for deserialization
        // seems a bit hacky
        if (_columns != null) 
          return;

        _columns = value as List<DataColumn>;
      }
    }

    public void DefineRow(out DataRow dataRow, Dictionary<string, object> metadata = null, int index = -1)
    {
      if (index < -1 || index > RowCount)
        throw new ArgumentException($"Index of value {index} is outside of the acceptable range, -1:{RowCount}");

      _rows ??= new List<DataRow>();

      if (index == -1)
      {
        index = RowCount;
      }

      dataRow = new DataRow(index, DataStorage, metadata);

      if (index == RowCount)
      {
        _rows.Add(dataRow);
        DataStorage.Data.Add(new List<object>());
      }
      else
      {
        DataStorage.Data.Insert(index, new List<object>());
        _rows.Insert(index, dataRow);
        
        for (var i = index + 1; i < RowCount; i++)
        {
          _rows[i].Index++;
        }
      }
    }

    public void AddRow(Dictionary<string, object> metadata = null, int index = -1, params object[] objects)
    {
      #region validation
      if (objects.Length != ColumnCount)
        throw new ArgumentException($"\"AddRow\" method was passed {objects.Length} objects, but the DataTable has {ColumnCount} column. Partial and extended table rows are not accepted by the DataTable object.");

      var newRow = new List<object>();
      for (var i = 0; i < ColumnCount; i++)
      {
        //if (objects[i] != null
        //  && (objects[i].GetType() != Columns[i].DataType
        //    || objects[i].GetType().IsSubclassOf(Columns[i].DataType)))
        //  throw new ArgumentException($"Trying to add value \"{objects[i]}\" of type \"{objects[i].GetType().Name}\" to table column at index {i}. This column is expecting a value of type {Columns[i].GetType().Name}");

        newRow.Add(objects[i]);
      }
      #endregion

      DefineRow(out var dataRow, metadata, index);
      dataRow.Populate(objects);
    }

    public void DefineColumn<T>(out DataColumn dataColumn, Dictionary<string, object> metadata = null, int index = -1)
    {
      if (index < -1 || index > ColumnCount)
        throw new ArgumentException($"Index of value {index} is outside of the acceptable range, -1:{ColumnCount}");

      _columns ??= new List<DataColumn>();

      if (index == -1)
      {
        index = ColumnCount;
      }

      dataColumn = new DataColumn(index, DataStorage, metadata);

      if (index == ColumnCount)
      {
        _columns.Add(dataColumn);
        //DataStorage.Data.Add(new List<object>());
      }
      else
      {
        //DataStorage.Data.Insert(index, new List<object>());
        _columns.Insert(index, dataColumn);

        for (var i = index + 1; i < ColumnCount; i++)
        {
          _columns[i].Index++;
        }
      }
    }

    private void IncrementDataRowIndicies(int startingIndex, bool elementAdded)
    {
      throw new NotImplementedException();
    }

    //public void InitBindings()
    //{
    //  _rows.ListChanged += Rows_ListChanged;
    //}

    //private void Rows_ListChanged(object sender, ListChangedEventArgs e)
    //{
    //  if (e.ListChangedType == ListChangedType.ItemAdded)
    //  {
    //    for (var i = e.NewIndex + 1; i < RowCount; i++)
    //    {
    //      _rows[i].Index++;
    //    }
    //  }
    //  else if (e.ListChangedType == ListChangedType.ItemDeleted)
    //  {
    //    var old = e.OldIndex;
    //    var new1 = e.NewIndex;
    //    //for (var i = e.o)
    //  }
    //}
  }

  public class DataStorage : Base
  {
    public List<List<object>> Data { get; set; } = new List<List<object>>();
  }

  public class DataRow : Base
  {
    public DataRow() { }
    public int Index { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    [DetachProperty]
    public DataStorage DataStorage { get; set; }
    public DataRow(int index, DataStorage storage, Dictionary<string, object> metadata = null)
    {
      Index = index;
      DataStorage = storage;
      Metadata = metadata;
    }
    internal void Populate(object[] objects)
    {
      DataStorage.Data[Index] = new List<object>(objects);
    }
  }
  
  public class DataColumn : Base
  {
    public DataColumn() { }
    public int Index { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    [DetachProperty]
    public DataStorage DataStorage { get; set; }
    [JsonIgnore]
    public Type DataType { get; set; }
    public DataColumn(int index, DataStorage storage, Dictionary<string, object> metadata = null)
    {
      Index = index;
      DataStorage = storage;
      Metadata = metadata;
    }
  }
}
