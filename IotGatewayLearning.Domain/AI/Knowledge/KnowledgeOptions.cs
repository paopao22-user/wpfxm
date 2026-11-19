namespace IotGatewayLearning.Domain.AI.Knowledge
{
    /// <summary>
    /// 知识库配置。放在 Domain 是因为 Application 与 Infrastructure 都要用，
    /// 而 Infrastructure 不引用 Application。
    /// </summary>
    public sealed class KnowledgeOptions
    {
        public const string Section = "Knowledge";

        // 教学版文件来源目录；fileId 映射为 {RootPath}/{fileId}.txt。
        public string RootPath { get; set; } = "knowledge";

        // 单个导入文件上限，默认 2MB。
        public long MaxBytes { get; set; } = 2 * 1024 * 1024;

        // 单个文档提取文字上限，默认 10 万字符。
        public int MaxChars { get; set; } = 100000;

        // 切片大小与重叠：默认 800 / 100。
        public int ChunkSize { get; set; } = 800;

        public int ChunkOverlap { get; set; } = 100;

        // 最终送入模型的引用条数。
        public int FinalLimit { get; set; } = 5;

        // 模型回答超时，默认 120 秒。
        public int ModelTimeoutSeconds { get; set; } = 120;
    }
}
