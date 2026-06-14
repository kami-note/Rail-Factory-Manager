using FluentAssertions;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="Person"/> aggregate root.
/// </summary>
public class PersonTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var name = "Carlos Silva";
        var doc = "123.456.789-00";
        var type = PersonType.Employee;
        var email = "CARLOS@exemplo.com";
        var imageUrl = "http://minio/images/carlos.jpg";
        var id = Guid.NewGuid();

        // Act
        var person = Person.Create(name, doc, type, email, imageUrl, id);

        // Assert
        person.Id.Should().Be(id);
        person.Name.Should().Be("Carlos Silva");
        person.DocumentNumber.Should().Be("123.456.789-00");
        person.Type.Should().Be(type);
        person.Email.Should().Be("carlos@exemplo.com"); // Should trim and lowercase
        person.ImageUrl.Should().Be(imageUrl);
        person.Status.Should().Be(PersonStatus.Active);
    }

    [Theory]
    [InlineData("", "12345678900")]
    [InlineData("Carlos", "")]
    [InlineData("   ", "12345678900")]
    [InlineData("Carlos", "   ")]
    public void Create_WithInvalidNameOrDoc_ShouldThrowArgumentException(string name, string doc)
    {
        // Act
        Action act = () => Person.Create(name, doc, PersonType.Employee);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateImageUrl_ShouldSetImageUrlAndTrim()
    {
        // Arrange
        var person = Person.Create("Carlos", "123", PersonType.Employee);

        // Act
        person.UpdateImageUrl("  http://new-url.jpg  ");

        // Assert
        person.ImageUrl.Should().Be("http://new-url.jpg");
    }

    [Fact]
    public void Deactivate_ShouldChangeStatusToInactive()
    {
        // Arrange
        var person = Person.Create("Carlos", "123", PersonType.Employee);

        // Act
        person.Deactivate();

        // Assert
        person.Status.Should().Be(PersonStatus.Inactive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var person = Person.Create("Carlos", "123", PersonType.Employee);
        person.Deactivate();

        // Act
        Action act = () => person.Deactivate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already inactive*");
    }

    [Fact]
    public void Activate_ShouldChangeStatusToActive_WhenInactive()
    {
        // Arrange
        var person = Person.Create("Carlos", "123", PersonType.Employee);
        person.Deactivate();

        // Act
        person.Activate();

        // Assert
        person.Status.Should().Be(PersonStatus.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var person = Person.Create("Carlos", "123", PersonType.Employee);

        // Act
        Action act = () => person.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already active*");
    }
}
