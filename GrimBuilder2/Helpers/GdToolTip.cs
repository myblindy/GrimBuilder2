using GrimBuilder2.Core.Models;
using GrimBuilder2.Views.ToolTips;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrimBuilder2.Helpers;

static class GdToolTip
{
    public static readonly DependencyProperty SkillProperty =
        DependencyProperty.RegisterAttached("Skill", typeof(GdSkill), typeof(GdToolTip), new(null, OnSkillChanged));

    public static GdSkill? GetSkill(DependencyObject obj) => (GdSkill?)obj.GetValue(SkillProperty);
    public static void SetSkill(DependencyObject obj, GdSkill value) => obj.SetValue(SkillProperty, value);
    static void OnSkillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GdSkill { } skill)
            ToolTipService.SetToolTip(d, new SkillToolTip { Skill = skill });
        else
            ToolTipService.SetToolTip(d, null);
    }
}
