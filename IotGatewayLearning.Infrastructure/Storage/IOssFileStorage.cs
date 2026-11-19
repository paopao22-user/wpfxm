using System.IO;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Storage
{
    /// <summary>
    /// 对象存储（阿里云 OSS）的能力清单。
    ///
    /// 【为什么要写成接口】
    ///   将来换存储（本地磁盘 / 七牛 / MinIO）时，Controller 一行都不用改 ——
    ///   它只认 IOssFileStorage 这个名字，不认背后是阿里云还是别的。
    ///   （和你在 WPF 里学的 IRoleApiClient / IDeviceApiClient 是同一个思路）
    ///
    /// 【为什么放在 Infrastructure】
    ///   它访问的是"外部系统"（对象存储），和 Repositories（数据库）属于同一类；
    ///   而 Application 层放的是业务规则（UserService / RoleService）。
    /// </summary>
    public interface IOssFileStorage
    {
        /// <summary>
        /// 上传一个文件到 OSS，返回它在 OSS 里的【对象名 objectKey】，例如 avatars/8f3a1c2b...jpg
        ///
        /// ⚠️ 注意返回的是 objectKey，【不是】能直接点开的 URL。
        ///    原因：Bucket 是私有的，能访问的 URL 是"临时签名 URL"、会过期。
        ///    如果把签名 URL 存进数据库，过一段时间头像就打不开了。
        ///    所以数据库里存 objectKey，要显示时用 BuildSignedUrl 现签一个。
        /// </summary>
        /// <param name="stream">文件内容流（从上传的 HTTP 请求里拿）</param>
        /// <param name="originalFileName">用户上传时的原文件名，如 me.jpg（只取它的后缀名）</param>
        Task<string> UploadAsync(Stream stream, string originalFileName);

        /// <summary>
        /// 把一个 objectKey 变成"能直接访问的临时 URL"（带签名，有效期见配置 SignedUrlExpireMinutes）。
        /// 这是纯计算（本地算签名），不会发网络请求。
        /// </summary>
        string BuildSignedUrl(string objectKey);
    }
}
