using Taskmanagement_API.Data;
using Taskmanagement_API.Models.DTOs;
using Taskmanagement_API.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Taskmanagement_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MM_Task_DetailsController : BaseController
    {
        private readonly MM_TaskDetailsService _TaskDetailService;

        public MM_Task_DetailsController(
            MM_TaskDetailsService TaskDetailService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _TaskDetailService = TaskDetailService;
        }


        [HttpPost("CreateTaskDetail")]
        public async Task<IActionResult> CreateTaskDetail([FromBody] MM_Task_DetailsCreateDto TaskDetailDto)
        {
            try
            {
                var createdTaskDetail = await _TaskDetailService.CreateTaskDetailAsync(TaskDetailDto);
                var response = _responseHelper.CreateSuccessResponse(createdTaskDetail, "Success", "Task detail created successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "CreateTaskDetail");
            }
        }


        [HttpGet("GetTasksByEmployee")]
        public async Task<IActionResult> GetTasksByEmployee( [FromQuery] decimal employeeId, [FromQuery] string? mode, [FromQuery] decimal? teamId)
        {
            try
            {
                var activities = await _TaskDetailService.GetTasksByEmployee( employeeId, mode, teamId);
                var response = _responseHelper.CreateSuccessResponse(activities, "Success", "Activities retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetActivitiesByDate");
            }
        }

        // DeleteTaskDetail/${TaskId}
        [HttpDelete("DeleteTaskDetail/{TaskId}")]
        public async Task<IActionResult> DeleteTaskDetail(decimal TaskId)
        {
            try
            {
                var isDeleted = await _TaskDetailService.DeleteTaskDetailAsync(TaskId);
                if (isDeleted)
                {
                    var response = _responseHelper.CreateSuccessResponse<object?>(null, "Success", "Task detail deleted successfully");
                    return Ok(response);
                }
                else
                {
                    var response = _responseHelper.CreateErrorResponse<object>("Not Found", "Task detail not found");
                    return NotFound(response);
                }
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "DeleteTaskDetail");
            }
        }

        
        [HttpPost("UpdateManagerRemark/{taskId}")]
        public async Task<IActionResult> UpdateManagerRemark(decimal taskId, [FromBody] MM_Task_RemarkUpdateDto dto)
        {
            try
            {
                var updated = await _TaskDetailService.UpdateManagerRemarkAsync(taskId, dto);
                if (!updated)
                {
                    var notFound = _responseHelper.CreateErrorResponse<object>("Not Found", "Task not found.");
                    return NotFound(notFound);
                }

                var response = _responseHelper.CreateSuccessResponse<object?>(null, "Success", "Manager remark updated.");
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                var forbidden = _responseHelper.CreateErrorResponse<object>("Forbidden", ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, forbidden);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "UpdateManagerRemark");
            }
        }

        [HttpPut("UpdateTaskDetail/{Task_ID}")]
        public async Task<IActionResult> UpdateTaskDetail([FromRoute] decimal Task_ID, [FromBody] MM_Task_DetailsUpdateDto TaskDetailDto)
        {
            try
            {
                var updatedTaskDetail = await _TaskDetailService.UpdateTaskDetailAsync(TaskDetailDto);
                var response = _responseHelper.CreateSuccessResponse(updatedTaskDetail, "Success", "Task detail updated successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "UpdateTaskDetail");
            }
        }
    }
}