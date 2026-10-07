using NotesClient.Models;
using NotesClient.Services;
using System.Windows;
using System.Windows.Controls;

namespace NotesClient.Views;

public partial class AdminView : UserControl
{
    // Shared API service for administration requests.
    private readonly NotesApiService _apiService;

    private List<AdminUser> _users = new();

    public AdminView(NotesApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;

        // Load users when the view is displayed.
        Loaded += async (_, _) =>
        {
            await RefreshUsersAsync();
        };
    }

    // Loads the users from the API.
    private async Task RefreshUsersAsync()
    {
        try
        {
            _users =
                await _apiService.GetAdminUsersAsync();

            UsersList.ItemsSource = _users;
        }
        catch
        {
            // Keep the list empty if loading fails.
            _users = new List<AdminUser>();
            UsersList.ItemsSource = _users;
        }
    }

    // Displays information for the selected user.
    private void UsersList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (UsersList.SelectedItem
            is not AdminUser user)
        {
            return;
        }

        SelectedUsernameTextBlock.Text =
            user.Username;

        foreach (ComboBoxItem item
                 in RoleComboBox.Items)
        {
            if (item.Tag?.ToString() == user.Role)
            {
                RoleComboBox.SelectedItem = item;
                break;
            }
        }
    }

    // Requests a role change from the API.
    private async void ChangeRoleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Require a selected user.
        if (UsersList.SelectedItem
            is not AdminUser user)
        {
            MessageBox.Show(
                "Please select a user first.",
                "Change role",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        // Require a selected role.
        if (RoleComboBox.SelectedItem
            is not ComboBoxItem selectedRoleItem)
        {
            MessageBox.Show(
                "Please select a role.",
                "Change role",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var newRole =
            selectedRoleItem.Tag?.ToString();

        if (string.IsNullOrWhiteSpace(newRole))
            return;

        try
        {
            // The API validates whether the role change is allowed.
            await _apiService.ChangeUserRoleAsync(
                user.Id,
                newRole);

            await RefreshUsersAsync();

            ClearSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not change user role.\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // Requests user deletion from the API.
    private async void DeleteUserButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Require a selected user.
        if (UsersList.SelectedItem
            is not AdminUser user)
        {
            MessageBox.Show(
                "Please select a user first.",
                "Delete user",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        // Confirm the deletion locally.
        var result = MessageBox.Show(
            $"Are you sure you want to delete the user \"{user.Username}\"?",
            "Delete user",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            // The API validates whether the deletion is allowed.
            await _apiService.DeleteUserAsync(
                user.Id);

            await RefreshUsersAsync();

            ClearSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not delete user.\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // Refreshes the user list.
    private async void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await RefreshUsersAsync();
    }

    // Clears the current selection.
    private void ClearSelection()
    {
        UsersList.SelectedItem = null;
        SelectedUsernameTextBlock.Text = "";
        RoleComboBox.SelectedIndex = -1;
    }
}