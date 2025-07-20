using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI;

public class Positioner
{
    public int TopPadding;
    public int LeftPadding;
    public int BlankLineHeight;
    public int IndentWidth;

    public int X, Y, LastY;

    public Positioner(int leftPadding = 2, int topPadding = 5, int blankLineHeight = 20, int indentation = 40)
    {
        TopPadding = topPadding;
        BlankLineHeight = blankLineHeight;
        IndentWidth = indentation;
        
        Y = LastY = TopPadding;
        X = leftPadding;
    }

    public void BlankLine()
    {
        LastY = Y;
        Y += BlankLineHeight;
    }

    public void Indent()
    {
        X += IndentWidth;
    }

    public void RemoveIndent()
    {
        X -= IndentWidth;
    }

    public Control Position(Control c)
    {
        c.X = X;
        c.Y = Y;

        LastY = Y;
        Y += c.Height + TopPadding;

        return c;
    }

    public Control PositionRightOf(Control c, Control other, int padding = 5)
    {
        c.Y = other.Y;
        c.X = other.X + other.Width + padding;

        return c;
    }

    public Control PositionExact(Control c, int x, int y)
    {
        c.X = x;
        c.Y = y;

        return c;
    }

    public void Reset()
    {
        X = LeftPadding;
        Y = LastY = TopPadding;
    }
}