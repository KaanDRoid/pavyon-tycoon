// src/UI/Staff/StaffInfoUI.cs
using Godot;
using System;
using System.Collections.Generic;
using PavyonTycoon.Staff;
using PavyonTycoon.Core;

namespace PavyonTycoon.UI.Staff
{
	public partial class StaffInfoUI : Control
	{
		// UI elemanları
		private Label staffCountLabel;
		private Label dailySalaryLabel;
		private ProgressBar loyaltyBar;
		private Label staffStatusLabel;
		private Button openManagementButton;
		private Panel staffAlertPanel;
		private VBoxContainer alertsContainer;
		
		// Referanslar
		private StaffManager staffManager;
		private GameManager gameManager;
		
		// Uyarı takibi
		private List<StaffAlert> activeAlerts = new List<StaffAlert>();
		
		private class StaffAlert
		{
			public StaffMember Staff;
			public string Message;
			public float Severity; // 0.0-1.0 arası (1.0 = acil)
			public DateTime Time;
		}
		
		public override void _Ready()
		{
			// UI elemanlarını al
			staffCountLabel = GetNode<Label>("StatsContainer/StaffCountLabel");
			dailySalaryLabel = GetNode<Label>("StatsContainer/DailySalaryLabel");
			loyaltyBar = GetNode<ProgressBar>("StatsContainer/LoyaltyBar");
			staffStatusLabel = GetNode<Label>("StatsContainer/StatusLabel");
			openManagementButton = GetNode<Button>("StatsContainer/OpenButton");
			staffAlertPanel = GetNode<Panel>("AlertsPanel");
			alertsContainer = GetNode<VBoxContainer>("AlertsPanel/ScrollContainer/AlertsContainer");
			
			// Buton sinyallerini bağla
			openManagementButton.Pressed += OnOpenManagementPressed;
			
			// Yöneticilere referans al
			gameManager = GetNode<GameManager>("/root/Main/GameManager");
			staffManager = gameManager?.GetNode<StaffManager>("StaffManager");
			
			if (staffManager != null)
			{
				// StaffManager olaylarını dinle
				staffManager.Connect(StaffManager.SignalName.StaffHired, Callable.From(OnStaffHired));
				staffManager.Connect(StaffManager.SignalName.StaffFired, Callable.From(OnStaffFired));
				staffManager.Connect(StaffManager.SignalName.StaffLoyaltyChanged, Callable.From(OnStaffLoyaltyChanged));
				staffManager.Connect(StaffManager.SignalName.SalariesPaid, Callable.From(OnSalariesPaid));
			}
			else
			{
				GD.PrintErr("StaffInfoUI: StaffManager bulunamadı!");
			}
			
			// İstatistikleri başlangıçta güncelle
			UpdateStats();
			
			// Başlangıçta uyarı panelini gizle
			staffAlertPanel.Visible = false;
			
			GD.Print("Personel bilgi arayüzü başlatıldı");
		}
		
		public override void _Process(double delta)
		{
			// Oyun içi değişen değerleri sürekli güncelle (maaş, sadakat vb.)
			if (staffManager != null && IsVisibleInTree())
			{
				UpdateStats();
			}
			
			// Uyarıları kontrol et
			CheckForStaffAlerts();
		}
		
		// İstatistikleri güncelleme
		private void UpdateStats()
		{
			if (staffManager == null) return;
			
			// Personel sayısı
			int staffCount = staffManager.GetAllStaff().Count;
			staffCountLabel.Text = $"Toplam Personel: {staffCount}";
			
			// Günlük maaş
			float dailySalary = staffManager.DailySalaryCost;
			dailySalaryLabel.Text = $"Günlük Maaş: {dailySalary:F0}₺";
			
			// Ortalama sadakat
			float avgLoyalty = CalculateAverageLoyalty();
			UpdateLoyaltyBar(avgLoyalty);
			
			// Durum metni
			staffStatusLabel.Text = GetStaffStatusText(staffCount, avgLoyalty);
		}
		
		// Ortalama sadakat hesaplama
		private float CalculateAverageLoyalty()
		{
			var allStaff = staffManager.GetAllStaff();
			if (allStaff.Count == 0) return 0f;
			
			float totalLoyalty = 0f;
			foreach (var staff in allStaff)
			{
				totalLoyalty += staff.Loyalty;
			}
			
			return totalLoyalty / allStaff.Count;
		}
		
		// Sadakat çubuğunu güncelleme
		private void UpdateLoyaltyBar(float avgLoyalty)
		{
			loyaltyBar.Value = avgLoyalty;
			
			// Sadakat değerine göre renk ayarla
			if (avgLoyalty < 30f)
			{
				loyaltyBar.AddThemeColorOverride("fill_color", Colors.Red);
			}
			else if (avgLoyalty < 50f)
			{
				loyaltyBar.AddThemeColorOverride("fill_color", Colors.Orange);
			}
			else if (avgLoyalty < 70f)
			{
				loyaltyBar.AddThemeColorOverride("fill_color", Colors.Yellow);
			}
			else if (avgLoyalty < 90f)
			{
				loyaltyBar.AddThemeColorOverride("fill_color", Colors.Green);
			}
			else
			{
				loyaltyBar.AddThemeColorOverride("fill_color", Colors.LightGreen);
			}
		}
		
		// Personel durum mesajı oluşturma
		private string GetStaffStatusText(int staffCount, float avgLoyalty)
		{
			if (staffCount == 0)
			{
				return "Personel yok! İşe alım yapmalısınız.";
			}
			
			string status = "";
			
			// Sadakate göre durum
			if (avgLoyalty < 30f)
			{
				status = "Kriz! Personel çok mutsuz.";
			}
			else if (avgLoyalty < 50f)
			{
				status = "Personel memnuniyetsiz, dikkat edin.";
			}
			else if (avgLoyalty < 70f)
			{
				status = "Personel durumu normal.";
			}
			else if (avgLoyalty < 90f)
			{
				status = "Personel memnun.";
			}
			else
			{
				status = "Personel çok mutlu!";
			}
			
			return status;
		}
		
		// Personel uyarılarını kontrol et
		private void CheckForStaffAlerts()
		{
			if (staffManager == null) return;
			
			var allStaff = staffManager.GetAllStaff();
			bool hasNewAlerts = false;
			
			// Çok düşük sadakate sahip personelleri kontrol et
			foreach (var staff in allStaff)
			{
				if (staff.Loyalty < 30f)
				{
					// Bu personel için aktif uyarı var mı kontrol et
					bool alertExists = activeAlerts.Exists(a => a.Staff == staff && a.Message.Contains("Sadakat"));
					
					if (!alertExists)
					{
						// Yeni uyarı ekle
						StaffAlert alert = new StaffAlert
						{
							Staff = staff,
							Message = $"{staff.FullName} çok mutsuz! Sadakat çok düşük (%{staff.Loyalty:F0}).",
							Severity = (30f - staff.Loyalty) / 30f, // 0f (30%) - 1.0f (0%) arası
							Time = DateTime.Now
						};
						
						activeAlerts.Add(alert);
						hasNewAlerts = true;
						
						GD.Print($"Personel uyarısı: {alert.Message}");
					}
				}
			}
			
			// Personeli olmayan iş pozisyonlarını kontrol et
			var positions = staffManager.GetJobPositions();
			foreach (var position in positions.Values)
			{
				int count = staffManager.GetAllStaff().Count(s => s.JobTitle == position.Title);
				
				if (count == 0)
				{
					// Bu pozisyon için aktif uyarı var mı kontrol et
					bool alertExists = activeAlerts.Exists(a => a.Message.Contains(position.Title) && a.Message.Contains("Personel eksik"));
					
					if (!alertExists)
					{
						// Yeni uyarı ekle
						StaffAlert alert = new StaffAlert
						{
							Staff = null,
							Message = $"{position.Title} pozisyonu için personel eksik! İşe alım yapmalısınız.",
							Severity = 0.7f, // Önemli ama acil değil
							Time = DateTime.Now
						};
						
						activeAlerts.Add(alert);
						hasNewAlerts = true;
						
						GD.Print($"Personel uyarısı: {alert.Message}");
					}
				}
			}
			
			// Eski uyarıları temizle (5 dakikadan eski)
			for (int i = activeAlerts.Count - 1; i >= 0; i--)
			{
				if ((DateTime.Now - activeAlerts[i].Time).TotalMinutes > 5)
				{
					activeAlerts.RemoveAt(i);
					hasNewAlerts = true;
				}
			}
			
			// Yeni uyarı varsa UI'ı güncelle
			if (hasNewAlerts)
			{
				UpdateAlertsList();
			}
		}
		
		// Uyarı listesini güncelle
		private void UpdateAlertsList()
		{
			// Eski uyarıları temizle
			foreach (Node child in alertsContainer.GetChildren())
			{
				child.QueueFree();
			}
			
			// Uyarı yoksa paneli gizle
			if (activeAlerts.Count == 0)
			{
				staffAlertPanel.Visible = false;
				return;
			}
			
			// Uyarıları önem sırasına göre sırala
			activeAlerts.Sort((a, b) => b.Severity.CompareTo(a.Severity));
			
			// Uyarıları ekle
			foreach (var alert in activeAlerts)
			{
				// Uyarı konteynerı
				PanelContainer alertContainer = new PanelContainer();
				alertContainer.AddThemeStyleboxOverride("panel", new StyleBoxFlat
				{
					BgColor = GetAlertColor(alert.Severity),
					BorderWidth = new float[] { 0, 0, 1, 0 }, // Alt kenar çizgisi
					BorderColor = Colors.Gray
				});
				
				// İçerik düzeni
				HBoxContainer hbox = new HBoxContainer();
				alertContainer.AddChild(hbox);
				
				// Uyarı ikonu
				TextureRect icon = new TextureRect();
				icon.ExpandMode = TextureRect.ExpandModeEnum.FitWidth;
				icon.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
				icon.CustomMinimumSize = new Vector2(32, 32);
				icon.Texture = ResourceLoader.Load<Texture2D>("res://assets/icons/alert_icon.png");
				hbox.AddChild(icon);
				
				// Uyarı mesajı
				Label messageLabel = new Label();
				messageLabel.Text = alert.Message;
				messageLabel.AutowrapMode = TextServer.AutowrapMode.Word;
				messageLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				hbox.AddChild(messageLabel);
				
				// Eğer personel uyarısıysa, personele git butonu ekle
				if (alert.Staff != null)
				{
					Button goToStaffButton = new Button();
					goToStaffButton.Text = "Git";
					goToStaffButton.TooltipText = $"{alert.Staff.FullName} personeline git";
					goToStaffButton.Pressed += () => {
						GoToStaffDetails(alert.Staff);
					};
					hbox.AddChild(goToStaffButton);
				}
				
				// Uyarıyı kapat butonu
				Button closeButton = new Button();
				closeButton.Text = "X";
				closeButton.TooltipText = "Uyarıyı kapat";
				closeButton.Pressed += () => {
					activeAlerts.Remove(alert);
					alertContainer.QueueFree();
					
					// Tüm uyarılar kapatıldıysa paneli gizle
					if (activeAlerts.Count == 0)
					{
						staffAlertPanel.Visible = false;
					}
				};
				hbox.AddChild(closeButton);
				
				// Uyarıyı konteynere ekle
				alertsContainer.AddChild(alertContainer);
			}
			
			// Uyarı panelini göster
			staffAlertPanel.Visible = true;
		}
		
		// Uyarı önem derecesine göre renk
		private Color GetAlertColor(float severity)
		{
			if (severity > 0.8f)
				return new Color(1.0f, 0.3f, 0.3f, 0.8f); // Kırmızı (çok önemli)
			else if (severity > 0.5f)
				return new Color(1.0f, 0.6f, 0.2f, 0.8f); // Turuncu (önemli)
			else if (severity > 0.3f)
				return new Color(1.0f, 1.0f, 0.3f, 0.8f); // Sarı (dikkat)
			else
				return new Color(0.3f, 0.7f, 1.0f, 0.8f); // Mavi (bilgi)
		}
		
		// Personel detaylarına git
		private void GoToStaffDetails(StaffMember staff)
		{
			// Personel yönetim ekranını aç
			OnOpenManagementPressed();
			
			// TODO: Personel yönetim ekranına bu personeli seç komutunu iletme
			// Şimdilik sadece ekranı açıyor, sonra personel seçme mekanizması eklenebilir
		}
		
		// Buton olay işleyicileri
		
		private void OnOpenManagementPressed()
		{
			// Personel yönetim ekranını aç
			var ui = GetNode<Control>("/root/Main/UI");
			if (ui != null)
			{
				// Personel yönetim sahnesi yüklü değilse yükle ve ekle
				if (!ui.HasNode("StaffManagementUI"))
				{
					PackedScene managementScene = ResourceLoader.Load<PackedScene>("res://scenes/UI/StaffManagementUI.tscn");
					var managementUI = managementScene.Instantiate();
					managementUI.Name = "StaffManagementUI";
					ui.AddChild(managementUI);
				}
				else
				{
					// Zaten açıksa öne getir
					var managementUI = ui.GetNode<Control>("StaffManagementUI");
					managementUI.Visible = true;
					managementUI.MoveToFront();
				}
			}
		}
		
		// StaffManager olayları
		
		private void OnStaffHired(StaffMember staff)
		{
			// İstatistikleri güncelle
			UpdateStats();
			
			// İşe alım uyarısı (bilgi)
			AnimateHireNotification(staff);
		}
		
		private void OnStaffFired(StaffMember staff)
		{
			// İstatistikleri güncelle
			UpdateStats();
			
			// Buna bağlı uyarıları temizle
			for (int i = activeAlerts.Count - 1; i >= 0; i--)
			{
				if (activeAlerts[i].Staff == staff)
				{
					activeAlerts.RemoveAt(i);
				}
			}
			
			UpdateAlertsList();
		}
		
		private void OnStaffLoyaltyChanged(StaffMember staff, float newLoyalty)
		{
			// Büyük sadakat değişimleri için uyarı göster
			if (newLoyalty < 20f && !activeAlerts.Exists(a => a.Staff == staff && a.Message.Contains("acil")))
			{
				StaffAlert alert = new StaffAlert
				{
					Staff = staff,
					Message = $"ACİL: {staff.FullName} istifa etmek üzere olabilir! (%{newLoyalty:F0} sadakat)",
					Severity = 0.9f, // Çok yüksek önem
					Time = DateTime.Now
				};
				
				activeAlerts.Add(alert);
				UpdateAlertsList();
			}
		}
		
		private void OnSalariesPaid(float totalAmount)
		{
			// Maaş ödemesi animasyonu
			AnimateSalaryNotification(totalAmount);
		}
		
		// Animasyonlar
		
		private void AnimateHireNotification(StaffMember staff)
		{
			// Geçici bir bildirim göster
			var notification = new Label();
			notification.Text = $"{staff.FullName} işe alındı!";
			notification.AddThemeColorOverride("font_color", Colors.Green);
			notification.Position = new Vector2(0, -30);
			notification.Modulate = new Color(1, 1, 1, 0); // Başlangıçta saydam
			AddChild(notification);
			
			// Fade-in animasyonu
			var tweenIn = CreateTween();
			tweenIn.TweenProperty(notification, "modulate:a", 1.0f, 0.3f);
			tweenIn.TweenProperty(notification, "position:y", -50, 0.5f);
			
			// Bekle
			tweenIn.TweenInterval(1.5f);
			
			// Fade-out animasyonu
			tweenIn.TweenProperty(notification, "modulate:a", 0.0f, 0.3f);
			
			// Animasyon bitince kaldır
			tweenIn.TweenCallback(Callable.From(() => {
				notification.QueueFree();
			}));
		}
		
		private void AnimateSalaryNotification(float amount)
		{
			// Geçici bir bildirim göster
			var notification = new Label();
			notification.Text = $"Maaşlar ödendi: {amount:F0}₺";
			notification.AddThemeColorOverride("font_color", Colors.OrangeRed);
			notification.Position = new Vector2(0, -30);
			notification.Modulate = new Color(1, 1, 1, 0); // Başlangıçta saydam
			AddChild(notification);
			
			// Fade-in animasyonu
			var tweenIn = CreateTween();
			tweenIn.TweenProperty(notification, "modulate:a", 1.0f, 0.3f);
			tweenIn.TweenProperty(notification, "position:y", -50, 0.5f);
			
			// Bekle
			tweenIn.TweenInterval(1.5f);
			
			// Fade-out animasyonu
			tweenIn.TweenProperty(notification, "modulate:a", 0.0f, 0.3f);
			
			// Animasyon bitince kaldır
			tweenIn.TweenCallback(Callable.From(() => {
				notification.QueueFree();
			}));
		}
	}
}
