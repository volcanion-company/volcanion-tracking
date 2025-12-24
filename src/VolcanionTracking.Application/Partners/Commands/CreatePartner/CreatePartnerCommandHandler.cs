using MediatR;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;
using VolcanionTracking.Domain.Aggregates.PartnerAggregate;

namespace VolcanionTracking.Application.Partners.Commands.CreatePartner;

/// <summary>
/// Handles commands to create new partners, coordinating validation, persistence, and cache invalidation as part of the
/// partner onboarding process.
/// </summary>
/// <remarks>This handler ensures that partner codes and emails are unique before creating a new partner. It also
/// invalidates relevant cache entries to ensure data consistency after a partner is added. This class is typically used
/// within a CQRS or MediatR pipeline to process partner creation requests.</remarks>
/// <param name="partnerRepository">The repository used to access and persist partner entities.</param>
/// <param name="unitOfWork">The unit of work that manages transactional operations for persisting changes.</param>
/// <param name="cacheService">The cache service used to invalidate partner-related cache entries after creation.</param>
/// <param name="logger">The logger used to record informational and error messages during partner creation.</param>
public class CreatePartnerCommandHandler(
    IPartnerRepository partnerRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService,
    ILogger<CreatePartnerCommandHandler> logger) : IRequestHandler<CreatePartnerCommand, CreatePartnerResult>
{
    /// <summary>
    /// Handles the creation of a new partner based on the specified command.
    /// </summary>
    /// <param name="request">The command containing the details required to create a new partner.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the details of the newly created
    /// partner.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a partner with the specified email or code already exists, or if partner creation fails due to invalid
    /// input.</exception>
    public async Task<CreatePartnerResult> Handle(CreatePartnerCommand request, CancellationToken cancellationToken)
    {

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Creating partner: {PartnerName}", request.Name);
        }

        // Check if partner with email already exists
        var existingPartner = await partnerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingPartner != null)
        {
            throw new InvalidOperationException($"Partner with email {request.Email} already exists");
        }

        // Check if partner with code already exists
        var partners = await partnerRepository.GetAllAsync(cancellationToken);
        if (partners.Any(p => p.Code == request.Code))
        {
            throw new InvalidOperationException($"Partner with code {request.Code} already exists");
        }

        // Create partner aggregate
        var partnerResult = Partner.Create(
            request.Code,
            request.Name,
            request.Email,
            request.AESKey,
            request.RSAPublicKey,
            request.RSAPrivateKey);

        if (!partnerResult.IsSuccess)
        {
            throw new InvalidOperationException(partnerResult.Error);
        }

        var partner = partnerResult.Value!;

        // Persist to database
        await partnerRepository.AddAsync(partner, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Partner created successfully: {PartnerId}", partner.Id);
        }

        // Invalidate cache
        await cacheService.RemoveByPrefixAsync("partners:", cancellationToken);
        await cacheService.RemoveByPrefixAsync("partner:", cancellationToken);

        return new CreatePartnerResult(
            partner.Id,
            partner.Code,
            partner.Name,
            partner.Email,
            partner.IsActive);
    }
}
