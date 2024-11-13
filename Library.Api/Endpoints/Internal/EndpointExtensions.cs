using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Library.Api.Endpoints.Internal
{
    public static class EndpointExtensions
    {
        public static void AddEndpoints(this IServiceCollection services, Type typeMarker, IConfiguration configuration)
        {
            IEnumerable<TypeInfo> endpointTypes = GetEndpointTypesFromAssemblyContaining(typeMarker);

            foreach (var endpointType in endpointTypes)
            {
                endpointType.GetMethod(nameof(IEndpoint.AddServices))!.Invoke(null, new object[] { services, configuration });
            }
        }

        public static void UseEndpoints(this IApplicationBuilder app, Type typeMarker)
        {
            IEnumerable<TypeInfo> endpointTypes = GetEndpointTypesFromAssemblyContaining(typeMarker);

            foreach (var endpointType in endpointTypes)
            {
                endpointType.GetMethod(nameof(IEndpoint.DefineEndpoints))!.Invoke(null, new object[] { app });
            }
        }

        private static IEnumerable<TypeInfo> GetEndpointTypesFromAssemblyContaining(Type typeMarker)
        {
            return typeMarker.Assembly.DefinedTypes
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IEndpoint).IsAssignableFrom(t));
        }
    }
}
