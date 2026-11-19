using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Infrastructure.AI
{
    // 配置由 WebAPI 读取；密钥不会通过任何响应 DTO 返回给 WPF。
    public sealed class AiModelOptions
    {
        // 配置节点名对应 appsettings 中的 AiModel 对象。
        public const string Section = "AiModel";

        // 模型基础地址；客户端自行追加具体接口路径。
        [Required]
        public string Endpoint { get; set; } = "https://api.deepseek.com";

        // DeepSeek 接受的模型标识，不是自定义的助手显示名称。
        [Required, StringLength(100)]
        public string ModelId { get; set; } = "deepseek-flash";

        // 由 User Secrets 或环境变量注入，不写入业务响应和数据库。
        [Required]
        public string ApiKey { get; set; } = "";

        // 一次模型调用的等待上限，单位为秒。
        [Range(10, 180)]
        public int TimeoutSeconds { get; set; } = 120;

        // 输出 Token 预算；不是汉字数，也不代表模型准确率。
        [Range(1024, 32768)]
        public int MaxOutputTokens { get; set; } = 8192;
    }
}
