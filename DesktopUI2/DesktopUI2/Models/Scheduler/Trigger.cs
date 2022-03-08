namespace DesktopUI2.Models.Scheduler
{
  public class Trigger
  {
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }

    public Trigger()
    {

    }
    public Trigger(string name, string slug, string icon)
    {
      Name = name;
      Slug = slug;
      Icon = icon;
    }
  }
}
