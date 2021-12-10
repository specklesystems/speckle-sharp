class ApplicationTestData
{
    public string ApplicationName;
    public List<string> Versions;
    public List<string> TestFiles;
    private string _applicationExecutable(string version) => Path.Combine(ApplicationName, " ", version);

    public ApplicationTestData(string applicationName, List<string> versions, List<string> testFiles)
    {
        ApplicationName = applicationName;
        Versions = versions;
        TestFiles = testFiles;
    }
}