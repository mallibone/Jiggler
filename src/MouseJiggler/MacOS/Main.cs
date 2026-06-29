using AppKit;

namespace MouseJiggler;

public class MainClass
{
	static void Main(string[] args)
	{
		NSApplication.Init();
		NSApplication.SharedApplication.Delegate = new MouseJigglerDelegate();
		NSApplication.Main(args);
	}
}
