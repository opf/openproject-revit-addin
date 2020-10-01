using Autodesk.Revit.UI;
using System;
using System.Diagnostics;
using System.IO;
using ZetaIpc.Runtime.Helper;

namespace OpenProject.Revit.Entry
{
  public static class RibbonButtonClickHandler
  {

#if Version2021

    public const string RevitVersion = "2021";

#elif Version2020

    public const string RevitVersion = "2020";

#elif Version2019

    public const string RevitVersion = "2019";

#endif

    private static Process _bcfierWinProcess;
    public static BcfierIpcHandler IpcHandler { get; private set; }

    public static Result OpenMainPluginWindow(ExternalCommandData commandData, ref string message)
    {
      try
      {
        EnsureExternalOpenProjectAppIsRunning(commandData);
        IpcHandler.SendBringBrowserToForegroundRequestToDesktopApp();

        return Result.Succeeded;
      }
      catch (Exception e)
      {
        message = e.Message;
        return Result.Failed;
      }
    }

    public static Result OpenSettingsPluginWindow(ExternalCommandData commandData, ref string message)
    {
      try
      {
        EnsureExternalOpenProjectAppIsRunning(commandData);
        IpcHandler.SendOpenSettingsRequestToDesktopApp();
        IpcHandler.SendBringBrowserToForegroundRequestToDesktopApp();
        return Result.Succeeded;
      }
      catch (Exception e)
      {
        message = e.Message;
        return Result.Failed;
      }
    }

    private static void EnsureExternalOpenProjectAppIsRunning(ExternalCommandData commandData)
    {
      //Version check
      if (!commandData.Application.Application.VersionName.Contains(RevitVersion))
      {
        using (var td = new TaskDialog("Untested version"))
        {
          td.TitleAutoPrefix = false;
          td.MainInstruction = "Untested Revit Version";
          td.MainContent = "This Add-In was built and tested only for Revit " + RevitVersion + ", proceed at your own risk";
          td.Show();
        }
      }

      // Form Running?
      if (_bcfierWinProcess != null && !_bcfierWinProcess.HasExited)
      {
        return;
      }

      IpcHandler = new BcfierIpcHandler(commandData.Application);
      var revitServerPort = IpcHandler.StartLocalServerAndReturnPort();
      var bcfierWinProcessPath = ConfigurationLoader.GetBcfierWinExecutablePath();
      if (!File.Exists(bcfierWinProcessPath))
      {
        // The configuration can be used to override the path, if there's no valid file given then
        // the default installation location is used
        var defaultInstallationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "OpenProject Revit AddIn", "OpenProject.Windows.exe");
        bcfierWinProcessPath = defaultInstallationPath;
      }

      var bcfierWinServerPort = FreePortHelper.GetFreePort();
      var bcfWinProcessArguments = $"ipc {bcfierWinServerPort} {revitServerPort}";
      _bcfierWinProcess = Process.Start(bcfierWinProcessPath, bcfWinProcessArguments);
      IpcHandler.StartLocalClient(bcfierWinServerPort);
    }
  }
}
