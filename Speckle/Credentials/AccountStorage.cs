using System;
using System.Data.SQLite;
using System.IO;

namespace Speckle.Credentials
{
  public class AccountSqlLiteStorage
  {
    public string RootPath { get; set; }

    public string ConnectionString { get; set; }

    private SQLiteConnection Connection { get; set; }

    public AccountSqlLiteStorage(string basePath = null, string applicationName = "Speckle", string scope = "Accounts")
    {
      if (basePath == null)
        basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

      Directory.CreateDirectory(Path.Combine(basePath, applicationName)); //ensure dir is there

      RootPath = Path.Combine(basePath, applicationName, $"{scope}.db");
      ConnectionString = $@"URI=file:{RootPath};";

      Initialize();
    }

    private void Initialize()
    {
      using (var c = new SQLiteConnection(ConnectionString))
      {
        c.Open();
        using (var command = new SQLiteCommand(c))
        {
          command.CommandText = @"
            CREATE TABLE IF NOT EXISTS accounts(
              id TEXT PRIMARY KEY,
              serverUrl TEXT,
              email TEXT,
              content TEXT
            ) WITHOUT ROWID;
          ";
          command.ExecuteNonQuery();
        }
      }
    }

    public Account GetAccount(string id)
    {
      throw new NotImplementedException();
    }

    public Account GetAllAccounts()
    {
      throw new NotImplementedException();
    }

    public void SaveAccount(Account account)
    {
      throw new NotImplementedException();
    }

    public void DeleteAccount(Account account)
    {
      throw new NotImplementedException();
    }

  }
}
