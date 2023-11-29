using GrimBuilder2.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.Helpers;

static class TextBlockGdMarkupHelper
{
    static readonly StringBuilder tempSB = new();
    public static void SetMarkup(this TextBlock textBlock, string? s)
    {
        textBlock.Inlines.Clear();
        if (string.IsNullOrWhiteSpace(s))
            return;

        // parse markup into inlines
        char lastColor = '\0';
        tempSB.Clear();

        void createNewRun()
        {
            var run = new Run
            {
                Text = tempSB.ToString()
            };
            if (lastColor is not '\0' and not 'w')  // w = white, default color
                run.Foreground = new SolidColorBrush(lastColor switch
                {
                    'o' => Colors.DarkOrange,
                    _ => throw new NotImplementedException()
                });

            textBlock.Inlines.Add(run);
            tempSB.Clear();
        }

        for (int i = 0; i < s.Length; ++i)
            if (s[i] == '^' && i < s.Length - 1)
            {
                createNewRun();
                lastColor = s[++i];
            }
            else
                tempSB.Append(s[i]);
        createNewRun();
    }
}
