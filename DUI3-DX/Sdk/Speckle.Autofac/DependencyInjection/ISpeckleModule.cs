namespace Speckle.Autofac.DependencyInjection;

public interface ISpeckleModule
{
  void Load(SpeckleContainerBuilder builder);
}
