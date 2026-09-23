using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Common.Responses;

namespace IotGatewayLearningApi.Middleware
{
    public class ExceptionHandlingMiddleware
    {

        // 后面的请求管道
        private readonly RequestDelegate _next; //_next: 让请求继续往后走

        // 日志
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;

            _logger = logger;

        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // 继续让请求往后走
                await _next(context);
            }

            // ==============================
            // 我们自己定义的业务异常
            // ==============================
            catch (AppException ex)
            {
                await WriteErrorResponseAsync(context, ex.StatusCode, ex.Message);
            }

            // ==============================
            // 未预料异常
            // ==============================
            catch (Exception ex)
            {
                // 真正的错误详细记录到服务器日志
                _logger.LogError(ex, "请求处理过程中发生未处理异常，Path：{RequestPath}", context.Request.Path);

                // 但是绝不能把内部异常详情给客户端
                await WriteErrorResponseAsync(context, 500, "服务器内部错误");
            }
        }


        private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;

            var response = new ApiResponse<object?>
            {
                Code = statusCode,
                Message = message,
                Data = null
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
