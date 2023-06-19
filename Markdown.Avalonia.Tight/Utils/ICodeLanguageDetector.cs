#region Copyright 2023 Rahul TR. All Rights Reserved.
// ILanguageDetector.cs
// Authors : Jayasankar NJ
// Created :  17/06/2023 02:34 PM
#endregion

namespace Markdown.Avalonia.Utils
{
    public interface ICodeLanguageDetector
    {
        string DetectLanguage(string text);
    }
}