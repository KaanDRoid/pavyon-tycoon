// src/Customers/CustomerManager.cs
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PavyonTycoon.Core;
using PavyonTycoon.Furniture;

namespace PavyonTycoon.Customers
{
	public partial class CustomerManager : Node
	{
		// Müşteri koleksiyonları
		private Dictionary<string, CustomerGroupData> customerGroupDatabase = new Dictionary<string, CustomerGroupData>();
		private List<CustomerGroup> activeCustomerGroups = new List<CustomerGroup>();
		
		// Müşteri oluşturma ayarları
		[Export] private int maxActiveCustomerGroups = 15;
		[Export] private float baseSpawnInterval = 180.0f; // 3 dakika
		private float timeSinceLastSpawn = 0.0f;
		private float currentSpawnInterval;
		
		// Pavyon özellikleri
		private float pavyonPopularity = 50.0f; // 0-100 arası değer
		private float pavyonQuality = 50.0f; // 0-100 arası değer
		
		// Node referansları
		private Node2D customerContainer;
		
		// Manager referansları
		private TimeManager timeManager;
		private EconomyManager economyManager;
		private FurnitureManager furnitureManager;
		
		// Müşteri spawn noktaları
		private Vector2[] spawnPoints = new Vector2[] {
			new Vector2(100, 550),  // Pavyon girişi
			new Vector2(150, 550),
			new Vector2(200, 550)
		};
		
		public override void _Ready()
		{
			// Node referanslarını al
			var gameNode = GetTree().Root.GetNode("Main");
			customerContainer = gameNode.GetNodeOrNull<Node2D>("CustomerContainer");
			
			if (customerContainer == null)
			{
				customerContainer = new Node2D();
				gameNode.AddChild(customerContainer);
				customerContainer.Name = "CustomerContainer";
				GD.Print("CustomerContainer node oluşturuldu");
			}
			
			// Manager referanslarını al
			var gameManager = GetParent() as GameManager;
			if (gameManager != null)
			{
				timeManager = gameManager.Time;
				economyManager = gameManager.Economy;
				furnitureManager = gameManager.Furniture;
			}
			
			// Müşteri veritabanını yükle
			LoadCustomerDatabase();
			
			// Gece moduna geçildiğinde müşteri oluşturmayı başlat
			if (timeManager != null)
			{
				timeManager.Connect(TimeManager.SignalName.NewDayStarted, Callable.From(OnNewDayStarted));
				timeManager.Connect(TimeManager.SignalName.DayEnded, Callable.From(OnDayEnded));
				timeManager.Connect(TimeManager.SignalName.HourChanged, Callable.From(OnHourChanged));
			}
			else
			{
				GD.PrintErr("CustomerManager: TimeManager referansı alınamadı");
			}
			
			// Başlangıç ayarlarını yap
			currentSpawnInterval = baseSpawnInterval;
			
			GD.Print("👥 Müşteri sistemi başlatıldı");
		}
		
		public override void _Process(double delta)
		{
			if (timeManager != null && !timeManager.IsTimePaused && GameManager.Instance.CurrentState == GameManager.GameState.NightMode)
			{
				// Yeni müşteri oluşturma zamanı geldi mi kontrol et
				timeSinceLastSpawn += (float)delta;
				if (timeSinceLastSpawn >= currentSpawnInterval && activeCustomerGroups.Count < maxActiveCustomerGroups)
				{
					SpawnRandomCustomerGroup();
					timeSinceLastSpawn = 0.0f;
					
					// Zaman geçtikçe spawn aralığını ayarla (saat ilerledikçe daha az müşteri)
					AdjustSpawnInterval();
				}
				
				// Aktif müşterileri güncelle
				UpdateActiveCustomers((float)delta);
			}
		}
		
		private void LoadCustomerDatabase()
		{
			// Müşteri grupları ve özelliklerini tanımla
			// TODO: İleride JSON/CSV dosyasından yüklenecek
			
			// Normal müşteriler
			AddCustomerGroupToDatabase(new CustomerGroupData
			{
				Id = "regular_low",
				Name = "Sıradan Müşteriler (Düşük)",
				GroupSize = new Vector2I(1, 3),
				SpendingPower = 20.0f,
				LoyaltyChance = 0.2f,
				SpawnWeight = 100,
				PreferredFurniture = new string[] { "table_basic" },
				Description = "Sıradan, düşük gelirli müşteriler. Az harcarlar ama sık gelirler."
			});
			
			AddCustomerGroupToDatabase(new CustomerGroupData
			{
				Id = "regular_mid",
				Name = "Sıradan Müşteriler (Orta)",
				GroupSize = new Vector2I(2, 4),
				SpendingPower = 40.0f,
				LoyaltyChance = 0.3f,
				SpawnWeight = 70,
				PreferredFurniture = new string[] { "table_basic", "table_vip" },
				Description = "Orta gelirli müşteriler. Makul miktarda harcarlar."
			});
			
			// İş adamları
			AddCustomerGroupToDatabase(new CustomerGroupData
			{
				Id = "businessman",
				Name = "İş Adamları",
				GroupSize = new Vector2I(2, 5),
				SpendingPower = 80.0f,
				LoyaltyChance = 0.4f,
				SpawnWeight = 40,
				PreferredFurniture = new string[] { "table_vip", "cons_table_basic" },
				Description = "İş adamları grubu. İyi harcarlar ve kons masası tercih ederler."
			});
			
			// Zengin müşteriler
			AddCustomerGroupToDatabase(new CustomerGroupData
			{
				Id = "rich",
				Name = "Zengin Müşteriler",
				GroupSize = new Vector2I(3, 6),
				SpendingPower = 150.0f,
				LoyaltyChance = 0.5f,
				SpawnWeight = 20,
				PreferredFurniture = new string[] { "table_vip", "cons_table_deluxe" },
				Description = "Zengin ve nüfuzlu müşteriler. Çok para harcarlar ve en iyi hizmeti beklerler."
			});
			
			// Şantaj/Blackmail potansiyeli olan VIP'ler
			AddCustomerGroupToDatabase(new CustomerGroupData
			{
				Id = "vip_blackmailable",
				Name = "Özel VIP Misafirler",
				GroupSize = new Vector2I(1, 3),
				SpendingPower = 200.0f,
				LoyaltyChance = 0.6f,
				SpawnWeight = 5,
				PreferredFurniture = new string[] { "cons_table_deluxe" },
				BlackmailPotential = true,
				Description = "Çok zengin ve önemli kişiler. Muhtemel şantaj hedefleri."
			});
			
			GD.Print($"📋 {customerGroupDatabase.Count} müşteri grubu veritabanına yüklendi");
		}
		
		private void AddCustomerGroupToDatabase(CustomerGroupData data)
		{
			if (customerGroupDatabase.ContainsKey(data.Id))
			{
				GD.PrintErr($"CustomerManager: Bu ID'ye sahip müşteri grubu zaten var: {data.Id}");
				return;
			}
			
			customerGroupDatabase[data.Id] = data;
		}
		
		private void OnNewDayStarted(int day)
		{
			// Yeni gün başladığında müşteri akışını başlat
			GD.Print("🌃 Müşteri akışı başlatılıyor...");
			
			// Pavyon özelliklerini hesapla
			CalculatePavyonProperties();
			
			// Spawn aralığını resetle
			currentSpawnInterval = baseSpawnInterval;
			timeSinceLastSpawn = 0.0f;
			
			// Tüm aktif müşterileri temizle
			ClearAllCustomers();
		}
		
		private void OnDayEnded(int day)
		{
			// Gün sonunda tüm müşterileri gönder
			GD.Print("🌄 Müşterilerin ayrılması sağlanıyor...");
			
			foreach (var group in activeCustomerGroups.ToList())
			{
				MakeCustomerGroupLeave(group, "Pavyon kapanıyor");
			}
		}
		
		private void OnHourChanged(int hour)
		{
			// Saat ilerledikçe müşteri davranışlarını ayarla
			AdjustCustomerBehaviorByHour(hour);
		}
		
		private void SpawnRandomCustomerGroup()
		{
			// Pavyon dolu mu kontrol et
			if (activeCustomerGroups.Count >= maxActiveCustomerGroups)
			{
				GD.Print("👥 Pavyon dolu, yeni müşteri alınmıyor");
				return;
			}
			
			// Rastgele bir müşteri grubu seç (spawn ağırlıklarına göre)
			string selectedGroupId = SelectRandomCustomerGroupId();
			
			if (string.IsNullOrEmpty(selectedGroupId) || !customerGroupDatabase.ContainsKey(selectedGroupId))
			{
				GD.PrintErr("CustomerManager: Geçersiz müşteri grubu ID'si");
				return;
			}
			
			CustomerGroupData groupData = customerGroupDatabase[selectedGroupId];
			
			// Grup boyutunu belirle
			int groupSize = GD.RandRange(groupData.GroupSize.X, groupData.GroupSize.Y);
			
			// Rastgele bir spawn noktası seç
			Vector2 spawnPoint = spawnPoints[GD.RandRange(0, spawnPoints.Length - 1)];
			
			// Yeni müşteri grubu oluştur
			CustomerGroup newGroup = new CustomerGroup();
			newGroup.Initialize(groupData, groupSize, spawnPoint);
			
			// Müşteri grubunu sahneye ekle
			customerContainer.AddChild(newGroup);
			activeCustomerGroups.Add(newGroup);
			
			// Müşteri grubu sinyallerini bağla
			newGroup.Connect(CustomerGroup.SignalName.CustomerGroupLeft, Callable.From(OnCustomerGroupLeft));
			newGroup.Connect(CustomerGroup.SignalName.CustomerGroupSpent, Callable.From(OnCustomerGroupSpent));
			
			GD.Print($"👥 Yeni müşteri grubu geldi: {groupData.Name} (Kişi sayısı: {groupSize})");
			
			// Signal gönder
			EmitSignal(SignalName.CustomerGroupArrived, groupData.Id, groupSize);
		}
		
		private string SelectRandomCustomerGroupId()
		{
			// Spawn ağırlıklarına göre rastgele bir müşteri grubu seç
			int totalWeight = 0;
			foreach (var pair in customerGroupDatabase)
			{
				totalWeight += pair.Value.SpawnWeight;
			}
			
			if (totalWeight <= 0)
				return null;
				
			int randomValue = GD.RandRange(1, totalWeight);
			int accumulatedWeight = 0;
			
			foreach (var pair in customerGroupDatabase)
			{
				accumulatedWeight += pair.Value.SpawnWeight;
				if (randomValue <= accumulatedWeight)
				{
					return pair.Key;
				}
			}
			
			// Varsayılan olarak ilk grubu döndür
			return customerGroupDatabase.Keys.First();
		}
		
		private void UpdateActiveCustomers(float delta)
		{
			// Tüm aktif müşteri gruplarını güncelle
			foreach (var group in activeCustomerGroups.ToList())
			{
				group.UpdateState(delta);
				
				// Müşteri memnuniyeti çok düşükse, ayrılmalarını sağla
				if (group.Satisfaction < 20.0f && !group.IsLeaving)
				{
					MakeCustomerGroupLeave(group, "Memnuniyetsizlik");
				}
			}
		}
		
		private void AdjustSpawnInterval()
		{
			// Geceye göre spawn aralığını ayarla
			int hour = timeManager.CurrentTime.Hour;
			
			// Gece ilerledikçe daha az müşteri gelsin (3-4 gibi)
			if (hour >= 2 && hour < 6)
			{
				currentSpawnInterval = baseSpawnInterval * 2.0f;
			}
			else if (hour >= 6)
			{
				currentSpawnInterval = baseSpawnInterval * 5.0f; // Neredeyse hiç yeni müşteri gelmesin
			}
			else
			{
				// Normal saatler (18-2 arası)
				currentSpawnInterval = baseSpawnInterval * (1.0f - (pavyonPopularity / 200.0f)); // Popülariteye göre aralığı azalt
			}
		}
		
		private void AdjustCustomerBehaviorByHour(int hour)
		{
			// Saat ilerledikçe müşteri davranışlarını ayarla
			float spendingMultiplier = 1.0f;
			
			if (hour >= 22 && hour < 2) // En hareketli saatler
			{
				spendingMultiplier = 1.3f;
				GD.Print("🍸 En hareketli saatler: Müşteriler daha fazla harcıyor");
			}
			else if (hour >= 2 && hour < 4) // Geç saatler
			{
				spendingMultiplier = 1.1f;
			}
			else if (hour >= 4) // Sabaha karşı
			{
				spendingMultiplier = 0.8f;
				GD.Print("🌅 Sabaha karşı: Müşteriler daha az harcıyor");
			}
			
			// Tüm aktif müşteri gruplarının harcama çarpanını ayarla
			foreach (var group in activeCustomerGroups)
			{
				group.AdjustSpendingMultiplier(spendingMultiplier);
			}
		}
		
		private void CalculatePavyonProperties()
		{
			// Pavyon kalitesini hesapla (mobilyalara göre)
			float totalAtmosphereBonus = 0.0f;
			
			if (furnitureManager != null)
			{
				totalAtmosphereBonus = furnitureManager.CalculateTotalAtmosphereBonus();
			}
			
			// Temel değerler + mobilya bonusu
			pavyonQuality = Mathf.Clamp(50.0f + totalAtmosphereBonus, 0.0f, 100.0f);
			
			// Popülariteyi hesapla (kalite ve diğer faktörlerden)
			// Not: İleride itibar sistemi de bunu etkileyecek
			pavyonPopularity = Mathf.Clamp(pavyonQuality * 0.7f + 30.0f, 0.0f, 100.0f);
			
			GD.Print($"🏢 Pavyon özellikleri hesaplandı - Kalite: {pavyonQuality:F1}, Popülarite: {pavyonPopularity:F1}");
		}
		
		private void OnCustomerGroupLeft(string groupId, int groupSize, float totalSpent, string reason)
		{
			// Listeden müşteri grubunu bul ve kaldır
			CustomerGroup groupToRemove = null;
			
			foreach (var group in activeCustomerGroups)
			{
				if (group.GroupId == groupId)
				{
					groupToRemove = group;
					break;
				}
			}
			
			if (groupToRemove != null)
			{
				activeCustomerGroups.Remove(groupToRemove);
				GD.Print($"👋 Müşteri grubu ayrıldı: {groupToRemove.GroupData.Name} - Neden: {reason}");
				GD.Print($"  Toplam harcama: {totalSpent:F2}₺");
			}
			
			// Signal gönder
			EmitSignal(SignalName.CustomerGroupLeft, groupId, groupSize, totalSpent, reason);
		}
		
		private void OnCustomerGroupSpent(string groupId, float amount, string category)
		{
			// Ekonomiye harcama ekle
			if (economyManager != null)
			{
				economyManager.AddIncome(amount, category, $"Müşteri harcaması: {category}");
			}
			
			// Signal gönder
			EmitSignal(SignalName.CustomerSpending, groupId, amount, category);
		}
		
		public void MakeCustomerGroupLeave(CustomerGroup group, string reason)
		{
			if (group != null && !group.IsLeaving)
			{
				group.StartLeaving(reason);
			}
		}
		
		private void ClearAllCustomers()
		{
			foreach (var group in activeCustomerGroups.ToList())
			{
				group.QueueFree();
			}
			
			activeCustomerGroups.Clear();
		}
		
		// Dışarıdan erişilebilir özellikler
		public int GetActiveCustomerCount()
		{
			int totalCount = 0;
			foreach (var group in activeCustomerGroups)
			{
				totalCount += group.GroupSize;
			}
			return totalCount;
		}
		
		public float GetPavyonPopularity()
		{
			return pavyonPopularity;
		}
		
		// Signal tanımlamaları
		[Signal] public delegate void CustomerGroupArrivedEventHandler(string groupId, int groupSize);
		[Signal] public delegate void CustomerGroupLeftEventHandler(string groupId, int groupSize, float totalSpent, string reason);
		[Signal] public delegate void CustomerSpendingEventHandler(string groupId, float amount, string category);
	}
}
