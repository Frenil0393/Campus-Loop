using System.ComponentModel.DataAnnotations;
using CampusLoop.ViewModels;
using Xunit;

namespace CampusLoop.Tests.Controllers;

public class AccountValidationTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData("student@gmail.com", false)]
    [InlineData("test@yahoo.com", false)]
    [InlineData("user@ddu.edu", false)]
    [InlineData("student@outlook.com", false)]
    [InlineData("student@ddu.ac.in", true)]
    [InlineData("RAHUL.CE162@DDU.AC.IN", true)]
    [InlineData("faculty@it.ddu.ac.in", false)]
    [InlineData("faculty@ddu.ac.in", true)]
    public void CollegeEmailDomainRule_ValidatesCorrectly(string email, bool shouldBeValid)
    {
        // Act
        var isValidDduEmail = email.Trim().EndsWith("@ddu.ac.in", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.Equal(shouldBeValid, isValidDduEmail);
    }

    [Fact]
    public void RegisterViewModel_FailsWhenRequiredFieldsAreEmpty()
    {
        // Arrange
        var model = new RegisterViewModel();

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RegisterViewModel.FullName)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RegisterViewModel.Email)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RegisterViewModel.Password)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RegisterViewModel.PhoneNumber)));
    }

    [Fact]
    public void RegisterViewModel_FailsWhenSemesterIsOutOfRange()
    {
        // Arrange - Semester must be between 1 and 8
        var model = new RegisterViewModel
        {
            FullName = "Test Student",
            Email = "test@ddu.ac.in",
            Password = "Password@123",
            ConfirmPassword = "Password@123",
            PhoneNumber = "9876543210",
            Branch = "Computer Engineering",
            Semester = 10 // Invalid semester
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RegisterViewModel.Semester)));
    }

    [Fact]
    public void RegisterViewModel_FailsWhenPasswordsDoNotMatch()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            FullName = "Test Student",
            Email = "test@ddu.ac.in",
            Password = "Password@123",
            ConfirmPassword = "DifferentPassword@123",
            PhoneNumber = "9876543210",
            Branch = "Computer Engineering",
            Semester = 6
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
    }

    [Fact]
    public void RegisterViewModel_PassesWhenValid()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            FullName = "Deep Thesiya",
            Email = "deep@ddu.ac.in",
            Password = "Password@123",
            ConfirmPassword = "Password@123",
            PhoneNumber = "9876543210",
            Branch = "Computer Engineering",
            Semester = 6
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void LoginViewModel_RequiresEmailAndPassword()
    {
        // Arrange
        var model = new LoginViewModel();

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(LoginViewModel.Email)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(LoginViewModel.Password)));
    }
}
