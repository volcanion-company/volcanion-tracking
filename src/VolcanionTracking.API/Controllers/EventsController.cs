using MediatR;
using Microsoft.AspNetCore.Mvc;
using VolcanionTracking.Application.TrackingEvents.Commands.IngestEvent;
using VolcanionTracking.Application.TrackingEvents.Queries.GetEventsByPartnerSystem;
using VolcanionTracking.Application.TrackingEvents.Queries.GetEventStatistics;
using System.Diagnostics;

namespace VolcanionTracking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IMediator mediator, ILogger<EventsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Ingest a tracking event (high-throughput endpoint)
    /// </summary>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(IngestEventResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IngestEvent(
        [FromBody] IngestEventRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Ingesting event: {EventName} with correlation ID: {CorrelationId}",
            request.EventName,
            correlationId);

        var command = new IngestEventCommand(
            request.ApiKey,
            request.EventName,
            request.EventTimestamp,
            request.UserId,
            request.AnonymousId,
            request.EventProperties ?? "{}",
            correlationId);

        var result = await _mediator.Send(command, cancellationToken);

        return Accepted(result);
    }

    /// <summary>
    /// Query events by partner system (analytics endpoint)
    /// </summary>
    [HttpGet("partner-system/{partnerSystemId:guid}")]
    [ProducesResponseType(typeof(GetEventsByPartnerSystemResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventsByPartnerSystem(
        Guid partnerSystemId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEventsByPartnerSystemQuery(
            partnerSystemId,
            startDate,
            endDate,
            pageNumber,
            pageSize);

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get event statistics for a partner system
    /// </summary>
    [HttpGet("partner-system/{partnerSystemId:guid}/statistics")]
    [ProducesResponseType(typeof(EventStatisticsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventStatistics(
        Guid partnerSystemId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEventStatisticsQuery(partnerSystemId, startDate, endDate);

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }
}

public record IngestEventRequest(
    string ApiKey,
    string EventName,
    DateTime EventTimestamp,
    string? UserId,
    string AnonymousId,
    string? EventProperties);
