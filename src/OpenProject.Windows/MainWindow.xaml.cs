using OpenProject.WebViewIntegration;
using System;
using System.ComponentModel;
using System.Windows;

namespace OpenProject.Windows
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private const double WindowMinWidth = 730.00; // This width allows to IFC issue cards even when the OP menu on the left is open.
    private const double TaskBarHeight = 50; // Height of Windows Task bar.
    public MainWindow()
    {
      this.Width = MainWindowInitialWidth();
      this.Height = System.Windows.SystemParameters.PrimaryScreenHeight - TaskBarHeight; // Full visible window height.
      this.Top = 0;
      this.Left = SystemParameters.PrimaryScreenWidth - this.Width;
      InitializeComponent();
      JavaScriptBridge.Instance
        .OnAppForegroundRequestReceived += (s) => {
          Application.Current.Dispatcher.Invoke(() => Activate());
        };
    }

    /// <summary>
    /// passing event to the user control
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Closing(object sender, CancelEventArgs e)
    {
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
    }

    private double MainWindowInitialWidth()
    {
      if (SystemParameters.PrimaryScreenHeight < WindowMinWidth)
      {
        return SystemParameters.PrimaryScreenHeight;
      }
      else
      {
        return Math.Max(WindowMinWidth, System.Windows.SystemParameters.PrimaryScreenHeight * 0.25);
      }

    }
  }
}
