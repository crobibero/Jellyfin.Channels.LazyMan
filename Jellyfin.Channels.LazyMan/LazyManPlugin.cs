using System;
using Jellyfin.Channels.LazyMan.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Channels.LazyMan
{
    /// <summary>
    /// LazyMan plugin initializer.
    /// </summary>
    public class LazyManPlugin : BasePlugin<PluginConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LazyManPlugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public LazyManPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <inheritdoc />
        public override string Name => "LazyMan";

        /// <inheritdoc />
        public override Guid Id => new ("22e6a5be-b134-4a8e-9413-38249a891c9e");

        /// <inheritdoc />
        public override string Description => "Play NHL and MLB games.";

        /// <summary>
        /// Gets the current instance of the plugin.
        /// </summary>
        public static LazyManPlugin? Instance { get; private set; }
    }
}