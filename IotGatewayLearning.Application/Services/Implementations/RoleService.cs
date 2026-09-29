using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Implementations
{
    /// <summary>
    /// 角色领域业务服务实现类
    /// </summary>
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        // 构造函数注入工作单元
        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 规范化并校验角色编码（去除空格、转小写、正则匹配）
        /// </summary>
        private static string NormalizeRoleCode(string code)
        {
            // 1. 去除首尾空格并强制转为纯小写
            var result = code.Trim().ToLowerInvariant();

            // 2. 空白检查
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new ValidationAppException("角色编码不能为空");
            }

            // 3. 长度防御
            if (result.Length > 50)
            {
                throw new ValidationAppException("角色编码长度不能超过50个字符");
            }

            // 4. 正则约束：必须以小写字母开头，只允许小写字母、数字、下划线、横线
            if (!Regex.IsMatch(result, "^[a-z][a-z0-9_-]*$"))
            {
                throw new ValidationAppException("角色编码只能由小写字母、数字、下划线和连字符组成，且必须以小写字母开头");
            }

            return result;
        }


        /// <summary>
        /// 规范化并校验角色显示名称
        /// </summary>
        private static string NormalizeRoleName(string name)
        {
            var result = name.Trim();

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new ValidationAppException("角色名称不能为空");
            }

            if (result.Length > 100)
            {
                throw new ValidationAppException("角色名称长度不能超过 100 个字符");
            }

            return result;
        }


        /// <summary>
        /// 校验角色状态范围
        /// </summary>
        private static void ValidateStatus(sbyte status)
        {
            if (status != 0 && status != 1)
            {
                throw new ValidationAppException("角色状态只能是 0（停用）或 1（启用）");
            }
        }


        /// <summary>
        /// 将 Role 领域实体映射为安全的 AdminRoleDto 数据传输对象
        /// </summary>
        private static AdminRoleDto MapRole(Role role)
        {
            return new AdminRoleDto
            {
                Id = role.Id,

                Code = role.Code,

                Name = role.Name,

                Status = role.Status
            };
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

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<AdminRoleDto> CreateRoleAsync(SaveRoleRequest request)
        {
            // 步骤 1：格式校验与状态
            var code = NormalizeRoleCode(request.Code);

            var name = NormalizeRoleName(request.Name);

            ValidateStatus(request.Status);

            // 步骤 2：核心安全规则 —— 检查角色编码 Code 是否重复
            bool exists = await _unitOfWork.Roles.AnyAsync(x => x.Code == code);

            if (exists)
            {
                throw new AppException("角色编码已存在", 409);
            }

            // 步骤 3：根据领域模型实例化 Role 实体
            var role = new Role
            {
                Code = code,

                Name = name,

                Status = request.Status,

                CreatedAt = DateTime.UtcNow
            };

            // 步骤 4：通过通用泛型仓储将实体加入变更追踪器（状态标记为 Added）
            await _unitOfWork.Roles.AddAsync(role);

            // 步骤 5：提交事务，向 MySQL 发送物理 INSERT INTO 语句
            await _unitOfWork.SaveChangesAsync();

            // 步骤 6：脱敏返回最新的角色 DTO（此时 role.Id 已由数据库自增主键自动回填）
            return MapRole(role);
        }

        /// <summary>
        /// 软删除角色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task DeleteRoleAsync(long id)
        {
            // 防线 1：入参 ID 防御性校验（数据库自增主键必然从 1 开始）
            if (id <= 0)
            {
                throw new ValidationAppException("角色id必须大于0");
            }

            // 防线 2：查询待删除的目标角色是否存在
            var role = await _unitOfWork.Roles.GetByIdAsync(id);
            if (role == null)
            {
                throw new AppException("角色不存在", 404);
            }

            // 防线 3：核心安全红线 —— 内置超级管理员 admin 终身禁止删除
            if (role.Code.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new AppException("内置管理员角色不能删除", 409);
            }

            // 防线 4：引用完整性检查 —— 若该角色当前已分配给用户，禁止直接删除
            bool assigned = await _unitOfWork.UserRoles.AnyAsync(x => x.RoleId == id);

            if (assigned)
            {
                throw new AppException("该角色已被分配给用户，不能删除", 409);
            }

            // 防线 5：幂等性防重检查 —— 若已处于停用/软删除状态，直接拦截
            if (role.Status == 0)
            {
                throw new AppException("该角色已被删除或停用", 400);
            }

            // 步骤 6：执行逻辑假删除：修改内存状态为 0（停用）
            role.Status = 0;

            // 步骤 7：通知仓储追踪器将其标记为已修改 (Modified)
            _unitOfWork.Roles.Update(role);

            // 步骤 8：提交事务，向 MySQL 发送物理 UPDATE 语句
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 通过角色id获取角色详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<AdminRoleDto?> GetRoleByIdAsync(long id)
        {
            // 1. 调用工作单元通用角色仓储，根据主键查询实体
            var role = await _unitOfWork.Roles.GetByIdAsync(id);

            // 2. 若数据库未查到匹配记录，直接返回 null
            if (role == null)
            {
                return null;
            }


            // 3. 查到实体后，调用纯函数 MapRole 转换为对外暴露的 DTO 模型
            return MapRole(role);
        }

        /// <summary>
        /// 根据角色主键 ID 查询当前已分配的权限 ID 集合
        /// </summary>
        public async Task<List<long>> GetRolePermissionIdsAsync(long roleId)
        {
            // 防线 1：参数基础校验
            if (roleId <= 0)
            {
                throw new AppException("角色id必须大于0", 400);
            }

            // 防线 2：验证目标角色是否存在
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);

            if (role == null)
            {
                throw new AppException("目标角色不存在", 404);
            }

            // 步骤 3：从角色-权限中间仓储中查询属于该角色的全部关联行
            var rolePermissions = await _unitOfWork.RolePermissions.FindAllAsync(x => x.RoleId == roleId);

            // 步骤 4：通过 LINQ 投影 (Select) 只提取权限 ID，并按升序规整输出
            return rolePermissions.Select(x => x.PermissionId)
                                .OrderBy(permissionId => permissionId)
                                .ToList();
        }

        /// <summary>
        /// 获取角色列表（支持根据关键字对 Code 和 Name 进行模糊匹配）
        /// </summary>
        public async Task<List<AdminRoleDto>> GetRolesAsync(string? keyword)
        {
            List<Role> roles;

            // 1. 若没有传入搜索关键字，直接全量查询所有角色
            if (string.IsNullOrWhiteSpace(keyword))
            {
                roles = await _unitOfWork.Roles.GetAllAsync();
            }

            // 2. 若传入了关键字，去除首尾空格并在 Code 或 Name 中进行模糊匹配
            else
            {
                string key = keyword.Trim();

                roles = await _unitOfWork.Roles.FindAllAsync(x => x.Code.Contains(key) || x.Name.Contains(key));
            }

            // 3. 按照角色主键 Id 升序排列，逐一映射为 DTO 并返回
            return roles.OrderBy(x => x.Id)
                .Select(MapRole)
                .ToList();
        }

        /// <summary>
        /// 为指定角色完整分配/替换权限集合（基于差集算法实现幂等更新）
        /// </summary>
        public async Task SetRolePermissionsAsync(long roleId, AssignIdsRequest request)
        {
            // 防线 1：参数合法性与目标角色存在性检查
            if (roleId <= 0)
            {
                throw new AppException("角色id必须大于0", 400);
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
            if (role == null)
            {
                throw new AppException("目标权限没有找到", 404);
            }

            // 防线 2：核心安全红线 —— 系统内置 admin 超管角色的权限受底层保护，禁止在此处修改
            if (role.Code.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new AppException("系统内置管理员角色的权限受保护，不可在此修改", 409);
            }

            // 防线 3：执行数据清洗（去重、正数检查、空数组合法化）
            var ids = NormalizeIds(request);

            // 防线 4：权限全量合法性校验 —— 确保所提交的权限在系统中全部存在且处于启用状态 (Status == 1)
            if (ids.Length > 0)
            {
                var availablePermissions = await _unitOfWork.Permissions.FindAllAsync(x => ids.Contains(x.Id) && x.Status == 1);

                if (availablePermissions.Count != ids.Length)
                {
                    throw new AppException("提交的权限列表中包含不存在或已被停用的权限", 400);
                }
            }

            // 步骤 5：查询该角色在数据库中现有的所有权限关联
            var links = await _unitOfWork.RolePermissions.FindAllAsync(x => x.RoleId == roleId);

            // 步骤 6：差集比对与内存标记
            // 6.1 计算待删除差集 (ToRemove = Old - New)：原先有，但新提交列表中没有的项
            foreach (var link in links.Where(x => !ids.Contains(x.PermissionId)))
            {
                _unitOfWork.RolePermissions.Delete(link);
            }

            // 6.2 构造旧权限 ID 的哈希集合，保证后续查找达到 O(1) 极致性能
            var oldIds = links.Select(x => x.PermissionId).ToHashSet();

            // 6.3 计算待新增差集 (ToAdd = New - Old)：新提交列表中有，但原先没有的项
            foreach (var permissionId in ids.Where(x => !oldIds.Contains(x)))
            {
                await _unitOfWork.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 步骤 7：统一单事务提交入库，保证原子一致性
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<AdminRoleDto> UpdateRoleAsync(long id, SaveRoleRequest request)
        {
            // 步骤 1：入参 ID 合法性防御
            if (id <= 0)
            {
                throw new ValidationAppException("角色id 必须大于0");
            }

            // 步骤 2：查询待修改的目标角色是否存在
            var role = await _unitOfWork.Roles.GetByIdAsync(id);
            if (role == null)
            {
                throw new AppException("角色不存在", 404);
            }

            // 步骤 3：对传入的新数据进行规范化清洗与格式校验
            var newCode = NormalizeRoleCode(request.Code);
            var newName = NormalizeRoleName(request.Name);
            ValidateStatus(request.Status);

            // 步骤 4：核心安全红线 1 —— 保护内置超级管理员 admin
            bool isAdmin = role.Code.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            if (isAdmin && (newCode != "admin" || request.Status == 0))
            {
                throw new AppException("内置管理员角色不能修改编码或禁用", 409);
            }

            // 步骤 5：判断角色编码 Code 是否真正发生了改变（必须取反：相等表示没变，不相等才表示发生改变）
            bool codeChanged = !role.Code.Equals(newCode, StringComparison.OrdinalIgnoreCase);


            // 步骤 6：如果角色编码发生了变化，执行两项严苛的安全查验
            if (codeChanged)
            {
                // 查验 A：检查该角色当前是否已经分配给了具体用户
                bool assigned = await _unitOfWork.UserRoles.AnyAsync(x => x.RoleId == id);
                if (assigned)
                {
                    throw new AppException("角色已分配给用户，不能修改角色编码", 409);
                }

                // 查验 B：排除自身主键 ID 后的编码防重查询
                bool duplicated = await _unitOfWork.Roles.AnyAsync(x => x.Code == newCode && x.Id != id);
                if (duplicated)
                {
                    throw new AppException("角色编码已存在", 409);
                }
            }

            // 步骤 7：更新实体的内存属性
            role.Code = newCode;
            role.Name = newName;
            role.Status = request.Status;

            // 步骤 8：显式通知仓储标记为已修改 (Modified)
            _unitOfWork.Roles.Update(role);

            // 步骤 9：提交事务，向 MySQL 发送物理 UPDATE 语句
            await _unitOfWork.SaveChangesAsync();

            // 步骤 10：脱敏返回最新的角色 DTO
            return MapRole(role);
        }
    }
}
