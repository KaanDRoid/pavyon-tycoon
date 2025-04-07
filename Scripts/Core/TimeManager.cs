// PavyonTycoon/Scripts/Core/TimeManager.cs
using Godot;
using System;
using System.Collections.Generic;


public partial class TimeManager : Node
{
	// Zaman sabitleri
	public const float REAL_SECONDS_PER_GAME_MINUTE = 0.5f; // 0.5 gerçek saniye = 1 oyun dakikası
	public const int MINUTES_PER_HOUR = 60;
	public const int HOURS_PER_DAY = 24;
	public const int DAYS_PER_WEEK = 7;
	public const int WEEKS_PER_MONTH = 4;
	public const int MONTHS_PER_YEAR = 12;
	public const int WORKING_HOUR_START = 18; // Pavyon açılış saati (18:00)
	public const int WORKING_HOUR_END = 4;    // Pavyon kapanış saati (04:00)

	// Haftanın günleri
	public enum DayOfWeek
	{
		Pazartesi = 0,
		Salı = 1,
		Çarşamba = 2,
		Perşembe = 3,
		Cuma = 4,
		Cumartesi = 5,
		Pazar = 6
	}

	// Aylar
	public enum Month
	{
		Ocak = 0,
		Şubat = 1,
		Mart = 2,
		Nisan = 3,
		Mayıs = 4,
		Haziran = 5,
		Temmuz = 6,
		Ağustos = 7,
		Eylül = 8,
		Ekim = 9,
		Kasım = 10,
		Aralık = 11
	}

	// Her ayın gün sayısı
	private readonly int[] _daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

	// Oyun zamanı durumu
	public enum TimeState
	{
		Morning,     // Sabah (05:00 - 17:59) - Sabah modu
		Evening,     // Akşam (18:00 - 04:59) - Gece modu
		Paused       // Durduruldu
	}

	// Özel günler ve etkinlikler
	private Dictionary<string, DateInfo> _specialDates = new Dictionary<string, DateInfo>();

	// Tarih bilgisi sınıfı
	public class DateInfo
	{
		public int Day;
		public int Month;
		public bool IsYearly;
		public string EventName;
		public bool IsHoliday;

		public DateInfo(int day, int month, string eventName, bool isYearly = true, bool isHoliday = false)
		{
			Day = day;
			Month = month;
			EventName = eventName;
			IsYearly = isYearly;
			IsHoliday = isHoliday;
		}
	}

	// Mevcut zaman durumu
	private TimeState _currentTimeState = TimeState.Morning;
	public TimeState CurrentTimeState 
	{ 
		get => _currentTimeState;
		private set
		{
			if (_currentTimeState != value)
			{
				_currentTimeState = value;
				EmitSignal(SignalName.TimeStateChanged, (int)_currentTimeState);
			}
		}
	}

	// Zaman değişkenleri
	private int _currentDay = 1;
	private int _currentDayOfWeek = 0; // Pazartesi
	private int _currentMonth = 0;    // Ocak
	private int _currentYear = 1;
	private int _currentWeek = 1;
	
	public int CurrentDay
	{
		get => _currentDay;
		private set
		{
			if (_currentDay != value)
			{
				_currentDay = value;
				
				// Ay değişikliği kontrolü
				if (_currentDay > _daysInMonth[_currentMonth])
				{
					_currentDay = 1;
					_currentMonth = (_currentMonth + 1) % MONTHS_PER_YEAR;
					
					if (_currentMonth == 0)
					{
						_currentYear++;
						OnYearChanged(_currentYear);
					}
					
					OnMonthChanged(_currentMonth);
				}
				
				// Hafta değişikliği kontrolü
				_currentDayOfWeek = (_currentDayOfWeek + 1) % DAYS_PER_WEEK;
				if (_currentDayOfWeek == 0) // Yeni hafta (Pazartesi)
				{
					_currentWeek++;
					if (_currentWeek > WEEKS_PER_MONTH)
					{
						_currentWeek = 1;
					}
					OnWeekChanged(_currentWeek);
				}
				
				OnDayChanged(_currentDay);
				OnDayOfWeekChanged(_currentDayOfWeek);
				
				// Özel gün kontrolü
				CheckForSpecialDates();
			}
		}
	}

	private int _currentHour = 9;     // Oyun 09:00'da başlar
	private int _currentMinute = 0;
	private float _minuteTimer = 0f;
	private bool _timeRunning = false;
	private float _timeScale = 1.0f;  // Zaman hızlandırma çarpanı

	// Hız ayarları
	public enum TimeSpeed
	{
		Normal = 1,
		Fast = 2,
		VeryFast = 4,
		SuperFast = 8
	}

	// Sinyaller (Events)
	[Signal]
	public delegate void TimeStateChangedEventHandler(int newTimeState);

	[Signal]
	public delegate void HourChangedEventHandler(int newHour);

	[Signal]
	public delegate void MinuteChangedEventHandler(int newMinute);

	public delegate void DayChangedEventHandler(int newDay);
	public event DayChangedEventHandler DayChanged;

	public delegate void DayOfWeekChangedEventHandler(int newDayOfWeek);
	public event DayOfWeekChangedEventHandler DayOfWeekChanged;

	public delegate void WeekChangedEventHandler(int newWeek);
	public event WeekChangedEventHandler WeekChanged;

	public delegate void MonthChangedEventHandler(int newMonth);
	public event MonthChangedEventHandler MonthChanged;

	public delegate void YearChangedEventHandler(int newYear);
	public event YearChangedEventHandler YearChanged;

	[Signal]
	public delegate void WorkHoursStartedEventHandler();

	[Signal]
	public delegate void WorkHoursEndedEventHandler();

	[Signal]
	public delegate void SpecialDateEventHandler(string eventName, bool isHoliday);

	// Public properties
	public int CurrentDayOfWeek => _currentDayOfWeek;
	public DayOfWeek CurrentDayOfWeekEnum => (DayOfWeek)_currentDayOfWeek;
	public int CurrentMonth => _currentMonth;
	public Month CurrentMonthEnum => (Month)_currentMonth;
	public int CurrentYear => _currentYear;
	public int CurrentWeek => _currentWeek;
	public int CurrentHour => _currentHour;
	public int CurrentMinute => _currentMinute;
	public bool IsWeekend => _currentDayOfWeek == 5 || _currentDayOfWeek == 6; // Cumartesi veya Pazar
	public bool IsWorkingHours => (_currentHour >= WORKING_HOUR_START) || (_currentHour < WORKING_HOUR_END);

	public override void _Ready()
	{
		GD.Print("TimeManager initialized.");
		InitializeSpecialDates();
		UpdateTimeState();
	}

	private void InitializeSpecialDates()
	{
		// Resmi tatil ve özel günleri ekle
		_specialDates.Add("new_year", new DateInfo(1, 0, "Yılbaşı", true, true));
		_specialDates.Add("national_sovereignty", new DateInfo(23, 3, "Ulusal Egemenlik ve Çocuk Bayramı", true, true));
		_specialDates.Add("labor_day", new DateInfo(1, 4, "İşçi Bayramı", true, true));
		_specialDates.Add("youth_day", new DateInfo(19, 4, "Gençlik ve Spor Bayramı", true, true));
		_specialDates.Add("democracy_day", new DateInfo(15, 6, "Demokrasi ve Milli Birlik Günü", true, true));
		_specialDates.Add("victory_day", new DateInfo(30, 7, "Zafer Bayramı", true, true));
		_specialDates.Add("republic_day", new DateInfo(29, 9, "Cumhuriyet Bayramı", true, true));
		
		// Oyun içi özel günler
		_specialDates.Add("pavyon_anniversary", new DateInfo(15, 2, "Pavyon Kuruluş Yıldönümü", true, false));
		_specialDates.Add("vip_night", new DateInfo(10, 5, "VIP Müşteri Gecesi", true, false));
		_specialDates.Add("special_performance", new DateInfo(5, 8, "Özel Performans Gecesi", true, false));
		
		GD.Print("Special dates initialized.");
	}

	public override void _Process(double delta)
	{
		if (!_timeRunning)
			return;

		// Zaman ilerleme mantığı
		_minuteTimer += (float)delta * _timeScale;
		
		// Yeterli süre geçtiyse dakikayı artır
		if (_minuteTimer >= REAL_SECONDS_PER_GAME_MINUTE)
		{
			_minuteTimer -= REAL_SECONDS_PER_GAME_MINUTE;
			IncrementMinute();
		}
	}

	private void IncrementMinute()
	{
		_currentMinute++;
		EmitSignal(SignalName.MinuteChanged, _currentMinute);
		
		if (_currentMinute >= MINUTES_PER_HOUR)
		{
			_currentMinute = 0;
			IncrementHour();
		}
	}

	private void IncrementHour()
	{
		_currentHour++;
		EmitSignal(SignalName.HourChanged, _currentHour);
		
		// Mesai saati kontrolü
		if (_currentHour == WORKING_HOUR_START)
		{
			EmitSignal(SignalName.WorkHoursStarted);
			GD.Print("Pavyon açılış saati!");
		}
		else if (_currentHour == WORKING_HOUR_END)
		{
			EmitSignal(SignalName.WorkHoursEnded);
			GD.Print("Pavyon kapanış saati!");
		}
		
		if (_currentHour >= HOURS_PER_DAY)
		{
			_currentHour = 0;
			CurrentDay++;
		}
		
		UpdateTimeState();
	}

	private void UpdateTimeState()
	{
		// Zaman durumunu güncelle
		if (_currentHour >= WORKING_HOUR_START || _currentHour < WORKING_HOUR_END)
		{
			CurrentTimeState = TimeState.Evening;
			
			// Oyun durumunu da güncelle
			if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Paused)
			{
				GameManager.Instance.CurrentState = GameManager.GameState.NightMode;
			}
		}
		else
		{
			CurrentTimeState = TimeState.Morning;
			
			// Oyun durumunu da güncelle
			if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Paused)
			{
				GameManager.Instance.CurrentState = GameManager.GameState.MorningMode;
			}
		}
	}

	private void CheckForSpecialDates()
	{
		bool foundSpecialDate = false;
		
		foreach (var specialDate in _specialDates.Values)
		{
			if (specialDate.Day == _currentDay && specialDate.Month == _currentMonth)
			{
				if (!specialDate.IsYearly || (specialDate.IsYearly && _currentYear > 1))
				{
					EmitSignal(SignalName.SpecialDate, specialDate.EventName, specialDate.IsHoliday);
					GD.Print($"Özel Gün: {specialDate.EventName}");
					foundSpecialDate = true;
				}
			}
		}
		
		if (foundSpecialDate && GameManager.Instance != null)
		{
			GameManager.Instance.ShowGameNotification("Bugün özel bir gün!", GameManager.NotificationType.Info);
		}
	}

	private void OnDayChanged(int newDay)
	{
		DayChanged?.Invoke(newDay);
		GD.Print($"Day changed to: {newDay}");
	}

	private void OnDayOfWeekChanged(int newDayOfWeek)
	{
		DayOfWeekChanged?.Invoke(newDayOfWeek);
		GD.Print($"Day of week changed to: {(DayOfWeek)newDayOfWeek}");
	}

	private void OnWeekChanged(int newWeek)
	{
		WeekChanged?.Invoke(newWeek);
		GD.Print($"Week changed to: {newWeek}");
	}

	private void OnMonthChanged(int newMonth)
	{
		MonthChanged?.Invoke(newMonth);
		GD.Print($"Month changed to: {(Month)newMonth}");
	}

	private void OnYearChanged(int newYear)
	{
		YearChanged?.Invoke(newYear);
		GD.Print($"Year changed to: {newYear}");
	}

	// Zaman kontrolü için public metodlar
	public void StartGameTime()
	{
		_timeRunning = true;
		GD.Print("Game time started.");
	}

	public void PauseGameTime()
	{
		_timeRunning = false;
		CurrentTimeState = TimeState.Paused;
		GD.Print("Game time paused.");
	}

	public void ResumeGameTime()
	{
		_timeRunning = true;
		UpdateTimeState();
		GD.Print("Game time resumed.");
	}

	public void SetTimeSpeed(TimeSpeed speed)
	{
		_timeScale = (float)speed;
		GD.Print($"Time speed set to: {speed}");
	}

	// Zamanı özel bir saate ayarla
	public void SetTime(int hour, int minute)
	{
		_currentHour = Mathf.Clamp(hour, 0, 23);
		_currentMinute = Mathf.Clamp(minute, 0, 59);
		UpdateTimeState();
		GD.Print($"Time set to: {_currentHour:D2}:{_currentMinute:D2}");
	}

	// Tarihi özel bir güne ayarla
	public void SetDate(int day, int month, int year, int dayOfWeek = -1)
	{
		_currentMonth = Mathf.Clamp(month, 0, 11);
		_currentDay = Mathf.Clamp(day, 1, _daysInMonth[_currentMonth]);
		_currentYear = Mathf.Max(1, year);
		
		if (dayOfWeek >= 0 && dayOfWeek < DAYS_PER_WEEK)
		{
			_currentDayOfWeek = dayOfWeek;
		}
		
		OnDayChanged(_currentDay);
		OnMonthChanged(_currentMonth);
		OnYearChanged(_currentYear);
		OnDayOfWeekChanged(_currentDayOfWeek);
		
		CheckForSpecialDates();
		GD.Print($"Date set to: {_currentDay}/{_currentMonth + 1}/{_currentYear}, {(DayOfWeek)_currentDayOfWeek}");
	}

	// Günü ilerlet
	public void AdvanceDay(int days = 1)
	{
		for (int i = 0; i < days; i++)
		{
			CurrentDay++;
		}
		GD.Print($"Advanced {days} days. Current date: {_currentDay}/{_currentMonth + 1}/{_currentYear}, {(DayOfWeek)_currentDayOfWeek}");
	}

	// Mevcut zamanı string olarak formatla
	public string GetFormattedTime()
	{
		return $"{_currentHour:D2}:{_currentMinute:D2}";
	}

	// Mevcut tarihi string olarak formatla
	public string GetFormattedDate()
	{
		return $"{_currentDay:D2}/{(_currentMonth + 1):D2}/{_currentYear}";
	}

	// Mevcut tarihi ve günü formatla
	public string GetFormattedDateWithDay()
	{
		return $"{(DayOfWeek)_currentDayOfWeek}, {_currentDay:D2}/{(_currentMonth + 1):D2}/{_currentYear}";
	}

	// Mevcut hafta içi ya da hafta sonu olduğunu kontrol et
	public bool IsWeekDay()
	{
		return !IsWeekend;
	}

	// Bir sonraki özel günü bul
	public DateInfo GetNextSpecialDate()
	{
		DateInfo nextSpecialDate = null;
		int minDaysAway = int.MaxValue;
		
		foreach (var specialDate in _specialDates.Values)
		{
			int monthDiff = specialDate.Month - _currentMonth;
			if (monthDiff < 0) monthDiff += 12;
			
			int dayDiff = specialDate.Day - _currentDay;
			if (dayDiff < 0 && monthDiff == 0) dayDiff += _daysInMonth[_currentMonth];
			
			int totalDaysAway = monthDiff * 30 + dayDiff; // Yaklaşık hesaplama
			
			if (totalDaysAway < minDaysAway && totalDaysAway > 0)
			{
				minDaysAway = totalDaysAway;
				nextSpecialDate = specialDate;
			}
		}
		
		return nextSpecialDate;
	}

	// Özel bir gün ekle
	public void AddSpecialDate(string key, int day, int month, string eventName, bool isYearly = true, bool isHoliday = false)
	{
		if (!_specialDates.ContainsKey(key))
		{
			_specialDates.Add(key, new DateInfo(day, month, eventName, isYearly, isHoliday));
			GD.Print($"Added special date: {eventName} on {day}/{month + 1}");
		}
	}

	// Özel gün çıkar
	public void RemoveSpecialDate(string key)
	{
		if (_specialDates.ContainsKey(key))
		{
			_specialDates.Remove(key);
			GD.Print($"Removed special date with key: {key}");
		}
	}

	// Bir günün özel gün olup olmadığını kontrol et
	public bool IsSpecialDate(int day, int month)
	{
		foreach (var specialDate in _specialDates.Values)
		{
			if (specialDate.Day == day && specialDate.Month == month)
			{
				return true;
			}
		}
		return false;
	}

	// İş saatleri içinde olup olmadığını kontrol et
	public bool IsInWorkingHours()
	{
		if (_currentHour >= WORKING_HOUR_START || _currentHour < WORKING_HOUR_END)
		{
			return true;
		}
		return false;
	}

	// Özel gün tipini kontrol et
	public bool IsHoliday()
	{
		foreach (var specialDate in _specialDates.Values)
		{
			if (specialDate.Day == _currentDay && specialDate.Month == _currentMonth && specialDate.IsHoliday)
			{
				return true;
			}
		}
		return false;
	}
}
