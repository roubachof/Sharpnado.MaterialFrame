namespace Sharpnado.Acrylic.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new NavigationPage(new MainPage())
        {
            BarBackgroundColor = Colors.Transparent,
            BarTextColor = Colors.Transparent
        };
    }
}
