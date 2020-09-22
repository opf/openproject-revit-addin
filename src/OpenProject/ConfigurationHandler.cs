using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Config.Net;

namespace OpenProject
{
  public static class ConfigurationHandler
  {
    static ConfigurationHandler()
    {
      var configurationFilePath = GetConfigurationFilePath();
      Settings = new ConfigurationBuilder<IOpenProjectSettings>()
        .UseJsonFile(configurationFilePath)
        .Build();
    }

    public static IOpenProjectSettings Settings { get; }

    public static bool ShouldEnableDevelopmentTools()
    {
      return Settings.EnableDevelopmentTools;
    }

    public static void RemoveSavedInstance(string instanceUrl)
    {
      if (Settings.OpenProjectInstances?.Any() ?? false)
      {
        var existingValues = Settings.GetOpenProjectInstances();
        var existing = existingValues.FirstOrDefault(e => e.ToString() == instanceUrl);
        if (existing != null)
        {
          existingValues.Remove(existing);
          Settings.SetOpenProjectInstances(existingValues);
        }
      }
    }

    public static List<string> LoadAllInstances()
    {
      // Calling ToList() here to ensure the caller doesn't get the
      // locally cached list, thus preventing accidental modifications
      return Settings.GetOpenProjectInstances().ToList();
    }

    public static void SaveLastVisitedPage(string url)
    {
      Settings.LastVisitedPage = url;
    }

    public static string LastVisitedPage()
    {
      return Settings.LastVisitedPage ?? string.Empty;
    }

    public static void SaveSelectedInstance(string instanceUrl)
    {
      var existingValues = Settings.GetOpenProjectInstances();
      if (existingValues == null)
      {
        Settings.SetOpenProjectInstances(new List<string>
        {
          instanceUrl
        });
      }
      else
      {
        var existing = existingValues.FirstOrDefault(e => e.ToString() == instanceUrl);
        if (existing != null)
        {
          existingValues.Remove(existing);
        }
        existingValues.Insert(0, instanceUrl);
        Settings.SetOpenProjectInstances(existingValues);
      }
    }

    private static string GetConfigurationFilePath()
    {
      var configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenProject.Revit",
        "OpenProject.Configuration.json");

      if (!File.Exists(configPath))
      {
        // If the file doesn't yet exist, the default one is created
        using (var configStream = typeof(ConfigurationHandler).Assembly.GetManifestResourceStream("OpenProject.OpenProject.Configuration.json"))
        {
          var configDirName = Path.GetDirectoryName(configPath);
          if (!Directory.Exists(configDirName))
          {
            Directory.CreateDirectory(configDirName);
          }

          using (var fs = File.Create(configPath))
          {
            configStream.CopyTo(fs);
          }
        }
      }

      return configPath;
    }
  }
}
