using Taskmanagement_API.Data;
using Taskmanagement_API.Models.DTOs;
using Taskmanagement_API.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Taskmanagement_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MM_DeploymentController : BaseController
    {
        private readonly MM_DeploymentDataService _deploymentService;
        private readonly MM_DeploymentNotificationService _notificationService;

        public MM_DeploymentController(
            MM_DeploymentDataService deploymentService,
            MM_DeploymentNotificationService notificationService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _deploymentService = deploymentService;
            _notificationService = notificationService;
        }

        [HttpPost("CreateRequest")]
        public async Task<IActionResult> CreateRequest([FromBody] MM_Deployment_CreateDto dto)
        {
            try
            {
                var newId = await _deploymentService.CreateRequestAsync(dto);

                // Notify manager (To) with team lead + requester in CC. Mail issues
                // must never fail the request itself.
                try
                {
                    await _notificationService.NotifyNewRequestAsync(newId);
                }
                catch { /* delivery + tracking handled inside EmailService */ }

                var response = _responseHelper.CreateSuccessResponse(new { Request_ID = newId }, "Success", "Deployment request submitted");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "CreateRequest");
            }
        }

        [HttpGet("GetRequestsByEmployee")]
        public async Task<IActionResult> GetRequestsByEmployee([FromQuery] decimal employeeId, [FromQuery] string? mode)
        {
            try
            {
                var requests = await _deploymentService.GetRequestsByEmployeeAsync(employeeId, mode);
                var response = _responseHelper.CreateSuccessResponse(requests, "Success", "Deployment requests retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetRequestsByEmployee");
            }
        }

        [HttpPost("UpdateDecision/{requestId}")]
        public async Task<IActionResult> UpdateDecision(decimal requestId, [FromBody] MM_Deployment_DecisionDto dto)
        {
            try
            {
                var updated = await _deploymentService.UpdateDecisionAsync(requestId, dto);
                if (!updated)
                {
                    var notFound = _responseHelper.CreateErrorResponse<object>("Not Found", "Request not found or already decided.");
                    return NotFound(notFound);
                }

                // Notify requester (To) with team lead + deciding manager in CC.
                try
                {
                    await _notificationService.NotifyDecisionAsync(requestId);
                }
                catch { /* delivery + tracking handled inside EmailService */ }

                var response = _responseHelper.CreateSuccessResponse<object?>(null, "Success", $"Deployment request {dto.Status?.ToLower()}.");
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                var forbidden = _responseHelper.CreateErrorResponse<object>("Forbidden", ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, forbidden);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "UpdateDecision");
            }
        }

        [HttpDelete("DeleteRequest/{requestId}")]
        public async Task<IActionResult> DeleteRequest(decimal requestId, [FromQuery] decimal employeeId)
        {
            try
            {
                var deleted = await _deploymentService.DeleteRequestAsync(requestId, employeeId);
                if (!deleted)
                {
                    var notFound = _responseHelper.CreateErrorResponse<object>("Not Found", "Request not found or cannot be withdrawn.");
                    return NotFound(notFound);
                }

                var response = _responseHelper.CreateSuccessResponse<object?>(null, "Success", "Deployment request withdrawn.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "DeleteRequest");
            }
        }
    }
}
