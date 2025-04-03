// src/Core/TimeManager.cs
using Godot;
using System;

namespace PavyonTycoon.Core
{
	public partial class TimeManager : Node
	{
		// Time properties
		public DateTime CurrentTime { get; private set; }
		public int CurrentDay { get; private set; } = 1;
		public bool IsNightMode => CurrentTime.Hour >= 18 || CurrentTime.Hour < 6;
		public bool IsTimePaused { get; private set; } = true;
		
		// Time settings
		[Export] private float timeScale = 1.0f; // How fast time passes
		[Export] private int hourDurationInSeconds = 60; // Real seconds per game hour
		
		// Business hours
		private readonly int openingHour = 18; // 6 PM
		private readonly int closingHour = 6;  // 6 AM
		
		private float timeAccumulator = 0f;
		
		// For string representations
		private string[] turkishMonthNames = new string[] 
		{
			"Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", 
			"Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
		};
		
		public override void _Ready()
		{
			// Initialize with starting time (6 PM)
			CurrentTime = new DateTime(2023, 1, 1, openingHour, 0, 0);
			GD.Print($"⏰ Zaman başlatıldı: {GetFormattedDateTime()}");
		}
		
		public override void _Process(double delta)
		{
			if (!IsTimePaused && GameManager.Instance.CurrentState == GameManager.GameState.NightMode)
			{
				AdvanceTime((float)delta);
			}
		}
		
		public void StartNightMode()
		{
			// Reset time to opening hour
			CurrentTime = new DateTime(CurrentTime.Year, CurrentTime.Month, CurrentTime.Day, openingHour, 0, 0);
			
			// If we crossed midnight in morning mode, add a day
			if (openingHour < closingHour)
			{
				CurrentTime = CurrentTime.AddDays(1);
			}
			
			GD.Print($"🌃 Gece vardiyası başladı: {GetFormattedDateTime()}");
			IsTimePaused = false;
			
			EmitSignal(SignalName.NewDayStarted, CurrentDay);
		}
		
		public void PauseTime()
		{
			IsTimePaused = true;
			GD.Print("⏸️ Zaman durduruldu");
		}
		
		public void ResumeTime()
		{
			IsTimePaused = false;
			GD.Print("▶️ Zaman devam ediyor");
		}
		
		private void AdvanceTime(float deltaSeconds)
		{
			timeAccumulator += deltaSeconds * timeScale;
			
			// Check if enough time has accumulated to advance an hour
			if (timeAccumulator >= hourDurationInSeconds)
			{
				// Advance the time by one hour
				CurrentTime = CurrentTime.AddHours(1);
				timeAccumulator -= hourDurationInSeconds;
				
				GD.Print($"🕰️ Saat ilerledi: {CurrentTime.Hour:00}:00");
				
				// Emit signal for hour change
				EmitSignal(SignalName.HourChanged, CurrentTime.Hour);
				
				// Check for day change (if we've reached closing time)
				if (CurrentTime.Hour == closingHour)
				{
					EndDay();
				}
			}
		}
		
		private void EndDay()
		{
			CurrentDay++;
			GD.Print($"📅 Gün sona erdi, yeni gün: {CurrentDay}");
			
			// Emit signal for day end - bu sinyal EconomyManager ve FurnitureManager tarafından dinleniyor
			EmitSignal(SignalName.DayEnded, CurrentDay);
			
			// Transition to morning mode
			GameManager.Instance.ChangeGameState(GameManager.GameState.MorningMode);
		}
		
		// Set custom time scale (for fast-forwarding, etc.)
		public void SetTimeScale(float newScale)
		{
			timeScale = Mathf.Max(0.1f, newScale);
			GD.Print($"⏱️ Zaman hızı ayarlandı: {timeScale}x");
		}
		
		// Get formatted date and time string (in Turkish)
		public string GetFormattedDateTime()
		{
			return $"{CurrentDay}. Gün, {CurrentTime.Day} {turkishMonthNames[CurrentTime.Month - 1]}, {CurrentTime.Hour:00}:{CurrentTime.Minute:00}";
		}
		
		// Get formatted time string
		public string GetFormattedTime()
		{
			return $"{CurrentTime.Hour:00}:{CurrentTime.Minute:00}";
		}
		
		// Signal definitions
		[Signal] public delegate void HourChangedEventHandler(int hour);
		[Signal] public delegate void DayEndedEventHandler(int day);
		[Signal] public delegate void NewDayStartedEventHandler(int day);
	}
}
