﻿using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using ExplogineMonoGame.Input;

namespace LudJam;

public class PlaceCatTool : IEditorTool
{
    public string Name => "Cat";
    public void UpdateInput(ConsumableInput input, HitTestStack hitTestStack, Level level)
    {
        if (input.Mouse.GetButton(MouseButton.Left, true).IsDown)
        {
            level.SetCatPosition(input.Mouse.Position(hitTestStack.WorldMatrix));
        }
    }

    public void Draw(Painter painter)
    {
        
    }
}