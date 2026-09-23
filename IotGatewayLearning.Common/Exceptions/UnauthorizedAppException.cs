using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Common.Exceptions
{
    public class UnauthorizedAppException:AppException
    {
        // ======================================
        // 身份验证失败：401
        // ======================================
        public UnauthorizedAppException(string message):base(message, 401)
        {
            
        }
    }
}
