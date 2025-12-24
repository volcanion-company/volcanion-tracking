using MediatR;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;

namespace VolcanionTracking.Application.PartnerSystems.Commands.CreatePartnerSystem;

public class CreatePartnerSystemCommandHandler 
    : IRequestHandler<CreatePartnerSystemCommand, CreatePartnerSystemResult>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IPartnerSystemRepository _partnerSystemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreatePartnerSystemCommandHandler> _logger;

    public CreatePartnerSystemCommandHandler(
        IPartnerRepository partnerRepository,
        IPartnerSystemRepository partnerSystemRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<CreatePartnerSystemCommandHandler> logger)
    {
        _partnerRepository = partnerRepository;
        _partnerSystemRepository = partnerSystemRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<CreatePartnerSystemResult> Handle(
        CreatePartnerSystemCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating partner system: {SystemName} for partner: {PartnerId}", 
            request.Name, 
            request.PartnerId);

        // Get partner aggregate
        var partner = await _partnerRepository.GetByIdWithSystemsAsync(request.PartnerId, cancellationToken);
        if (partner == null)
        {
            throw new InvalidOperationException($"Partner {request.PartnerId} not found");
        }

        // Use aggregate method to add system
        var systemResult = partner.AddSystem(request.Name, request.Type, request.Description);
        if (!systemResult.IsSuccess)
        {
            throw new InvalidOperationException(systemResult.Error);
        }

        var system = systemResult.Value!;

        // Persist changes
        _partnerRepository.Update(partner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Partner system created: {SystemId}", system.Id);

        // Invalidate cache
        await _cacheService.RemoveAsync($"partner_system:{system.Id}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync($"partner_systems:{request.PartnerId}", cancellationToken);

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
