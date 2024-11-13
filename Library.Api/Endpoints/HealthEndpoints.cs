using Library.Api.Endpoints.Internal;
using Microsoft.AspNetCore.Cors;

namespace Library.Api.Endpoints
{
    public class HealthEndpoints : IEndpoint
    {
        public static void AddServices(IServiceCollection services, IConfiguration configuration)
        {
        }

        public static void DefineEndpoints(IEndpointRouteBuilder app)
        {
            app.MapGet("/", () => "Hello World!")
            .WithTags("Health");

            app.MapGet("status", [EnableCors("AnyOriginDuuuh")] () =>
            {
                return Results.Extensions.Html(@"
                    <html>
                        <body>
                            <h2>Status</h2>
                            <p style='color: purple;'>The Server is working fine. Bye bye!</p>
                        </body>
                    </html>");
            })
            .WithTags("Health")
            .ExcludeFromDescription(); // exclude from Swagger

        }
    }
}
