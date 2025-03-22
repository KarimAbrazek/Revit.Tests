using System.Windows.Interop;
using System.Windows;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;


namespace Revit.Tests.NewUIThreadTest;

[Transaction(TransactionMode.Manual)]
public class RevitCommand : IExternalCommand
{

    private static MainWindow _mainWindow;
    public static Thread _mainWindowThread;
    public static ExternalEvent RevitEvent;
    public static WallCreationHandler EventHandler;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            UIApplication uiApp = commandData.Application;
            EventHandler = new WallCreationHandler();
            RevitEvent = ExternalEvent.Create(EventHandler);


            // Check if window already exists
            if (_mainWindowThread != null && _mainWindowThread.IsAlive && _mainWindow != null)
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.Activate();
                    _mainWindow.WindowState = WindowState.Normal;
                });
                return Result.Succeeded;
            }

            // Create and start a thread for the main window
            _mainWindowThread = new Thread(() =>
            {
                try
                {
                    MainViewModel viewModel = new MainViewModel(uiApp);
                    _mainWindow = new MainWindow(viewModel);
                    ComponentDispatcher.ThreadFilterMessage += (ref MSG msg, ref bool handled) => { };
                    _mainWindow.Closed += (s, e) =>
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                        _mainWindow = null;
                    };
                    _mainWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error in window thread: {ex.Message}\n{ex.StackTrace}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            // Configure and start thread
            _mainWindowThread.SetApartmentState(ApartmentState.STA);
            _mainWindowThread.IsBackground = true;
            _mainWindowThread.Start();

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
