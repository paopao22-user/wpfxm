using System.Threading;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.AI
{
    // AI 分析应用服务契约；实现在 AiAnalysisService（partial 分文件）。
    public interface IAiAnalysisService
    {
        // 44.4 连接检查：固定文字，确认服务端能调用模型；不读设备、不写记录。
        Task<AiConnectionResult> TestConnectionAsync(CancellationToken ct);

        // 44.5 发起分析：requestId 幂等，返回已保存的详情。
        Task<AiAnalysisDetail> CreateAsync(
            CreateAiAnalysisRequest request,
            string actor,
            CancellationToken ct);

        // 44.6 查询我的记录：beforeId 游标分页，actor 限定创建者。
        Task<AiAnalysisPage> ListAsync(
            string deviceCode,
            long? beforeId,
            string actor,
            CancellationToken ct);

        // 44.7 查看详情：只读已保存快照，不重新调模型。
        Task<AiAnalysisDetail> GetAsync(
            long id,
            string actor,
            CancellationToken ct);
    }
}
