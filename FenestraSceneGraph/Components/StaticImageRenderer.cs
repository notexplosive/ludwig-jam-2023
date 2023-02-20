using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using FenestraSceneGraph;
using Microsoft.Xna.Framework.Graphics;

namespace Fenestra.Components;

public class StaticImageRenderer : BaseComponent
{
    private ImageAsset _asset = null!;
    private DrawOrigin _drawOrigin;
    private XyBool _flip;

    public StaticImageRenderer(Actor actor) : base(actor)
    {
    }

    public StaticImageRenderer Init(ImageAsset asset, XyBool? flip = null, DrawOrigin? drawOrigin = null)
    {
        _flip = flip ?? XyBool.False;
        _drawOrigin = drawOrigin ?? DrawOrigin.Center;
        _asset = asset;
        return this;
    }

    public StaticImageRenderer Init(Texture2D texture, XyBool? flip = null, DrawOrigin? drawOrigin = null)
    {
        return Init(new ImageAsset(texture, texture.Bounds), flip, drawOrigin);
    }
    
    public StaticImageRenderer Init(StaticImageRenderer renderer)
    {
        return Init(renderer._asset, renderer._flip, renderer._drawOrigin);
    }

    public override void Draw(Painter painter)
    {
        _asset.DrawAtPosition(painter, Actor.Position, Actor.Scale,
            new DrawSettings {Depth = Actor.Depth, Angle = Actor.Angle, Origin = _drawOrigin, Flip = _flip});
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }
}
