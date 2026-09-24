using IotGatewayLearning.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services
{
    public interface ITokenService
    {
        //(string Token, long ExpiresAt):代表一次性返回两个东西，Token和ExpiresAt
        (string Token, long ExpiresAt) CreateToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions);
    }
}
