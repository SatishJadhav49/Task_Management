using Microsoft.AspNetCore.Mvc;
using Taskmanagement_API.Data;
using Taskmanagement_API.Models.DTOs;
using Taskmanagement_API.Utils;

namespace Taskmanagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MM_Daily_StatusController : BaseController
    {
        private readonly MM_Daily_StatusDataService _DailyStatusService;

        public MM_Daily_StatusController(
            MM_Daily_StatusDataService DailyStatusService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _DailyStatusService = DailyStatusService;
        } 

        [HttpGet("GetDailyStatusByDate")]
        public async Task<IActionResult> GetDailyStatusByDate( [FromQuery] string date, [FromQuery] decimal employeeId)
        {
            try
            {
                var activities = await _DailyStatusService.GetDailyStatusByDate( date, employeeId);
                var response = _responseHelper.CreateSuccessResponse(activities, "Success", "Activities retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetDailyStatusByDate");
            }
        }

        // CreateDailyStatus
        [HttpPost("CreateDailyStatus")]
        public async Task<IActionResult> CreateDailyStatus([FromBody] MM_Daily_StatusDto dailyStatus)
        {
            try
            {
                // Implementation for creating daily status
                var createdDailyStatus = await _DailyStatusService.CreateDailyStatus(dailyStatus);
                var response = _responseHelper.CreateSuccessResponse(createdDailyStatus, "Success", "Daily status created successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "CreateDailyStatus");
            }
        }

        // UpdateDailyStatus
        [HttpPut("UpdateDailyStatus/{statusId}")]
        public async Task<IActionResult> UpdateDailyStatus([FromRoute] decimal statusId, [FromBody] MM_Daily_StatusDto dailyStatus)
        {
            try
            {
                dailyStatus.Status_ID = statusId;
                var updatedDailyStatus = await _DailyStatusService.UpdateDailyStatus(dailyStatus);
                var response = _responseHelper.CreateSuccessResponse(updatedDailyStatus, "Success", "Daily status updated successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "UpdateDailyStatus");
            }
        }


    }
}