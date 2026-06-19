using MouseJiggler.Services;

namespace MouseJiggler;

public class MainPage : ContentPage
{
	private const string IdleThresholdKey = "idle_threshold_seconds";
	private const double DefaultIdleThreshold = 30.0;

	private readonly MouseJiggleService _service = new();
	private readonly Switch _runSwitch;
	private readonly Entry _idleEntry;
	private readonly Label _statusLabel;
	private readonly Label _methodLabel;

	public MainPage()
	{
		Title = "Mouse Jiggler";

		var savedThreshold = Preferences.Default.Get(IdleThresholdKey, DefaultIdleThreshold);
		_service.IdleThresholdSeconds = savedThreshold;

		var heading = new Label
		{
			Text = "Mouse Jiggler",
			FontSize = 28,
			FontAttributes = FontAttributes.Bold,
			HorizontalOptions = LayoutOptions.Center,
		};

		_runSwitch = new Switch { HorizontalOptions = LayoutOptions.Start };
		_runSwitch.Toggled += OnRunToggled;

		var runRow = new HorizontalStackLayout
		{
			Spacing = 12,
			Children =
			{
				new Label { Text = "Jiggling", VerticalOptions = LayoutOptions.Center, FontSize = 16 },
				_runSwitch,
			},
		};

		_idleEntry = new Entry
		{
			Text = savedThreshold.ToString("0"),
			Keyboard = Keyboard.Numeric,
			WidthRequest = 80,
			HorizontalOptions = LayoutOptions.Start,
		};
		_idleEntry.Unfocused += OnIdleEntryCommitted;
		_idleEntry.Completed += OnIdleEntryCommitted;

		var idleRow = new HorizontalStackLayout
		{
			Spacing = 12,
			Children =
			{
				new Label { Text = "Idle threshold (seconds)", VerticalOptions = LayoutOptions.Center, FontSize = 16 },
				_idleEntry,
			},
		};

		_statusLabel = new Label { Text = "Stopped.", FontSize = 15 };
		_methodLabel = new Label { Text = string.Empty, FontSize = 13, TextColor = Colors.Gray };

		_service.StatusChanged += OnStatusChanged;

		Content = new VerticalStackLayout
		{
			Spacing = 20,
			Padding = new Thickness(30),
			VerticalOptions = LayoutOptions.Start,
			Children = { heading, runRow, idleRow, _statusLabel, _methodLabel },
		};
	}

	private void OnRunToggled(object? sender, ToggledEventArgs e)
	{
		if (e.Value)
		{
			CommitIdleThreshold();
			_service.Start();
		}
		else
		{
			_service.Stop();
		}
	}

	private void OnIdleEntryCommitted(object? sender, EventArgs e) => CommitIdleThreshold();

	private void CommitIdleThreshold()
	{
		if (double.TryParse(_idleEntry.Text, out var seconds) && seconds > 0)
		{
			_service.IdleThresholdSeconds = seconds;
			Preferences.Default.Set(IdleThresholdKey, seconds);
		}
		else
		{
			// Reset the field to the last valid value.
			_idleEntry.Text = _service.IdleThresholdSeconds.ToString("0");
		}
	}

	private void OnStatusChanged(JiggleStatus status)
	{
		// The service timer fires off the UI thread; marshal back before touching controls.
		Dispatcher.Dispatch(() =>
		{
			if (!status.IsRunning)
			{
				_statusLabel.Text = "Stopped.";
				_methodLabel.Text = string.Empty;
				return;
			}

			_statusLabel.Text = status.JustJiggled
				? $"Jiggled — was idle {status.IdleSeconds:0.0}s."
				: $"Running — idle {status.IdleSeconds:0.0}s of {status.IdleThreshold:0}s.";

			_methodLabel.Text = status.Method == JiggleMethod.SyntheticEvent
				? "Using synthetic mouse events (keeps the Mac active)."
				: "Using cursor warp — grant Accessibility permission for full activity simulation.";
		});
	}
}
