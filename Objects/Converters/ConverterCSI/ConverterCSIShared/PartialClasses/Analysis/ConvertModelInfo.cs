using Objects.Structural.Analysis;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public void ModelInfoToNative(ModelInfo modelInfo)
  {
    Model.SetProjectInfo("Engineer", modelInfo.initials);
    Model.SetProjectInfo("Model Description", modelInfo.description);
    Model.SetProjectInfo("Project Number", modelInfo.projectNumber);
    Model.SetProjectInfo("Project Name", modelInfo.projectName);
    if (modelInfo.settings != null)
    {
      modelSettingsToNative(modelInfo.settings);
    }

    return;
  }

  public ModelInfo ModelInfoToSpeckle()
  {
    var modelInfo = new ModelInfo();
    modelInfo.name = Model.GetModelFilename(false);
    string programName,
      programVersion,
      programLevel;
    programVersion = programName = programLevel = null;
    Model.GetProgramInfo(ref programName, ref programVersion, ref programLevel);
    modelInfo.application = programName;
    modelInfo.settings = modelSettingsToSpeckle();
    int numberItems = 0;
    string[] items,
      data;
    items = data = null;
    Model.GetProjectInfo(ref numberItems, ref items, ref data);
    if (numberItems != 0)
    {
      for (int index = 0; index < numberItems; index++)
      {
        switch (items[index])
        {
          default:
            break;
          case "Engineer":
            modelInfo.initials = data[index];
            break;
          case "Model Description":
            modelInfo.description = data[index];
            break;
          case "Project Name":
            modelInfo.projectName = data[index];
            break;
          case "Project Number":
            modelInfo.projectNumber = data[index];
            break;
        }
      }
    }
    return modelInfo;
  }
}
