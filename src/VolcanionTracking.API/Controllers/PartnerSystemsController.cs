using MediatR;
using Microsoft.AspNetCore.Mvc;
using VolcanionTracking.Application.PartnerSystems.Commands.CreatePartnerSystem;

namespace VolcanionTracking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartnerSystemsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PartnerSystemsController> _logger;

    public PartnerSystemsController(IMediator mediator, ILogger<PartnerSystemsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new partner system (data source)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePartnerSystemResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePartnerSystem(
        [FromBody] CreatePartnerSystemCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating partner system: {SystemName}", command.Name);

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetPartnerSystem),
            new { id = result.Id },
            result);
    }

    /// <summary>
    /// Get a partner system by ID (placeholder)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPartnerSystem(Guid id)
    {
        // TODO: Implement GetPartnerSystemQuery
        return Ok(new { id, message = "Partner system details endpoint - to be implemented" });
    }
}
