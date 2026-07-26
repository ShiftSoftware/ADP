using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ShiftSoftware.ADP.Surveys.API.Extensions;

/// <summary>
/// Caps the request body on the anonymous submit endpoint at
/// <see cref="SurveyApiOptions.MaxResponseBodyBytes"/>.
///
/// A resource filter rather than a check inside the action: resource filters run BEFORE
/// model binding, so an oversized body is rejected without ever being read into a
/// <c>SurveyResponseSubmissionDto</c>. Doing it in the action would mean the body has
/// already been buffered and deserialized — which is the cost we're trying to avoid.
///
/// Both halves matter. Setting the size feature caps what Kestrel will read for
/// chunked/unknown-length requests; the Content-Length check gives a clean 413 for the
/// ordinary case instead of a connection-level abort.
/// </summary>
public sealed class SurveyResponseSizeLimitAttribute : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var max = context.HttpContext.RequestServices
            .GetService<SurveyApiOptions>()?.MaxResponseBodyBytes;

        if (max is > 0)
        {
            var sizeFeature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (sizeFeature is { IsReadOnly: false })
                sizeFeature.MaxRequestBodySize = max;

            if (context.HttpContext.Request.ContentLength is long length && length > max)
            {
                context.Result = new ObjectResult(new
                {
                    Message = $"Response body is too large ({length} bytes; limit {max}).",
                })
                {
                    StatusCode = StatusCodes.Status413PayloadTooLarge,
                };
                return;
            }
        }

        await next();
    }
}

/// <summary>
/// Name of the rate-limiting policy applied to the anonymous survey endpoints.
/// </summary>
/// <remarks>
/// The policy is registered unconditionally so the name always resolves — a
/// <c>[EnableRateLimiting]</c> attribute naming an unregistered policy throws at request
/// time, which would be a worse failure than the limit being off. When
/// <see cref="SurveyApiOptions.PublicEndpointRateLimit"/> is null the policy returns a
/// no-op partition.
///
/// Enforcement still requires the host to call <c>app.UseRateLimiter()</c>. Without that
/// middleware this is inert metadata — there is no way for a module to add middleware to
/// someone else's pipeline.
/// </remarks>
public static class SurveysRateLimitPolicy
{
    public const string Name = "adp-surveys-public";
}
