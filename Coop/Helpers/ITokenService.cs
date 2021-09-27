using Coop.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Coop.Helpers
{
    public interface ITokenService
    {
        string BuildToken(string key, string issuer, UserForLoginDto user);
        bool ValidateToken(string key, string issuer,string token);
    }
}
