namespace Speckle.Converters.RevitShared.Services;

internal interface ICachingService
{
  public T GetOrAdd<T>(string key, Func<string, T> valueFactory);
  public T GetOrAdd<T>(string key, Func<string, T> valueFactory, out bool isExistingValue);
  public bool TryGet<T>(string key, out T? cachedObject);
  void AddMany<T>(IEnumerable<T> elements, Func<T, string> keyFactory);
}
