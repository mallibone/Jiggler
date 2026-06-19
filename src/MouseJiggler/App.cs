namespace MouseJiggler;

public class App : Application
{
	protected override Window CreateWindow(IActivationState? activationState)
		=> new Window(new MainPage())
		{
			Title = "Mouse Jiggler",
			Width = 460,
			Height = 320,
		};
}
