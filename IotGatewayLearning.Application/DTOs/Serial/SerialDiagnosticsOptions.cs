using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Serial
{
    // ==========================================
    // 3. 端口白名单配置
    // ==========================================
    public class SerialDiagnosticsOptions
    {
        // 默认只允许课程虚拟串口的上位机端
        public List<string> AllowedPorts { get; set; } = new List<string> { "COM11" };
    }
}
