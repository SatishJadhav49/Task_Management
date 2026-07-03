using Taskmanagement_API.Data;
using Taskmanagement_API.Models.DTOs;
using Taskmanagement_API.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace Taskmanagement_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseController : ControllerBase
    {
        protected readonly IApiResponseHelper _responseHelper;
        private readonly MM_ErrorLogDataService _errorLogService;

        public BaseController( IApiResponseHelper responseHelper, MM_ErrorLogDataService errorLogService)
        {
            _responseHelper = responseHelper;
            _errorLogService = errorLogService;
        }

        protected async Task<IActionResult> HandleExceptionAsync(Exception ex, string operation, [CallerMemberName] string methodName = "")
        {
            // Log error to database
            var controllerName = this.GetType().Name;
            await _errorLogService.LogErrorAsync(controllerName, operation, methodName, ex);
            
            var errorResponse = _responseHelper.CreateErrorResponse<object>(
                Constants.Messages.INTERNAL_ERROR,
                ex.Message
            );

            return StatusCode(500, errorResponse);
        }

        protected IActionResult HandleException(Exception ex, string operation)
        {
            var errorResponse = _responseHelper.CreateErrorResponse<object>(
                Constants.Messages.INTERNAL_ERROR,
                ex.Message
            );

            return StatusCode(500, errorResponse);
        }
    }
}
