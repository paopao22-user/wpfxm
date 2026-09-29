using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Implementations
{
    /// <summary>
    /// 用户领域业务服务实现类
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        // 构造函数注入工作单元，所有持久化操作通过 UoW 完成
        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
        /// 新增用户
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

            if (request.Status != 0 && request.Status != 1)
            {
                throw new ValidationAppException("用户状态只能是 0（禁用）或 1（启用）");
            }

            // 3.密码强度与 BCrypt 72 字节上限校验
            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8 || Encoding.UTF8.GetByteCount(request.Password) > 72)
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
            if (id <= 0)
            {
                throw new ValidationAppException("用户Id必须大于0");
            }

            // 步骤 2：查询目标用户是否存在
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                throw new AppException("用户不存在", 404);
            }

            // 步骤 3：核心安全防护规则——禁止删除当前登录者自己，禁止删除内置超级管理员 admin
            if (id == actorId || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new AppException("用户不能删除自己或者删除管理员", 409);
            }

            //检查是否已经被软删除
            if (user.Status == 0)
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

        /// <summary>
        /// 根据id查询用户
        /// </summary>
        /// <param name="targetUserId"></param>
        /// <returns></returns>
        public async Task<AdminUserDto?> GetUserByIdAsync(long targetUserId)
        {
            //调用工作单元中 Users 仓储的按 ID 查询方法
            var user = await _unitOfWork.Users.GetByIdAsync(targetUserId);

            if (user == null)
            {
                return null;
            }

            //复用统一的 MapUser 纯函数完成脱敏转换
            return MapUser(user);
        }

        /// <summary>
        /// 分页获取管理员用户列表实现
        /// </summary>
        public async Task<PageResult<AdminUserDto>> GetUserPageListAsync(int page, int pageSize, string? keyword, CancellationToken cancellationToken = default)
        {
            // 防线 1：参数合法性边界防御
            int validPage = page < 1 ? 1 : page; // 页码小于 1 强制纠正为第 1 页
            int validPageSize = Math.Clamp(pageSize, 1, 100); // 单页限制在 1 到 100 条之间，防范恶意爬取

            // 步骤 2：调度仓储层执行物理分页
            var (users, total) = await _unitOfWork.Users.GetPagedUsersAsync(validPage, validPageSize, keyword, cancellationToken);

            // 步骤 3：利用已有的 MapUser 纯函数脱敏转换（隐藏 PasswordHash）
            var items = users.Select(MapUser).ToList();

            // 步骤 4：封装为标准 PageResult 模型返回
            return new PageResult<AdminUserDto>
            {
                Items = items,
                Total = total,
                Page = validPage,
                PageSize = validPageSize
            };
        }


        /// <summary>
        /// 根据用户主键 ID 查询当前已分配的角色 ID 集合
        /// </summary>
        public async Task<List<long>> GetUserRoleIdsAsync(long userId)
        {
            // 防线 1：参数合法性校验
            if (userId <= 0)
            {
                throw new AppException("用户Id必须大于0", 400);
            }

            // 防线 2：验证目标用户是否存在于系统（避免为幽灵账号查询关系）
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                throw new AppException("用户必须存在", 404);
            }

            // 步骤 3：从用户-角色中间仓储中查询属于该用户的全部关联行
            var userRoles = await _unitOfWork.UserRoles.FindAllAsync(x => x.UserId == userId); //返回List<UserRole>（也就是一个装满 UserRole 实体对象的动态列表

            // 步骤 4：通过 LINQ 投影（Select）只提取角色 ID，并按升序规整输出
            return userRoles.Select(x => x.RoleId)
                            .OrderBy(roleId => roleId)  //OrderBy(元素 => 根据元素的什么指标来排序),这里前面只剩下数字了，按数字来排序
                            .ToList();
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
        /// 为指定用户完整分配/替换角色集合（基于差集算法实现幂等更新）
        /// </summary>
        public async Task SetUserRolesAsync(long userId, AssignIdsRequest request, long actorId)
        {
            // 防线 1：参数基础校验与目标用户存在性检查
            if (userId <= 0)
            {
                throw new AppException("用户id必须大于0", 400);
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                throw new AppException("找不到用户", 404);
            }

            // 防线 2：核心安全红线 —— 严禁修改当前操作者自己、严禁修改内置 admin 账号
            if (userId == actorId || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new AppException("不能在此修改当前登录账号或系统内置管理员的角色", 409);
            }

            // 防线 3：执行数据清洗（去重、正数检查、空数组合法化）
            var ids = NormalizeIds(request);

            // 防线 4：角色全量合法性校验 —— 确保所提交的角色在系统中全部存在且处于启用状态 (Status == 1)
            if (ids.Length > 0)
            {
                var availableRoles = await _unitOfWork.Roles.FindAllAsync(x => ids.Contains(x.Id) && x.Status == 1);

                if (availableRoles.Count != ids.Length)
                {
                    throw new AppException("提交的角色列表中包含不存在或已被停用的角色", 400);
                }
            }

            // 步骤 5：查询该用户在数据库中现有的所有角色关联
            var links = await _unitOfWork.UserRoles.FindAllAsync(x => x.UserId == userId);

            // 步骤 6：差集比对与内存标记（巧妙的 LINQ 差集实现）
            // 6.1 计算待删除差集 (ToRemove = Old - New)：原先有，但新提交列表中没有的项,有的话取反不删除，没有的话取反删除。
            foreach (var link in links.Where(x => !ids.Contains(x.RoleId)))
            {
                _unitOfWork.UserRoles.Delete(link); //如果Ids里面没有 对应的RoleId，则删除这一条关系。有的话不执行删除保留。
            }

            // 6.2 构造旧角色 ID 的哈希集合，保证后续查找达到 O(1) 极致性能。oldIds是找到的用户角色表里面的角色id集合
            var oldIds = links.Select(x => x.RoleId).ToHashSet();

            // 6.3 计算待新增差集 (ToAdd = New - Old)：新提交列表中有，但原先没有的项。 如果有的话就不用动，如果没有就新增
            foreach (var roleId in ids.Where(x => !oldIds.Contains(x)))
            {
                await _unitOfWork.UserRoles.AddAsync(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 步骤 7：统一单事务提交入库，保证全量成功或全量失败
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="actorId"></param>
        /// <returns></returns>
        /// <exception cref="ValidationAppException"></exception>
        /// <exception cref="AppException"></exception>
        public async Task<AdminUserDto> UpdateUserAsync(long id, UpdateUserRequest request, long actorId)
        {
            // 步骤 1：入参合法性校验
            if (id <= 0)
            {
                throw new ValidationAppException("用户id 必须大于 0");
            }

            // 步骤 2：查询目标用户是否存在
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
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
            if (request.Status != 0 && request.Status != 1)
            {
                throw new ValidationAppException("用户状态只能是 0（禁用）或 1（启用）");
            }

            // 步骤 5：核心安全防护规则——禁止禁用自己或内置超级管理员 admin
            if (request.Status == 0 && (id == actorId || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase)))
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
        /// 对客户端提交的角色/权限 ID 集合执行防御性数据清洗
        /// </summary>
        /// <param name="request">请求 DTO</param>
        /// <returns>去重且校验后的合法 ID 数组</returns>
        private static long[] NormalizeIds(AssignIdsRequest request)
        {
            // 防线 A：空引用校验、单次数量上限（1000条）校验、必须为正整数校验
            if (request.Ids == null || request.Ids.Count > 1000 || request.Ids.Any(x => x <= 0))
            {
                throw new AppException("授权 ID 必须为正整数，且单次操作最多不超过 1000 项", 400);
            }

            // 防线 B：利用 Distinct() 消除重复 ID，并固化为长整型数组
            return request.Ids.Distinct().ToArray();
        }
    }
}
