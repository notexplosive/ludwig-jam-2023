using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using ExplogineMonoGame.Input;

namespace LudJam;

public class PlaceSpawnTool : IEditorTool
{
    public string Name => "Spawn";
    public void UpdateInput(ConsumableInput input, HitTestStack hitTestStack, Level level, bool isWithinScreen)
    {
        if (!isWithinScreen)
        {
            return;
        }
        
        if (input.Mouse.GetButton(MouseButton.Left, true).IsDown)
        {
            level.SetSpawnPosition(input.Mouse.Position(hitTestStack.WorldMatrix));
        }
    }

    public void Draw(Painter painter)
    {
        
    }
}
