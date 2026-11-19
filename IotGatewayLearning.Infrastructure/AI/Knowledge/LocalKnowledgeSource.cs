using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.AI.Knowledge;
using Microsoft.Extensions.Options;

namespace IotGatewayLearning.Infrastructure.AI.Knowledge
{
    /// <summary>
    /// 教学版文件来源：把 fileId 映射到 {RootPath}/{fileId}.txt。
    /// 项目当前没有文件中心元数据表，因此无法校验“文件属于该用户和设备”；
    /// 这里只做路径、大小与编码校验，接口保留将来接 OSS/文件中心的位置。
    /// </summary>
    public sealed class LocalKnowledgeSource : IKnowledgeSource
    {
        private readonly KnowledgeOptions _options;

        public LocalKnowledgeSource(IOptions<KnowledgeOptions> options) => _options = options.Value;

        public async Task<KnowledgeSource> ReadAsync(
            long fileId,
            long deviceId,
            string actor,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(actor))
                throw new AppException("身份无效", 401);

            var root = Path.GetFullPath(_options.RootPath, AppContext.BaseDirectory);
            var path = Path.Combine(root, fileId.ToString() + ".txt");

            if (!File.Exists(path))
                throw new AppException("文件不存在", 404);

            var info = new FileInfo(path);
            if (info.Length > _options.MaxBytes)
                throw new AppException("教学导入文件最多2MB", 400);

            var bytes = await File.ReadAllBytesAsync(path, ct);
            return new KnowledgeSource(bytes, "text/plain");
        }
    }
}
