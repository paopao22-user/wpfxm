using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.Common
{
    /// <summary>
    /// 命令结果码常量
    /// </summary>
    public static class CommandResultCodes
    {
        public const string Ok = "OK";      
        public const string DuplicateIgnored = "DUPLICATE_IGNORED";
        public const string InvalidPayload = "INVALID_PAYLOAD";
        public const string InvalidJson = "INVALID_JSON";
        public const string InvalidCommand = "INVALID_COMMAND";
        public const string InvalidMessageType = "INVALID_MESSAGE_TYPE";
        public const string DeviceMismatch = "DEVICE_MISMATCH";
        public const string UnsupportedSchema = "UNSUPPORTED_SCHEMA";
        public const string Expired = "EXPIRED";
        public const string InvalidParameter = "INVALID_PARAMETER";
        public const string OutOfRange = "OUT_OF_RANGE";
        public const string UnsupportedAction = "UNSUPPORTED_ACTION";
        public const string InternalError = "INTERNAL_ERROR";
    }
}
