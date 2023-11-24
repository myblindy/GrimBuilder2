using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using GrimBuilder2.Core.Models;
using Microsoft.UI.Xaml.Documents;
using System.Text;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace GrimBuilder2.Views.ToolTips;

public sealed partial class SkillToolTip : UserControl
{
    public static readonly DependencyProperty SkillProperty =
        DependencyProperty.Register(nameof(Skill), typeof(GdSkill), typeof(SkillToolTip), new(null, OnSkillPropertyChanged));

    public GdSkill? Skill
    {
        get => (GdSkill?)GetValue(SkillProperty);
        set => SetValue(SkillProperty, value);
    }

    static readonly StringBuilder tempSB = new();
    private static void OnSkillPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tt = (SkillToolTip)d;
        if (e.NewValue is not GdSkill { } skill || string.IsNullOrWhiteSpace(skill.Description))
            tt.DescriptionTextBlock.Text = null;
        else
        {
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

                tt.DescriptionTextBlock.Inlines.Add(run);
                tempSB.Clear();
            }

            tt.DescriptionTextBlock.Inlines.Clear();
            for (int i = 0; i < skill.Description.Length; ++i)
                if (skill.Description[i] == '^' && i < skill.Description.Length - 1)
                {
                    createNewRun();
                    lastColor = skill.Description[++i];
                }
                else
                    tempSB.Append(skill.Description[i]);
            createNewRun();
        }
    }

    public SkillToolTip()
    {
        InitializeComponent();
    }
}
