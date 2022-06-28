namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public enum ToNativeMeshSettingEnum
    {
      Default,
      DxfImport,
      DxfImportInFamily
    }

    public ToNativeMeshSettingEnum ToNativeMeshSetting
    {
      get
      {
        if (!Settings.ContainsKey("pretty-mesh")) return ToNativeMeshSettingEnum.DxfImport;
        var value = Settings["pretty-mesh"];
        switch (value)
        {
          case "dxf":
            return ToNativeMeshSettingEnum.DxfImport;
          case "family-dxf":
            return ToNativeMeshSettingEnum.DxfImportInFamily;
          case "default":
          default:
            return ToNativeMeshSettingEnum.Default; 
        }
      }
      set
      {
        Settings["pretty-mesh"] = value switch
        {
          ToNativeMeshSettingEnum.DxfImport => "dxf",
          ToNativeMeshSettingEnum.DxfImportInFamily => "family-dxf",
          ToNativeMeshSettingEnum.Default => "default",
          _ => Settings["pretty-mesh"]
        };
      }
    }
  }
}