using Jellyfin.Channels.LazyMan.GameApi;
using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Channels.LazyMan
{
    /// <inheritdoc />
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<PowerSportsApi>();
        }
    }
}