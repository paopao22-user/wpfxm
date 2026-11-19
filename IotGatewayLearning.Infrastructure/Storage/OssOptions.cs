using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Storage
{
    /// <summary>
    /// 阿里云 OSS 的配置（对应 appsettings.Development.json 里的 "AliOss" 段）。
    ///
    /// 【它是什么】
    ///   一个"纯粹装数据的类"——把 json 里的 5 个值，变成 C# 对象的 5 个属性。
    ///   这样代码里就能写 _options.BucketName，而不是到处去读 json 字符串。
    ///
    /// 【属性名必须和 json 里的 key 对上】（大小写不敏感，但拼错读不到 —— 而且【不报错】，只会是空字符串）
    ///
    /// 【为什么放在 Infrastructure 层】
    ///   它是"配置 + 外部系统访问"的东西，和 Repositories（数据库）同一类；
    ///   而 Application 层放的是业务规则（UserService / RoleService）。
    /// </summary>
    public class OssOptions
    {
        /// <summary>地域地址，如 oss-cn-shenzhen.aliyuncs.com</summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>Bucket 名称，如 iot-wpf-slbccpp</summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>RAM 子账号的 AccessKeyId</summary>
        public string AccessKeyId { get; set; } = string.Empty;

        /// <summary>RAM 子账号的 AccessKeySecret</summary>
        public string AccessKeySecret { get; set; } = string.Empty;

        /// <summary>签名 URL 的有效期（分钟）。Bucket 是私有的，图片地址是临时签名的</summary>
        public int SignedUrlExpireMinutes { get; set; } = 30;
    }
}
