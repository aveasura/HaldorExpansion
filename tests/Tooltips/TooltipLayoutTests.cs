using System;
using HaldorExpansion.Presentation;

class TooltipLayoutTests
{
    static int failures;

    static void Check(bool value, string name)
    {
        Console.WriteLine((value ? "PASS: " : "FAIL: ") + name);
        if (!value) failures++;
    }

    static bool Near(float a, float b) => Math.Abs(a - b) < 0.02f;

    static void Main()
    {
        var tall = TooltipLayoutMath.Measure(1080, 1f, 30, 3000, 20);
        Check(tall.Height <= 972 && tall.BodyHeight > 0 && tall.MaxScroll > 0,
            "long descriptions fit screen and retain scrollable content");
        Check(Near(tall.MaxScroll + tall.BodyHeight, 3000),
            "scroll range reaches the last line without cutting content");
        var shortText = TooltipLayoutMath.Measure(1080, 1f, 30, 80, 20);
        Check(shortText.MaxScroll == 0 && shortText.FooterHeight == 0,
            "short descriptions need no scroll hint or empty footer");
        bool fits = true;
        foreach (float scale in new[] { 0.75f, 1f, 1.5f, 2f })
        foreach (float height in new[] { 480f, 720f, 1080f, 2160f })
        {
            var layout = TooltipLayoutMath.Measure(height, scale, 40, 10000, 25);
            fits &= layout.Height * scale <= height - 24 + 0.02f && layout.BodyHeight > 0;
        }

        Check(fits, "small screens and large UI scales stay inside physical bounds");
        Check(Near(TooltipLayoutMath.Width(640, 2, 400), 308), "width cap includes screen margins and UI scale");
        Check(Near(TooltipLayoutMath.Width(1920, 1, 360), 360), "ordinary width is preserved");
        var right = TooltipLayoutMath.Place(1920, 1080, 400, 800, 1700, 1764, 1030);
        Check(right.Left < 1700 && right.Left >= 12 && right.Top <= 1068 && right.Top - 800 >= 12,
            "right edge places panel on the left and clamps height");
        var left = TooltipLayoutMath.Place(1280, 720, 400, 648, 0, 64, 50);
        Check(left.Left == 76 && left.Top - 648 >= 12, "bottom-left source keeps entire panel on screen");
        var again = TooltipLayoutMath.Place(1920, 1080, 400, 800, 1700, 1764, 1030);
        Check(right.Left == again.Left && right.Top == again.Top, "stationary item has stable placement across frames");
        Check(TooltipLayoutMath.Scroll(200, 1000, 9000) == 1000 && TooltipLayoutMath.Scroll(200, 1000, -9000) == 0,
            "scroll clamps at both ends");
        Check(TooltipLayoutMath.Scroll(500, 100, 0) == 100,
            "text shrinking or resizing clamps the saved scroll offset");
        if (failures != 0) Environment.Exit(1);
    }
}
