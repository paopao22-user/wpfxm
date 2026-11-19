using System.ClientModel;
using Microsoft.AspNetCore.Mvc;
using Day44.MafWebApi.Models;
using Day44.MafWebApi.Services;

namespace Day44.MafWebApi.Controllers;

[ApiController]         // 启用 API 模型绑定和参数校验
[Route("api/chat")]     // 控制器地址前缀
public sealed class ChatController : ControllerBase
{
    private readonly DeepSeekChatService service;

    // 依赖注入：框架创建 Controller 时提供已注册的 Service。
    public ChatController(DeepSeekChatService service) => this.service = service;

    [HttpPost]         // 合起来就是 POST /api/chat
    public async Task<ActionResult<ChatReply>> Ask(ChatRequest request,CancellationToken ct)
    {
        try
        {
            var reply = await service.AskAsync(request.Question, ct);
            return Ok(reply); // HTTP 200，正文是 ChatReply 的 JSON
        }
        catch (ArgumentException)
        {
            return BadRequest(new { message = "问题长度必须为1至1000字符" });
        }
        catch (OperationCanceledException)
        {
            // 客户端已断开时可能看不到这个响应；不说明厂商调用未发生。
            return StatusCode(504, new { message = "等待已取消或超时" });
        }
        catch (ClientResultException ex)
        {
            var message = ex.Status switch
            {
                401 or 403 => "请在服务端检查DeepSeek密钥及权限",
                402 => "DeepSeek账户余额不足",
                429 => "DeepSeek服务限流，请稍后操作",
                400 or 404 => "请核对模型名、请求参数和基础地址",
                _ => "DeepSeek暂时不可用"
            };
            return StatusCode(502, new { message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { message = "无法连接DeepSeek" });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(502, new { message = "未能取得最终回答，请核对服务端配置与输出预算" });
        }
    }
}
