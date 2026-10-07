using NotesClient.Services;
using System.Net.Http;
using System.Windows;

namespace NotesClient;

public partial class LoginWindow : Window
{
    // Shared API service for authentication.
    private readonly NotesApiService _apiService;

    public LoginWindow(NotesApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;
    }

    // Attempts to log the user in.
    private async void LoginButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Prevent multiple login requests.
        LoginButton.IsEnabled = false;

        // Check that both fields contain values.
        if (string.IsNullOrWhiteSpace(UsernameTextBox.Text) ||
            string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            MessageBox.Show(
                "Please enter your username and password.",
                "Login",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            LoginButton.IsEnabled = true;

            return;
        }

        try
        {
            // Send the login request to the API.
            await _apiService.LoginAsync(
                UsernameTextBox.Text,
                PasswordBox.Password);

            // Open the main application window.
            var mainWindow =
                new MainWindow(_apiService);

            mainWindow.Show();

            // Close the login window.
            Close();
        }
        catch (HttpRequestException)
        {
            // Show an error when authentication fails.
            MessageBox.Show(
                "Invalid username or password.",
                "Login failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            // Re-enable the button after the request.
            LoginButton.IsEnabled = true;
        }
    }

    // Opens the registration window.
    private void RegisterButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var registerWindow =
            new RegisterWindow(_apiService);

        registerWindow.Show();

        // Close the login window.
        Close();
    }
}