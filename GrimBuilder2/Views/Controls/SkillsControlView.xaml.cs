using GrimBuilder2.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace GrimBuilder2.Views.Controls;

public sealed partial class SkillsControlView : UserControl
{
    static readonly DependencyProperty ClassProperty =
        DependencyProperty.Register(nameof(Class), typeof(GdClass), typeof(SkillsControlView), new PropertyMetadata(null));

    public GdClass? Class
    {
        get => (GdClass?)GetValue(ClassProperty);
        set => SetValue(ClassProperty, value);
    }

    public SkillsControlView()
    {
        InitializeComponent();
    }
}
