using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IotGatewayLearningApi.Controllers
{
    /// <summary>
    /// 头像上传（对象存储）相关接口。
    ///
    /// ⚠️ 这里只要求【登录】（[Authorize]），不要求 user:manage ——
    ///    因为"改自己的头像"是每个登录用户都该有的能力；
    ///    而用户管理那些接口（增/删/改用户）才需要 user:manage。
    /// </summary>
    [ApiController]
    [Route("api/oss")]
    [Authorize]
    public class OssController : ControllerBase
    {
        private readonly IOssFileStorage _ossFileStorage;
        private readonly IUserService _userService;

        public OssController(IOssFileStorage ossFileStorage, IUserService userService)
        {
            _ossFileStorage = ossFileStorage;
            _userService = userService;
        }

        /// <summary>
        /// 上传头像：接收表单里的文件 → 传到 OSS → 记到当前用户头上 → 返回可显示的 URL。
        ///
        /// ⚠️ 两件事特别注意：
        ///   ① [FromForm] IFormFile file 的【参数名 file】必须和前端表单里的字段名一致，
        ///      不一致的话 file 会是 null，报"请选择图片文件"（而且不报编译错）
        ///   ② 用户身份【从 JWT 里拿】，绝不能让前端传 userId ——
        ///      否则任何登录用户改个数字就能换掉别人的头像
        /// </summary>
        [HttpPost("upload-avatar")]
        public async Task<ActionResult<ApiResponse<string>>> UploadAvatar(IFormFile file)
        {
            // ---------- ① 校验文件 ----------
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<string> { Code = 400, Message = "请选择图片文件" });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new ApiResponse<string> { Code = 400, Message = "图片不能超过 5MB" });
            }

            // ---------- ② 当前登录用户：从 JWT 的 Claim 里取 ----------
            var userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            long userId;
            if (long.TryParse(userIdText, out userId) == false)
            {
                return Unauthorized(new ApiResponse<string> { Code = 401, Message = "无法识别当前用户" });
            }

            // ---------- ③ 上传到 OSS，拿到 objectKey ----------
            string objectKey;
            using (var stream = file.OpenReadStream())
            {
                objectKey = await _ossFileStorage.UploadAsync(stream, file.FileName);
            }

            // ---------- ④ 把 objectKey 记到 users.avatar_url ----------
            await _userService.SaveAvatarKeyAsync(userId, objectKey);

            // ---------- ⑤ 返回"能直接显示的临时 URL"给前端 ----------
            var url = _ossFileStorage.BuildSignedUrl(objectKey);

            return Ok(new ApiResponse<string>
            {
                Code = 200,
                Message = "头像上传成功",
                Data = url
            });
        }
    }
}
