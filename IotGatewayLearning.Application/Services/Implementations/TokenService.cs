using IotGatewayLearning.Application.Services;
using IotGatewayLearning.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;         // 注入全局配置接口

        public TokenService(IConfiguration configuration)       // 构造函数依赖注入
        {
            _configuration = configuration;                     // 初始化配置字段
        }

        public (string Token, long ExpiresAt) CreateToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions)        // 核心签发方法实现
        {
            // 1.读取JWT配置：密钥、签发者、使用者
            string key = _configuration["Jwt:Key"] ?? throw new Exception("未配置 Jwt:Key");

            string issuer = _configuration["Jwt:Issuer"] ?? throw new Exception("未配置 Jwt:Issuer");

            string audience = _configuration["Jwt:Audience"] ?? throw new Exception("未配置 Jwt:Audience");

            // 2.设置过期时间
            DateTime expires = DateTime.UtcNow.AddHours(8);     // JWT 和服务器时间一般采用 UTC，避免服务器时区变化造成歧义

            // 3.准备写进 Token 里面的用户身份信息:   用户登录成功后，服务端把用户的 Id、用户名、角色直接打包封装成一堆 Claim,服务端用私钥SecretKey
            //给这堆Claim 盖个数据公章给客户端，客户端请求带着Token,只要公章无误，直接从Token包解析出Claim,瞬间拿到当前操作员的 Id 和角色，零次访问数据库
            var claims = new List<Claim>    // 声明并实例化一个 Claim 对象的强类型集合容器
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),    // 注入用户唯一主键 ID (标识"你是哪一条数据")

                new Claim(ClaimTypes.Name, user.Username),  // 注入登录用户名 (用于业务展示与日志审计)

                
            };
            //把所有 Role 加进去Claims身份信息里
            foreach (var role in roles.Distinct())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            //把所有 Permission 加进去Claims身份信息里
            foreach(var permission in permissions.Distinct())
            {
                claims.Add(new Claim("permission", permission));
            }


            // 4.把密钥转换为字节：new  SymmetricSecurityKey()：把这一串字节正式包装成一个“对称签名密钥”,对称密钥这样保证认证和授权使用的是同一把密钥。
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));    // 将字符串密钥转为二进制字节数组


            // 5.创建签名凭据   : SigningCredentials:签名方案
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // 6.创建JWT对象
            var jwtToken = new JwtSecurityToken(
                issuer: issuer,     // 签发机构
                audience: audience,  // 接收受众
                claims: claims,         // 身份清单
                expires: expires,        // 到期时间
                signingCredentials: credentials);   // 盖上防伪公章


            // 7.转换为真正的Token字符串
            //JwtSecurityTokenHandler (令牌处理器)：JWT 的处理工具;  WriteToken: 序列化
            string token = new JwtSecurityTokenHandler().WriteToken(jwtToken);  // 序列化为最终的三段式字符串

            // 8.把过期时间转换成时间戳
            long expiresAt = new DateTimeOffset(expires).ToUnixTimeSeconds();   // 转换为长整型时间戳

            return (token, expiresAt);       // 返回生成结果
        }
    }
}
