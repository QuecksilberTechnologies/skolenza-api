
using skolenza.application.DTOS.TicketDTO.Ticket;
using skolenza.application.Features.TickeAllCmd.Ticket;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace skolenza.api.Controllers.Ticket;

/// <summary>
/// Controller responsible for managing Ticket Classification operations.
/// Handles all Create, Read, Update, and Delete (CRUD) APIs for Ticket Classifications.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TicketCreation : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TicketClassificationController> _logger;

    public TicketCreation(IMediator mediator, ILogger<TicketClassificationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // ----------------------------------------------------------------------------------------------------
    // 1️⃣ CREATE - Add new Ticket Classification
    // ----------------------------------------------------------------------------------------------------
    [HttpPost("open")]   
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequestDTO dto)
    {
       
            _logger.LogInformation("🎯 Received request to create Ticket: {Data}", JsonConvert.SerializeObject(dto));
            var command = new CreateTicketCommand(dto);
            var result = await _mediator.Send(command);          
            return Ok(result);
       
      
    }

    
  
}
