using NotesClient.Services;
using System.Windows;

namespace NotesClient;

public partial class App : Application
{
    // Starts the application with the login window.
    protected override void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create the shared API service.
        var apiService = new NotesApiService();

        // Open the login window.
        var loginWindow =
            new LoginWindow(apiService);

        loginWindow.Show();
    }
}