// src/Tests/StaffSystemTest.cs
using Godot;
using System;
using System.Collections.Generic;
using PavyonTycoon.Core;
using PavyonTycoon.Staff;
using PavyonTycoon.Economy;

namespace PavyonTycoon.Tests
{
	public partial class StaffSystemTest : Node
	{
		// UI elemanları
		private Button createStaffButton;
		private Button runTestButton;
		private Button simulateDayButton;
		private RichTextLabel logTextBox;
		
		// Test için gerekli bileşenler
		private GameManager gameManager;
		private StaffManager staffManager;
		private EconomyManager economyManager;
		private TimeManager timeManager;
		
		// Örnek personel listesi
		private List<StaffMember> sampleStaff = new List<StaffMember>();
		
		public override void _Ready()
		{
			// UI elemanlarını al
			createStaffButton = GetNode<Button>("VBoxContainer/ButtonPanel/CreateStaffButton");
			runTestButton = GetNode<Button>("VBoxContainer/ButtonPanel/RunTestButton");
			simulateDayButton = GetNode<Button>("VBoxContainer/ButtonPanel/SimulateDayButton");
			logTextBox = GetNode<RichTextLabel>("VBoxContainer/LogPanel/ScrollContainer/LogTextBox");
			
			// Buton sinyallerini bağla
			createStaffButton.Pressed += OnCreateStaffButtonPressed;
			runTestButton.Pressed += OnRunTestButtonPressed;
			simulateDayButton.Pressed += OnSimulateDayButtonPressed;
			
			// Yöneticilere referans al
			gameManager = GetNode<GameManager>("/root/Main/GameManager");
			
			if (gameManager != null)
			{
				staffManager = gameManager.GetNode<StaffManager>("StaffManager");
				economyManager = gameManager.GetNode<EconomyManager>("EconomyManager");
				timeManager = gameManager.GetNode<TimeManager>("TimeManager");
				
				Log("Personel sistemi test sahnesi hazır.");
				Log($"StaffManager: {(staffManager != null ? "OK ✓" : "Missing ✗")}");
				Log($"EconomyManager: {(economyManager != null ? "OK ✓" : "Missing ✗")}");
				Log($"TimeManager: {(timeManager != null ? "OK ✓" : "Missing ✗")}");
			}
			else
			{
				Log("HATA: GameManager bulunamadı!", Colors.Red);
			}
		}
		
		// Log işlevi
		private void Log(string message, Color color = default)
		{
			if (color == default)
				color = Colors.White;
				
			logTextBox.PushColor(color);
			logTextBox.AddText($"\n{message}");
			logTextBox.Pop();
			
			// Otomatik aşağı kaydır
			logTextBox.ScrollToLine(logTextBox.GetLineCount() - 1);
			
			// Konsola da yazdır
			GD.Print(message);
		}
		
		// Örnek personel oluştur
		private void OnCreateStaffButtonPressed()
		{
			if (staffManager == null || economyManager == null)
			{
				Log("Personel oluşturulamadı: Yöneticiler bulunamadı!", Colors.Red);
				return;
			}
			
			// Örnek personelleri oluştur
			Log("Örnek personeller oluşturuluyor...", Colors.LightBlue);
			
			// Tüm personel türleri için örnekler oluştur
			CreateSampleKons();
			CreateSampleSecurity();
			CreateSampleWaiter();
			CreateSampleMusician();
			CreateSampleCook();
			CreateSampleIllegalStaff();
			
			// Sonuç
			Log($"Toplam {sampleStaff.Count} örnek personel oluşturuldu.", Colors.Green);
		}
		
		// Testleri çalıştır
		private void OnRunTestButtonPressed()
		{
			if (staffManager == null || economyManager == null)
			{
				Log("Test çalıştırılamadı: Yöneticiler bulunamadı!", Colors.Red);
				return;
			}
			
			Log("Personel sistemi testi başlatılıyor...", Colors.Yellow);
			
			// Mevcut personeli kontrol et
			var allStaff = staffManager.GetAllStaff();
			Log($"Mevcut personel sayısı: {allStaff.Count}");
			
			// Personel yoksa örnek personel oluştur
			if (allStaff.Count == 0 && sampleStaff.Count == 0)
			{
				Log("Önce örnek personel oluşturun.", Colors.Orange);
				return;
			}
			
			// İlk üç test grubunu çalıştır
			RunStaffAttributeTests();
			RunStaffAssignmentTests();
			RunStaffLoyaltyTests();
			
			Log("Personel sistemi testi tamamlandı.", Colors.Green);
		}
		
		// Gün simülasyonu
		private void OnSimulateDayButtonPressed()
		{
			if (gameManager == null || staffManager == null || timeManager == null)
			{
				Log("Simülasyon çalıştırılamadı: Yöneticiler bulunamadı!", Colors.Red);
				return;
			}
			
			Log("Bir pavyon günü simüle ediliyor...", Colors.Yellow);
			
			// Vardiya başlat
			if (gameManager.CurrentState != GameManager.GameState.NightMode)
			{
				gameManager.ChangeGameState(GameManager.GameState.NightMode);
			}
			
			// Zaman hızını ayarla
			timeManager.SetTimeScale(5.0f); // Hızlı simülasyon için
			
			// Simülasyon ilerlemesini gösteren timer
			Timer simulationTimer = new Timer();
			simulationTimer.WaitTime = 1.0;
			simulationTimer.OneShot = false;
			simulationTimer.Timeout += SimulationTick;
			AddChild(simulationTimer);
			simulationTimer.Start();
			
			// Simülasyon düğmelerini devre dışı bırak
			createStaffButton.Disabled = true;
			runTestButton.Disabled = true;
			simulateDayButton.Disabled = true;
			
			Log("Simülasyon başladı. Her saat için bir güncellemeyeyi bekleyin...");
		}
		
		// Simülasyon tick'i
		private void SimulationTick()
		{
			if (timeManager == null) return;
			
			// Saati kontrol et
			if (timeManager.CurrentTime.Hour == 6 && !timeManager.IsTimePaused)
			{
				// Gün tamamlandı, sabah modu başladı
				Log("Simülasyon tamamlandı. Gece vardiyası sona erdi.", Colors.Green);
				
				// Saati durdur
				timeManager.PauseTime();
				
				// Timer'ı durdur ve kaldır
				var timer = GetNode<Timer>("Timer");
				timer.Stop();
				timer.QueueFree();
				
				// Düğmeleri tekrar aktifleştir
				createStaffButton.Disabled = false;
				runTestButton.Disabled = false;
				simulateDayButton.Disabled = false;
				
				// Sonuçları göster
				ShowDayResults();
			}
			else
			{
				// Saat durumunu yazdır
				Log($"Simülasyon devam ediyor... Saat: {timeManager.GetFormattedTime()}", Colors.LightBlue);
			}
		}
		
		// Gün sonuçlarını göster
		private void ShowDayResults()
		{
			if (staffManager == null || economyManager == null) return;
			
			Log("=== GÜN SONU RAPORU ===", Colors.Yellow);
			
			// Personel durumları
			var allStaff = staffManager.GetAllStaff();
			Log($"Toplam Personel: {allStaff.Count}");
			
			float totalLoyalty = 0f;
			foreach (var staff in allStaff)
			{
				totalLoyalty += staff.Loyalty;
				
				// Personel türüne göre özel detaylar
				string specialInfo = "";
				
				if (staff is Kons kons)
				{
					specialInfo = $"Müdavim: {kons.RegularCustomers.Count}/{kons.MaxRegularCustomers}";
				}
				else if (staff is SecurityStaff security)
				{
					specialInfo = $"Olaylar: {security.IncidentsResolved}";
				}
				else if (staff is Musician musician)
				{
					specialInfo = $"Dayanıklılık: {musician.StaminaLevel:F0}/100";
				}
				
				Log($"• {staff.FullName}: Sadakat %{staff.Loyalty:F0} {specialInfo}");
			}
			
			// Ortalama sadakat
			float avgLoyalty = allStaff.Count > 0 ? totalLoyalty / allStaff.Count : 0f;
			Log($"Ortalama Sadakat: %{avgLoyalty:F0}");
			
			// Ekonomik durum
			Log($"Günlük Personel Gideri: {staffManager.DailySalaryCost:F0}₺");
			Log($"Toplam Para: {economyManager.Money:F0}₺");
		}
		
		// Test grupları
		
		// Özellik testleri
		private void RunStaffAttributeTests()
		{
			Log("\n--- Personel Özellik Testleri ---", Colors.Cyan);
			
			// Personeli seç
			StaffMember testStaff = GetRandomStaff();
			if (testStaff == null) return;
			
			Log($"Test personeli: {testStaff.FullName} ({testStaff.JobTitle})");
			
			// Mevcut özellikleri kontrol et
			var attributes = testStaff.GetAllAttributes();
			Log("Mevcut özellikler:");
			foreach (var attr in attributes)
			{
				Log($"  {attr.Key}: {attr.Value:F1}");
			}
			
			// Özellik değiştirme testi
			string testAttr = attributes.Keys.ToArray()[0]; // İlk özelliği al
			float oldValue = testStaff.GetAttributeValue(testAttr);
			float newValue = Mathf.Min(10f, oldValue + 1f);
			
			testStaff.SetAttributeValue(testAttr, newValue);
			Log($"{testAttr} özelliği değiştirildi: {oldValue:F1} -> {newValue:F1}", Colors.Green);
			
			// Eğitim simülasyonu
			if (staffManager.TrainStaff(testStaff, testAttr))
			{
				Log($"{testStaff.FullName} eğitildi: {testAttr}", Colors.Green);
			}
			else
			{
				Log($"Eğitim başarısız oldu!", Colors.Orange);
			}
			
			// Özellik ekleme testi
			string newAttr = "TestAttribute";
			if (!testStaff.HasAttribute(newAttr))
			{
				testStaff.SetAttributeValue(newAttr, 5f);
				Log($"Yeni özellik eklendi: {newAttr} = 5.0", Colors.Green);
			}
		}
		
		// Görev atama testleri
		private void RunStaffAssignmentTests()
		{
			Log("\n--- Personel Görev Testleri ---", Colors.Cyan);
			
			// Personeli seç
			StaffMember testStaff = GetRandomStaff();
			if (testStaff == null) return;
			
			Log($"Test personeli: {testStaff.FullName} ({testStaff.JobTitle})");
			
			// Mevcut görevi kontrol et
			if (testStaff.CurrentTask != null)
			{
				Log($"Mevcut görev: {testStaff.CurrentTask.Name} (Durum: {testStaff.CurrentTask.Status})");
			}
			else
			{
				Log("Personelin mevcut görevi yok.");
			}
			
			// Yeni görev oluştur ve ata
			StaffTask newTask = null;
			
			if (testStaff is Kons)
			{
				newTask = StaffTask.CreateCustomerInteractionTask(null);
			}
			else if (testStaff is SecurityStaff)
			{
				newTask = StaffTask.CreateSecurityTask(Vector2.Zero);
			}
			else if (testStaff is Waiter)
			{
				newTask = StaffTask.CreateDrinkServiceTask(null);
			}
			else if (testStaff is Musician)
			{
				newTask = StaffTask.CreateMusicPerformanceTask();
			}
			else if (testStaff is Cook)
			{
				newTask = StaffTask.CreateFoodPreparationTask();
			}
			else if (testStaff is IllegalFloorStaff)
			{
				newTask = StaffTask.CreateIllegalActivityTask("kumar");
			}
			
			if (newTask != null)
			{
				bool success = testStaff.AssignTask(newTask);
				
				if (success)
				{
					Log($"Yeni görev atandı: {newTask.Name}", Colors.Green);
					
					// Görevi başlat
					if (timeManager != null)
					{
						newTask.StartTask(timeManager.CurrentTime);
						Log("Görev başlatıldı.", Colors.Green);
						
						// İlerleme simülasyonu
						for (int i = 1; i <= 5; i++)
						{
							float progress = i * 0.2f;
							newTask.Progress = progress;
							Log($"Görev ilerlemesi: %{progress * 100:F0}");
						}
						
						// Görevi tamamla
						newTask.CompleteTask();
						testStaff.CompleteTask();
						Log("Görev tamamlandı!", Colors.Green);
					}
					else
					{
						Log("TimeManager bulunamadığı için görev başlatılamadı.", Colors.Orange);
					}
				}
				else
				{
					Log("Görev atanamadı!", Colors.Orange);
				}
			}
			else
			{
				Log("Uygun görev türü bulunamadı.", Colors.Orange);
			}
		}
		
		// Sadakat testleri
		private void RunStaffLoyaltyTests()
		{
			Log("\n--- Personel Sadakat Testleri ---", Colors.Cyan);
			
			// Personeli seç
			StaffMember testStaff = GetRandomStaff();
			if (testStaff == null) return;
			
			Log($"Test personeli: {testStaff.FullName} ({testStaff.JobTitle})");
			Log($"Mevcut sadakat: %{testStaff.Loyalty:F0}");
			
			// Sadakat artırma testi
			float increaseAmount = GD.RandRange(5f, 15f);
			float oldLoyalty = testStaff.Loyalty;
			testStaff.IncreaseLoyalty(increaseAmount);
			
			Log($"Sadakat arttırıldı: %{oldLoyalty:F0} -> %{testStaff.Loyalty:F0} (+{increaseAmount:F0})", Colors.Green);
			
			// Sadakat azaltma testi
			float decreaseAmount = GD.RandRange(5f, 15f);
			oldLoyalty = testStaff.Loyalty;
			testStaff.ReduceLoyalty(decreaseAmount);
			
			Log($"Sadakat azaltıldı: %{oldLoyalty:F0} -> %{testStaff.Loyalty:F0} (-{decreaseAmount:F0})", Colors.Orange);
			
			// Terfi testi
			if (testStaff.Level < 5 && staffManager != null)
			{
				Log($"Terfi testi yapılıyor (Mevcut seviye: {testStaff.Level})");
				
				if (staffManager.PromoteStaff(testStaff))
				{
					Log($"{testStaff.FullName} terfi ettirildi! Yeni seviye: {testStaff.Level}", Colors.Green);
				}
				else
				{
					Log("Terfi başarısız oldu.", Colors.Orange);
				}
			}
			else
			{
				Log("Personel maksimum seviyede veya StaffManager bulunamadı.", Colors.Orange);
			}
		}
		
		// Örnek personel oluşturma metodları
		
		private void CreateSampleKons()
		{
			Kons kons = new Kons();
			kons.FullName = "Ayşe 'Sultan'";
			kons.Level = 2;
			kons.Salary = 550f;
			kons.Loyalty = 85f;
			kons.SetAttributeValue("Karizma", 9f);
			kons.SetAttributeValue("Sosyallik", 8f);
			kons.SetAttributeValue("İkna", 7f);
			
			sampleStaff.Add(kons);
			
			if (staffManager != null)
			{
				if (staffManager.HireStaff(kons) != null)
				{
					Log($"Kons örneği işe alındı: {kons.FullName}", Colors.Green);
				}
				else
				{
					Log($"Kons örneği işe alınamadı!", Colors.Orange);
				}
			}
		}
		
		private void CreateSampleSecurity()
		{
			SecurityStaff security = new SecurityStaff();
			security.FullName = "Mehmet 'Tank'";
			security.Level = 3;
			security.Salary = 600f;
			security.Loyalty = 90f;
			security.SetAttributeValue("Güç", 9f);
			security.SetAttributeValue("Tehdit", 8f);
			security.SetAttributeValue("Uyanıklık", 7f);
			security.HasRadio = true;
			
			sampleStaff.Add(security);
			
			if (staffManager != null)
			{
				if (staffManager.HireStaff(security) != null)
				{
					Log($"Güvenlik örneği işe alındı: {security.FullName}", Colors.Green);
				}
				else
				{
					Log($"Güvenlik örneği işe alınamadı!", Colors.Orange);
				}
			}
		}
		
		private void CreateSampleWaiter()
		{
			Waiter waiter = new Waiter();
			waiter.FullName = "Ali Yılmaz";
			waiter.Level = 1;
			waiter.Salary = 400f;
			waiter.Loyalty = 70f;
			waiter.SetAttributeValue("Hız", 8f);
			waiter.SetAttributeValue("Dikkat", 7f);
			
			sampleStaff.Add(waiter);
			
			if (staffManager != null)
			{
				if (staffManager.HireStaff(waiter) != null)
				{
					Log($"Garson örneği işe alındı: {waiter.FullName}", Colors.Green);
				}
				else
				{
					Log($"Garson örneği işe alınamadı!", Colors.Orange);
				}
			}
		}
		
		private void CreateSampleMusician()
		{
			Musician musician = new Musician();
			musician.FullName = "Ahmet Özkan (Udi)";
			musician.Level = 2;
			musician.Salary = 550f;
			musician.Loyalty = 80f;
			musician.SetAttributeValue("Müzik", 9f);
			musician.SetAttributeValue("Performans", 8f);
			musician.Instrument = Musician.InstrumentType.Ud;
			
			sampleStaff.Add(musician);
			
			if (staffManager != null)
			{
				if (staffManager.HireStaff(musician) != null)
				{
					Log($"Müzisyen örneği işe alındı: {musician.FullName}", Colors.Green);
				}
				else
				{
					Log($"Müzisyen örneği işe alınamadı!", Colors.Orange);
				}
			}
		}
		
		private void CreateSampleCook()
		{
			Cook cook = new Cook();
			cook.FullName = "Mustafa Şef";
			cook.Level = 2;
			cook.Salary = 500f;
			cook.Loyalty = 75f;
			cook.SetAttributeValue("Yemek", 8f);
			cook.SetAttributeValue("Yaratıcılık", 7f);
			cook.Specialty = Cook.CuisineType.Meze;
			
			sampleStaff.Add(cook);
			
			if (staffManager != null)
			{
				if (staffManager.HireStaff(cook) != null)
				{
					Log($"Aşçı örneği işe alındı: {cook.FullName}", Colors.Green);
				}
				else
				{
					Log($"Aşçı örneği işe alınamadı!", Colors.Orange);
				}
			}
		}
		
		private void CreateSampleIllegalStaff()
		{
			IllegalFloorStaff illegalStaff = new IllegalFloorStaff();
			illegalStaff.FullName = "Selim";
			illegalStaff.Level = 2;
			illegalStaff.Salary = 700f;
			illegalStaff.Loyalty = 95f;
			illegalStaff.SetAttributeValue("Gizlilik", 9f);
			illegalStaff.SetAttributeValue("Sadakat", 10f);
			illegalStaff.PrimaryActivity = IllegalFloorStaff.ActivityType.Kumar;
			
			sampleStaff.Add(illegalStaff);
			
			if (staffManager != null)
			{
				if (staffManager.HireStaff(illegalStaff) != null)
				{
					Log($"Kaçak kat personeli örneği işe alındı: {illegalStaff.FullName}", Colors.Green);
				}
				else
				{
					Log($"Kaçak kat personeli örneği işe alınamadı!", Colors.Orange);
				}
			}
		}
		
		// Yardımcı metodlar
		
		// Rastgele bir personel seç (örnek personel veya staffManager'dan)
		private StaffMember GetRandomStaff()
		{
			if (staffManager != null)
			{
				var allStaff = staffManager.GetAllStaff();
				if (allStaff.Count > 0)
				{
					int index = GD.RandRange(0, allStaff.Count - 1);
					return allStaff[index];
				}
			}
			
			if (sampleStaff.Count > 0)
			{
				int index = GD.RandRange(0, sampleStaff.Count - 1);
				return sampleStaff[index];
			}
			
			return null;
		}
	}
}
