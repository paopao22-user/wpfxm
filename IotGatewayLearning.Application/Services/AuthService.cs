using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IotGatewayLearning.Application.Services
{
    public class AuthService : IAuthService
    {
        //IUnitOfWork:我要数据
        private readonly IUnitOfWork _unitOfWork;

        //IConfiguration:我要Token服务
        private readonly ITokenService _tokenService;

        //日记记录：AuthService
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;

            _tokenService = tokenService;

            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // 1.检查用户名 和密码
            if(string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new UnauthorizedAppException("用户名或密码错误");
            }

            // 2. 先取出用户名，并去掉前后空格
            var username = request.Username.Trim();

            // 3.查询 User + RBAC
            var user = await _unitOfWork.Users.GetByUsernameWithRbacAsync(username);

            // 4.用户不存在
            if (user == null)
            {
                _logger.LogWarning("登录失败，账号不存在：{Username}", username);

                throw new UnauthorizedAppException("用户名或密码错误");
            }

            // 5.检查用户状态
            if (user.Status != 1)
            {
                _logger.LogWarning("登录失败，账号已禁用: {Username}", user.Username);

                throw new UnauthorizedAppException("用户名或密码错误");
            }

            // 6.验证密码
            bool passwordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!passwordCorrect)
            {
                _logger.LogWarning("登录失败，账号{Username}的密码错误", user.Username);

                throw new UnauthorizedAppException("用户名或密码错误");
            }

            //7.提取角色
            var roles = user.UserRoles
                .Where(x => x.Role.Status == 1)
                .Select(x => x.Role.Code)
                .Distinct()
                .ToList();

            //8.提取权限
            var permissions = user.UserRoles
                .Where(x => x.Role.Status == 1)
                .SelectMany(x => x.Role.RolePermissions)
                .Where(x => x.Permission.Status == 1)
                .Select(x => x.Permission.Code)
                .Distinct()
                .ToList();

            // 9.生成 JWT
            var (token, expiresAt) = _tokenService.CreateToken(user);

            //10.更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();   //提交到数据库

            // ==============================
            // 11. 登录成功日志
            // ==============================
            _logger.LogInformation("登录成功，用户id为:{UserId}, 用户账号为:{Username}", user.Id, user.Username);

            //12.返回登录结果
            return new LoginResponseDto
            {
                Token = token,

                ExpiresAt = expiresAt,

                User = new UserDto
                {
                    Id = user.Id,

                    Username = user.Username,

                    RealName = user.RealName,

                    Role = user.Role,

                    Roles = roles,

                    Permissions = permissions,

                    LastLoginAt = user.LastLoginAt
                }
            };
        }
    }
}
