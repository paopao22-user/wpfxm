using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Common.Exceptions;

namespace IotGatewayLearning.Application.AI
{
    public sealed partial class AiAnalysisService
    {
        // id 定位记录，actor 限制归属；读取详情不重新分析。
        public async Task<AiAnalysisDetail> GetAsync(
            long id,
            string actor,
            CancellationToken ct)
        {
            actor = RequireActor(actor);        // 校验已认证用户标识，防止匿名或超长用户名写入分析记录。

            if (id <= 0)
                throw new AppException("分析ID必须大于0", 400);

            // 查询数据库，按 ID 和归属查找记录；不存在或无权访问均返回 null
            var row = await _repository.FindAsync(id, actor, ct)        // 同时校验记录 ID 与归属；不存在或无权访问均返回 null
                ?? throw new AppException("分析记录不存在或无权访问", 404);

            // 只返回已保存的输入和回答，刷新详情不会再次消耗模型额度。
            return Detail(row);
        }
    }
}
