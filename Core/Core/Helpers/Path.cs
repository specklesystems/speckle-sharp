# nullable enable

using System;
using System.IO;
using System.Reflection;

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
    public static void OverrideApplicationName(string applicationName)
    {
      _applicationName = applicationName;
    }

    private static string _userDataPathEnvVar => "SPECKLE_USERDATA_PATH";
    private static string? _path => Environment.GetEnvironmentVariable(_userDataPathEnvVar);

    /// <summary>
    /// Override the global Speckle application data path.
    /// </summary>
    public static void OverrideApplicationDataPath(string? path)
    {
      Environment.SetEnvironmentVariable(_userDataPathEnvVar, path);
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
    public static string UserApplicationDataPath()
    {
      // if we have an override, just return that
      var pathOverride = _path;
      if (pathOverride != null && !string.IsNullOrEmpty(pathOverride))
        return pathOverride;

      // on desktop linux and macos we use the appdata.
      // but we might not have write access to the disk
      // so the catch falls back to the user profile
      try
      {
        return Environment.GetFolderPath(
          Environment.SpecialFolder.ApplicationData,
          // if the folder doesn't exist, we get back an empty string on OSX,
          // which in turn, breaks other stuff down the line.
          // passing in the Create option ensures that this directory exists,
          // which is not a given on all OS-es.
          Environment.SpecialFolderOption.Create
        );
      }
      catch
      {
        // on server linux, there might not be a user setup, things can run under root
        // in that case, the appdata variable is most probably not set up
        // we fall back to the value of the home folder
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
      }
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
    public static string InstallSpeckleFolderPath =>
      EnsureFolderExists(InstallApplicationDataPath, _applicationName);

    /// <summary>
    /// Get the folder where the user's Speckle data should be stored.
    /// </summary>
    public static string UserSpeckleFolderPath =>
      EnsureFolderExists(UserApplicationDataPath(), _applicationName);

    /// <summary>
    /// Get the folder where the user's Speckle blobs should be stored.
    /// </summary>
    public static string BlobStoragePath(string? path = null) =>
      EnsureFolderExists(path ?? UserSpeckleFolderPath, _blobFolderName);

    /// <summary>
    /// Get the folder where the Speckle kits should be stored.
    /// </summary>
    public static string KitsFolderPath =>
      EnsureFolderExists(InstallSpeckleFolderPath, _kitsFolderName);

    /// <summary>
    ///
    /// </summary>
    public static string ObjectsFolderPath =>
      EnsureFolderExists(KitsFolderPath, _objectsFolderName);

    /// <summary>
    /// Get the folder where the Speckle accounts data should be stored.
    /// </summary>
    public static string AccountsFolderPath =>
      EnsureFolderExists(UserSpeckleFolderPath, _accountsFolderName);

    private static string EnsureFolderExists(string basePath, string folderName)
    {
      var path = Path.Combine(basePath, folderName);
      Directory.CreateDirectory(path);
      return path;
    }

    private static string _logFolderName = "Logs";

    /// <summary>
    /// Get the folder where the Speckle logs should be stored.
    /// </summary>
    /// <param name="hostApplicationName">Name of the application using this SDK ie.: "Rhino"</param>
    /// <param name="hostApplicationVersion">Public version slug of the application using this SDK ie.: "2023"</param>
    public static string LogFolderPath(
      string hostApplicationName,
      string? hostApplicationVersion
    ) =>
      EnsureFolderExists(
        EnsureFolderExists(UserSpeckleFolderPath, _logFolderName),
        $"{hostApplicationName}{hostApplicationVersion ?? ""}"
      );
  }
}
