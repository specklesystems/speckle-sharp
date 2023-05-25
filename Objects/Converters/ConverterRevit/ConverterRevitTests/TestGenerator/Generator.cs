using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace TestGenerator
{
  [Generator]
  public class Generator : ISourceGenerator
  {
    public void Execute(GeneratorExecutionContext context)
    {
      var sb = new StringBuilder();

      sb.Append(TestTemplate.StartCode);
      sb.Append(TestTemplate.Create("Beam", "Beam", "DB.FamilyInstance", "AssertFamilyInstanceEqual"));
      sb.Append(TestTemplate.EndCode);

      // Add the source code to the compilation
      context.AddSource($"GeneratedTests.g.cs", sb.ToString());
    }

    public void Initialize(GeneratorInitializationContext context)
    {
      // No initialization required for this one
      //Debugger.Launch();
    }
  }
}
