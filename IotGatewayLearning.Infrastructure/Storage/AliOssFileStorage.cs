using Aliyun.OSS;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Storage
{
    /// <summary>
    /// IOssFileStorage 的【阿里云实现】：把文件真的传到阿里云 OSS 的 Bucket 里。
    ///
    /// 做法：拿到配置（Endpoint / Bucket / AK / SK）→ new 一个 OssClient → 调它的方法。
    /// </summary>
    public class AliOssFileStorage : IOssFileStorage
    {
        /// <summary>只允许这几种图片格式（白名单）</summary>
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        /// <summary>在 Bucket 里的哪个"目录"下（OSS 其实没有目录，只是 key 的前缀，看起来像目录）</summary>
        private const string Folder = "avatars";

        /// <summary>阿里云 OSS 的客户端（用它上传 / 算签名）</summary>
        private readonly OssClient _client;

        /// <summary>配置（Endpoint、Bucket 名、AK/SK、签名有效期）</summary>
        private readonly OssOptions _options;

        /// <summary>
        /// 构造函数参数由容器注入：
        ///   IOptions&lt;OssOptions&gt; 能拿到值，靠的是 Program.cs 里那句 Configure&lt;OssOptions&gt;(...)
        /// </summary>
        public AliOssFileStorage(IOptions<OssOptions> options)
        {
            _options = options.Value;

            // OssClient 的构造函数：OssClient(endpoint, accessKeyId, accessKeySecret)
            _client = new OssClient(_options.Endpoint, _options.AccessKeyId, _options.AccessKeySecret);
        }

        /// <summary>
        /// 上传：四步 —— ①查后缀 ②生成 objectKey ③传上去 ④把 objectKey 返回给调用方
        /// </summary>
        public async Task<string> UploadAsync(Stream stream, string originalFileName)
        {
            // ---------- ① 检查后缀名（白名单） ----------
            // 为什么必须查：不查的话别人能传 .exe / .html 这类文件，是安全隐患
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            var isAllowed = false;
            foreach (var allowed in AllowedExtensions)
            {
                if (allowed == extension)
                {
                    isAllowed = true;
                    break;
                }
            }

            if (isAllowed == false)
            {
                throw new InvalidOperationException(
                    "只允许上传这些格式的图片：" + string.Join("、", AllowedExtensions));
            }

            // ---------- ② 生成 objectKey（在 OSS 里的"路径 + 文件名"） ----------
            // 为什么用 Guid 当文件名：两个人可能都传 me.jpg，用原名会互相覆盖
            var objectKey = Folder + "/" + Guid.NewGuid().ToString("N") + extension;
            // 结果类似 avatars/8f3a1c2b9d7e4f5a8b6c0d1e2f3a4b5c.jpg

            // ---------- ③ 上传到 OSS ----------
            // ⚠️ 这个版本的 SDK【只有同步方法】（没有 PutObjectAsync），
            //    所以我们用 Task.Run 把它丢到后台线程去跑，避免卡住当前请求线程。
            await Task.Run(() =>
            {
                _client.PutObject(_options.BucketName, objectKey, stream);
            });

            // ---------- ④ 返回 objectKey（不是 URL！见接口注释） ----------
            return objectKey;
        }

        /// <summary>
        /// 生成"能直接访问的临时 URL"：由 SDK 在本地算出带签名的地址
        /// </summary>
        public string BuildSignedUrl(string objectKey)
        {
            var expiration = DateTime.Now.AddMinutes(_options.SignedUrlExpireMinutes);

            // SignHttpMethod.Get：这条签名 URL 只允许用来【读取】文件（不允许上传/删除）
            Uri uri = _client.GeneratePresignedUri(_options.BucketName, objectKey, expiration, SignHttpMethod.Get);

            return uri.ToString();
        }
    }
}
