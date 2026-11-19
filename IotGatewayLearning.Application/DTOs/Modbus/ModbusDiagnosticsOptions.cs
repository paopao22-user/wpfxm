using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Modbus
{
    // ==========================================
    // 5. 主机白名单
    // ==========================================
    public class ModbusDiagnosticsOptions
    {
        // 默认只允许本机模拟器
        public List<string> AllowedHosts { get; set; } = new List<string>
        {
            "127.0.0.1",
            "localhost"
        };
    }
}
