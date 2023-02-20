using ExplogineDesktop;
using ExplogineMonoGame;
using LudJam;
using Microsoft.Xna.Framework;

var config = new WindowConfigWritable
{
    WindowSize = new Point(1920, 1080),
#if !DEBUG
    Fullscreen = true,
#endif
    Title = "NotExplosive.net"
};
Bootstrap.Run(args, new WindowConfig(config), runtime => new LudCoreCartridge(runtime));