using IotGatewayLearning.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    public interface IAdminService
    {
        /// <summary>
        /// 根据目标用户id来查找用户
        /// </summary>
        /// <param name="targetUserId"></param>
        /// <returns></returns>
        Task<AdminUserDto?> GetUserByIdAsync(long targetUserId);

        /// <summary>
        /// 根据keyword查找到所有包含keyword的List集合
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        Task<List<AdminUserDto>> GetUsersAsync(string? keyword);

        /// <summary>
        /// 新增管理员用户
        /// </summary>
        /// <param name="request">新增用户的入参模型</param>
        /// <returns>创建成功后脱敏的用户 DTO（包含生成的自增 ID）</returns>
        Task<AdminUserDto> CreateUserAsync(CreateUserRequest request);


        /// <summary>
        /// 修改用户基础资料与状态
        /// </summary>
        /// <param name="id">要修改的目标用户 ID</param>
        /// <param name="request">修改的数据内容</param>
        /// <param name="actorId">当前执行操作的操作者用户 ID</param>
        /// <returns>修改成功后的用户脱敏 DTO</returns>
        Task<AdminUserDto> UpdateUserAsync(long id,UpdateUserRequest request, long actorId);

        /// <summary>
        /// 删除指定用户（含安全防护规则）
        /// </summary>
        /// <param name="id">要删除的目标用户 ID</param>
        /// <param name="actorId">当前执行操作的操作者用户 ID</param>
        Task DeleteUserAsync(long id, long actorId);


    }
}
