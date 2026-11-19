using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.Common
{
    /// <summary>
    /// 上下线原因常量
    /// </summary>
    public static class StatusReasons
    {
        public const string NormalConnect = "normal-connect";        // 正常连接

        public const string NormalDisconnect = "normal-disconnect";  // 正常断开

        public const string AbnormalDisconnect = "abnormal-disconnect";  // 异常断开
    }
}
