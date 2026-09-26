using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Repositories;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 根据id查询用户
        /// </summary>
        /// <param name="targetUserId"></param>
        /// <returns></returns>
        public async Task<AdminUserDto?> GetUserByIdAsync(long targetUserId)
        {
            //调用工作单元中 Users 仓储的按 ID 查询方法
            var user = await _unitOfWork.Users.GetByIdAsync(targetUserId);

            if(user == null)
            {
                return null;
            }

            //复用统一的 MapUser 纯函数完成脱敏转换
            return MapUser(user);
        }

        /// <summary>
        /// 根据关键字条件，异步查询用户列表
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<List<AdminUserDto>> GetUsersAsync(string? keyword)
        {
            //1.先声明用户集合
            List<User> users;

            //2.如果关键字为空，则返回所有的用户
            if (string.IsNullOrWhiteSpace(keyword))
            {
                users = await _unitOfWork.Users.GetAllAsync();
            }
            else
            {
                //3. 给关键字去掉空格
                var key = keyword.Trim();

                //4.按keyword进行查询
                users = await _unitOfWork.Users.FindAllAsync(x => x.Username.Contains(key) || x.RealName.Contains(key));
            }

            //5.内存排序 + 安全脱敏转换（Entity -> DTO）
            return users.OrderBy(x => x.Id).Select(MapUser).ToList();
        }

        /// <summary>
        /// 纯映射函数：负责将内部 User 实体转换为对外安全的 AdminUserDto
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private static AdminUserDto MapUser(User user)
        {
            return new AdminUserDto
            {
                Id = user.Id,

                Username = user.Username,

                RealName = user.RealName,

                Status = user.Status
            };
        }

        /// <summary>
        /// 新增管理员用户
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<AdminUserDto> CreateUserAsync(CreateUserRequest request)
        {
            // 1：去除首尾空格清洗入参
            var username = request.Username?.Trim() ?? string.Empty;
            var realName = request.RealName?.Trim() ?? string.Empty;

            // 2：基础非空防御校验
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ValidationAppException("用户名不能为空");
            }

            if (string.IsNullOrWhiteSpace(realName))
            {
                throw new ValidationAppException("用户真实姓名不能为空");
            }

            if(request.Status != 0 && request.Status != 1)
            {
                throw new ValidationAppException("用户状态只能是 0（禁用）或 1（启用）");
            }

            // 3.密码强度与 BCrypt 72 字节上限校验
            if(string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8 || Encoding.UTF8.GetByteCount(request.Password) > 72)
            {
                throw new ValidationAppException("密码至少需要 8 个字符，且 UTF-8 编码后不能超过 72 个字节");
            }

            //  4：查重——校验用户名是否已被注册占用
            bool exists = await _unitOfWork.Users.AnyAsync(x => x.Username == username);

            if (exists)
            {
                throw new AppException("用户名已存在", 409);
            }

            //  5：使用 BCrypt 对明文密码进行安全加盐哈希
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

            //  6：构造领域实体对象
            var user = new User
            {
                Username = username,
                RealName = realName,
                PasswordHash = passwordHash,
                Role = string.Empty,
                Status = request.Status
            };


            //  7：将实体登记入 EF Core 变更追踪器（状态变为 Added）
            await _unitOfWork.Users.AddAsync(user);

            //  8：向数据库提交事务，真正发送 INSERT 语句并自动回填自增 user.Id
            await _unitOfWork.SaveChangesAsync();

            //  9：脱敏映射为 DTO 返回
            return MapUser(user);
        }

        public async Task<AdminUserDto> UpdateUserAsync(long id, UpdateUserRequest request, long actorId)
        {
            // 步骤 1：入参合法性校验
            if(id < 0)
            {
                throw new ValidationAppException("用户id 必须大于 0");
            }

            // 步骤 2：查询目标用户是否存在
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if(user == null)
            {
                throw new AppException("用户不存在", 404);
            }

            // 步骤 3：清洗并校验真实姓名
            var realname = request.RealName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(realname))
            {
                throw new ValidationAppException("真实姓名不能为空");
            }

            // 步骤 4：校验状态值合法性
            if(request.Status != 0 && request.Status != 1)
            {
                throw new ValidationAppException("用户状态只能是 0（禁用）或 1（启用）");
            }

            // 步骤 5：核心安全防护规则——禁止禁用自己或内置超级管理员 admin
            if(request.Status == 0 && (id == actorId || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase)))
            {
                throw new AppException("不能禁用当前登录账号或系统内置管理员", 409);
            }

            // 步骤 6：更新实体属性
            user.RealName = realname;
            user.Status = request.Status;

            // 显式标记更新（养成良好仓储习惯）
             _unitOfWork.Users.Update(user);

            // 步骤 7：提交更改，向 MySQL 发送 UPDATE 语句
            await _unitOfWork.SaveChangesAsync();

            // 步骤 8：脱敏返回最新 DTO
            return MapUser(user);
        }

        /// <summary>
        /// 删除指定用户
        /// </summary>
        /// <param name="id">要删除的用户id</param>
        /// <param name="actorId">当前登录的用户id</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task DeleteUserAsync(long id, long actorId)
        {
            // 步骤 1：入参 ID 合法性校验
            if(id <= 0)
            {
                throw new ValidationAppException("用户Id必须大于0");
            }

            // 步骤 2：查询目标用户是否存在
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if(user == null)
            {
                throw new AppException("用户不存在", 404);
            }

            // 步骤 3：核心安全防护规则——禁止删除当前登录者自己，禁止删除内置超级管理员 admin
            if (id == actorId || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new AppException("用户不能删除自己或者删除管理员", 409);
            }

            //检查是否已经被软删除
            if(user.Status == 0)
            {
                throw new AppException("该用户已被删除", 400);
            }

            //修改status状态
            user.Status = 0;

            // 步骤 4：在 EF Core 变更追踪器中将其实体标记为已删除（Deleted）
            _unitOfWork.Users.Update(user);

            // 步骤 5：提交事务，向 MySQL 发送物理 DELETE 语句（级联清理 user_roles）
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
