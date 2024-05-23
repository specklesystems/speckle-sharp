using System.Reflection;
using NUnit.Framework;
using Speckle.Autofac;

namespace Speckle.Converters.Revit2023.Tests;

[SetUpFixture]
public class RevitTests
{
  [OneTimeSetUp]
  public void Setup()
  {
    AppDomain.CurrentDomain.AssemblyResolve += (_, args) => AssemblyResolver.OnAssemblyResolve(Assembly.GetExecutingAssembly().Location, args);
    Console.WriteLine("done");
  }
}
