using Taskmanagement_API.Data;
using Taskmanagement_API.Models.DTOs;
using Taskmanagement_API.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Taskmanagement_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MM_ShiftController : BaseController
    {
        private readonly MM_ShiftDataService _shiftService;

        public MM_ShiftController(
            MM_ShiftDataService shiftService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _shiftService = shiftService;
        }

        [HttpGet("GetAllShifts")]
        public async Task<IActionResult> GetAllShifts()
        {
            try
            {
                var shifts = await _shiftService.GetAllShiftsAsync();
                var response = _responseHelper.CreateSuccessResponse(shifts, "Success", "Shifts retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetAllShifts");
            }
        }

        [HttpGet("GetWeeklySchedule")]
        public async Task<IActionResult> GetWeeklySchedule([FromQuery] decimal teamId, [FromQuery] string weekStart)
        {
            try
            {
                var roster = await _shiftService.GetWeeklyScheduleAsync(teamId, weekStart);
                var response = _responseHelper.CreateSuccessResponse(roster, "Success", "Weekly schedule retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetWeeklySchedule");
            }
        }

        [HttpPost("SaveWeeklyAssignment")]
        public async Task<IActionResult> SaveWeeklyAssignment([FromBody] MM_ShiftWeeklyAssignmentDto dto)
        {
            try
            {
                await _shiftService.SaveWeeklyAssignmentAsync(dto);
                var response = _responseHelper.CreateSuccessResponse<object?>(null, "Success", "Shift assignment saved");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "SaveWeeklyAssignment");
            }
        }

        [HttpGet("GetDayOverrides")]
        public async Task<IActionResult> GetDayOverrides([FromQuery] decimal employeeId, [FromQuery] string weekStart)
        {
            try
            {
                var days = await _shiftService.GetDayOverridesAsync(employeeId, weekStart);
                var response = _responseHelper.CreateSuccessResponse(days, "Success", "Day shifts retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetDayOverrides");
            }
        }

        [HttpPost("SaveDayOverride")]
        public async Task<IActionResult> SaveDayOverride([FromBody] MM_ShiftDayOverrideDto dto)
        {
            try
            {
                await _shiftService.SaveDayOverrideAsync(dto);
                var response = _responseHelper.CreateSuccessResponse<object?>(null, "Success", "Day shift saved");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "SaveDayOverride");
            }
        }
    }
}
