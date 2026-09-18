using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services
{
    public class AuthService : IAuthService
    {
        //IUnitOfWork:我要数据
        private readonly IUnitOfWork _unitOfWork;

        //IConfiguration:我要配置
        private readonly ITokenService _tokenService;

        public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;

            _tokenService = tokenService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                throw new ArgumentException("用户名不能为空");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("密码不能为空");
            }

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == request.Username.Trim());

            if(user == null)
            {
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            if(user.Status != 1)
            {
                throw new UnauthorizedAccessException("账户已被禁用");
            }

            bool passwordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!passwordCorrect)
            {
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 生成 JWT
            var (token, expiresAt) = _tokenService.CreateToken(user);

            //更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();   //提交到数据库

            //组装 `LoginResponseDto`
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

                    LastLoginAt = user.LastLoginAt
                }
            };
        }
    }
}
