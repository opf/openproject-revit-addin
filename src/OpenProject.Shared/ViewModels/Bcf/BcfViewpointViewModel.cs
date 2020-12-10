using Dangl;
using iabi.BCF.APIObjects.V21;

namespace OpenProject.Shared.ViewModels.Bcf
{
  public class BcfViewpointViewModel : BindableBase
  {
    public Viewpoint_GET Viewpoint { get; set; }

    public string SnapshotData { get; set; }

    public Components Components { get; set; }
  }
}
