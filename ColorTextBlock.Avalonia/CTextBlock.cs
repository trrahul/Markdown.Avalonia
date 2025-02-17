﻿using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using ColorTextBlock.Avalonia.Geometries;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace ColorTextBlock.Avalonia
{
    public class CTextBlock : Control
    {
        private static readonly StyledProperty<double> BaseHeightProperty =
            AvaloniaProperty.Register<CTextBlock, double>("BaseHeight");

        public static readonly StyledProperty<double> LineHeightProperty =
            AvaloniaProperty.Register<CTextBlock, double>(nameof(LineHeight), defaultValue: Double.NaN);

        public static readonly StyledProperty<double> LineSpacingProperty =
            AvaloniaProperty.Register<CTextBlock, double>(nameof(LineSpacing), defaultValue: 0);

        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<CTextBlock>();

        public static readonly StyledProperty<IBrush?> ForegroundProperty =
            TextBlock.ForegroundProperty.AddOwner<CTextBlock>();

        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<CTextBlock>();

        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            TextBlock.FontWeightProperty.AddOwner<CTextBlock>();

        public static readonly StyledProperty<double> FontSizeProperty =
            TextBlock.FontSizeProperty.AddOwner<CTextBlock>();

        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            TextBlock.FontStyleProperty.AddOwner<CTextBlock>();

        public static readonly StyledProperty<TextVerticalAlignment> TextVerticalAlignmentProperty =
            AvaloniaProperty.Register<CTextBlock, TextVerticalAlignment>(
                nameof(TextVerticalAlignment),
                defaultValue: TextVerticalAlignment.Base,
                inherits: true);

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            AvaloniaProperty.Register<CTextBlock, TextWrapping>(nameof(TextWrapping), defaultValue: TextWrapping.Wrap);

        public static readonly DirectProperty<CTextBlock, AvaloniaList<CInline>> ContentProperty =
            AvaloniaProperty.RegisterDirect<CTextBlock, AvaloniaList<CInline>>(
                nameof(Content),
                    o => o.Content,
                    (o, v) => o.Content = v);

        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            AvaloniaProperty.Register<CTextBlock, TextAlignment>(
                nameof(TextAlignment), defaultValue: TextAlignment.Left);

        static CTextBlock()
        {
            ClipToBoundsProperty.OverrideDefaultValue<CTextBlock>(true);

            AffectsRender<CTextBlock>(
                BackgroundProperty,
                TextBlock.ForegroundProperty,
                TextBlock.FontWeightProperty,
                TextBlock.FontSizeProperty,
                TextBlock.FontStyleProperty);
        }

        private double _computedBaseHeight;
        private AvaloniaList<CInline> _content;
        private Size _constraint;
        private Size _measured;
        private readonly List<CGeometry> _metries;
        private readonly List<CInlineUIContainer> _containers;
        private bool _isPressed;
        private CGeometry? _entered;
        private CGeometry? _pressed;
        private string? _text;

        public IBrush? Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public IBrush? Foreground
        {
            get { return GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public FontFamily FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public double FontSize
        {
            get { return GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public FontStyle FontStyle
        {
            get { return GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public FontWeight FontWeight
        {
            get { return GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        public TextAlignment TextAlignment
        {
            get { return GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        public TextVerticalAlignment TextVerticalAlignment
        {
            get { return GetValue(TextVerticalAlignmentProperty); }
            set { SetValue(TextVerticalAlignmentProperty, value); }
        }

        public double LineHeight
        {
            get { return GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        public double LineSpacing
        {
            get { return GetValue(LineSpacingProperty); }
            set { SetValue(LineSpacingProperty, value); }
        }

        [Content]
        public AvaloniaList<CInline> Content
        {

            get => _content;
            set
            {
                var olds = _content;

                if (SetAndRaise(ContentProperty, ref _content, value))
                {
                    olds.CollectionChanged -= ContentCollectionChangedd;

                    DetachChildren(olds);
                    AttachChildren(_content);

                    _content.CollectionChanged += ContentCollectionChangedd;
                }
            }
        }

        public string Text
        {
            get => _text ??= String.Join("", Content.Select(c => c.AsString()));
        }

        public CTextBlock()
        {
            _content = new AvaloniaList<CInline>();
            _content.CollectionChanged += ContentCollectionChangedd;

            _metries = new List<CGeometry>();
            _containers = new List<CInlineUIContainer>();

            RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
        }

        public CTextBlock(string text) : this()
        {
            _content.Add(new CRun() { Text = text });
        }

        public CTextBlock(IEnumerable<CInline> inlines) : this()
        {
            _content.AddRange(inlines);
        }

        #region pointer event

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);

            if (_entered is not null)
            {
                _entered.OnMouseLeave?.Invoke(this);
                _entered = null;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            Point point = e.GetPosition(this);

            bool isEntered(CGeometry metry)
            {
                var relX = point.X - metry.Left;
                var relY = point.Y - metry.Top;

                return 0 <= relX && relX <= metry.Width
                    && 0 <= relY && relY <= metry.Height;
            }

            if (_entered is not null)
            {
                var relX = point.X - _entered.Left;
                var relY = point.Y - _entered.Top;

                if (!isEntered(_entered))
                {
                    _entered.OnMouseLeave?.Invoke(this);
                    _entered = null;
                }
                else return;
            }

            foreach (CGeometry metry in _metries)
            {
                if (isEntered(metry))
                {
                    metry.OnMouseEnter?.Invoke(this);
                    _entered = metry;
                    break;
                }
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isPressed = true;
                e.Handled = true;

                Point point = e.GetPosition(this);

                bool isEntered(CGeometry metry)
                {
                    var relX = point.X - metry.Left;
                    var relY = point.Y - metry.Top;

                    return 0 <= relX && relX <= metry.Width
                        && 0 <= relY && relY <= metry.Height;
                }

                foreach (CGeometry metry in _metries)
                {
                    if (isEntered(metry))
                    {
                        metry.OnMousePressed?.Invoke(this);
                        _pressed = metry;
                        break;
                    }
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_isPressed && e.InitialPressMouseButton == MouseButton.Left)
            {
                _isPressed = false;
                e.Handled = true;

                if (_pressed is not null)
                {
                    _pressed.OnMouseReleased?.Invoke(this);
                    _pressed = null;
                }


                Point point = e.GetPosition(this);

                foreach (CGeometry metry in _metries)
                {
                    var relX = point.X - metry.Left;
                    var relY = point.Y - metry.Top;

                    if (0 <= relX && relX <= metry.Width
                        && 0 <= relY && relY <= metry.Height)
                    {
                        metry.OnClick?.Invoke(this);
                        break;
                    }
                }
            }
        }

        #endregion

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(Content):
                case nameof(TextBlock.FontSize):
                case nameof(TextBlock.FontStyle):
                case nameof(TextBlock.FontWeight):
                case nameof(TextWrapping):
                case nameof(Bounds):
                case nameof(TextVerticalAlignment):
                case nameof(LineHeight):
                case nameof(LineSpacing):
                    OnMeasureSourceChanged();
                    break;

                case nameof(BaseHeightProperty):
                    CheckHaveToMeasure();
                    break;
            }
        }

        public void ObserveBaseHeightOf(CTextBlock target)
        {
            if (target is not null)
                this.Bind(BaseHeightProperty, target.GetBindingObservable(BaseHeightProperty));
        }

        private void ContentCollectionChangedd(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems is not null)
                        DetachChildren(e.OldItems.Cast<CInline>());
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems is not null)
                        DetachChildren(e.OldItems.Cast<CInline>());

                    if (e.NewItems is not null)
                        AttachChildren(e.NewItems.Cast<CInline>());
                    break;

                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems is not null)
                        AttachChildren(e.NewItems.Cast<CInline>());
                    break;
            }
        }

        private void AttachChildren(IEnumerable<CInline> newItems)
        {
            foreach (CInline item in newItems)
            {
                LogicalChildren.Add(item);
                AttachForVisual(item);
            }

            void AttachForVisual(CInline item)
            {
                if (item is CInlineUIContainer container)
                {
                    var content = container.Content;

                    var visparent = container.Content.GetVisualParent();
                    if (visparent is CTextBlock cblock)
                    {
                        cblock.VisualChildren.Remove(content);
                        cblock.LogicalChildren.Remove(content);
                    }
                    else if (visparent is object)
                    {
                        Debug.Print("Control has another parent");
                        return;
                    }

                    VisualChildren.Add(container.Content);
                    LogicalChildren.Add(container.Content);

                    _containers.Add(container);
                }
                else if (item is CSpan span)
                    foreach (var child in span.Content)
                        AttachForVisual(child);
            }
        }

        private void DetachChildren(IEnumerable<CInline> removeItems)
        {
            foreach (CInline item in removeItems)
            {
                LogicalChildren.Remove(item);
                DetachForVisual(item);
            }

            void DetachForVisual(CInline item)
            {
                if (item is CInlineUIContainer container)
                {
                    VisualChildren.Remove(container.Content);
                    LogicalChildren.Remove(container.Content);

                    _containers.Remove(container);
                }
                else if (item is CSpan span)
                    foreach (var child in span.Content)
                        DetachForVisual(child);
            }
        }

        private void CheckHaveToMeasure()
        {
            if (_computedBaseHeight != GetValue(BaseHeightProperty))
            {
                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        internal void OnMeasureSourceChanged()
        {
            SetValue(BaseHeightProperty, default);
            InvalidateMeasure();
            InvalidateArrange();
        }

        private void RepaintRequested()
        {
            InvalidateVisual();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_measured.Width > finalSize.Width)
            {
                finalSize = finalSize.WithWidth(Math.Ceiling(_measured.Width));
            }
            foreach (var container in _containers)
            {
                var indicator = container.Indicator;
                if (indicator is null) continue;

                container.Content.Arrange(new Rect(indicator.Left, indicator.Top, indicator.Width, indicator.Height));
            }
            if (MathUtilities.AreClose(_constraint.Width, finalSize.Width))
            {
                return finalSize;
            }

            _constraint = new Size(finalSize.Width, Double.PositiveInfinity);
            _measured = UpdateGeometry();

            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_measured.Width == 0d || !MathUtilities.AreClose(availableSize.Width, _constraint.Width))
            {
                _constraint = availableSize;
                _measured = UpdateGeometry();
            }

            InvalidateArrange();

            return _measured;
        }

        private Size UpdateGeometry()
        {
            _metries.Clear();

            double entireWidth = _constraint.Width;
            if (Double.IsInfinity(_constraint.Width) && Bounds.Width != 0)
                entireWidth = Bounds.Width;


            double width = 0;
            double height = 0;

            // measure & split by linebreak
            var reqHeight = GetValue(BaseHeightProperty);
            var entireLineHeight = LineHeight;
            var lines = new List<LineInfo>();
            {
                LineInfo? now = null;

                double remainWidth = entireWidth;

                foreach (CInline inline in Content)
                {
                    IEnumerable<CGeometry> inlineGeometry =
                        inline.Measure(
                            (TextWrapping == TextWrapping.NoWrap) ? Double.PositiveInfinity : entireWidth,
                            (TextWrapping == TextWrapping.NoWrap) ? Double.PositiveInfinity : remainWidth);

                    foreach (CGeometry metry in inlineGeometry)
                    {
                        if (now is null)
                        {
                            lines.Add(now = new LineInfo());
                            if (lines.Count == 1)
                                now.RequestBaseHeight = reqHeight;
                        }

                        if (now.Add(metry))
                        {
                            if (!Double.IsNaN(entireLineHeight))
                                now.OverwriteHeight(entireLineHeight);

                            width = Math.Max(width, now.Width);
                            height += now.Height;

                            now = null;
                            remainWidth = entireWidth;
                        }
                        else remainWidth -= metry.Width;
                    }
                }

                if (now is not null)
                {
                    if (!Double.IsNaN(entireLineHeight))
                        now.OverwriteHeight(entireLineHeight);

                    width = Math.Max(width, now.Width);
                    height += now.Height;
                }
            }

            if (lines.Count > 0)
            {
                _computedBaseHeight = lines[0].BaseHeight;
                SetValue(BaseHeightProperty, lines[0].BaseHeight);
            }

            var lineSpc = LineSpacing;
            height += lineSpc * (lines.Count - 1);

            // set position
            {
                var topOffset = 0d;
                var leftOffset = 0d;

                foreach (LineInfo lineInf in lines)
                {
                    switch (TextAlignment)
                    {
                        case TextAlignment.Left:
                            leftOffset = 0d;
                            break;
                        case TextAlignment.Center:
                            leftOffset = (entireWidth - lineInf.Width) / 2;
                            break;
                        case TextAlignment.Right:
                            leftOffset = entireWidth - lineInf.Width;
                            break;
                    }

                    foreach (CGeometry metry in lineInf.Metries)
                    {
                        metry.Left = leftOffset;
                        switch (metry.TextVerticalAlignment)
                        {
                            case TextVerticalAlignment.Top:
                                metry.Top = topOffset;
                                break;
                            case TextVerticalAlignment.Center:
                                metry.Top = topOffset + (lineInf.Height - metry.Height) / 2;
                                break;
                            case TextVerticalAlignment.Bottom:
                                metry.Top = topOffset + lineInf.Height - metry.Height;
                                break;
                            case TextVerticalAlignment.Base:
                                metry.Top = topOffset + lineInf.BaseHeight - metry.BaseHeight;
                                break;
                        }

                        leftOffset += metry.Width;

                        _metries.Add(metry);
                    }

                    topOffset += lineInf.Height + lineSpc;
                }
            }

            foreach (CGeometry metry in _metries) metry.RepaintRequested += RepaintRequested;

            return new Size(width, height);
        }

        public override void Render(DrawingContext context)
        {
            UpdateGeometry();
            if (Background != null)
            {
                context.FillRectangle(Background, new Rect(0, 0, Bounds.Width, Bounds.Height));
            }

            foreach (var metry in _metries)
            {
                metry.Render(context);
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new CTextBlockAutomationPeer(this);
        }
    }

    public class CTextBlockAutomationPeer : ControlAutomationPeer
    {
        public CTextBlockAutomationPeer(CTextBlock owner) : base(owner)
        { }

        public new CTextBlock Owner
            => (CTextBlock)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.Text;

        protected override string? GetNameCore()
            => Owner.Text;

        protected override bool IsControlElementCore()
            => Owner.TemplatedParent is null && base.IsControlElementCore();
    }


    class LineInfo
    {
        public List<CGeometry> Metries = new();

        public double RequestBaseHeight;
        private double BaseHeight1;
        private double BaseHeight2;

        private double _height;
        private double _dheightTop;
        private double _dheightBtm;

        public double Width { private set; get; }
        public double Height => Math.Max(_height, _dheightTop + _dheightBtm);
        public double BaseHeight => Math.Max(RequestBaseHeight, BaseHeight1 != 0 ? BaseHeight1 : BaseHeight2);

        public bool Add(CGeometry metry)
        {
            Metries.Add(metry);

            Width += metry.Width;

            switch (metry.TextVerticalAlignment)
            {
                case TextVerticalAlignment.Base:
                    Max(ref BaseHeight1, metry.BaseHeight);
                    Max(ref _dheightTop, metry.BaseHeight);
                    Max(ref _dheightBtm, metry.Height - metry.BaseHeight);
                    break;

                case TextVerticalAlignment.Top:
                    Max(ref BaseHeight1, metry.BaseHeight);
                    Max(ref _height, metry.Height);
                    break;

                case TextVerticalAlignment.Center:
                    Max(ref BaseHeight1, metry.Height / 2);
                    Max(ref _height, metry.Height);
                    break;

                case TextVerticalAlignment.Bottom:
                    Max(ref BaseHeight2, metry.BaseHeight);
                    Max(ref _height, metry.Height);
                    break;

                default:
                    Throw("sorry library manager forget to modify.");
                    break;
            }

            return metry.LineBreak;
        }

        public void OverwriteHeight(double height)
        {
            _height = height;
            _dheightBtm = _dheightTop = 0;
        }

        private static void Max(ref double v1, double v2) => v1 = Math.Max(v1, v2);
        private static void Throw(string msg) => throw new InvalidOperationException(msg);
    }
}
