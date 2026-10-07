using System.ComponentModel.DataAnnotations;

namespace NotesAPI.Validation;

public static class ValidationHelper
{
    public static bool IsValid(
        object model,
        out List<string> errors)
    {
        var context = new ValidationContext(model);

        var validationResults =
            new List<ValidationResult>();

        // Validate all properties and collect every validation error.
        var isValid = Validator.TryValidateObject(
            model,
            context,
            validationResults,
            validateAllProperties: true);

        // Convert validation results into simple error messages for the API response.
        errors = validationResults
            .Select(error =>
                error.ErrorMessage ?? "Invalid value.")
            .ToList();

        return isValid;
    }
}