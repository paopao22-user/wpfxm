using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services;
using IotGatewayLearning.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController:ControllerBase
    {
        // 1. 声明登录业务服务
        private readonly IAuthService _authService;

        //2.构造函数注入
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }


        // 3. 登录接口
        [AllowAnonymous]
        [HttpPost("login")]
        //ActionResult<ApiResponse<LoginResponseDto>>可以表示返回：HTTP状态 + ApiResponse<LoginResponseDto>
        //Login([FromBody] LoginRequestDto request):asp.net core可以自动 HTTP Request Body -> 读取 JSON -> 反序列化 -> LoginRequestDto
        //这个[FromBody] 可以自动把json 转为Dto
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            
            // 调用真正的登录业务
            var result = await _authService.LoginAsync(request);

            // 登录成功
            return Ok(new ApiResponse<LoginResponseDto>
            {
                Code = 200,
                Message = "登录成功",
                Data = result
            });

            
        }

        // ==============================
        // JWT认证测试
        // ==============================
        [Authorize]
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Message = "JWT认证成功",
                Data = "当前token有效"
            });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            //读取Claim里面的User信息
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Message = "获取当前用户成功",
                Data = new 
                {
                    UserId = userId,
                    Username = username,
                    Role = role
                }
            });
        }

        [Authorize(Roles="Admin")]
        [HttpGet("admintest")]
        public IActionResult AdminTest()
        {
            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Message = "管理员授权成功",
                Data = "当前用户拥有admin角色"
            });
        }
    }
}
