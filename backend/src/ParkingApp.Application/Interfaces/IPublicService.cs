using ParkingApp.Application.DTOs.Public;

namespace ParkingApp.Application.Interfaces;

public interface IPublicService
{
    Task<PublicTicketDto> GetTicketByTokenAsync(string token);
}
