using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit.TextMate;
using Markdown.Avalonia.Utils;
using Markdown.Avalonia.Controls;
using TextMateSharp.Grammars;

namespace Markdown.Avalonia.SyntaxHigh
{
    public class SyntaxSetup
    {
        public IEnumerable<KeyValuePair<string, Func<Match,ICodeLanguageDetector, Control>>> GetOverrideConverters()
        {
            yield return new KeyValuePair<string, Func<Match,ICodeLanguageDetector, Control>>(
                "CodeBlocksWithLangEvaluator",
                CodeBlocksEvaluator);
        }

        private CodeBlockBorder CodeBlocksEvaluator(Match match,ICodeLanguageDetector? detector)
        {
            var lang = match.Groups[2].Value;
            var code = match.Groups[3].Value;
            // check wheither style is set
            if (!ThemeDetector.IsAvalonEditSetup)
            {
                SetupStyle();

            }

            var txtEdit = new TextEditor();
            txtEdit.Text = code;
            txtEdit.HorizontalAlignment = HorizontalAlignment.Stretch;
            txtEdit.IsReadOnly = true;
            var result = new CodeBlockBorder();
            result.Classes.Add(Markdown.CodeBlockClass);
            result.Child = txtEdit;
            result.Loaded += (sender, args) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var currentTheme = Application.Current.ActualThemeVariant == ThemeVariant.Dark
                        ? ThemeName.DarkPlus
                        : ThemeName.LightPlus;
                    var registryOptions = new RegistryOptions(currentTheme);
                    lang = string.IsNullOrWhiteSpace(lang) ? detector?.DetectLanguage(code) : lang;
                    var language = registryOptions.GetAvailableLanguages().Find(l => l.Aliases.Contains(lang));
                    if (language != null)
                    {
                        var textMateInstallation = txtEdit.InstallTextMate(registryOptions);
                        textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(language.Id));
                    }
                    result.Language = lang;
                    txtEdit.Tag = lang;
                });

            };
            return result;
        }

        private static void SetupStyle()
        {
            if (Application.Current is null)
                return;

            string resourceUriTxt;
            if (ThemeDetector.IsFluentUsed)
                resourceUriTxt = "avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml";
            else if (ThemeDetector.IsSimpleUsed)
                resourceUriTxt = "avares://AvaloniaEdit/Themes/Simple/AvaloniaEdit.xaml";
            else
            {
                Debug.Print("Markdown.Avalonia.SyntaxHigh can't add style for AvaloniaEdit. See https://github.com/whistyun/Markdown.Avalonia/wiki/Setup-AvaloniaEdit-for-syntax-hightlighting");
                return;
            }

            var aeStyle = new StyleInclude(new Uri("avares://Markdown.Avalonia/"))
            {
                Source = new Uri(resourceUriTxt)
            };

            Application.Current.Styles.Add(aeStyle);
        }
    }
}
