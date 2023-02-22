using System;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Data;
using ExTween;
using ExTweenMonoGame;
using Fenestra.Components;
using FenestraSceneGraph;
using FenestraSceneGraph.Components;
using Microsoft.Xna.Framework;

namespace LudJam;

public class Cat : BaseComponent
{
    private readonly BoundingRectangle _boundingRectangle;
    private readonly TweenableFloat _handAngle = new();
    private readonly TweenableVector2 _handPosition = new();
    private readonly TweenableVector2 _handScale = new(Vector2.One);
    private readonly SequenceTween _tween = new();
    private int _handFrame = 4;
    private bool _handVisible;

    public Cat(Actor actor) : base(actor)
    {
        _boundingRectangle = RequireComponent<BoundingRectangle>();
    }

    public RectangleF Rectangle => _boundingRectangle.Rectangle;

    public override void Draw(Painter painter)
    {
        if (_handVisible)
        {
            var sheet = Client.Assets.GetAsset<SpriteSheet>("Sheet");
            sheet.DrawFrameAtPosition(painter, _handFrame, _handPosition, new Scale2D(LudGameCartridge.ActorScale.Value.StraightMultiply(_handScale)),
                new DrawSettings
                {
                    Depth = Depth.Front,
                    Color = G.CharacterColor,
                    Angle = _handAngle,
                    Origin = DrawOrigin.Center,
                });
        }
    }

    public override void Update(float dt)
    {
        LudGameCartridge.Instance.AddCameraFocusPoint(Actor.Position);
        
        if (_handVisible)
        {
            LudGameCartridge.Instance.AddCameraFocusPoint(_handPosition);
            _tween.Update(dt);
        }
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }

    public void AnimateVictory(Vector2 handStartingPosition)
    {
        _handVisible = true;
        
        
        void TransitionFrameTo(int frame)
        {
            _tween.Add(_handScale.TweenTo(new Vector2(1, 0.75f), 0.1f, Ease.QuadSlowFast));
            _tween.Add(new CallbackTween(() => _handFrame = frame));
            _tween.Add(_handScale.TweenTo(new Vector2(1, 1.25f), 0.1f, Ease.QuadFastSlow));
            _tween.Add(_handScale.TweenTo(new Vector2(1, 1), 0.2f, Ease.QuadFastSlow));
        }

        G.ImpactFreeze(0.05f);
        var abovePosition = Actor.Position + new Vector2(32, -128);
        var moreAbovePosition = Actor.Position + new Vector2(32, -128 - 64);
        var pettingPosition = Actor.Position + new Vector2(32, -64);
        _tween.Clear();
        _handPosition.Value = handStartingPosition;
        _tween.Add(_handPosition.TweenTo(abovePosition, 0.5f, Ease.CubicFastSlow));
        TransitionFrameTo(10);

        _tween.Add(_handPosition.TweenTo(pettingPosition, 0.25f, Ease.CubicFastSlow));

        var totalNumberOfPets = 3;
        for (var i = 0; i < totalNumberOfPets; i++)
        {
            _tween.Add(_handAngle.TweenTo(-MathF.PI / 8f, 0.15f, Ease.Linear));
            _tween.Add(_handAngle.TweenTo(MathF.PI / 8f, 0.15f, Ease.Linear));
        }

        _tween.Add(_handAngle.TweenTo(0, 0.15f, Ease.Linear));
        _tween.Add(_handPosition.TweenTo(moreAbovePosition, 0.25f, Ease.CubicFastSlow));

        TransitionFrameTo(7);

        _tween.Add(new WaitSecondsTween(1f));

        _tween.Add(new CallbackTween(() => { LudGameCartridge.Instance.TransitionToNextLevel(); }));
    }
}
