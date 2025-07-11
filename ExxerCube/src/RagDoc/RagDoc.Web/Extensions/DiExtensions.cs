namespace RagDoc.Web.Extensions
{
    /// <summary>
    /// Provides extension methods for dependency injection container configuration.
    /// </summary>
    public static class DiExtensions
    {
        /// <summary>
        /// Used to register <see cref="IHostedService"/> class which defines an referenced <typeparamref name="TInterface"/> interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface other components will use</typeparam>
        /// <typeparam name="TService">The actual <see cref="IHostedService"/> service.</typeparam>
        /// <param name="services">The service collection to register the services with.</param>
        public static void AddHostedApiService<TInterface, TService>(this IServiceCollection services)
            where TInterface : class
            where TService : class, IHostedService, TInterface
        {
            services.AddSingleton<TInterface, TService>();
            services.AddSingleton<IHostedService>(p => (TService)p.GetRequiredService<TInterface>());
        }
    }
}
