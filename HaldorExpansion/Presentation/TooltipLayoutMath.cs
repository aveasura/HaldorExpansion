using System;

namespace HaldorExpansion.Presentation
{
    // All dimensions here are UI-local units except screen/placement inputs.
    internal static class TooltipLayoutMath
    {
        internal const float Margin = 12f;
        internal const float Padding = 14f;
        internal const float Gap = 8f;
        internal const float ScrollGutter = 10f;

        internal struct Layout
        {
            internal float Height, HeaderHeight, BodyHeight, FooterHeight, MaxScroll;
        }

        internal struct Position
        {
            internal float Left, Top;
        }

        internal static float Width(float screenWidth, float scale, float preferredWidth)
        {
            return Math.Max(1f, Math.Min(preferredWidth, (screenWidth - 2f * Margin) / scale));
        }

        internal static float MaximumHeight(float screenHeight, float scale)
        {
            return Math.Max(1f, Math.Min(screenHeight * 0.90f, screenHeight - 2f * Margin) / scale);
        }

        internal static Layout Measure(float screenHeight, float scale, float header, float content, float footer)
        {
            float maximum = MaximumHeight(screenHeight, scale);
            header = Math.Min(Math.Max(0f, header), maximum * 0.30f);
            float body = Math.Max(1f, maximum - 2f * Padding - header - Gap);
            bool overflow = content > body;
            footer = overflow ? Math.Min(Math.Max(0f, footer), maximum * 0.10f) : 0f;
            if (overflow) body = Math.Max(1f, body - footer - Gap);
            body = Math.Min(Math.Max(0f, content), body);
            return new Layout
            {
                HeaderHeight = header,
                BodyHeight = body,
                FooterHeight = footer,
                Height = 2f * Padding + header + Gap + body + (overflow ? footer + Gap : 0f),
                MaxScroll = Math.Max(0f, content - body)
            };
        }

        internal static Position Place(float screenWidth, float screenHeight, float width, float height,
            float sourceLeft, float sourceRight, float sourceTop)
        {
            float left = sourceRight + Margin;
            if (left + width > screenWidth - Margin) left = sourceLeft - Margin - width;
            return new Position
            {
                Left = Math.Max(Margin, Math.Min(left, screenWidth - Margin - width)),
                Top = Math.Min(screenHeight - Margin, Math.Max(height + Margin, sourceTop))
            };
        }

        internal static float Scroll(float offset, float maximum, float delta)
        {
            return Math.Max(0f, Math.Min(maximum, offset + delta));
        }
    }
}