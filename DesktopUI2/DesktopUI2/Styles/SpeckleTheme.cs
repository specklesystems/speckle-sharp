using Avalonia.Media;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;

namespace DesktopUI2.Styles
{
  public class SpeckleTheme : BundledTheme
  {
    public static readonly Color Primary = Color.FromRgb(59, 130, 246);
    public static readonly Color Secondary = Color.FromRgb(131, 180, 255);
    public static readonly Color Accent = Color.FromRgb(255, 191, 0);

    public static readonly IBrush PrimaryBrush = new SolidColorBrush(Primary);
    public static readonly IBrush SecondaryBrush = new SolidColorBrush(Secondary);
    public static readonly IBrush AccentBrush = new SolidColorBrush(Accent);
    protected override void ApplyTheme(ITheme theme)
    {
      var bundledTheme = new BundledTheme
      {
        BaseTheme = BaseThemeMode.Light,
        //PrimaryColor = Material.Colors.PrimaryColor.Grey,
        //SecondaryColor = Material.Colors.SecondaryColor.Orange
      };
      var speckleTheme = Material.Styles.Themes.Theme.Create(bundledTheme.BaseTheme.Value.GetBaseTheme(), Primary, Accent);

      base.ApplyTheme(speckleTheme);


    }
  }

}
