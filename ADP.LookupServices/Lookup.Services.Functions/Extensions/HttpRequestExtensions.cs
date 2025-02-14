using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lookup.Services.Functions.Extensions;

public static class HttpRequestExtensions
{
    public static string GetBaseUrl(this HttpRequest req)
    {
        return $"{req.Scheme}://{req.Host}";
    }
}
