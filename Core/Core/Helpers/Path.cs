# nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Speckle.Core.Helpers
{
  /// <summary>
  /// Helper class dedicated for Speckle specific Path operations.
  /// </summary>
  public static class SpecklePathProvider
  {
    private static string _applicationName = "Speckle";

    /// <summary>
    /// Override the global Speckle application name.
    /// </summary>
    /// <param name="applicationName"></param>
    public static void OverrideApplicationName(string applicationName) { _applicationName = applicationName; }

    private static string? _path = null;

    /// <summary>
    /// Override the global Speckle application data path.
    /// </summary>
    public static void OverrideApplicationDataPath(string? path)
    {
      _path = path;
    }

    private static string _blobFolderName = "Blobs";

    /// <summary>
    /// Override the global Blob storage folder name.
    /// </summary>
    public static void OverrideBlobStorageFolder(string blobFolderName)
    {
      _blobFolderName = blobFolderName;
    }

    private static string _kitsFolderName = "Kits";

    /// <summary>
    ///  Override the global Kits folder name.
    /// </summary>
    public static void OverrideKitsFolderName(string kitsFolderName)
    {
      _kitsFolderName = kitsFolderName;
    }

    private static string _accountsFolderName = "Accounts";

    /// <summary>
    /// Override the global Accounts folder name.
    /// </summary>
    public static void OverrideAccountsFolderName(string accountsFolderName)
    {
      _accountsFolderName = accountsFolderName;
    }
    
    /// <summary>
    ///  
    /// </summary>
    public static void OverrideObjectsFolderName(string objectsFolderName)
    {
      _objectsFolderName = objectsFolderName;
    }

    private static string _objectsFolderName = "Objects";

    /// <summary>
    /// Get the platform specific user configuration folder path.
    /// </summary>
    public static string UserApplicationDataPath() {
      // if we have an override, just return that
      if (_path != null) return _path;

      // on windows we do this
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        // don't switch to the one below it causes issues for users:
        // https://speckle.community/t/cant-find-speckle-kits-when-using-gh-sdk/3297/14
        // return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Roaming");

      // on desktop linux and macos we use the appdata.
      var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      if (!String.IsNullOrEmpty(appDataFolder)) return appDataFolder;

      // on server linux, there might not be a user setup, things can run under root
      // in that case, the appdata variable is most probably not set up
      // we fall back to the value of the home folder
      return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    }

    /// <summary>
    /// Get the installation path.
    /// </summary>
    public static string InstallApplicationDataPath =>
        Assembly.GetAssembly(typeof(SpecklePathProvider)).Location.Contains("ProgramData")
          ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
          : UserApplicationDataPath();

    /// <summary>
    /// Get the path where the Speckle applications should be installed
    /// </summary>
    public static string InstallSpeckleFolderPath => EnsureFolderExists(InstallApplicationDataPath, _applicationName);

    /// <summary>
    /// Get the folder where the user's Speckle data should be stored.
    /// </summary>
    public static string UserSpeckleFolderPath => EnsureFolderExists(UserApplicationDataPath(), _applicationName);

    /// <summary>
    /// Get the folder where the user's Speckle blobs should be stored.
    /// </summary>
    public static string BlobStoragePath(string? path = null)
      => EnsureFolderExists(path ?? UserSpeckleFolderPath, _blobFolderName);

    /// <summary>
    /// Get the folder where the Speckle kits should be stored.
    /// </summary>
    public static string KitsFolderPath => EnsureFolderExists(InstallSpeckleFolderPath, _kitsFolderName);
    
    /// <summary>
    /// 
    /// </summary>
    public static string ObjectsFolderPath => EnsureFolderExists(KitsFolderPath, _objectsFolderName);

    /// <summary>
    /// Get the folder where the Speckle accounts data should be stored.
    /// </summary>
    public static string AccountsFolderPath => EnsureFolderExists(UserSpeckleFolderPath, _accountsFolderName);

    private static string EnsureFolderExists(string basePath, string folderName)
    {
      var path = Path.Combine(basePath, folderName);
      Directory.CreateDirectory(path);
      return path;
    }
  }
}
