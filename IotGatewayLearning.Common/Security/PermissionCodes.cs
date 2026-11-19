using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Common.Security
{
    public static class PermissionCodes
    {
        public const string DeviceRead = "device:read";

        public const string DeviceControl = "device:control";

        public const string UserManage = "user:manage";

        public const string DeviceConfigure = "device:configure";

        // 设备事件接入（Day48）：给受认证的桥接程序，与计划管理权限分离。
        public const string AiEventIngest = "ai:event:ingest";

        // 审批设备控制（Day50）：审批他人控制申请。
        public const string DeviceApprove = "device:approve";

        // 查看控制申请（Day50）：device:control 或 device:approve 任一即可。
        public const string ControlView = "control:view";
    }
}
