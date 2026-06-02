using ApiGeneral.AuthApi.DTOs.SeatDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.DTOs.ShowtimesDTOs;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Controllers;

[ApiController]
[Route("api/showtimes")]
public class ShowtimesController : ControllerBase
{
    private readonly IShowtimeService _showtimes;
    private readonly ILogger<ShowtimesController> _logger;

    public ShowtimesController(IShowtimeService showtimes, ILogger<ShowtimesController> logger)
    {
        _showtimes = showtimes;
        _logger    = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? eventId = null)
    {
        try
        {
            var result = await _showtimes.GetAllAsync(page, pageSize, eventId);
            return Ok(ApiResponse<PagedResult<ShowtimeDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving showtimes.");
            return StatusCode(500, ApiResponse<object>.Fail("An unexpected error occurred."));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _showtimes.GetByIdAsync(id);
            if (result == null)
                return NotFound(ApiResponse<object>.Fail("Showtime not found."));
            return Ok(ApiResponse<ShowtimeDto>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving showtime {Id}.", id);
            return StatusCode(500, ApiResponse<object>.Fail("An unexpected error occurred."));
        }
    }

    [HttpGet("{id:int}/seats")]
    public async Task<IActionResult> GetSeats(int id)
    {
        try
        {
            var result = await _showtimes.GetSeatsAsync(id);
            return Ok(ApiResponse<List<SeatDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving seats for showtime {Id}.", id);
            return StatusCode(500, ApiResponse<object>.Fail("An unexpected error occurred."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateShowtimeRequest request)
    {
        try
        {
            var result = await _showtimes.CreateAsync(request);
            return Created($"/api/showtimes/{result.Id}",
                ApiResponse<ShowtimeDto>.Ok(result, "Showtime created."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating showtime.");
            return StatusCode(500, ApiResponse<object>.Fail("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Update a showtime's StartTime and/or BasePrice. Admin only.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShowtimeRequest request)
    {
        try
        {
            var result = await _showtimes.UpdateAsync(id, request);
            if (result == null)
                return NotFound(ApiResponse<object>.Fail("Showtime not found."));
            return Ok(ApiResponse<ShowtimeDto>.Ok(result, "Showtime updated."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating showtime {Id}.", id);
            return StatusCode(500, ApiResponse<object>.Fail("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Delete a showtime. Fails if it has sold tickets. Admin only.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _showtimes.DeleteAsync(id);
            if (!deleted)
                return NotFound(ApiResponse<object>.Fail("Showtime not found."));
            return Ok(ApiResponse<object>.Ok(null!, "Showtime deleted."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting showtime {Id}.", id);
            return StatusCode(500, ApiResponse<object>.Fail("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Activate or deactivate a showtime (Active / Cancelled). Admin only.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool active)
    {
        try
        {
            var result = await _showtimes.SetActiveAsync(id, active);
            if (result == null)
                return NotFound(ApiResponse<object>.Fail("Showtime not found."));

            var msg = active ? "Showtime activated." : "Showtime deactivated.";
            return Ok(ApiResponse<ShowtimeDto>.Ok(result, msg));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing status for showtime {Id}.", id);
            return StatusCode(500, ApiResponse<object>.Fail("An unexpected error occurred."));
        }
    }
}
