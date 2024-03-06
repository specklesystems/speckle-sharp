namespace Speckle.Autofac.DependencyInjection;

// POC: NEXT UP
// * begin scope: https://stackoverflow.com/questions/49595198/autofac-resolving-through-factory-methods
// Interceptors?

// POC: this might be somehting that could go in a wholly converter agnostic project
public interface IFactory<K, T>
  where T : class
{
  T ResolveInstance(K strongName);
}
