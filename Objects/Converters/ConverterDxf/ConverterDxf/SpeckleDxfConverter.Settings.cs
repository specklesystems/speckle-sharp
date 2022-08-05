using System;

namespace Objects.Converters.DxfConverter
{
    public partial class SpeckleDxfConverter
    {
        public void SetConverterSettings(object settings) => SetConverterSettings((ConverterDxfSettings)settings);

        private void SetConverterSettings(ConverterDxfSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
    }
}