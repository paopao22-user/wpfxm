using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Common.Responses
{
    public class ApiResponse<T>
    {
        public int Code { get; set; }

        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }
    }
}
