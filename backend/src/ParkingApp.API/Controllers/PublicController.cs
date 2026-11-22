using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingApp.Application.DTOs.Public;
using ParkingApp.Application.Interfaces;

namespace ParkingApp.API.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController(IPublicService publicService) : ControllerBase
{
    /// <summary>
    /// Anonymous QR ticket lookup. Returns a live pass while active and a paid
    /// receipt after checkout. Sequential ids are never exposed.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("tickets/{token}")]
    [ProducesResponseType(typeof(PublicTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicTicketDto>> GetTicket(string token)
    {
        return Ok(await publicService.GetTicketByTokenAsync(token));
    }
}
