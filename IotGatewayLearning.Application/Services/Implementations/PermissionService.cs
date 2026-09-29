using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Common.Security;
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
    /// 权限领域业务服务实现类
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        // 构造函数注入工作单元
        public PermissionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 规范化并校验权限编码（去除空格、转小写、严格执行 module:action 正则匹配）
        /// </summary>
        private static string NormalizePermissionCode(string code)
        {
            // 1. 去除首尾空白并强制转换为纯小写
            var result = code.Trim().ToLower();

            // 2. 非空防御
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new ValidationAppException("权限编码不能为空");
            }

            // 3. 长度防御（与数据库物理字段 varchar(100) 对齐）
            if (result.Length > 100)
            {
                throw new ValidationAppException("权限编码长度不能超过 100 个字符");
            }

            // 4. 正则约束：必须以小写字母开头，中间有且仅有一个冒号，冒号前后只允许小写字母、数字、下划线和连字符
            if (!Regex.IsMatch(result, "^[a-z][a-z0-9_-]*:[a-z0-9_-]+$"))
            {
                throw new ValidationAppException("权限编码格式必须为 module:action（如 device:read），且只能由小写字母、数字、下划线、连字符和冒号组成");
            }

            return result;
        }


        /// <summary>
        /// 规范化并校验权限名称
        /// </summary>
        private static string NormalizePermissionName(string name)
        {
            var result = name.Trim();

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new ValidationAppException("权限名称不能为空");
            }

            if (result.Length > 100)
            {
                throw new ValidationAppException("权限名称长度不能超过 100 个字符");
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
        /// 纯映射函数：负责将内部 Permission 实体安全转换为对外的 AdminPermissionDto
        /// </summary>
        private static AdminPermissionDto MapPermission(Permission permission)
        {
            return new AdminPermissionDto
            {
                Id = permission.Id,

                Code = permission.Code,

                Name = permission.Name,

                Description = permission.Description,

                Status = permission.Status
            };
        }

        /// <summary>
        /// 新增权限
        /// </summary>
        /// <param name="request">保存权限请求模型</param>
        /// <returns>新增成功后的权限 DTO</returns>
        public async Task<AdminPermissionDto> CreatePermissionAsync(SavePermissionRequest request)
        {
            // 步骤 1：格式规范化清洗与状态范围校验
            var code = NormalizePermissionCode(request.Code);
            var name = NormalizePermissionName(request.Name);
            ValidateStatus(request.Status);  // 复用已有的状态校验（只能是 0 或 1）

            // 步骤 2：核心安全防重 —— 查验权限编码 Code 是否已在数据库存在
            bool exists = await _unitOfWork.Permissions.AnyAsync(x => x.Code == code);
            if (exists)
            {
                throw new AppException("权限编码已存在", 409);
            }

            // 步骤 3：根据领域模型实例化 Permission 实体
            var permission = new Permission
            {
                Code = code,
                Name = name,
                Description = request.Description?.Trim(),
                Status = request.Status
            };

            // 步骤 4：通过通用泛型仓储将实体加入变更追踪器（状态标记为 Added）
            await _unitOfWork.Permissions.AddAsync(permission);

            // 步骤 5：提交事务，向 MySQL 发送物理 INSERT 语句
            await _unitOfWork.SaveChangesAsync();

            // 步骤 6：脱敏返回最新的权限 DTO（此时 permission.Id 已由数据库自动回填）
            return MapPermission(permission);
        }

        /// <summary>
        /// 软删除指定权限（逻辑删除：修改 Status 为 0）
        /// </summary>
        /// <param name="id">权限主键 ID</param>
        public async Task DeletePermissionAsync(long id)
        {
            // 防线 1：入参 ID 合法性防御（自增主键必然从 1 开始）
            if (id <= 0)
            {
                throw new ValidationAppException("权限id必须大于0");
            }

            // 防线 2：查询待删除的目标权限是否存在
            var permission = await _unitOfWork.Permissions.GetByIdAsync(id);
            if (permission == null)
            {
                throw new AppException("权限不存在", 404);
            }

            // 防线 3：核心安全红线 1 —— 系统核心内置权限终身禁止删除
            bool isBuiltIn = permission.Code.Equals(PermissionCodes.DeviceRead, StringComparison.OrdinalIgnoreCase)
                || permission.Code.Equals(PermissionCodes.DeviceControl, StringComparison.OrdinalIgnoreCase)
                || permission.Code.Equals(PermissionCodes.UserManage, StringComparison.OrdinalIgnoreCase);

            if (isBuiltIn)
            {
                throw new AppException("系统内置权限不能删除", 409);
            }

            // 防线 4：核心安全红线 2 —— 引用完整性检查（若已分配给角色，严禁直接删除）
            bool assigned = await _unitOfWork.RolePermissions.AnyAsync(x => x.PermissionId == id);
            if (assigned)
            {
                throw new AppException("该权限已被分配给角色，不能删除，请先在角色管理中解除关联", 409);
            }

            // 防线 5：幂等性防重检查 —— 若已处于停用/软删除状态，直接拦截
            if (permission.Status == 0)
            {
                throw new AppException("该权限已被停用或者删除", 409);
            }

            // 步骤 6：执行逻辑软删除：修改内存状态为 0（停用）
            permission.Status = 0;

            // 步骤 7：通知仓储追踪器将其标记为已修改 (Modified)
            _unitOfWork.Permissions.Update(permission);

            // 步骤 8：提交事务，向 MySQL 发送物理 UPDATE 语句
            await _unitOfWork.SaveChangesAsync();
        }


        /// <summary>
        /// 通过权限主键 ID 获取权限详情
        /// </summary>
        /// <param name="id">权限 ID</param>
        /// <returns>脱敏后的 DTO 或 null</returns>
        public async Task<AdminPermissionDto?> GetPermissionByIdAsync(long id)
        {
            // 1. 调用工作单元中通用权限仓储的主键查询方法
            var permission = await _unitOfWork.Permissions.GetByIdAsync(id);

            // 2. 若数据库未查到匹配记录，直接返回 null
            if (permission == null)
            {
                return null;
            }

            // 3. 查到实体后，复用上一节写好的 MapPermission 纯函数完成转换
            return MapPermission(permission);
        }

        /// <summary>
        /// 获取权限列表：（支持根据关键字对 Code 和 Name 进行模糊匹配）
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<List<AdminPermissionDto>> GetPermissionsAsync(string? keyword)
        {
            List<Permission> permissions;

            // 1. 若没有传入搜索关键字，直接全量查询所有权限记录
            if (string.IsNullOrWhiteSpace(keyword))
            {
                permissions = await _unitOfWork.Permissions.GetAllAsync();
            }

            // 2. 若传入了关键字，去除首尾多余空格，并在 Code 或 Name 中执行模糊匹配
            else
            {
                string key = keyword.Trim();

                permissions = await _unitOfWork.Permissions.FindAllAsync(x => x.Name.Contains(key) || x.Code.Contains(key));
            }


            // 3. 按照权限主键 Id 升序排列，逐一转换为 DTO 并返回
            return permissions.OrderBy(x => x.Id)
                .Select(MapPermission)
                .ToList();
        }

        /// <summary>
        /// 获取层级权限树
        /// </summary>
        public async Task<List<PermissionNodeDto>> GetPermissionTreeAsync()
        {
            // 步骤 1：从仓储只拉取所有处于“启用状态 (Status == 1)”的有效权限
            var permissions = await _unitOfWork.Permissions.FindAllAsync(x => x.Status == 1);

            // 步骤 2：建立模块英文代码到中文友好名称的字典映射
            var moduleNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "device", "设备管理" },
                { "alarm", "告警中心" },
                { "user", "用户与安全" },
                { "report", "报表分析" }
            };

            // 步骤 3：利用 LINQ 提取冒号前半部分进行分组，并按模块名升序排序
            var groups = permissions.GroupBy(x => x.Code.Split(':')[0].ToLowerInvariant())
                                    .OrderBy(g => g.Key);

            var tree = new List<PermissionNodeDto>();
            // 步骤 4：双层循环组装层级树（第一层是模块父节点，第二层是权限叶子节点）
            foreach (var group in groups)    // 这里的 groups 是一个“桶集合”，比如里面有两个桶：device 桶、alarm 桶
            {
                var moduleKey = group.Key;  // 1. group.Key 就是这个桶的名字，比如 "device" 

                // 2. 查字典把 "device" 翻译成中文 "设备管理"
                var moduleLabel = moduleNames.TryGetValue(moduleKey, out var cnName) ? cnName : $"【{moduleKey}】模块";

                // 4.1 构造虚拟分组父节点（注意：父节点绝对没有 PermissionId）
                var parentNode = new PermissionNodeDto      // 3. 在内存中创建一个崭新的“文件夹”（父节点对象）
                {
                    Key = $"group:{moduleKey}", // 节点唯一标记，比如 "group:device"
                    Label = moduleLabel,        // 界面显示的标题，比如 "设备管理"
                    PermissionId = null,        // 文件夹不是真实权限，所以没有数据库 ID
                    Code = moduleKey,           // 模块标识
                    IsGroup = true              // 明确告诉前端：我是一个分类文件夹！
                };

                // 4.2 将该模块下的所有真实权限作为子节点填充入 Children
                foreach (var perm in group.OrderBy(x => x.Id))   // 这里的 group 本身就是一个装满该模块权限的集合！
                {
                    parentNode.Children.Add(new PermissionNodeDto
                    {
                        Key = $"perm:{perm.Id}",            // 唯一标识，如 "perm:1"
                        Label = $"{perm.Name} ({perm.Code})",    // 界面显示友好名称，如 "查看设备 (device:read)"
                        PermissionId = perm.Id,                 // 【关键】数据库真实主键 ID！
                        Code = perm.Code,                       // 权限编码
                        IsGroup = false                 // 明确告诉前端：我不是文件夹，我是真实权限！
                    });
                }

                // 4. 当子节点全部装满后，把这个父节点放进最终列表
                tree.Add(parentNode);
            }

            return tree;
        }

        /// <summary>
        /// 修改权限
        /// </summary>
        /// <param name="id">要被修改权限的id</param>
        /// <param name="request">修改的内容</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<AdminPermissionDto> UpdatePermissionAsync(long id, SavePermissionRequest request)
        {
            // 步骤 1：入参 ID 合法性防御（主键必然从 1 开始）
            if (id <= 0)
            {
                throw new ValidationAppException("权限id必须大于0");
            }

            // 步骤 2：查询待修改的目标权限是否存在
            var permission = await _unitOfWork.Permissions.GetByIdAsync(id);
            if (permission == null)
            {
                throw new AppException("权限不存在", 404);
            }

            // 步骤 3：对传入的新数据进行规范化清洗与格式校验
            var newCode = NormalizePermissionCode(request.Code);
            var newName = NormalizePermissionName(request.Name);
            ValidateStatus(request.Status);

            // 步骤 4：核心安全红线 1 —— 保护系统核心内置权限（与代码中的 Policy 严格锚定）
            bool isBuiltIn = permission.Code.Equals(PermissionCodes.DeviceRead, StringComparison.OrdinalIgnoreCase)
                || permission.Code.Equals(PermissionCodes.DeviceControl, StringComparison.OrdinalIgnoreCase)
                || permission.Code.Equals(PermissionCodes.UserManage, StringComparison.OrdinalIgnoreCase);

            if (isBuiltIn && (!newCode.Equals(permission.Code, StringComparison.OrdinalIgnoreCase) || request.Status == 0))
            {
                throw new AppException("系统核心内置权限不能修改编码或禁用", 409);
            }

            // 步骤 5：判断权限编码 Code 是否真正发生了改变
            bool codeChanged = !permission.Code.Equals(newCode, StringComparison.OrdinalIgnoreCase);

            // 步骤 6：如果权限编码发生了改变，执行两项严苛的安全查验
            if (codeChanged)
            {
                // 查验 A：检查该权限当前是否已经分配给了某个角色（引用完整性保护）
                bool assigned = await _unitOfWork.RolePermissions.AnyAsync(x => x.PermissionId == id);
                if (assigned)
                {
                    throw new AppException("权限已经被分配", 409);
                }

                // 查验 B：排除自身主键 ID 后的编码防重查询（唯一索引保护）
                bool duplicated = await _unitOfWork.Permissions.AnyAsync(x => x.Code == newCode && x.Id != id);
                if (duplicated)
                {
                    throw new AppException("权限编码已存在", 409);
                }
            }

            // 步骤 7：更新实体的内存属性
            permission.Code = newCode;
            permission.Name = newName;
            permission.Description = request.Description?.Trim();
            permission.Status = request.Status;

            // 步骤 8：显式通知仓储标记为已修改 (Modified)
            _unitOfWork.Permissions.Update(permission);

            // 步骤 9：提交事务，向 MySQL 发送物理 UPDATE 语句
            await _unitOfWork.SaveChangesAsync();

            // 步骤 10：脱敏返回最新的权限 DTO
            return MapPermission(permission);

        }
    }
}
