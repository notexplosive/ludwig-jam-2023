using System;
using System.IO;
using ExplogineCore;
using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Microsoft.Xna.Framework;

namespace LudJam;

public static class G
{
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
        var backgroundColor = ColorExtensions.FromRgbHex(0x0D5D56);
        var backgroundAccentColor = ColorExtensions.FromRgbHex(0x0C8346);
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

    public static Color JumpParticleColor => Color.Orange.DimmedBy(0.2f);
    public static Color FlameColor => Color.OrangeRed;
    public static Color CharacterColor => ColorExtensions.FromRgbHex(0xffffff);

    public static void ImpactFreeze(float seconds)
    {
        G.ImpactTimer = seconds;
    }

    public static float ImpactTimer { get; set; }
}
