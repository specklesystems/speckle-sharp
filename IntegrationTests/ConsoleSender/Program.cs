Console.WriteLine("Hello, Console sender!");

var config = await File.ReadAllTextAsync(Path.Combine(Path.GetTempPath(), "revit.json"));

Console.WriteLine(config);

Console.WriteLine("I' done.");