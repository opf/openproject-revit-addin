namespace OpenProject
{
  public interface IOpenProjectSettings
  {
    public bool EnableDevelopmentTools { get; set; }
    public string OpenProjectInstances { get; set; }
    public string LastVisitedPage { get; set; }
  }
}
