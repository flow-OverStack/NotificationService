using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Enums;
using NotificationService.Domain.Results;

namespace NotificationService.Api.Controllers.Base;

/// <inheritdoc />
[Consumes(MediaTypeNames.Application.Json)]
[Route("api/v{version:apiVersion}/[controller]")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[ApiController]
public class BaseController : ControllerBase
{
    private static readonly IReadOnlyDictionary<int, int> ErrorStatusCodeMap = new Dictionary<int, int>
    {
        {(int)ErrorCodes.UserEventNotFound, StatusCodes.Status404NotFound},
        {(int)ErrorCodes.InvalidPagination, StatusCodes.Status400BadRequest},
        {(int)ErrorCodes.OperationForbidden, StatusCodes.Status403Forbidden}
    };

    /// <summary>
    ///     Handles the BaseResult of type T and returns the corresponding ActionResult
    /// </summary>
    /// <param name="result"></param>
    /// <param name="successStatusCode"></param>
    /// <typeparam name="T">Type of BaseResult</typeparam>
    /// <returns></returns>
    protected ActionResult<BaseResult<T>> HandleBaseResult<T>(BaseResult<T> result,
        HttpStatusCode successStatusCode = HttpStatusCode.OK) where T : class
    {
        var statusCode = GetStatusCode(result.IsSuccess, result.ErrorCode, (int)successStatusCode);

        return StatusCode(statusCode, result);
    }
    
    /// <summary>
    ///     Handles the CollectionResult of type T and returns the corresponding ActionResult
    /// </summary>
    /// <param name="result"></param>
    /// <param name="successStatusCode"></param>
    /// <typeparam name="T">Type of BaseResult</typeparam>
    /// <returns></returns>
    protected ActionResult<CollectionResult<T>> HandleCollectionResult<T>(CollectionResult<T> result,
        HttpStatusCode successStatusCode = HttpStatusCode.OK) where T : class
    {
        var statusCode = GetStatusCode(result.IsSuccess, result.ErrorCode, (int)successStatusCode);

        return StatusCode(statusCode, result);
    }

    private static int GetStatusCode(bool isSuccess, int? errorCode, int successStatusCode)
    {
        const int defaultCode = StatusCodes.Status400BadRequest;

        if (isSuccess) return successStatusCode;
        if (errorCode == null || !ErrorStatusCodeMap.TryGetValue((int)errorCode, out var code)) return defaultCode;
        return code;
    }
}