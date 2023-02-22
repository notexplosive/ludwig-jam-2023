using System;
using System.IO;
using ExplogineCore;
using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Microsoft.Xna.Framework;

namespace LudJam;

public static class G
{
    public static MusicPlayer Music = new();

    public static Color BackgroundColor1 => ColorExtensions.FromRgbHex(0x162627);
    public static Color BackgroundColor2 => ColorExtensions.FromRgbHex(0x00523D);

    public static Color JumpParticleColor => Color.Orange.DimmedBy(0.2f);
    public static Color FlameColor => Color.OrangeRed;
    public static Color CharacterColor => ColorExtensions.FromRgbHex(0xffffff);

    public static float ImpactTimer { get; set; }
    public static float TransitionDuration => 1f;
    public static Color CurtainColor1 => ColorExtensions.FromRgbHex(0x0E131B);
    public static Color CurtainColor2 => ColorExtensions.FromRgbHex(0x2B3A67);
    public static Color WallColor1 => ColorExtensions.FromRgbHex(0x2D7DD2);
    public static Color WallColor2 => ColorExtensions.FromRgbHex(0x85B404);

    public static IFileSystem EditorDevelopmentFileSystem(IRuntime runtime)
    {
#if DEBUG
        return new RealFileSystem(Path.Join(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Assets"));
#else
            return runtime.FileSystem.Local;
#endif
    }

    public static void DrawBackground(Painter painter, Point renderResolution, Camera camera)
    {
        // Draw background
        painter.BeginSpriteBatch();
        var backgroundColor = G.BackgroundColor1;
        var backgroundAccentColor = G.BackgroundColor2;
        var renderResolutionRect = renderResolution.ToRectangleF();
        painter.DrawRectangle(renderResolutionRect, new DrawSettings {Color = backgroundColor});
        painter.DrawAsRectangle(Client.Assets.GetTexture("Background"), renderResolutionRect,
            new DrawSettings
            {
                SourceRectangle = new Rectangle((camera.CenterPosition / 2f).ToPoint(),
                    (renderResolutionRect.Size * LudEditorCartridge.BackgroundScalar).ToPoint()),
                Color = backgroundAccentColor
            });
        painter.EndSpriteBatch();
    }

    public static void ImpactFreeze(float seconds)
    {
        G.ImpactTimer = seconds;
    }
}
