using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Reflection;

namespace OpenProject.Revit.Entry
{
  /// <summary>
  /// Obfuscation Ignore for External Interface
  /// </summary>
  [Obfuscation(Exclude = true, ApplyToMembers = false)]
  [Transaction(TransactionMode.Manual)]
  [Regeneration(RegenerationOption.Manual)]
  public class CmdMainSettings : IExternalCommand
  {
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet _)
    {
      return RibbonButtonClickHandler.OpenSettingsPluginWindow(commandData, ref message);
    }
  }
}
