using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.Common
{
    /// <summary>
    /// 四类消息名常量
    /// </summary>
    public static class MqttMessageTypes
    {
        public const string Telemetry = "telemetry";        // 遥测数据

        public const string Status = "status";              // 状态上报
        public const string Command = "command";            // 命令请求

        public const string CommandResult = "command-result";  // 命令响应


    }
}
