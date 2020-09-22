using Newtonsoft.Json;
using OpenProject.Api;
using OpenProject.Data.Utils;
using OpenProject.Shared;
using OpenProject.WebViewIntegration;
using OpenProject.Windows;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OpenProject.UserControls
{
  /// <summary>
  /// Main panel UI and logic that need to be used by all modules
  /// </summary>
  public partial class BcfierPanel : UserControl
  {
    private readonly BrowserManager _browserManager;

    public BcfierPanel()
    {
      // This call sets up the global Chromium settings, e.g. the User Agent
      CefBrowserInitializer.InitializeCefBrowser();

      InitializeComponent();

      _browserManager = new BrowserManager(Browser);

      // TODO this is for quick testing to ensure a 'loaded' event is sent to OpenProject at the start
      // var hassenttestmessage = false;
      // browser.loadingstatechanged += (s, e) =>
      // {
      //   if (!e.isloading)
      //   {
      //     if (hassenttestmessage)
      //     {
      //       return;
      //     }
      // 
      //     hassenttestmessage = true;
      //     var mytimer = new system.timers.timer(1000);
      //     mytimer.elapsed += (s2, e2) =>
      //     {
      //       javascriptbridge.instance
      //       .sendmessagetoopenproject(messagetypes.revit_loaded, string.empty, jsonconvert.serializeobject(new
      //       {
      //         title = "demo.ifc"
      //       }));
      //       mytimer.stop();
      //     };
      //     mytimer.start();
      //   }
      // };

      var commandLineArgs = Environment.GetCommandLineArgs();
      if (commandLineArgs?.Any(arg => arg == "ipc") ?? false)
      {
        var args = commandLineArgs.SkipWhile(arg => arg != "ipc")
          .Skip(1)
          .ToList();
        if (args.Count == 2)
        {
          var serverPort = int.Parse(args[0]);
          var clientPort = int.Parse(args[1]);
          IpcManager.StartIpcCommunication(serverPort, clientPort);
        }
      }

      var knownGoodUrls = ConfigurationHandler.LoadAllInstances();
      var lastVisitedPage = ConfigurationHandler.LastVisitedPage();
      var isWhiteListedUrl = knownGoodUrls.Any(goodUrl => lastVisitedPage.StartsWith(goodUrl, System.StringComparison.InvariantCultureIgnoreCase));

      if (isWhiteListedUrl)
      {
        Browser.Address = lastVisitedPage;
      }
      else
      {
        Browser.Address = EmbeddedLandingPageHandler.GetEmbeddedLandingPageIndexUrl();
      }

      if (UserSettings.GetBool("checkupdates"))
        CheckUpdates();
    }

    //check github API for new release
    private void CheckUpdates()
    {
      Task.Run(() =>
      {
        try
        {
          var release = GitHubRest.GetLatestRelease();
          if (release == null)
            return;

          var onlineIsNewer = new Api.Version(release.tag_name).CompareTo(new Api.Version(VersionsService.Version)) > 0;

          if (onlineIsNewer)
          {
            Application.Current.Dispatcher.Invoke(() =>
            {
              var dialog = new NewVersion();
              dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
              dialog.Description.Text = release.name + " has been released on " + release.published_at.ToLongDateString() + "\ndo you want to check it out now?";
              dialog.ShowDialog();
              if (dialog.DialogResult.HasValue && dialog.DialogResult.Value)
              {
                var downloadUrl = release.html_url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {downloadUrl}") { CreateNoWindow = true });
              }
            });
          }
        }
        catch (System.Exception ex1)
        {
          //warning suppressed
          Console.WriteLine("exception: " + ex1);
        }
      });
    }
  }
}
