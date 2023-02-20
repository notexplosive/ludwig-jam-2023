﻿using ExplogineMonoGame;
using ExplogineMonoGame.Data;

namespace LudJam;

public interface IEditorTool
{
    string Name { get; }
    void UpdateInput(ConsumableInput input, HitTestStack hitTestStack, Level level);
    void Draw(Painter painter);
}