using System;
using ExplogineMonoGame;
using Microsoft.Xna.Framework.Input;

namespace LudJam;

public class HotKeys
{
    public static void RunBinding(ConsumableInput input, Func<ConsumableInput, bool> modifier, Keys buttonKey,
        Action action)
    {
        if (input.Keyboard.GetButton(buttonKey).WasPressed && modifier(input))
        {
            action();
        }
    }

    public static bool Ctrl(ConsumableInput input)
    {
        return input.Keyboard.Modifiers.Control;
    }
    
    public static bool NoModifiers(ConsumableInput input)
    {
        return input.Keyboard.Modifiers.None;
    }

    public static bool CtrlShift(ConsumableInput input)
    {
        return input.Keyboard.Modifiers.ControlShift;
    }
}