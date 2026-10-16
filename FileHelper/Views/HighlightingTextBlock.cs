using System;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace FileHelper.Views;

public class HighlightingTextBlock : TextBlock
{
    public static readonly StyledProperty<string> SearchTextProperty =
        AvaloniaProperty.Register<HighlightingTextBlock, string>(nameof(SearchText), string.Empty);

    public static readonly StyledProperty<IBrush?> MatchBrushProperty =
        AvaloniaProperty.Register<HighlightingTextBlock, IBrush?>(nameof(MatchBrush), Brushes.Gold);

    public static readonly StyledProperty<IBrush?> TextBrushProperty =
        AvaloniaProperty.Register<HighlightingTextBlock, IBrush?>(nameof(TextBrush), Brushes.White);

    public static readonly StyledProperty<IBrush?> BackgroundBrushProperty =
        AvaloniaProperty.Register<HighlightingTextBlock, IBrush?>(nameof(BackgroundBrush), Brushes.Transparent);

    public static readonly StyledProperty<IBrush?> MatchBackgroundBrushProperty =
        AvaloniaProperty.Register<HighlightingTextBlock, IBrush?>(nameof(MatchBackgroundBrush), Brushes.Transparent);

    public static readonly StyledProperty<bool> IsMatchCaseProperty =
        AvaloniaProperty.Register<HighlightingTextBlock, bool>(nameof(IsMatchCase), false);

    public static readonly StyledProperty<bool> IsMatchWholeWordProperty =
        AvaloniaProperty.Register<HighlightingTextBlock, bool>(nameof(IsMatchWholeWord), false);

    public static readonly StyledProperty<bool> IsUseRegularProperty =
        AvaloniaProperty.Register<HighlightingTextBlock, bool>(nameof(IsUseRegular), false);

    public string SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public IBrush? MatchBrush
    {
        get => GetValue(MatchBrushProperty);
        set => SetValue(MatchBrushProperty, value);
    }

    public IBrush? TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public IBrush? BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    public IBrush? MatchBackgroundBrush
    {
        get => GetValue(MatchBackgroundBrushProperty);
        set => SetValue(MatchBackgroundBrushProperty, value);
    }

    public bool IsMatchCase
    {
        get => GetValue(IsMatchCaseProperty);
        set => SetValue(IsMatchCaseProperty, value);
    }

    public bool IsMatchWholeWord
    {
        get => GetValue(IsMatchWholeWordProperty);
        set => SetValue(IsMatchWholeWordProperty, value);
    }

    public bool IsUseRegular
    {
        get => GetValue(IsUseRegularProperty);
        set => SetValue(IsUseRegularProperty, value);
    }

    static HighlightingTextBlock()
    {
        TextProperty.Changed.AddClassHandler<HighlightingTextBlock>((control, _) => control.UpdateText());
        SearchTextProperty.Changed.AddClassHandler<HighlightingTextBlock>((control, _) => control.UpdateText());
        MatchBrushProperty.Changed.AddClassHandler<HighlightingTextBlock>((control, _) => control.UpdateText());
        TextBrushProperty.Changed.AddClassHandler<HighlightingTextBlock>((control, _) => control.UpdateText());
        BackgroundBrushProperty.Changed.AddClassHandler<HighlightingTextBlock>((control, _) => control.UpdateText());
        MatchBackgroundBrushProperty.Changed.AddClassHandler<HighlightingTextBlock>((control, _) => control.UpdateText());
        IsMatchCaseProperty.Changed.AddClassHandler<HighlightingTextBlock>((control, _) => control.UpdateText());
        IsMatchWholeWordProperty.Changed.AddClassHandler<HighlightingTextBlock>((control, _) => control.UpdateText());
        IsUseRegularProperty.Changed.AddClassHandler<HighlightingTextBlock>((control, _) => control.UpdateText());
    }

    private void UpdateText()
    {
        var text = Text ?? string.Empty;
        var highlight = SearchText ?? string.Empty;

        if (Inlines is { } inlines)
        {
            inlines.Clear();
        }
        else
        {
            Inlines = new InlineCollection();
        }

        if (string.IsNullOrEmpty(text))
            return;

        if (string.IsNullOrEmpty(highlight))
        {
            Inlines.Add(new Run(text)
            {
                Foreground = TextBrush ?? Foreground
            });
            return;
        }

        try
        {
            // Формируем опции: учет регистра + инвариантная культура (для стабильного поиска)
            RegexOptions options = (IsMatchCase ? RegexOptions.None : RegexOptions.IgnoreCase) | RegexOptions.CultureInvariant;

            // Если регулярные выражения не включены, экранируем спецсимволы поисковой строки
            string pattern = IsUseRegular ? highlight : Regex.Escape(highlight);

            // Если требуется поиск только целых слов, оборачиваем паттерн в границы слова \b
            if (IsMatchWholeWord)
            {
                pattern = $@"\b(?:{pattern})\b";
            }

            var matches = Regex.Matches(text, pattern, options);
            int startIndex = 0;

            foreach (Match match in matches)
            {
                // Добавляем обычный текст до найденного совпадения
                if (match.Index > startIndex)
                {
                    Inlines.Add(new Run(text.Substring(startIndex, match.Index - startIndex))
                    {
                        Foreground = TextBrush ?? Foreground
                    });
                }

                // Добавляем подсвеченное совпадение
                Inlines.Add(new Run(match.Value)
                {
                    Foreground = MatchBrush ?? Brushes.Gold,
                    Background = MatchBackgroundBrush ?? BackgroundBrush ?? Brushes.Transparent
                });

                startIndex = match.Index + match.Length;
            }

            // Добавляем оставшийся текст после последнего совпадения
            if (startIndex < text.Length)
            {
                Inlines.Add(new Run(text.Substring(startIndex))
                {
                    Foreground = TextBrush ?? Foreground
                });
            }
        }
        catch (ArgumentException)
        {
            // Если пользователь ввел невалидное регулярное выражение, 
            // безопасно отображаем весь текст без подсветки, чтобы приложение не упало
            Inlines.Add(new Run(text)
            {
                Foreground = TextBrush ?? Foreground
            });
        }
    }
}