namespace Speckle.Autofac.DependencyInjection;

// POC: NEXT UP
// * begin scope: https://stackoverflow.com/questions/49595198/autofac-resolving-through-factory-methods
// Interceptors?

// POC: this might be somehting that could go in a wholly converter agnostic project
public interface IFactory<in TKey, out TValue>
  where TValue : class
{
  TValue ResolveInstance(TKey strongName);
}
