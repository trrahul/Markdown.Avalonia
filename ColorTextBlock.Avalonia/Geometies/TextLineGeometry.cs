﻿using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System.Linq;

namespace ColorTextBlock.Avalonia.Geometries
{
    internal class CTextLayout
    {
        public string Text { get; }
        public Typeface Typeface { get; }
        public double FontSize { get; }

        private TextLayout _layout;

        public CTextLayout(string text, Typeface typeface, double fontSize, IBrush? foreground, TextLayout result)
        {
            Text = text;
            Typeface = typeface;
            FontSize = fontSize;

            _layout = result;
        }

        public CTextLayout(string text, Typeface typeface, double fontSize, IBrush? foreground, double maxWidth)
        {
            Text = text;
            Typeface = typeface;
            FontSize = fontSize;

            _layout = new TextLayout(
                            Text,
                            Typeface, FontSize,
                            foreground,
                            textWrapping: TextWrapping.Wrap,
                            maxWidth: maxWidth);
        }
    }

    internal class CTextLine
    {
        public string Text { get; }
        public Typeface Typeface { get; }
        public double FontSize { get; }

        public double WidthIncludingTrailingWhitespace => _result.WidthIncludingTrailingWhitespace;
        public double Height => _result.Height;

        private TextLine _result;

        public CTextLine(string text, Typeface typeface, double fontSize, TextLine result)
        {
            Text = text;
            Typeface = typeface;
            FontSize = fontSize;
            _result = result;
        }

        public void SetForegroundBrush(IBrush? foreground)
        {
            var layout = new TextLayout(Text, Typeface, FontSize, foreground);
            _result = layout.TextLines.First();
        }

        public void Draw(DrawingContext ctx, Point point) => _result.Draw(ctx, point);
    }

    internal class TextLineGeometry : TextGeometry
    {
        private CTextLine Line { set; get; }
        private IBrush? LayoutForeground { set; get; }

        internal TextLineGeometry(
            CInline owner,
            CTextLine tline,
            TextVerticalAlignment align,
            bool linebreak) :
            base(owner, tline.WidthIncludingTrailingWhitespace, tline.Height, tline.Height, align, linebreak)
        {
            Line = tline;
            LayoutForeground = owner.Foreground;
        }

        internal TextLineGeometry(
                TextLineGeometry baseGeometry,
                bool linebreak) :
            base(baseGeometry.Owner,
                 baseGeometry.Width, baseGeometry.Height, baseGeometry.Height,
                 baseGeometry.TextVerticalAlignment,
                 linebreak)
        {
            Line = baseGeometry.Line;
            LayoutForeground = baseGeometry.LayoutForeground;
        }

        public override void Render(DrawingContext ctx)
        {
            var foreground = TemporaryForeground ?? Foreground;
            var background = TemporaryBackground ?? Background;

            if (LayoutForeground != foreground)
            {
                LayoutForeground = foreground;
                Line.SetForegroundBrush(LayoutForeground);
            }

            if (background != null)
            {
                ctx.FillRectangle(background, new Rect(Left, Top, Width, Height));
            }

            Line.Draw(ctx, new Point(Left, Top));

            var pen = new Pen(foreground);
            if (IsUnderline)
            {
                ctx.DrawLine(pen,
                    new Point(Left, Top + Height),
                    new Point(Left + Width, Top + Height));
            }

            if (IsStrikethrough)
            {
                ctx.DrawLine(pen,
                    new Point(Left, +Top + Height / 2),
                    new Point(Left + Width, Top + Height / 2));
            }
        }
    }
}
