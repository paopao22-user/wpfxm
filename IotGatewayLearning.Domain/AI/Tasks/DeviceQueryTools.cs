using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.AI.Tasks
{
    /// <summary>
    /// 交给模型的三个只读工具。
    /// 设备、点位和时间窗口在构造时绑定，模型无法通过参数扩大查询范围。
    /// </summary>
    public sealed class DeviceQueryTools : IDisposable
    {
        private readonly IAiDeviceQuery _queries;
        private readonly string _device;
        private readonly string _point;
        private readonly DateTime _end;
        private readonly Func<List<ToolExecution>, Task> _save;
        private readonly CancellationToken _taskToken;

        // EF DbContext 不允许并发操作，同一任务的工具串行执行。
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly List<ToolExecution> _calls = new();
        private int _count;

        public DeviceQueryTools(
            IAiDeviceQuery queries,
            string device,
            string point,
            DateTime end,
            Func<List<ToolExecution>, Task> save,
            CancellationToken taskToken)
        {
            _queries = queries;
            _device = device;
            _point = point;
            _end = end;
            _save = save;
            _taskToken = taskToken;
        }

        // 先落 Started，再执行真实查询；成功和失败都更新同一条记录。
        private async Task<string> ExecuteAsync(string name, Func<Task<object>> action)
        {
            await _gate.WaitAsync(_taskToken);
            try
            {
                if (++_count > 4)
                    throw new InvalidOperationException("工具调用次数超过4次");

                var index = _calls.Count;
                _calls.Add(new ToolExecution(name, DateTime.UtcNow, null, "Started", ""));
                await _save(_calls);

                try
                {
                    var json = JsonSerializer.Serialize(await action());
                    _calls[index] = _calls[index] with
                    {
                        CompletedAt = DateTime.UtcNow,
                        Status = "Succeeded",
                        Result = json
                    };
                    await _save(_calls);
                    return json;
                }
                catch
                {
                    // 不把数据库异常、内部地址透传给模型。
                    _calls[index] = _calls[index] with
                    {
                        CompletedAt = DateTime.UtcNow,
                        Status = "Failed",
                        Result = "查询未完成"
                    };
                    await _save(_calls);
                    throw;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        [Description("读取本任务绑定设备的资料，不改变设备状态")]
        public Task<string> GetDevice() =>
            ExecuteAsync("GetDevice", () => _queries.DeviceAsync(_device, _taskToken));

        [Description("读取本任务绑定点位最近一分钟样本，包括质量和是否截断")]
        public Task<string> GetHistory() =>
            ExecuteAsync("GetHistory", () => _queries.HistoryAsync(_device, _point, _end, _taskToken));

        [Description("读取本任务绑定设备最多20条活动或已确认告警")]
        public Task<string> GetAlarms() =>
            ExecuteAsync("GetAlarms", () => _queries.AlarmsAsync(_device, _taskToken));

        public void Dispose() => _gate.Dispose();
    }
}
