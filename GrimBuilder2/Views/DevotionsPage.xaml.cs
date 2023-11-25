using GrimBuilder2.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace GrimBuilder2.Views;

public sealed partial class DevotionsPage : Page
{
    public DevotionsViewModel ViewModel { get; } = App.GetService<DevotionsViewModel>();

    public DevotionsPage()
    {
        InitializeComponent();
    }

    Point lastDrag;
    bool dragging;
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
        {
            (dragging, lastDrag) = (true, e.GetCurrentPoint(this).Position);
            CapturePointer(e.Pointer);
        }
        else
            base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        if (dragging && e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
        {
            var currentDrag = e.GetCurrentPoint(this).Position;
            var delta = new Point(currentDrag.X - lastDrag.X, currentDrag.Y - lastDrag.Y);

            var matrix = MapMatrixTransform.Matrix;
            matrix.OffsetX += delta.X;
            matrix.OffsetY += delta.Y;
            MapMatrixTransform.Matrix = matrix;

            lastDrag = currentDrag;
        }
        else
            base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        if (dragging && e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
        {
            ReleasePointerCaptures();
            dragging = false;
        }
        else
            base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
        {
            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            var matrix = MapMatrixTransform.Matrix;
            matrix.M11 += delta / 1000.0;
            matrix.M22 += delta / 1000.0;
            MapMatrixTransform.Matrix = matrix;
        }
        else
            base.OnPointerWheelChanged(e);
    }
}
