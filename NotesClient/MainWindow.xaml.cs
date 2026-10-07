using NotesClient.Services;
using NotesClient.Views;
using System.Windows;

namespace NotesClient;

public partial class MainWindow : Window
{
    // Shared API service for the current session.
    private readonly NotesApiService _apiService;

    public MainWindow(NotesApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;

        // Load the main application views.
        NotesContentControl.Content =
            new NotesView(_apiService);

        CategoriesContentControl.Content =
            new CategoriesView(_apiService);

        AdminContentControl.Content =
            new AdminView(_apiService);

        // Handle expired authentication sessions.
        _apiService.SessionExpired += OnSessionExpired;

        // Show the Admin tab only for authorized roles.
        AdminTab.Visibility =
            _apiService.IsAdmin
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    // Logs the current user out.
    private void LogoutButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _apiService.Logout();

        // Return to the login window.
        var loginWindow =
            new LoginWindow(_apiService);

        loginWindow.Show();

        Close();
    }

    // Handles an expired authentication session.
    private void OnSessionExpired()
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                "Your session has expired. Please log in again.",
                "Session expired",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Return to the login window.
            var loginWindow =
                new LoginWindow(_apiService);

            loginWindow.Show();

            Close();
        });
    }
}