namespace Speckle.Autofac.DependencyInjection;

public interface ISpeckleContainerContext
{
  T Resolve<T>()
    where T : notnull;
}
