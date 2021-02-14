using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Nuke.Common.IO;
using PuppeteerSharp;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

public class PdfDocsGenerator
{
  private readonly AbsolutePath _outputDirectory;
  private IHost _webHost;

  public PdfDocsGenerator(AbsolutePath outputDirectory)
  {
    _outputDirectory = outputDirectory;
  }

  public async Task BuildPdfDocsAsync()
  {
    // We're launching a local ASP.NET Core server to serve the documentation files,
    // which we then use to create PDF files via PuppeteerSharp

    var webserverPort = GetFreePort();
    try
    {
      await EnsureLocalBrowserForDocsIsUpAndRunning(webserverPort);

      // This call ensures Chromium is downloaded. In local builds, it will reuse Chromium
      // if it's already present.
      await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
      var browser = await Puppeteer.LaunchAsync(new LaunchOptions
      {
        Headless = true
      });
      var page = await browser.NewPageAsync();
      await page.GoToAsync($"http://localhost:{webserverPort}/installation-instructions.html");
      await page.PdfAsync(_outputDirectory / "InstallationInstructions.pdf", new PdfOptions
      {
        DisplayHeaderFooter = true,
        PrintBackground = true,
        Format = PuppeteerSharp.Media.PaperFormat.A4
      });
    }
    finally
    {
      await EnsureLocalBrowserForDocsIsStopped();
    }
  }

  private async Task EnsureLocalBrowserForDocsIsUpAndRunning(int webserverPort)
  {
    Console.WriteLine(_outputDirectory);
    _webHost = Host.CreateDefaultBuilder()
      .ConfigureWebHostDefaults(webBuilder => webBuilder
          .UseUrls($"http://*:{webserverPort}")
          .UseWebRoot(_outputDirectory / "docs")
          .Configure((app) => app.UseStaticFiles()))
      .UseContentRoot(_outputDirectory / "docs")
      .Build();

    await _webHost.StartAsync();
    await WaitUntilServerReadyAsync(webserverPort);
  }

  private async Task WaitUntilServerReadyAsync(int webserverPort)
  {
    using var httpClient = new HttpClient();
    var isReady = false;
    var start = DateTime.UtcNow;
    while (!isReady && (DateTime.UtcNow - start).TotalSeconds < 5)
    {
      isReady = (await httpClient.GetAsync($"http://localhost:{webserverPort}/index.html")).StatusCode == HttpStatusCode.OK;
      if (!isReady)
      {
        await Task.Delay(500);
      }
    }
  }

  private async Task EnsureLocalBrowserForDocsIsStopped()
  {
    await _webHost.StopAsync();
    _webHost.Dispose();
  }

  private int GetFreePort()
  {
    // Taken from https://stackoverflow.com/a/150974/4190785
    var tcpListener = new TcpListener(IPAddress.Loopback, 0);
    tcpListener.Start();
    var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
    tcpListener.Stop();
    return port;
  }
}
