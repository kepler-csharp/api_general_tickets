using ApiGeneral.AuthApi.DTOs.SeatDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.DTOs.ShowtimesDTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IShowtimeService
{
    Task<PagedResult<ShowtimeDto>> GetAllAsync(int page, int pageSize, int? eventId);
    Task<ShowtimeDto?> GetByIdAsync(int id);
    Task<ShowtimeDto> CreateAsync(CreateShowtimeRequest request);
    Task<List<SeatDto>> GetSeatsAsync(int showtimeId);
    Task<ShowtimeDto?> UpdateAsync(int id, UpdateShowtimeRequest request);
    Task<bool> DeleteAsync(int id);
    Task<ShowtimeDto?> SetActiveAsync(int id, bool active);
}
