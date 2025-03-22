using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Autodesk.Revit.UI;
using System.Windows.Threading;

namespace Revit.Tests.NewUIThreadTest;
public class MainViewModel : INotifyPropertyChanged
{
    private UIApplication _uiApp;
    public Dispatcher MainWindowDispatcher { get; set; }
    private int _wallCount = 50;
    private bool _isCreating = false;
    private int _currentProgress = 0;
    private string _statusMessage = "Ready to create walls";
    private Visibility _progressVisibility = Visibility.Collapsed;

    public MainViewModel(UIApplication uiApp)
    {
        _uiApp = uiApp;
        CreateWallsCommand = new RelayCommand(CreateWalls, CanCreateWalls);
        CancelCommand = new RelayCommand(CancelOperation, CanCancelOperation);

    }

    public int WallCount
    {
        get => _wallCount;
        set
        {
            if (_wallCount != value)
            {
                _wallCount = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsCreating
    {
        get => _isCreating;
        set
        {
            if (_isCreating != value)
            {
                _isCreating = value;
                OnPropertyChanged();
                ProgressVisibility = value ? Visibility.Visible : Visibility.Collapsed;
                CreateWallsCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int CurrentProgress
    {
        get => _currentProgress;
        set
        {
            if (_currentProgress != value)
            {
                _currentProgress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }
    }

    public int ProgressPercentage => WallCount > 0 ? (int)((double)CurrentProgress / WallCount * 100) : 0;

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public Visibility ProgressVisibility
    {
        get => _progressVisibility;
        set
        {
            if (_progressVisibility != value)
            {
                _progressVisibility = value;
                OnPropertyChanged();
            }
        }
    }

    public RelayCommand CreateWallsCommand { get; }
    public RelayCommand CancelCommand { get; }

    private bool CanCreateWalls(object parameter)
    {
        return !IsCreating && WallCount > 0;
    }

    private bool CanCancelOperation(object parameter)
    {
        return IsCreating;
    }

    private void CancelOperation(object parameter)
    {
        RevitCommand.EventHandler.IsCancelled = true;
        StatusMessage = "Cancelling...";
    }

    private void CreateWalls(object parameter)
    {
        try
        {
            CurrentProgress = 0;
            StatusMessage = "Starting wall creation...";
            IsCreating = true;

            RevitCommand.EventHandler.TotalWalls = WallCount;
            RevitCommand.EventHandler.IsCancelled = false;

            RevitCommand.EventHandler.ProgressCallback = (current, total) =>
            {
                MainWindowDispatcher.Invoke(() =>
                {
                    CurrentProgress = current;
                    StatusMessage = $"Creating wall {current} of {total}";

                    if (current >= total)
                    {
                        StatusMessage = $"Completed creating {total} walls";
                        IsCreating = false;
                    }
                });
            };

            RevitCommand.RevitEvent.Raise();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating walls: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error occurred";
            IsCreating = false;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


