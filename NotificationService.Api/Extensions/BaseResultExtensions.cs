using System.Net;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Enums;
using NotificationService.Domain.Results;

namespace NotificationService.Api.Extensions;

public static class BaseResultExtensions
{
    private static readonly IReadOnlyDictionary<int, int> ErrorStatusCodeMap = new Dictionary<int, int>
    {
        { (int)ErrorCodes.UserEventNotFound, StatusCodes.Status404NotFound },
        { (int)ErrorCodes.InvalidPagination, StatusCodes.Status400BadRequest },
        { (int)ErrorCodes.OperationForbidden, StatusCodes.Status403Forbidden }
    };

    /// <summary>
    ///     Converts a BaseResult of type T into the corresponding ActionResult
    /// </summary>
    /// <param name="result"></param>
    /// <param name="successStatusCode"></param>
    /// <typeparam name="T">Type of BaseResult</typeparam>
    /// <returns></returns>
    public static ActionResult<BaseResult<T>> ToActionResult<T>(
        this BaseResult<T> result,
        HttpStatusCode successStatusCode = HttpStatusCode.OK) where T : class
    {
        if (result.IsSuccess) return new ObjectResult(result) { StatusCode = (int)successStatusCode };

        return new ObjectResult(result) { StatusCode = GetStatusCode(result.ErrorCode) };
    }

    /// <summary>
    ///     Converts a CollectionResult of type T into the corresponding ActionResult
    /// </summary>
    /// <param name="result"></param>
    /// <param name="successStatusCode"></param>
    /// <typeparam name="T">Type of BaseResult</typeparam>
    /// <returns></returns>
    public static ActionResult<CollectionResult<T>> ToActionResult<T>(
        this CollectionResult<T> result,
        HttpStatusCode successStatusCode = HttpStatusCode.OK) where T : class
    {
        if (result.IsSuccess) return new ObjectResult(result) { StatusCode = (int)successStatusCode };

        return new ObjectResult(result) { StatusCode = GetStatusCode(result.ErrorCode) };
    }

    private static int GetStatusCode(int? errorCode)
    {
        if (errorCode != null && ErrorStatusCodeMap.TryGetValue((int)errorCode, out var code)) return code;

        return StatusCodes.Status500InternalServerError;
    }
}