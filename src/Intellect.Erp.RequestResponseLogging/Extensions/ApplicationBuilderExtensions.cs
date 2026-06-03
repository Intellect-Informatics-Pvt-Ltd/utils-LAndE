using Microsoft.AspNetCore.Builder;
using Intellect.Erp.RequestResponseLogging.Middleware;

namespace Intellect.Erp.RequestResponseLogging.Extensions
{
    /// <summary>
    /// Application builder extension methods for registering request/response logging middleware.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds request/response logging middleware into the ASP.NET Core pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<RequestResponseLoggingMiddleware>();
        }
    }
}
