using Microsoft.Maui.Controls.Shapes;
using MouseJiggler.Services;

namespace MouseJiggler;

public class MainPage : ContentPage
{
	private const string IdleThresholdKey = "idle_threshold_seconds";
	private const double DefaultIdleThreshold = 30.0;

	// Palette
	private static readonly Color Accent = Color.FromArgb("#2F6BFF");
	private static readonly Color Green = Color.FromArgb("#34C759");
	private static readonly Color Amber = Color.FromArgb("#FF9F0A");
	private static readonly Color Red = Color.FromArgb("#FF3B30");
	private static readonly Color Gray = Color.FromArgb("#8E8E93");

	private readonly MouseJiggleService _service = new(MouseInputFactory.Create());

	private readonly Border _statusDot;
	private readonly Label _statusText;
	private readonly Label _methodText;
	private readonly ProgressBar _idleProgress;
	private readonly Label _idleCaption;
	private readonly Label _countLabel;
	private readonly Label _lastLabel;
	private readonly Button _runButton;
	private readonly Label _thresholdValue;
	private readonly Stepper _thresholdStepper;
	private readonly Border _permissionBanner;

	public MainPage()
	{
		Title = "Mouse Jiggler";
		this.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#F2F2F7"), Color.FromArgb("#1A1A1C"));

		var savedThreshold = Preferences.Default.Get(IdleThresholdKey, DefaultIdleThreshold);
		_service.IdleThresholdSeconds = savedThreshold;

		// ---- Header --------------------------------------------------------
		var logo = new Border
		{
			WidthRequest = 46,
			HeightRequest = 46,
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 12 },
			// Note: the maui-labs Border handler doesn't paint a gradient Background brush,
			// so use a solid colour (matches the Accent used on the primary button).
			BackgroundColor = Color.FromArgb("#1E73E6"),
			Content = new Border
			{
				WidthRequest = 14,
				HeightRequest = 22,
				BackgroundColor = Colors.White,
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 7 },
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
			},
		};

		var title = new Label { Text = "Mouse Jiggler", FontSize = 22, FontAttributes = FontAttributes.Bold };
		title.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
		var subtitle = new Label { Text = "Keeps your computer awake", FontSize = 13, TextColor = Gray };

		var header = new HorizontalStackLayout
		{
			Spacing = 14,
			Children =
			{
				logo,
				new VerticalStackLayout
				{
					Spacing = 1,
					VerticalOptions = LayoutOptions.Center,
					Children = { title, subtitle },
				},
			},
		};

		// ---- Status card ---------------------------------------------------
		_statusDot = new Border
		{
			WidthRequest = 13,
			HeightRequest = 13,
			BackgroundColor = Gray,
			StrokeThickness = 0,
			StrokeShape = new Ellipse(),
			VerticalOptions = LayoutOptions.Center,
		};
		_statusText = new Label { Text = "Paused", FontSize = 19, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };
		_statusText.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
		_methodText = new Label { Text = "Idle and ready.", FontSize = 12.5, TextColor = Gray };
		_idleProgress = new ProgressBar { Progress = 0, ProgressColor = Accent, Margin = new Thickness(0, 4, 0, 0) };
		_idleCaption = new Label { Text = string.Empty, FontSize = 11.5, TextColor = Gray };

		var statusCard = Card(new VerticalStackLayout
		{
			Spacing = 8,
			Children =
			{
				new HorizontalStackLayout { Spacing = 9, Children = { _statusDot, _statusText } },
				_methodText,
				_idleProgress,
				_idleCaption,
			},
		});

		// ---- Indicator card (the headline feedback) ------------------------
		_countLabel = new Label { Text = "0", FontSize = 38, FontAttributes = FontAttributes.Bold };
		_countLabel.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
		var countCaption = new Label { Text = "jiggles", FontSize = 12, TextColor = Gray, VerticalOptions = LayoutOptions.End, Margin = new Thickness(0, 0, 0, 8) };
		_lastLabel = new Label { Text = "Last: —", FontSize = 12.5, TextColor = Gray };

		var indicatorCard = Card(new VerticalStackLayout
		{
			Spacing = 2,
			Children =
			{
				new HorizontalStackLayout { Spacing = 7, Children = { _countLabel, countCaption } },
				_lastLabel,
			},
		});

		// ---- Controls ------------------------------------------------------
		_runButton = new Button
		{
			Text = "Start",
			FontSize = 16,
			FontAttributes = FontAttributes.Bold,
			TextColor = Colors.White,
			BackgroundColor = Accent,
			CornerRadius = 10,
			HeightRequest = 46,
		};
		_runButton.Clicked += OnRunClicked;

		_thresholdValue = new Label
		{
			Text = $"{savedThreshold:0} s",
			FontSize = 14,
			FontAttributes = FontAttributes.Bold,
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Center,
		};
		_thresholdValue.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);
		_thresholdStepper = new Stepper
		{
			Minimum = 5,
			Maximum = 600,
			Increment = 5,
			Value = savedThreshold,
			VerticalOptions = LayoutOptions.Center,
		};
		_thresholdStepper.ValueChanged += OnThresholdChanged;

		var thresholdLabel = new Label { Text = "Idle threshold", FontSize = 14, VerticalOptions = LayoutOptions.Center };
		thresholdLabel.SetAppThemeColor(Label.TextColorProperty, Colors.Black, Colors.White);

		var thresholdRow = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Auto),
			},
			ColumnSpacing = 10,
			Children = { thresholdLabel, _thresholdValue, _thresholdStepper },
		};
		Grid.SetColumn(_thresholdValue, 1);
		Grid.SetColumn(_thresholdStepper, 2);

		var testButton = new Button
		{
			Text = "Test jiggle now",
			FontSize = 14,
			TextColor = Accent,
			BackgroundColor = Colors.Transparent,
			BorderColor = Accent,
			BorderWidth = 1,
			CornerRadius = 10,
			HeightRequest = 40,
		};
		testButton.Clicked += OnTestClicked;

		var controlsCard = Card(new VerticalStackLayout
		{
			Spacing = 14,
			Children = { _runButton, thresholdRow, testButton },
		});

		// ---- Permission banner (hidden unless events are blocked) ----------
		var bannerText = new Label
		{
			Text = "Synthetic events are blocked. Grant Accessibility, then quit and reopen the app.",
			FontSize = 12.5,
			TextColor = Color.FromArgb("#7A4E00"),
		};
		var bannerButton = new Button
		{
			Text = "Open Accessibility Settings",
			FontSize = 13,
			TextColor = Colors.White,
			BackgroundColor = Amber,
			CornerRadius = 8,
			HeightRequest = 38,
		};
		bannerButton.Clicked += OnOpenSettingsClicked;
		_permissionBanner = new Border
		{
			IsVisible = false,
			BackgroundColor = Color.FromArgb("#FFF4DE"),
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 14 },
			Padding = new Thickness(16),
			Content = new VerticalStackLayout { Spacing = 10, Children = { bannerText, bannerButton } },
		};

		_service.StatusChanged += OnStatusChanged;

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Spacing = 16,
				Padding = new Thickness(22, 26),
				Children = { header, statusCard, indicatorCard, controlsCard, _permissionBanner },
			},
		};
	}

	private static Border Card(View content)
	{
		var card = new Border
		{
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 16 },
			Padding = new Thickness(18),
			Content = content,
			Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.12f, Radius = 14, Offset = new Point(0, 4) },
		};
		card.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#2C2C2E"));
		return card;
	}

	private void OnRunClicked(object? sender, EventArgs e)
	{
		if (_service.IsRunning)
			_service.Stop();
		else
			_service.Start();
	}

	private void OnTestClicked(object? sender, EventArgs e) => _service.JiggleNow();

	private async void OnOpenSettingsClicked(object? sender, EventArgs e)
	{
#if MACOS
		try
		{
			await Launcher.Default.OpenAsync(
				"x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility");
		}
		catch
		{
			// Settings URL scheme not available — nothing actionable to do.
		}
#else
		// The permission banner (and this button) only surface on macOS, where synthetic
		// events can be blocked. On other platforms there is no permission gate.
		await Task.CompletedTask;
#endif
	}

	private void OnThresholdChanged(object? sender, ValueChangedEventArgs e)
	{
		var seconds = Math.Round(e.NewValue);
		_service.IdleThresholdSeconds = seconds;
		_thresholdValue.Text = $"{seconds:0} s";
		Preferences.Default.Set(IdleThresholdKey, seconds);
	}

	private void OnStatusChanged(JiggleStatus status)
	{
		// The service timer fires off the UI thread; marshal back before touching controls.
		Dispatcher.Dispatch(() => ApplyStatus(status));
	}

	private void ApplyStatus(JiggleStatus status)
	{
		_runButton.Text = status.IsRunning ? "Stop" : "Start";
		_runButton.BackgroundColor = status.IsRunning ? Red : Accent;

		_countLabel.Text = status.JiggleCount.ToString("N0");
		_lastLabel.Text = status.LastJiggleAt is { } at
			? $"Last: {at.LocalDateTime:HH:mm:ss}"
			: "Last: —";

		if (!status.IsRunning)
		{
			_statusDot.BackgroundColor = Gray;
			_statusText.Text = "Paused";
			_methodText.Text = "Idle and ready.";
			_idleProgress.Progress = 0;
			_idleCaption.Text = string.Empty;
			_permissionBanner.IsVisible = false;
			return;
		}

		_statusText.Text = "Active";
		_idleProgress.Progress = status.IdleThreshold > 0
			? Math.Clamp(status.IdleSeconds / status.IdleThreshold, 0, 1)
			: 0;
		_idleCaption.Text = $"idle {status.IdleSeconds:0}s / {status.IdleThreshold:0}s";

		switch (status.Method)
		{
			case JiggleMethod.SyntheticActive:
				_statusDot.BackgroundColor = Green;
				_methodText.Text = "Synthetic events — keeping your computer active.";
				_permissionBanner.IsVisible = false;
				break;
			case JiggleMethod.WarpFallback:
				_statusDot.BackgroundColor = Amber;
				_methodText.Text = "Cursor warp — events blocked (cursor moves only).";
				_permissionBanner.IsVisible = true;
				break;
			default:
				_statusDot.BackgroundColor = Accent;
				_methodText.Text = "Running — detecting best method…";
				_permissionBanner.IsVisible = false;
				break;
		}

		if (status.JustJiggled)
		{
			Pulse(_countLabel);
			Pulse(_statusDot);
		}
	}

	private static async void Pulse(VisualElement view)
	{
		await view.ScaleToAsync(1.18, 110, Easing.CubicOut);
		await view.ScaleToAsync(1.0, 110, Easing.CubicIn);
	}
}
