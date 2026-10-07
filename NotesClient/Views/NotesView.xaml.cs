using NotesClient.DTOs;
using NotesClient.Models;
using NotesClient.Services;
using System.Windows;
using System.Windows.Controls;

namespace NotesClient.Views;

public partial class NotesView : UserControl
{
    private readonly NotesApiService _apiService;

    private int? _selectedNoteId;
    private int _currentPage = 1;
    private const int PageSize = 10;
    private int _totalItems;

    private string _currentSearch = "";
    private string _currentSort = "last_changed";
    private List<Category> _categories = new();
    private int? _currentCategoryId;
    private List<CategoryFilterItem> _categoryFilterItems = new();

    public NotesView(NotesApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;

        // Load categories and notes when the view is displayed.
        Loaded += async (_, _) =>
        {
            await LoadCategoriesAsync();
            await RefreshNotesAsync();
        };
    }

    private async Task RefreshNotesAsync()
    {
        try
        {
            // Request notes using the current filters, page and sorting.
            var response = await _apiService.GetNotesAsync(
                _currentSearch,
                _currentCategoryId,
                _currentPage,
                PageSize,
                _currentSort);

            NotesList.Items.Clear();

            foreach (var note in response.Items)
            {
                NotesList.Items.Add(note);
            }

            _totalItems = response.TotalItems;

            UpdatePagination();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not load notes.\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            _categories =
                await _apiService.GetCategoriesAsync();

            CategoryComboBox.ItemsSource =
                _categories;

            // Create a separate list for the category filter.
            _categoryFilterItems =
                new List<CategoryFilterItem>
                {
                    new CategoryFilterItem
                    {
                        Id = null,
                        Name = "All categories"
                    }
                };

            // Add the user's categories to the filter list.
            _categoryFilterItems.AddRange(
                _categories.Select(
                    category =>
                        new CategoryFilterItem
                        {
                            Id = category.Id,
                            Name = category.Name
                        }));

            CategoryFilterComboBox.ItemsSource =
                _categoryFilterItems;

            CategoryFilterComboBox.SelectedIndex = 0;
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

    private async void SortComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        _currentSort = SortComboBox.SelectedIndex switch
        {
            1 => "newest",
            2 => "oldest",
            3 => "title_asc",
            4 => "title_desc",
            _ => "last_changed"
        };

        _currentPage = 1;

        await RefreshNotesAsync();
    }

    private void UpdatePagination()
    {
        // Calculate the number of pages from the total item count.
        int totalPages = Math.Max(
            1,
            (int)Math.Ceiling(
                (double)_totalItems / PageSize));

        PageInfoTextBlock.Text =
            $"Page {_currentPage} of {totalPages}";

        PreviousPageButton.IsEnabled =
            _currentPage > 1;

        NextPageButton.IsEnabled =
            _currentPage < totalPages;
    }

    private async void PreviousPageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_currentPage <= 1)
            return;

        _currentPage--;

        ClearEditor();

        await RefreshNotesAsync();
    }

    private async void NextPageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        int totalPages = Math.Max(
            1,
            (int)Math.Ceiling(
                (double)_totalItems / PageSize));

        if (_currentPage >= totalPages)
            return;

        _currentPage++;

        ClearEditor();

        await RefreshNotesAsync();
    }

    private async void SearchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _currentSearch =
            SearchTextBox.Text.Trim();

        _currentPage = 1;

        await RefreshNotesAsync();
    }

    private async void ClearSearchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SearchTextBox.Clear();

        _currentSearch = "";
        _currentCategoryId = null;

        _currentPage = 1;

        CategoryFilterComboBox.SelectedIndex = 0;

        await RefreshNotesAsync();
    }

    private async void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await RefreshNotesAsync();
    }

    private async void CategoryFilterComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        // Read the selected category filter.
        if (CategoryFilterComboBox.SelectedItem
            is CategoryFilterItem selectedCategory)
        {
            _currentCategoryId =
                selectedCategory.Id;
        }
        else
        {
            _currentCategoryId = null;
        }

        _currentPage = 1;

        await RefreshNotesAsync();
    }

    private void NotesList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (NotesList.SelectedItem is not Note note)
            return;

        _selectedNoteId = note.Id;

        TitleTextBox.Text = note.Title;
        ContentTextBox.Text = note.Content;

        CategoryComboBox.SelectedValue =
            note.CategoryId;
    }

    private async void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Check the input before sending the request.
        if (string.IsNullOrWhiteSpace(
                TitleTextBox.Text))
        {
            MessageBox.Show(
                "Title cannot be empty.",
                "Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            // Create a new note when no existing note is selected.
            if (_selectedNoteId == null)
            {
                var request = new CreateNoteRequest
                {
                    Title =
                        TitleTextBox.Text.Trim(),

                    Content =
                        ContentTextBox.Text,

                    CategoryId =
                        CategoryComboBox.SelectedValue
                        as int?
                };

                await _apiService.CreateNoteAsync(
                    request);
            }
            else
            {
                var request = new UpdateNoteRequest
                {
                    Title =
                        TitleTextBox.Text.Trim(),

                    Content =
                        ContentTextBox.Text,

                    CategoryId =
                        CategoryComboBox.SelectedValue
                        as int?
                };

                // Send the updated note to the API.
                await _apiService.UpdateNoteAsync(
                    _selectedNoteId.Value,
                    request);
            }

            ClearEditor();

            await RefreshNotesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not save note.\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void DeleteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedNoteId == null)
            return;

        var result = MessageBox.Show(
            "Are you sure you want to delete this note?",
            "Delete Note",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await _apiService.DeleteNoteAsync(
                _selectedNoteId.Value);

            ClearEditor();

            await RefreshNotesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not delete note.\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ClearEditor();
    }

    private void ClearEditor()
    {
        _selectedNoteId = null;

        NotesList.SelectedItem = null;

        TitleTextBox.Clear();
        ContentTextBox.Clear();

        CategoryComboBox.SelectedIndex = -1;
    }
}