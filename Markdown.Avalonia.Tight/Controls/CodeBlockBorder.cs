#region Copyright 2023 Rahul TR. All Rights Reserved.
// CodeBlockBorder.cs
// Authors : Jayasankar NJ
// Created :  17/06/2023 05:28 PM
#endregion

using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Markdown.Avalonia.Controls
{
    [TemplatePart(Name = CopyButtonPart,Type = typeof(Button))]
    public class CodeBlockBorder : TemplatedControl
    {
        private const string CopyButtonPart = "PART_CopyButton";

        public static readonly DirectProperty<CodeBlockBorder, string> LanguageProperty =
            AvaloniaProperty.RegisterDirect<CodeBlockBorder, string>(nameof(Language), b => b.Language,unsetValue:"");

        public static readonly DirectProperty<CodeBlockBorder, Control?> ChildProperty =
            AvaloniaProperty.RegisterDirect<CodeBlockBorder, Control?>(nameof(Child), b => b.Child);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            if (_button != null)
            {
                _button.Click -= CopyButtonOnClick;
            }
            _button = e.NameScope.Find<Button>(CopyButtonPart);
            if (_button != null)
            {
                _button.Click += CopyButtonOnClick;
            }
        }

        private async void CopyButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (Child != null)
            {
                var t = Child.GetType();
                var textPropertyInfo = t.GetProperty("Text");
                if (textPropertyInfo != null)
                {
                    var text = textPropertyInfo.GetValue(Child, null) as string;
                    IClipboard? clipboard =
                        (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!
                        .MainWindow!.Clipboard;
                    if (clipboard != null) await clipboard.SetTextAsync(text);
                }
            }
        }

        private string _language;
        private Button? _button;
        private Control _child;

        public string Language
        {
            get => _language;
            set => SetAndRaise(LanguageProperty, ref _language, value);
        }
        [Content]
        public Control? Child
        {
            get => _child;
            set => SetAndRaise(ChildProperty, ref _child, value);
        }
    }
}