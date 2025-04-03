// src/UI/GameUI.cs
using Godot;
using System;
using PavyonTycoon.Core;
using PavyonTycoon.Staff;
using PavyonTycoon.UI.Staff;

namespace PavyonTycoon.UI
{
	public partial class GameUI : Control
	{
		// UI Elements
		private Label dayLabel;
		private Label timeLabel;
		private Label moneyLabel;
		private Button pauseButton;
		private Button playButton;
		private Button fastButton;
		private Button furnitureButton;
		private Button staffButton;
		private RichTextLabel logText;
		
		// Personel UI elemanları
		private Control staffInfoPanel;
		private Control staffManagementUI;
		private Control staffHiringUI;
		
		// Müşteri durum paneli için UI elemanları
		private Label customerCountLabel;
		private Label popularityLabel;
		
		// Time speed factors
		private const float NORMAL_SPEED = 1.0f;
		private const float FAST_SPEED = 3.0f;
		
		public override void _Ready()
		{
			// Get references to UI elements
			dayLabel = GetNode<Label>("TopPanel/DayLabel");
			timeLabel = GetNode<Label>("TopPanel/TimeLabel");
			moneyLabel = GetNode<Label>("TopPanel/MoneyLabel");
			pauseButton = GetNode<Button>("ControlPanel/PauseButton");
			playButton = GetNode<Button>("ControlPanel/PlayButton");
			fastButton = GetNode<Button>("ControlPanel/FastButton");
			furnitureButton = GetNode<Button>("SidePanel/FurnitureButton");
			staffButton = GetNode<Button>("SidePanel/StaffButton");
			logText = GetNode<RichTextLabel>("LogPanel/LogText");
			
			// Personel UI elemanlarına referans al
			staffInfoPanel = GetNode<Control>("SidePanel/StaffInfoPanel");
			
			// Müşteri durum paneli referansları
			customerCountLabel = GetNode<Label>("SidePanel/CustomerCountLabel");
			popularityLabel = GetNode<Label>("SidePanel/PopularityLabel");
			
			// Connect signals
			pauseButton.Pressed += OnPausePressed;
			playButton.Pressed += OnPlayPressed;
			fastButton.Pressed += OnFastPressed;
			furnitureButton.Pressed += OnFurnitureButtonPressed;
			staffButton.Pressed += OnStaffButtonPressed;
			
			// Personel bilgi panelini yükle
			if (staffInfoPanel != null)
			{
				PackedScene staffInfoScene = ResourceLoader.Load<PackedScene>("res://scenes/UI/Staff/StaffInfoUI.tscn");
				var staffInfoUI = staffInfoScene.Instantiate();
				staffInfoPanel.AddChild(staffInfoUI);
			}
			
			// Connect to game signals
			var gameManager = GetNode<GameManager>("/root/Main/GameManager");
			if (gameManager != null)
			{
				var timeManager = gameManager.GetNode<TimeManager>("TimeManager");
				if (timeManager != null)
				{
					timeManager.Connect(TimeManager.SignalName.HourChanged, Callable.From(UpdateTimeDisplay));
					timeManager.Connect(TimeManager.SignalName.NewDayStarted, Callable.From(UpdateDayDisplay));
				}
				
				var economyManager = gameManager.GetNode<EconomyManager>("EconomyManager");
				if (economyManager != null)
				{
					economyManager.Connect(EconomyManager.SignalName.MoneyChanged, Callable.From(UpdateMoneyDisplay));
					economyManager.Connect(EconomyManager.SignalName.TransactionProcessed, Callable.From(LogTransaction));
				}
				
				gameManager.Connect(GameManager.SignalName.GameStateChanged, Callable.From(OnGameStateChanged));
				
				// Initial UI updates
				UpdateDayDisplay(gameManager.Time.CurrentDay);
				UpdateTimeDisplay(gameManager.Time.CurrentTime.Hour);
				UpdateMoneyDisplay(gameManager.Economy.Money);
			}
			else
			{
				GD.PrintErr("GameUI: GameManager not found");
				AddLogMessage("UI failed to connect to game systems", Colors.Red);
			}
			
			GD.Print("UI personel sistemi entegrasyonu tamamlandı.");
		}
		
		public override void _Process(double delta)
		{
			// Her karede müşteri bilgilerini güncelle
			UpdateCustomerInfo();
		}
		
		private void UpdateCustomerInfo()
		{
			var gameManager = GetNode<GameManager>("/root/Main/GameManager");
			if (gameManager?.Customers != null)
			{
				int activeCustomers = gameManager.Customers.GetActiveCustomerCount();
				float popularity = gameManager.Customers.GetPavyonPopularity();
				
				customerCountLabel.Text = $"Müşteriler: {activeCustomers}";
				popularityLabel.Text = $"Popülarite: {popularity:F1}%";
			}
			else
			{
				// CustomerManager henüz yoksa, varsayılan değerler göster
				if (customerCountLabel != null)
					customerCountLabel.Text = "Müşteriler: 0";
				
				if (popularityLabel != null)
					popularityLabel.Text = "Popülarite: 0.0%";
			}
		}
		
		private void OnPausePressed()
		{
			var gameManager = GetNode<GameManager>("/root/Main/GameManager");
			if (gameManager?.Time != null)
			{
				gameManager.Time.PauseTime();
				UpdateTimeControls(true);
				AddLogMessage("⏸️ Zaman durduruldu");
			}
		}
		
		private void OnPlayPressed()
		{
			var gameManager = GetNode<GameManager>("/root/Main/GameManager");
			if (gameManager?.Time != null)
			{
				gameManager.Time.SetTimeScale(NORMAL_SPEED);
				gameManager.Time.ResumeTime();
				UpdateTimeControls(false);
				AddLogMessage("▶️ Zaman normal hızda ilerliyor");
			}
		}
		
		private void OnFastPressed()
		{
			var gameManager = GetNode<GameManager>("/root/Main/GameManager");
			if (gameManager?.Time != null)
			{
				gameManager.Time.SetTimeScale(FAST_SPEED);
				gameManager.Time.ResumeTime();
				UpdateTimeControls(false);
				AddLogMessage("⏩ Zaman hızlı ilerliyor");
			}
		}
		
		private void OnFurnitureButtonPressed()
		{
			// FurnitureUI'ı göster/gizle
			var furnitureUI = GetNode<FurnitureUI>("/root/Main/FurnitureUI");
			if (furnitureUI != null)
			{
				if (furnitureUI.Visible)
				{
					furnitureUI.Hide();
				}
				else
				{
					furnitureUI.Show();
				}
			}
		}
		
		private void OnStaffButtonPressed()
		{
			// Personel yönetim ekranını göster/gizle
			ToggleStaffManagementUI();
			AddLogMessage("👥 Personel yönetim paneli açıldı");
		}
		
		// Personel yönetim panelini açıp kapatma
		private void ToggleStaffManagementUI()
		{
			// Eğer panel daha önce oluşturulmadıysa, oluştur
			if (staffManagementUI == null)
			{
				PackedScene managementScene = ResourceLoader.Load<PackedScene>("res://scenes/UI/Staff/StaffManagementUI.tscn");
				staffManagementUI = managementScene.Instantiate<Control>();
				staffManagementUI.Name = "StaffManagementUI";
				AddChild(staffManagementUI);
				
				// Kapat butonuna sinyal bağla
				var closeButton = staffManagementUI.GetNode<Button>("VBoxContainer/HBoxContainer/CloseButton");
				if (closeButton != null)
				{
					closeButton.Pressed += () => staffManagementUI.Visible = false;
				}
				
				// Personel alım butonuna sinyal bağla
				var hireButton = staffManagementUI.GetNode<Button>("VBoxContainer/HBoxContainer/HireButton");
				if (hireButton != null)
				{
					hireButton.Pressed += ShowStaffHiringUI;
				}
			}
			
			// Göster veya gizle
			staffManagementUI.Visible = !staffManagementUI.Visible;
			
			// Görünür olduğunda diğer açık UI panellerini kapat
			if (staffManagementUI.Visible && staffHiringUI != null)
			{
				staffHiringUI.Visible = false;
			}
		}
		
		// "İşe Al" butonuna basıldığında
		public void ShowStaffHiringUI()
		{
			// Eğer panel daha önce oluşturulmadıysa, oluştur
			if (staffHiringUI == null)
			{
				PackedScene hiringScene = ResourceLoader.Load<PackedScene>("res://scenes/UI/Staff/StaffHiringUI.tscn");
				staffHiringUI = hiringScene.Instantiate<Control>();
				staffHiringUI.Name = "StaffHiringUI";
				AddChild(staffHiringUI);
				
				// Kapat butonuna sinyal bağla
				var closeButton = staffHiringUI.GetNode<Button>("VBoxContainer/ButtonContainer/CloseButton");
				if (closeButton != null)
				{
					closeButton.Pressed += () => staffHiringUI.Visible = false;
				}
			}
			
			// Panel'i göster
			staffHiringUI.Visible = true;
			
			// Diğer açık UI panellerini kapat
			if (staffManagementUI != null)
			{
				staffManagementUI.Visible = false;
			}
		}
		
		// Oyun durumu değiştiğinde UI'ı güncelle
		private void OnGameStateChanged(int oldState, int newState)
		{
			// Oyun durumu MorningMode olduğunda işlemler
			if ((GameManager.GameState)newState == GameManager.GameState.MorningMode)
			{
				// StaffInfo panelini güncelle
				UpdateStaffInfoPanel();
			}
		}
		
		// Personel bilgi panelini güncelle
		private void UpdateStaffInfoPanel()
		{
			if (staffInfoPanel == null) return;
			
			// İlgili güncelleme metodlarını çağır
			// (StaffInfoUI içinde tanımlanmış metodlar)
			var staffInfoUI = staffInfoPanel.GetChild<StaffInfoUI>(0);
			if (staffInfoUI != null)
			{
				staffInfoUI.UpdateStats();
			}
		}
		
		private void UpdateTimeControls(bool isPaused)
		{
			pauseButton.Disabled = isPaused;
			playButton.Disabled = !isPaused && !fastButton.Disabled;
			fastButton.Disabled = !isPaused && !playButton.Disabled;
		}
		
		private void UpdateDayDisplay(int day)
		{
			dayLabel.Text = $"Gün: {day}";
		}
		
		private void UpdateTimeDisplay(int hour)
		{
			var gameManager = GetNode<GameManager>("/root/Main/GameManager");
			if (gameManager?.Time != null)
			{
				timeLabel.Text = gameManager.Time.GetFormattedTime();
			}
			else
			{
				timeLabel.Text = $"{hour:00}:00";
			}
		}
		
		private void UpdateMoneyDisplay(float money)
		{
			moneyLabel.Text = EconomyManager.FormatMoney(money);
		}
		
		private void LogTransaction(string description, float amount)
		{
			string color = amount >= 0 ? "green" : "red";
			string amountStr = EconomyManager.FormatMoney(amount);
			AddLogMessage($"{description}: [color={color}]{amountStr}[/color]");
		}
		
		private void AddLogMessage(string message, Color color = default)
		{
			if (color == default)
			{
				logText.AppendText($"\n{message}");
			}
			else
			{
				logText.AppendText($"\n[color=#{color.ToHtml()}]{message}[/color]");
			}
			
			// Auto-scroll to bottom
			logText.ScrollToLine(logText.GetLineCount() - 1);
		}
		
		// Oyun başladığında örneklem personel oluştur (debug için)
		public void CreateSampleStaff()
		{
			var gameManager = GetNode<GameManager>("/root/Main/GameManager");
			if (gameManager?.Staff == null) return;
			
			GD.Print("Örneklem personel oluşturuluyor...");
			
			// Bir kons oluşturup işe al
			Kons kons = new Kons();
			kons.FullName = "Ayşe 'Sultan'";
			kons.JobTitle = "Kons";
			kons.Level = 2;
			kons.Salary = 550f;
			kons.Loyalty = 85f;
			kons.SetAttributeValue("Karizma", 9f);
			kons.SetAttributeValue("Sosyallik", 8f);
			kons.SetAttributeValue("İkna", 7f);
			gameManager.Staff.HireStaff(kons);
			
			// Bir güvenlik personeli oluşturup işe al
			SecurityStaff security = new SecurityStaff();
			security.FullName = "Mehmet 'Tank'";
			security.JobTitle = "Güvenlik";
			security.Level = 3;
			security.Salary = 600f;
			security.Loyalty = 90f;
			security.SetAttributeValue("Güç", 9f);
			security.SetAttributeValue("Tehdit", 8f);
			security.SetAttributeValue("Uyanıklık", 7f);
			security.HasRadio = true;
			gameManager.Staff.HireStaff(security);
			
			// Bir garson oluşturup işe al
			Waiter waiter = new Waiter();
			waiter.FullName = "Ali Yılmaz";
			waiter.JobTitle = "Garson";
			waiter.Level = 1;
			waiter.Salary = 400f;
			waiter.Loyalty = 70f;
			waiter.SetAttributeValue("Hız", 8f);
			waiter.SetAttributeValue("Dikkat", 7f);
			gameManager.Staff.HireStaff(waiter);
			
			GD.Print("Örneklem personel oluşturuldu.");
			
			// Personel bilgi panelini güncelle
			UpdateStaffInfoPanel();
		}
	}
}
