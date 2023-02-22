using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using ExplogineMonoGame.Input;

namespace LudJam;

public class ParTool : IEditorTool
{
    public string Name => $"Par: {ParCount}";

    public int ParCount { private set; get; }

    public void UpdateInput(ConsumableInput input, HitTestStack hitTestStack, Level level, bool isWithinScreen)
    {
        if (isWithinScreen)
        {
            if (input.Mouse.GetButton(MouseButton.Left).WasPressed)
            {
                level.IncreasePar();
            }
            
            if (input.Mouse.GetButton(MouseButton.Right).WasPressed)
            {
                level.DecreasePar();
            }
        }
        
        ParCount = level.ParStrokeCount;
    }

    public void Draw(Painter painter)
    {
        
    }
}
