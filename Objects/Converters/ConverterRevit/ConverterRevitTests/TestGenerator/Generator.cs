using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace TestGenerator;

[Generator]
public class Generator : ISourceGenerator
{
  public const string ToNative = "ToNative";
  public const string Updated = "Updated";

  public void Execute(GeneratorExecutionContext context)
  {
    //Debugger.Launch();
    var sb = new StringBuilder();
    sb.Append(TestTemplate.StartNamespace);

    // get any directory in the speckle sharp repo
    var directoryInSharp = context.Compilation.Assembly.Locations.First().ToString();
    directoryInSharp = directoryInSharp.Replace("SourceFile(", "");

    // get the year for the test project as a string
    var assemblyName = context.Compilation.AssemblyName;
    var year = assemblyName.Substring(assemblyName.Length - 4);

    string testFolderLocation = Globals.GetTestModelFolderLocation(directoryInSharp, year);
    var subdirectories = Directory.GetDirectories(testFolderLocation);

    foreach (var subdir in subdirectories)
    {
      string[] splitter = subdir.Split('\\');
      string category = splitter[splitter.Length - 1];

      if (!Categories.CategoriesDict.TryGetValue(category.ToLower(), out var categoryProps))
      {
        continue;
        //throw new Exception($"Category {category} is not in the CategoriesDict in the class {typeof(Categories).FullName}");
      }

      var baseFiles = new List<string>();
      var toNativeFiles = new List<string>();
      var updatedFiles = new List<string>();
      foreach (var file in Directory.GetFiles(subdir))
      {
        var strippedFile = file.Split('\\').Last();
        if (!strippedFile.EndsWith(".rvt"))
        {
          continue;
        }

        strippedFile = strippedFile.Replace(".rvt", "");

        // illegal character in revit file name
        if (strippedFile.Contains('.'))
        {
          continue;
        }

        if (strippedFile.EndsWith(ToNative))
        {
          toNativeFiles.Add(strippedFile);
        }
        else if (strippedFile.EndsWith(Updated))
        {
          updatedFiles.Add(strippedFile);
        }
        else
        {
          baseFiles.Add(strippedFile);
        }
      }

      ValidateFilesInFolder(category, baseFiles, toNativeFiles, updatedFiles);
      AddTestToStringBuilder(sb, category, categoryProps, baseFiles, toNativeFiles, updatedFiles);
    }

    sb.Append(TestTemplate.EndNamespace);
    context.AddSource($"GeneratedTests.g.cs", sb.ToString());
  }

  private static void AddTestToStringBuilder(
    StringBuilder sb,
    string category,
    CategoryProperties categoryProps,
    List<string> baseFiles,
    List<string> toNativeFiles,
    List<string> updatedFiles
  )
  {
    foreach (var file in baseFiles)
    {
      sb.Append(TestTemplate.CreateFixture(category, file));
      var runToNativeTest = toNativeFiles.Contains(file + ToNative);
      var runUpdateTest = updatedFiles.Contains(file + Updated);

      sb.Append(TestTemplate.InitTest(category, file));
      sb.Append(TestTemplate.CreateToSpeckleTest(category, file));

      if (runToNativeTest)
      {
        sb.Append(
          TestTemplate.CreateToNativeTest(
            category,
            file,
            categoryProps.RevitType,
            categoryProps.SyncAssertFunc ?? "null",
            categoryProps.AsyncAssertFunc ?? "null"
          )
        );
        sb.Append(
          TestTemplate.CreateSelectionTest(
            category,
            file,
            categoryProps.RevitType,
            categoryProps.SyncAssertFunc ?? "null",
            categoryProps.AsyncAssertFunc ?? "null"
          )
        );
      }
      if (runUpdateTest)
      {
        sb.Append(
          TestTemplate.CreateUpdateTest(
            category,
            file,
            categoryProps.RevitType,
            categoryProps.SyncAssertFunc ?? "null",
            categoryProps.AsyncAssertFunc ?? "null"
          )
        );
      }
      sb.Append(TestTemplate.EndClass);
    }
  }

  private static void ValidateFilesInFolder(
    string category,
    List<string> baseFiles,
    List<string> toNativeFiles,
    List<string> updatedFiles
  )
  {
    foreach (var file in toNativeFiles)
    {
      if (!baseFiles.Contains(file.Substring(0, file.Length - ToNative.Length)))
      {
        throw new FileNotFoundException(
          $"There is a file named {file} in folder {category}, but there is no corrosponding base file (without the {ToNative} extension)."
        );
      }
    }
    foreach (var file in updatedFiles)
    {
      if (!baseFiles.Contains(file.Substring(0, file.Length - Updated.Length)))
      {
        throw new FileNotFoundException(
          $"There is a file named {file} in folder {category}, but there is no corrosponding base file (without the {Updated} extension)."
        );
      }
    }
  }

  public void Initialize(GeneratorInitializationContext context)
  {
    // No initialization required for this one
    //Debugger.Launch();
  }
}
