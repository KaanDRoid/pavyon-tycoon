using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Core
{
   
	public partial class GameManager : Node
	{
		// Singleton instance
		private static GameManager _instance;
		public static GameManager Instance => _instance;

		// Alt sistemlere referanslar
		public TimeManager TimeManager { get; private set; }
		public EconomyManager EconomyManager { get; private set; }
		public StaffManager StaffManager { get; private set; }
		public CustomerManager CustomerManager { get; private set; }
		public BuildingManager BuildingManager { get; private set; }
		public EventManager EventManager { get; private set; }
		public ReputationManager ReputationManager { get; private set; }
		public AIManager AIManager { get; private set; }
		public LocalizationManager LocalizationManager { get; private set; }
		public SaveSystem SaveSystem { get; private set; }

		// Oyun durumları
		public enum GameState
		{
			MainMenu,
			DayMode,   // Sabah modu
			NightMode, // Gece modu
			Event,     // Olay modu
			Paused
		}

		// Mevcut oyun durumu
		public GameState CurrentState { get; private set; } = GameState.MainMenu;

		// Oyun ayarları
		public float GameSpeed { get; private set; } = 1.0f;
		public bool IsPaused { get; private set; } = false;
		
		// Oyuncu bilgileri
		public string PlayerName { get; set; } = "Patron";
		public float Money { get; set; } = 10000.0f; // Başlangıç parası
		
		// Sahip olunan pavyonlar
		public List<Building> OwnedBuildings { get; private set; } = new List<Building>();
		
		// Ana pavyon referansı
		public Building MainPavyon { get; private set; }

		// Sinyaller (Events)
		[Signal]
		public delegate void GameStateChangedEventHandler(GameState newState);
		
		[Signal]
		public delegate void MoneyChangedEventHandler(float newAmount);
		
		[Signal]
		public delegate void DayChangedEventHandler(int newDay);

		// Mevcut oyun günü
		public int CurrentDay { get; private set; } = 1;

		public override void _Ready()
		{
			// Singleton kurulumu
			if (_instance != null)
			{
				QueueFree();
				return;
			}
			_instance = this;
			
			// Alt sistemleri başlat
			InitializeSubsystems();
			
			GD.Print("GameManager başlatıldı.");
		}

		private void InitializeSubsystems()
		{
			// Alt sistemlerin başlatılması
			TimeManager = GetNode<TimeManager>("/root/TimeManager");
			EconomyManager = GetNode<EconomyManager>("/root/EconomyManager");
			StaffManager = GetNode<StaffManager>("/root/StaffManager");
			CustomerManager = GetNode<CustomerManager>("/root/CustomerManager");
			BuildingManager = GetNode<BuildingManager>("/root/BuildingManager");
			EventManager = GetNode<EventManager>("/root/EventManager");
			ReputationManager = GetNode<ReputationManager>("/root/ReputationManager");
			AIManager = GetNode<AIManager>("/root/AIManager");
			LocalizationManager = GetNode<LocalizationManager>("/root/LocalizationManager");
			SaveSystem = GetNode<SaveSystem>("/root/SaveSystem");
			
			// Alt sistem eventlerini dinle
			TimeManager.Connect(TimeManager.SignalName.DayEnded, new Callable(this, nameof(OnDayEnded)));
		}

		public void StartNewGame()
		{
			GD.Print("Yeni oyun başlatılıyor...");
			
			// Yeni oyun için değerleri sıfırla
			Money = 10000.0f;
			CurrentDay = 1;
			
			// Ana pavyonu oluştur
			MainPavyon = BuildingManager.CreateMainPavyon();
			OwnedBuildings.Add(MainPavyon);
			
			// İlk personeli işe al
			StaffManager.HireInitialStaff();
			
			// Gündüz moduna geç
			ChangeGameState(GameState.DayMode);
			
			EmitSignal(SignalName.MoneyChanged, Money);
			EmitSignal(SignalName.DayChanged, CurrentDay);
			
			GD.Print($"Yeni oyun başlatıldı. Güncel para: {Money}, Gün: {CurrentDay}");
		}

		public void LoadGame(string saveName)
		{
			GD.Print($"Oyun yükleniyor: {saveName}");
			SaveSystem.LoadGame(saveName);
		}

		public void SaveGame(string saveName)
		{
			GD.Print($"Oyun kaydediliyor: {saveName}");
			SaveSystem.SaveGame(saveName);
		}

		public void ChangeGameState(GameState newState)
		{
			GD.Print($"Oyun durumu değişiyor: {CurrentState} -> {newState}");
			CurrentState = newState;
			
			switch (newState)
			{
				case GameState.DayMode:
					// Gündüz moduna özgü işlemler
					TimeManager.StartDayMode();
					break;
					
				case GameState.NightMode:
					// Gece moduna özgü işlemler
					TimeManager.StartNightMode();
					break;
					
				case GameState.Event:
					// Olay moduna özgü işlemler
					break;
					
				case GameState.Paused:
					// Oyunu duraklat
					IsPaused = true;
					break;
					
				case GameState.MainMenu:
					// Ana menüye dön
					break;
			}
			
			EmitSignal(SignalName.GameStateChanged, (int)newState);
		}

		public void SetGameSpeed(float speed)
		{
			GameSpeed = speed;
			TimeManager.SetTimeScale(speed);
			GD.Print($"Oyun hızı ayarlandı: {speed}x");
		}

		public void PauseGame(bool pause)
		{
			IsPaused = pause;
			TimeManager.SetPaused(pause);
			GD.Print($"Oyun {(pause ? "duraklatıldı" : "devam ediyor")}");
		}

		public void AddMoney(float amount)
		{
			Money += amount;
			EmitSignal(SignalName.MoneyChanged, Money);
			GD.Print($"Para eklendi: {amount}, Toplam: {Money}");
		}

		public bool TrySpendMoney(float amount)
		{
			if (Money >= amount)
			{
				Money -= amount;
				EmitSignal(SignalName.MoneyChanged, Money);
				GD.Print($"Para harcandı: {amount}, Kalan: {Money}");
				return true;
			}
			
			GD.Print($"Yetersiz para! Gerekli: {amount}, Mevcut: {Money}");
			return false;
		}

		private void OnDayEnded()
		{
			CurrentDay++;
			EmitSignal(SignalName.DayChanged, CurrentDay);
			GD.Print($"Gün sonu. Yeni gün: {CurrentDay}");
			
			// Günlük ekonomi hesapları
			EconomyManager.CalculateDailyFinances();
			
			// Gündüz moduna geç
			ChangeGameState(GameState.DayMode);
		}

		public override void _Process(double delta)
		{
			// Sürekli güncelleme gerektiren işlemler
			if (!IsPaused && (CurrentState == GameState.DayMode || CurrentState == GameState.NightMode))
			{
				TimeManager.UpdateTime((float)delta);
			}
		}
	}
}
