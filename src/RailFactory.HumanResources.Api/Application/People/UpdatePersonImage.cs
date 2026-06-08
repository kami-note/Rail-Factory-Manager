using RailFactory.HumanResources.Api.Application.Ports;

namespace RailFactory.HumanResources.Api.Application.People;

/// <summary>
/// Updates the profile image URL for a person within the human resources boundary.
/// </summary>
/// <remarks>
/// This implementation follows the Clean Architecture Use Case pattern to modify the Person aggregate.
/// </remarks>
public sealed class UpdatePersonImage(IPersonRepository repository)
{
    /// <summary>
    /// Executes the update profile image Use Case.
    /// </summary>
    /// <param name="id">The unique identifier of the person to update.</param>
    /// <param name="imageUrl">The new image URL generated after storage persistence.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="KeyNotFoundException">Thrown when the person is not found.</exception>
    public async Task ExecuteAsync(Guid id, string? imageUrl, CancellationToken cancellationToken)
    {
        var person = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Person {id} not found.");

        person.UpdateImageUrl(imageUrl);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
