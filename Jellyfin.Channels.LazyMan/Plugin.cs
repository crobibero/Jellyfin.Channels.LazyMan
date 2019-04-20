using System;
using System.IO;
using Jellyfin.Channels.LazyMan.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Channels.LazyMan
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        private readonly Guid _id = new Guid("22e6a5be-b134-4a8e-9413-38249a891c9e");
        
        public override string Name => "LazyMan";
        public override Guid Id => _id;
        public override string Description => "Play NHL and MLB games.";
        public static Plugin Instance { get; private set; }
        
        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".Images.LM.png");
        }

        public ImageFormat ThumbImageFormat => ImageFormat.Png;
    }
}