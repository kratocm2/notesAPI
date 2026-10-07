using NotesClient.Services;
using System.Net.Http;
using System.Windows;

namespace NotesClient;

public partial class RegisterWindow : Window
{
    // Shared API service for registration.
    private readonly NotesApiService _apiService;

    public RegisterWindow(NotesApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;
    }

    // Attempts to register a new account.
    private async void RegisterButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Check that the username was entered.
        if (string.IsNullOrWhiteSpace(
                UsernameTextBox.Text))
        {
            MessageBox.Show(
                "Username is required.",
                "Registration failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        // Check that the password was entered.
        if (string.IsNullOrWhiteSpace(
                PasswordBox.Password))
        {
            MessageBox.Show(
                "Password is required.",
                "Registration failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        // Confirm that both passwords match.
        if (PasswordBox.Password !=
            ConfirmPasswordBox.Password)
        {
            MessageBox.Show(
                "Passwords do not match.",
                "Registration failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        // Prevent multiple registration requests.
        RegisterButton.IsEnabled = false;

        try
        {
            // Send the registration request to the API.
            await _apiService.RegisterAsync(
                UsernameTextBox.Text,
                PasswordBox.Password);

            MessageBox.Show(
                "Account created successfully.",
                "Registration successful",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Return to the login window.
            var loginWindow =
                new LoginWindow(_apiService);

            loginWindow.Show();

            Close();
        }
        catch (HttpRequestException ex)
        {
            // Display the error returned by the API.
            MessageBox.Show(
                ex.Message,
                "Registration failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            // Re-enable the button after the request.
            RegisterButton.IsEnabled = true;
        }
    }

    // Returns to the login window.
    private void BackButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var loginWindow =
            new LoginWindow(_apiService);

        loginWindow.Show();

        Close();
    }
}