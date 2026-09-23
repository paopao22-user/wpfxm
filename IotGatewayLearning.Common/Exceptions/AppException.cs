using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Common.Exceptions
{
    public class AppException:Exception
    {
        // 异常最终应该返回什么 HTTP 状态码
        public int StatusCode { get; }  //http 状态码

        public AppException(string message, int statusCode):base(message)
        {
            StatusCode = statusCode;
        }
    }
}
