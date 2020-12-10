using OpenProject.Shared.ViewModels.Bcf;

namespace OpenProject.Shared
{
  public static class MessageSerializer
  {
    public static ViewpointApiMessage SerializeBcfViewpoint(BcfViewpointViewModel bcfViewpointViewModel)
    {
      var apiViewpoint = new ViewpointApiMessage
      {
        Viewpoint = bcfViewpointViewModel.Viewpoint,
        Components = bcfViewpointViewModel.Components
      };
      return apiViewpoint;
    }
  }
}
