using MediatR;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;
using VolcanionTracking.Domain.Aggregates.PartnerAggregate;

namespace VolcanionTracking.Application.Partners.Commands.CreatePartner;

public class CreatePartnerCommandHandler : IRequestHandler<CreatePartnerCommand, CreatePartnerResult>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreatePartnerCommandHandler> _logger;

    public CreatePartnerCommandHandler(
        IPartnerRepository partnerRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<CreatePartnerCommandHandler> logger)
    {
        _partnerRepository = partnerRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<CreatePartnerResult> Handle(CreatePartnerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating partner: {PartnerName}", request.Name);

        // Check if partner with email already exists
        var existingPartner = await _partnerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingPartner != null)
        {
            throw new InvalidOperationException($"Partner with email {request.Email} already exists");
        }

        // Check if partner with code already exists
        var partners = await _partnerRepository.GetAllAsync(cancellationToken);
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
        await _partnerRepository.AddAsync(partner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Partner created successfully: {PartnerId}", partner.Id);

        // Invalidate cache
        await _cacheService.RemoveByPrefixAsync("partners:", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("partner:", cancellationToken);

        return new CreatePartnerResult(
            partner.Id,
            partner.Code,
            partner.Name,
            partner.Email,
            partner.IsActive);
    }
}
