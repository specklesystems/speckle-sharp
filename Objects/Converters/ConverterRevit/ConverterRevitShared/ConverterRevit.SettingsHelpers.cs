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
        if (!conversionSettings.TryGetSettingBySlug("pretty-mesh", out var value))
        {
          return ToNativeMeshSettingEnum.Default;
        }
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
        var meshSetting = value switch
        {
          ToNativeMeshSettingEnum.DxfImport => dxf,
          ToNativeMeshSettingEnum.DxfImportInFamily => familyDxf,
          ToNativeMeshSettingEnum.Default => defaultValue,
          _ => null
        };
        if (meshSetting != null) conversionSettings.SetSettingBySlug("pretty-mesh", meshSetting);
      }
    }
  }
}
