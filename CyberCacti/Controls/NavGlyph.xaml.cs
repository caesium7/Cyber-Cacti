using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberCacti.Controls;

public partial class NavGlyph : UserControl
{
    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(
            nameof(Kind),
            typeof(string),
            typeof(NavGlyph),
            new PropertyMetadata("Home", OnKindChanged));

    public NavGlyph()
    {
        InitializeComponent();
        ApplyKind();
    }

    public string Kind
    {
        get => (string)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavGlyph glyph)
            glyph.ApplyKind();
    }

    private void ApplyKind()
    {
        if (IconPath == null)
            return;

        IconPath.Data = Geometry.Parse(Kind switch
        {
            "Dashboard" => "M4,4 H11 V11 H4 Z M13,4 H20 V11 H13 Z M4,13 H11 V20 H4 Z M13,13 H20 V20 H13 Z",
            "About Us" => "M12,12 A4,4 0 1 0 12,4 A4,4 0 1 0 12,12 Z M6,20 C6,16 9,14 12,14 C15,14 18,16 18,20 Z",
            "Hedgehogs" => "M5,14 C4,8 8,5 12,5 C14,3 18,4 20,8 C22,11 21,16 17,17 C14,20 8,19 6,16 C4,16 4,15 5,14 Z M16,12 A1.2,1.2 0 1 1 16,14.4 A1.2,1.2 0 1 1 16,12 Z",
            "How to Help" => "M9,16.2 L4.8,12 L6.2,10.6 L9,13.4 L17.6,4.8 L19,6.2 Z",
            "More" => "M5,11 A2,2 0 1 0 5,15 A2,2 0 1 0 5,11 Z M12,11 A2,2 0 1 0 12,15 A2,2 0 1 0 12,11 Z M19,11 A2,2 0 1 0 19,15 A2,2 0 1 0 19,11 Z",
            "Innovation Project" => "M12,2 L14.5,9 H22 L16,13.5 L18.5,21 L12,16.5 L5.5,21 L8,13.5 L2,9 H9.5 Z",
            "Contact Us" => "M3,6 H21 V18 H3 Z M3,6 L12,13 L21,6",
            "Dash Game" => "M8,5 L20,12 L8,19 Z",
            _ => "M4,12 L12,4 L20,12 V20 H14 V14 H10 V20 H4 Z"
        });
    }
}
