using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Common.Exceptions;

namespace IotGatewayLearning.Application.AI
{
    public sealed partial class AiAnalysisService
    {
        // 查询自己的记录；beforeId 是可选游标，返回 20 条及下一页位置。
        public async Task<AiAnalysisPage> ListAsync(
            string deviceCode,  // 设备编码：要查哪台设备的记录
            long? beforeId,     //  上一页最后一条的 ID
            string actor,       // 当前登录用户名
            CancellationToken ct)  // 取消令牌
        {
            // 1.先校验，再查库
            actor = RequireActor(actor);    // 校验当前用户标识，防止匿名或超长用户名写入分析记录
            var device = Code(deviceCode, 50);  // 清除首尾空格并校验长度；设备编码上限 50，点位编码上限 80（与数据库一致）

            // beforeId: 上一页最后一条的 ID
            if (beforeId is <= 0)           // 游标必须大于0，0或负数表示无效请求
                throw new AppException("分页游标必须大于0", 400);

            // 2.查询数据库，获取指定用户和设备的分析记录列表，支持分页
            var rows = await _repository.ListAsync(actor, device, beforeId, ct);    // 查询数据库，获取指定用户和设备的分析记录列表，支持分页

            var items = rows
                .Take(20)           // 只保留前 20 条。第 21 条被丢掉，不展示给用户
                .Select(x => new AiAnalysisRow(
                    x.Id,
                    x.RequestId,
                    x.DeviceCode,
                    x.PointCode,
                    x.Status,
                    x.ModelAttempted,
                    Utc(x.CreatedAt)))
                .ToList();

            // 第 21 条只判断是否还有下一页，不在当前页显示。
            return new AiAnalysisPage(items, rows.Count > 20 ? items[^1].Id : null);
        }
    }
}
