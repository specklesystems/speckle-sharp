namespace Speckle.Connectors.DUI;

// POC: XAML file accept Static only, but later we can search more is it possible to inject this? or necessary??

/// <summary>
/// Only place that we reference to URLs to connector UIs.
/// </summary>
/// <remarks>
/// If we are on 'work in progress' branch on UI repo,
/// we can replace the netlify url with 'preview' one which is provided within each PR.
///   sample url produced by PR on `dui3` branch on `speckle-server` -> deploy-preview-2076--boisterous-douhua-e3cefb.netlify.app
/// </remarks>
public static class Url
{
  public static readonly Uri Netlify = new("https://boisterous-douhua-e3cefb.netlify.app/");

  // In CefSharp XAML file we cannot call ToString() function over URI
  public static readonly string NetlifyString = Netlify.ToString();
}
