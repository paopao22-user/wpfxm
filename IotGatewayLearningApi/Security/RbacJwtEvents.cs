using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace IotGatewayLearningApi.Security
{
    public sealed class RbacJwtEvents:JwtBearerEvents
    {
        private readonly IUnitOfWork _unitOfWork;

        public RbacJwtEvents(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 验真后，刷新本次权限:  这里是已经验证过token令牌可信了，查最新权限
        /// TokenValidatedContext context： 本次 JWT 验证事件的上下文，里面有当前请求、已验证的身份以及控制认证结果的方法。
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task TokenValidated(TokenValidatedContext context)
        {
            //1. 确定 JWT 对应哪个用户
            //context.Principal:当前请求的 ClaimsPrincipal  .Identity:当前主要的身份 as ClaimsIdentity:转成能够读取、调整 Claim 的身份对象
            //当前请求的ClaimsPrincipal的主要身份转为 Claim 的身份对象。
            var identity = context.Principal?.Identity as ClaimsIdentity;
            //FindFirst(ClaimTypes.NameIdentifier):找用户 ID 这条 Claim  .Value:得到字符串，例如 "42"
            var userIdText = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            //如果认证没通过 或者 userIdText转换失败  或者 没有这个用户，就不能拿它继续查数据库
            if (identity?.IsAuthenticated != true || !long.TryParse(userIdText, out var userId) || userId <= 0)
            {
                context.Fail("无效的用户身份");    //向 JWT Bearer 报告“本次认证失败”；
                return;     //立即退出你的方法，不要继续查库、添加权限 Claim
            }


            //2. 确认账号至今仍有效
            //用刚才取出的 `userId` 查库
            var user = await _unitOfWork.Users.GetByIdWithRbacAsync(userId, context.HttpContext.RequestAborted);

            if(user ==null || user.Status != 1)
            {
                context.Fail("用户不存在或已禁用");
                return;
            }


            //3. 用与登录时相同的规则整理授权
            //现在查询得到的 user 是本次请求刚从数据库读取的用户，而不是登录时的用户快照
            //取到所有可用角色
            var enabledRoles = user.UserRoles
                                .Where(x => x.Role.Status == 1)
                                .Select(x => x.Role)
                                .ToList();

            //取出角色里面的所有不重复的角色编码
            var roles = enabledRoles.Select(x => x.Code)
                .Distinct().ToList();

            //取出所有权限里面的不重复权限编码
            var permissions = enabledRoles.SelectMany(x => x.RolePermissions)
                .Where(x => x.Permission.Status == 1)
                .Select(x => x.Permission.Code)
                .Distinct()
                .ToList();


            //4. 替换旧 Claim，不能只追加
            //先从本次请求的身份中移走原来的角色与权限：它要从本次请求的身份里，挑出 JWT 带来的旧角色和旧权限，待会儿删掉；但保留用户 ID、用户名
            //context.Principal!.Identities:所有的身份。 currentIdentity.Claims：拿到全部身份信息
            foreach (var currentIdentity in context.Principal!.Identities)
            {
                var staleClaims = currentIdentity.Claims
                    .Where(x => x.Type == ClaimTypes.Role || x.Type == currentIdentity.RoleClaimType
                    || x.Type == "role" || x.Type == "permission").ToArray();

                foreach (var claim in staleClaims)
                {
                    currentIdentity.RemoveClaim(claim);
                }
            }

            //然后再加入本次查库的最新结果： 加入角色和权限信息
            foreach(var role in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            foreach(var permission in permissions)
            {
                identity.AddClaim(new Claim("permission", permission));
            }
        }

        /// <summary>
        /// 认证失败：401
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task Challenge(JwtBearerChallengeContext context)
        {
            context.HandleResponse();//告诉框架：这个 Challenge 我已经接管了,默认响应不要再处理。
            context.Response.StatusCode = StatusCodes.Status401Unauthorized; //context.Response:当前这一次 HTTP 请求对应的 HTTP Response

            //.WriteAsJsonAsync():把c#对象 -> JSON序列化 -> 写入HTTP Response Body
            await context.Response.WriteAsJsonAsync(new ApiResponse<object>
            {
                Code = 401,
                Message = "未登录或登录已过期",
                Data = null
            });
        }

        /// <summary>
        /// 已认证，但是权限不足：403 ;  用户身份已经认证成功,但是权限不够
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task Forbidden(ForbiddenContext context)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            await context.Response.WriteAsJsonAsync(new ApiResponse<object>
            {
                Code = 403,

                Message = "没有权限访问该资源",

                Data = null
            });
        }
    }
}
