using Taskmanagement_API.Data;
using Taskmanagement_API.Models.DTOs;
using Taskmanagement_API.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Taskmanagement_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MM_Activity_DetailsController : BaseController
    {
        private readonly MM_ActivityDetailsService _ActivityDetailService;

        public MM_Activity_DetailsController(
            MM_ActivityDetailsService ActivityDetailService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _ActivityDetailService = ActivityDetailService;
        }

        [HttpGet("GetAllActivityTypes")]
        public async Task<IActionResult> GetAllActivityTypes()
        {
            try
            {
                var activityDetails = await _ActivityDetailService.GetActivityTypesAsync();
                var response = _responseHelper.CreateSuccessResponse(activityDetails, "Success", "Activity details retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetAllActivityTypes");
            }
        }

        [HttpPost("CreateActivityDetail")]
        public async Task<IActionResult> CreateActivityDetail([FromBody] MM_Activity_DetailsCreateDto activityDetailDto)
        {
            try
            {
                var createdActivityDetail = await _ActivityDetailService.CreateActivityDetailAsync(activityDetailDto);
                var response = _responseHelper.CreateSuccessResponse(createdActivityDetail, "Success", "Activity detail created successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "CreateActivityDetail");
            }
        }


        [HttpGet("GetActivitiesByDate")]
        public async Task<IActionResult> GetActivitiesByDate([FromQuery] string filterFrom, [FromQuery] string filterTo, [FromQuery] decimal employeeId)
        {
            try
            {
                var activities = await _ActivityDetailService.GetActivitiesByDateAsync(filterFrom, filterTo, employeeId);
                var response = _responseHelper.CreateSuccessResponse(activities, "Success", "Activities retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetActivitiesByDate");
            }
        }

        // DeleteActivityDetail/${activityId}
        [HttpDelete("DeleteActivityDetail/{activityId}")]
        public async Task<IActionResult> DeleteActivityDetail(decimal activityId)
        {
            try
            {
                var isDeleted = await _ActivityDetailService.DeleteActivityDetailAsync(activityId);
                if (isDeleted)
                {
                    var response = _responseHelper.CreateSuccessResponse<object>(null, "Success", "Activity detail deleted successfully");
                    return Ok(response);
                }
                else
                {
                    var response = _responseHelper.CreateErrorResponse<object>("Not Found", "Activity detail not found");
                    return NotFound(response);
                }
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "DeleteActivityDetail");
            }
        }

        
        [HttpPut("UpdateActivityDetail/{Activity_ID}")]
        public async Task<IActionResult> UpdateActivityDetail([FromRoute] decimal Activity_ID, [FromBody] MM_Activity_DetailsUpdateDto activityDetailDto)
        {
            try
            {
                var updatedActivityDetail = await _ActivityDetailService.UpdateActivityDetailAsync(activityDetailDto);
                var response = _responseHelper.CreateSuccessResponse(updatedActivityDetail, "Success", "Activity detail updated successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "UpdateActivityDetail");
            }
        }
    }
}