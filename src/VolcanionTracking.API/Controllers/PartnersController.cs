using MediatR;
using Microsoft.AspNetCore.Mvc;
using VolcanionTracking.Application.Partners.Commands.CreatePartner;

namespace VolcanionTracking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartnersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PartnersController> _logger;

    public PartnersController(IMediator mediator, ILogger<PartnersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new partner
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePartnerResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePartner(
        [FromBody] CreatePartnerCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating partner: {PartnerName}", command.Name);

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetPartner),
            new { id = result.Id },
            result);
    }

    /// <summary>
    /// Get a partner by ID (placeholder)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPartner(Guid id)
    {
        // TODO: Implement GetPartnerQuery
        return Ok(new { id, message = "Partner details endpoint - to be implemented" });
    }
}
