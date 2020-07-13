using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dangl;

namespace OpenProject.WebViewIntegration
{
  public class EmbeddedLandingPageHandler
  {
    public static string GetEmbeddedLandingPageIndexUrl()
    {
      var indexFilePath = GetIndexFilePath();

      return $"file:///{indexFilePath}";
    }

    private static string _landingPageZipHash;

    private static string GetLandingPageZipHash()
    {
      if (!string.IsNullOrWhiteSpace(_landingPageZipHash))
      {
        return _landingPageZipHash;
      }

      using (var zipStream = GetEmbeddedResourceZipStream())
      {
        using (var md5 = MD5.Create())
        {
          _landingPageZipHash = md5.ComputeHash(zipStream)
                    .Select(b => $"{b:x2}")
                    .Aggregate((c, n) => c + n);
          return _landingPageZipHash;
        }
      }
    }

    private static string GetIndexFilePath()
    {
      // The unpacked Html should be adjacent to this Dll
      var currentAssemblyPathUri = Assembly.GetExecutingAssembly().CodeBase;
      var currentAssemblyPath = Uri.UnescapeDataString(new Uri(currentAssemblyPathUri).AbsolutePath)
        // '/' comes from the uri, we need it to be '\' for the path
        .Replace("/", "\\");
      var currentFolder = Path.GetDirectoryName(currentAssemblyPath);
      // We're versioning the folder so as to not have to do a direct file comparison of the contents
      // in case an earlier version was aready present

      // The hash is used to ensure that when working locally in a debugging environment, any changes to
      // the zip files content ensure that the current version of the landing page is extracted.
      var landingPageZipHash = GetLandingPageZipHash();
      var landingPageFolder = Path.Combine(currentFolder, "LandingPage", VersionsService.Version, landingPageZipHash);
      if (!Directory.Exists(landingPageFolder))
      {
        ExtractEmbeddedLandingPageToFolder(landingPageFolder);
      }

      return Path.Combine(landingPageFolder, "index.html");
    }

    private static void ExtractEmbeddedLandingPageToFolder(string landingPageFolder)
    {
      var tempPath = Path.GetTempFileName();
      using (var resourceStream = GetEmbeddedResourceZipStream())
      {
        using (var fs = System.IO.File.Create(tempPath))
        {
          resourceStream.CopyTo(fs);
        }
      }

      ZipFile.ExtractToDirectory(tempPath, landingPageFolder);
    }

    private static Stream GetEmbeddedResourceZipStream()
    {
      var assembly = Assembly.GetExecutingAssembly();
      var resourceName = "OpenProject.WebViewIntegration.LandingPage.LandingPage.zip";
      return assembly.GetManifestResourceStream(resourceName);
    }
  }
}
