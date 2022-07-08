namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // CAUTION: these strings need to have the same values as in the connector
    const string defaultValue = "Default";
    const string dxf = "DXF";
    const string familyDxf = "Family DXF";

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
        if (!Settings.ContainsKey("pretty-mesh")) return ToNativeMeshSettingEnum.Default;
        var value = Settings["pretty-mesh"];
        switch (value)
        {
          case dxf:
            return ToNativeMeshSettingEnum.DxfImport;
          case familyDxf:
            return ToNativeMeshSettingEnum.DxfImportInFamily;
          case defaultValue:
          default:
            return ToNativeMeshSettingEnum.Default; 
        }
      }
      set
      {
        Settings["pretty-mesh"] = value switch
        {
          ToNativeMeshSettingEnum.DxfImport => dxf,
          ToNativeMeshSettingEnum.DxfImportInFamily => familyDxf,
          ToNativeMeshSettingEnum.Default => defaultValue,
          _ => Settings["pretty-mesh"]
        };
      }
    }
  }
}