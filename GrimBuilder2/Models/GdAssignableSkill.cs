using CommunityToolkit.Mvvm.ComponentModel;
using GrimBuilder2.Core.Models;

namespace GrimBuilder2.Models;

[INotifyPropertyChanged]
public partial class GdAssignableSkill : GdSkill
{
    [ObservableProperty]
    int assignedPoints;

    public GdAssignableSkill? AssignableDependency { get; set; }

    [ObservableProperty]
    bool isDependencyValid, isMasteryValid;

    public static string? GetFrameTexName(bool isDependencyValid, bool isMasteryValid, bool isCircular) =>
        (isDependencyValid, isMasteryValid, isCircular) switch
        {
            (true, true, true) => "/skills/skillallocation/skills_buttonborderroundgreen01.tex",
            (true, true, false) => "/skills/skillallocation/skills_buttonbordergreen01.tex",

            (false, true, true) => "/skills/skillallocation/skills_buttonborderroundred01.tex",
            (false, true, false) => "/skills/skillallocation/skills_buttonborderred01.tex",

            (_, false, true) => "/skills/skillallocation/skills_buttonborderroundgrayout01.tex",
            (_, false, false) => "/skills/skillallocation/skills_buttonbordergrayout01.tex",
        };

    public static bool GetGrayedOut(bool isDependencyValid, bool isMasteryValid) =>
        isDependencyValid == false || isMasteryValid == false;

    public static double GetOpacity(int assignedPoints) =>
        assignedPoints > 0 ? 1 : .5;
}
