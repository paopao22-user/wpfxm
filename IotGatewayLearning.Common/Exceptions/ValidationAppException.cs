using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Common.Exceptions
{
    public class ValidationAppException:AppException
    {
        // ======================================
        // 参数校验失败：400
        // ======================================
        public ValidationAppException(string message):base(message, 400)
        {
            
        }
    }
}
