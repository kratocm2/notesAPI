using NotesClient.Models;
using NotesClient.Services;
using System.Windows;
using System.Windows.Controls;

namespace NotesClient.Views;

public partial class CategoriesView : UserControl
{
    // Shared API service for category requests.
    private readonly NotesApiService _apiService;

    private List<Category> _categories = new();

    public CategoriesView(NotesApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;

        // Load categories when the view is displayed.
        Loaded += async (_, _) =>
        {
            await RefreshCategoriesAsync();
        };
    }

    // Loads the user's categories from the API.
    private async Task RefreshCategoriesAsync()
    {
        try
        {
            _categories =
                await _apiService.GetCategoriesAsync();

            CategoriesList.ItemsSource =
                _categories;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not load categories.\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // Creates a new category.
    private async void AddCategoryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var categoryName =
            CategoryNameTextBox.Text.Trim();

        // Check the input before sending the request.
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            MessageBox.Show(
                "Category name cannot be empty.",
                "Invalid category",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            // The API validates and creates the category.
            var category =
                await _apiService.CreateCategoryAsync(
                    categoryName);

            if (category == null)
            {
                MessageBox.Show(
                    "The category could not be created.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            CategoryNameTextBox.Clear();

            await RefreshCategoriesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not create category.\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // Displays the selected category name.
    private void CategoriesList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (CategoriesList.SelectedItem
            is not Category category)
        {
            return;
        }

        CategoryNameTextBox.Text =
            category.Name;
    }

    // Renames the selected category.
    private async void RenameCategoryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Require a selected category.
        if (CategoriesList.SelectedItem
            is not Category category)
        {
            MessageBox.Show(
                "Please select a category first.",
                "Rename category",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var newName =
            CategoryNameTextBox.Text.Trim();

        // Check the input before sending the request.
        if (string.IsNullOrWhiteSpace(newName))
        {
            MessageBox.Show(
                "Category name cannot be empty.",
                "Invalid category",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            // The API validates and updates the category.
            var updatedCategory =
                await _apiService.UpdateCategoryAsync(
                    category.Id,
                    newName);

            if (updatedCategory == null)
            {
                MessageBox.Show(
                    "The category could not be renamed.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            CategoryNameTextBox.Clear();

            await RefreshCategoriesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not rename category.\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // Deletes the selected category.
    private async void DeleteCategoryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Require a selected category.
        if (CategoriesList.SelectedItem
            is not Category category)
        {
            MessageBox.Show(
                "Please select a category first.",
                "Delete category",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        // Confirm the deletion locally.
        var result = MessageBox.Show(
            $"Are you sure you want to delete the category \"{category.Name}\"?",
            "Delete category",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            // The API performs the deletion.
            await _apiService.DeleteCategoryAsync(
                category.Id);

            CategoryNameTextBox.Clear();

            await RefreshCategoriesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to delete category.\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}