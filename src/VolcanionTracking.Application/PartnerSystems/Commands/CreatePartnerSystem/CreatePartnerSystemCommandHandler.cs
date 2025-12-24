using MediatR;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;

namespace VolcanionTracking.Application.PartnerSystems.Commands.CreatePartnerSystem;

/// <summary>
/// Handles commands to create a new partner system for a specified partner.
/// </summary>
/// <remarks>This handler coordinates the creation of a partner system, including validation, persistence, and
/// cache invalidation. It should be registered for use with the application's MediatR pipeline.</remarks>
/// <param name="partnerRepository">The repository used to access and update partner aggregates.</param>
/// <param name="unitOfWork">The unit of work used to persist changes to the data store.</param>
/// <param name="cacheService">The cache service used to invalidate partner system cache entries after creation.</param>
/// <param name="logger">The logger used to record informational and error messages during command handling.</param>
public class CreatePartnerSystemCommandHandler(
    IPartnerRepository partnerRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService,
    ILogger<CreatePartnerSystemCommandHandler> logger) : IRequestHandler<CreatePartnerSystemCommand, CreatePartnerSystemResult>
{
    /// <summary>
    /// Handles the creation of a new partner system for the specified partner.
    /// </summary>
    /// <param name="request">The command containing the details of the partner system to create, including the partner identifier, system
    /// name, type, and description.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a CreatePartnerSystemResult with
    /// details of the newly created partner system.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified partner does not exist or if the system cannot be added due to a business rule
    /// violation.</exception>
    public async Task<CreatePartnerSystemResult> Handle(
        CreatePartnerSystemCommand request,
        CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Creating partner system: {SystemName} for partner: {PartnerId}", request.Name, request.PartnerId);
        }

        // Get partner aggregate
        var partner = await partnerRepository.GetByIdWithSystemsAsync(request.PartnerId, cancellationToken) ?? throw new InvalidOperationException($"Partner {request.PartnerId} not found");

        // Use aggregate method to add system
        var systemResult = partner.AddSystem(request.Name, request.Type, request.Description);
        if (!systemResult.IsSuccess)
        {
            throw new InvalidOperationException(systemResult.Error);
        }

        var system = systemResult.Value!;

        // Persist changes
        partnerRepository.Update(partner);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Partner system created: {SystemId}", system.Id);
        }

        // Invalidate cache
        await cacheService.RemoveAsync($"partner_system:{system.Id}", cancellationToken);
        await cacheService.RemoveByPrefixAsync($"partner_systems:{request.PartnerId}", cancellationToken);

        return new CreatePartnerSystemResult(
            system.Id,
            system.PartnerId,
            system.Name,
            system.Type,
            system.Description,
            system.ApiKey.Value,
            system.IsActive);
    }
}
