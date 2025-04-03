// src/Staff/StaffTypes/Waiter.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Staff
{
	public class Waiter : StaffMember
	{
		// Garsona özel özellikler
		public float ServiceSpeed { get; set; } = 1.0f;
		public float ServingCapacity { get; set; } = 3f; // Aynı anda kaç müşteriye servis yapabilir
		public float SpillChance { get; set; } = 0.1f; // Bir şey dökme olasılığı
		public float TipRate { get; set; } = 0.05f; // Müşterilerden alınan içecek fiyatının %5'i bahşiş
		
		// Performans istatistikleri
		public int OrdersServed { get; private set; } = 0;
		public int TablesServed { get; private set; } = 0;
		public float TotalTipsCollected { get; private set; } = 0f;
		public int SpillsCount { get; private set; } = 0;
		
		// Sorumlu olduğu alan ve masalar
		public List<object> AssignedTables { get; private set; } = new List<object>();
		public float MaxTables => 2f + (Level * 0.5f); // Her seviye için 0.5 daha fazla masa
		
		// Özel özellikler
		public bool CanMixDrinks { get; set; } = false;
		public bool HasTray { get; set; } = true; // Standart ekipman
		public bool HasTablet { get; set; } = false; // Siparişleri daha hızlı alabilir
		
		// Constructor
		public Waiter() : base()
		{
			// İş pozisyonunu ayarla
			JobTitle = "Garson";
			
			// Garsona özel özellikleri başlat
			if (!HasAttribute("Hız")) SetAttributeValue("Hız", 4f);
			if (!HasAttribute("Dikkat")) SetAttributeValue("Dikkat", 3f);
			if (!HasAttribute("Dayanıklılık")) SetAttributeValue("Dayanıklılık", 3f);
			
			// Bağımlı değerleri hesapla
			RecalculateStats();
		}
		
		// Değerleri yeniden hesapla
		public void RecalculateStats()
		{
			// Servis hızı - siparişleri ne kadar hızlı işler
			ServiceSpeed = 1.0f + (GetAttributeValue("Hız") / 10f) * 1.5f; // 1.0-2.5 arası
			
			// Dökme şansı - düşük dikkat = daha yüksek dökme şansı
			SpillChance = 0.15f - (GetAttributeValue("Dikkat") / 10f) * 0.1f; // 0.05-0.15 arası
			SpillChance = Mathf.Max(0.01f, SpillChance); // Minimum %1 şans
			
			// Bahşiş oranı - karizma ve dikkat bunu etkiler
			float charismaBonus = HasAttribute("Karizma") ? GetAttributeValue("Karizma") / 100f : 0f;
			TipRate = 0.05f + charismaBonus; // %5 + karizmanın etkisi
			TipRate = Mathf.Clamp(TipRate, 0.05f, 0.15f); // %5-%15 arası
			
			// Ekipman bonusları
			if (HasTablet) ServiceSpeed *= 1.2f;
			if (CanMixDrinks) TipRate += 0.02f; // Özel içecekler daha fazla bahşiş getirir
		}
		
		// İçecek servisi yapma
		public float ServeDrinks(object table, int customerCount, float baseDrinkPrice)
		{
			if (table == null || customerCount <= 0) return 0f;
			
			// Sipariş sayısı
			int orderCount = customerCount;
			
			// Sipariş başarı olasılığı
			float orderSuccessChance = 0.9f + (GetAttributeValue("Dikkat") / 10f) * 0.1f; // %90-100 arası
			
			// Başarılı siparişleri hesapla
			int successfulOrders = 0;
			for (int i = 0; i < orderCount; i++)
			{
				if (GD.Randf() < orderSuccessChance)
				{
					successfulOrders++;
				}
			}
			
			// Dökülme kontrolü
			bool spill = GD.Randf() < SpillChance;
			if (spill)
			{
				// Sipariş başına dökme olasılığını kontrol et
				int spilledOrders = Mathf.Min(successfulOrders, GD.RandRange(1, 2));
				successfulOrders -= spilledOrders;
				SpillsCount++; // Dökülme sayacını artır
				
				GD.Print($"{FullName} {spilledOrders} siparişi döktü!");
				
				// Dökülme sadakati düşürebilir
				ReduceLoyalty(GD.RandRange(0.2f, 0.5f));
			}
			
			// Toplam satış hesaplama
			float totalSales = successfulOrders * baseDrinkPrice;
			
			// Bahşiş hesaplama
			float tips = totalSales * TipRate;
			TotalTipsCollected += tips;
			
			// İstatistikleri güncelle
			OrdersServed += successfulOrders;
			TablesServed++;
			
			// Başarılı servis sadakati artırabilir
			if (successfulOrders > 0)
			{
				float loyaltyGain = (float)successfulOrders / orderCount * GD.RandRange(0.1f, 0.3f);
				IncreaseLoyalty(loyaltyGain);
			}
			
			// Toplam satışı döndür (pavyona giden kısım)
			return totalSales;
		}
		
		// Masaları atama
		public bool AssignTable(object table)
		{
			if (table == null) return false;
			
			// Maksimum masa sayısını kontrol et
			if (AssignedTables.Count >= MaxTables)
			{
				GD.Print($"{FullName} daha fazla masa alamaz! (Maks: {MaxTables})");
				return false;
			}
			
			// Masayı ekle
			if (!AssignedTables.Contains(table))
			{
				AssignedTables.Add(table);
				GD.Print($"{FullName}'e yeni bir masa atandı. Toplam: {AssignedTables.Count} masa.");
				return true;
			}
			
			return false;
		}
		
		// Masaları temizleme
		public int CleanTables()
		{
			// Temizlenen masa sayısı serviste hıza ve dayanıklılığa bağlıdır
			int maxTablesToClean = Mathf.FloorToInt(ServiceSpeed * 2f);
			int tablesToClean = Mathf.Min(maxTablesToClean, AssignedTables.Count);
			
			// Temizleme işlemi
			for (int i = 0; i < tablesToClean; i++)
			{
				// İşlemler burada - masaların durumunu sıfırlama vs.
			}
			
			return tablesToClean;
		}
		
		// İçecek karıştırma (özel yetenek)
		public float MixSpecialDrink(string drinkName, float difficultyLevel = 1.0f)
		{
			if (!CanMixDrinks) return 0f;
			
			// Zorluk seviyesi başarı şansını etkiler (1.0=kolay, 3.0=zor)
			float successChance = GetAttributeValue("Yaratıcılık") / 10f / difficultyLevel;
			successChance = Mathf.Clamp(successChance, 0.3f, 0.9f); // %30-%90 arası
			
			// Başarı kontrolü
			bool success = GD.Randf() < successChance;
			
			if (success)
			{
				float qualityScore = GD.RandRange(5f, 10f) * (GetAttributeValue("Dikkat") / 10f);
				qualityScore = Mathf.Clamp(qualityScore, 5f, 10f);
				
				GD.Print($"{FullName} özel içeceği başarıyla hazırladı: {drinkName} (Kalite: {qualityScore:F1}/10)");
				return qualityScore;
			}
			else
			{
				GD.Print($"{FullName} özel içeceği hazırlarken sorun yaşadı: {drinkName}");
				return 0f;
			}
		}
		
		// Görev atama işlevini override et
		public override bool AssignTask(StaffTask task)
		{
			if (task.Type == "İçecekHazırlama" || task.Type == "TemizlikYapma")
			{
				// Garsonlar bu görevlerde daha etkili
				return base.AssignTask(task);
			}
			else if (task.Type == "MüşteriEğlendirme")
			{
				// Garsonlar müşteri eğlendirebilir ama ideal değil
				if (HasAttribute("Karizma") && GetAttributeValue("Karizma") >= 3.0f)
				{
					return base.AssignTask(task);
				}
				return false;
			}
			
			// Diğer görevler için uygun değil
			return false;
		}
		
		// Görev performans hesaplamasını override et
		protected override float CalculateTaskPerformance(StaffTask task)
		{
			float basePerformance = base.CalculateTaskPerformance(task);
			
			// Garsona özel performans artışları
			if (task.Type == "İçecekHazırlama")
			{
				// İçecek hazırlamada ekstra performans
				basePerformance *= 1.2f;
				
				// İçecek karıştırma yeteneği varsa bonus
				if (CanMixDrinks) basePerformance *= 1.3f;
			}
			else if (task.Type == "TemizlikYapma")
			{
				// Temizlikte ekstra performans
				basePerformance *= 1.1f;
			}
			
			return Mathf.Clamp(basePerformance, 0f, 10f);
		}
		
		// Özel yetenekleri override et
		public override string[] GetSpecialCapabilities()
		{
			List<string> capabilities = new List<string>();
			
			// Temel garson yetenekleri
			capabilities.Add("İçecek Servisi");
			capabilities.Add("Masa Temizliği");
			
			// Seviyeye bağlı yetenekler
			if (Level >= 2) capabilities.Add("Yoğun Masa Yönetimi");
			if (Level >= 3) capabilities.Add("VIP Servis");
			if (Level >= 4) capabilities.Add("Çok Masalı Servis");
			if (Level >= 5) capabilities.Add("Müşteri Memnuniyeti Uzmanı");
			
			// Özel yetenekler
			if (CanMixDrinks) capabilities.Add("Özel İçecek Hazırlama");
			if (HasTablet) capabilities.Add("Elektronik Sipariş Alma");
			
			return capabilities.ToArray();
		}
		
		// Clone metodunu override et
		public override StaffMember Clone()
		{
			Waiter clone = new Waiter
			{
				FullName = this.FullName,
				JobTitle = this.JobTitle,
				Level = this.Level,
				Salary = this.Salary,
				Loyalty = this.Loyalty,
				ServiceSpeed = this.ServiceSpeed,
				ServingCapacity = this.ServingCapacity,
				SpillChance = this.SpillChance,
				TipRate = this.TipRate,
				CanMixDrinks = this.CanMixDrinks,
				HasTray = this.HasTray,
				HasTablet = this.HasTablet
			};
			
			// Özellikleri kopyala
			foreach (var attr in this.attributes)
			{
				clone.attributes[attr.Key] = attr.Value;
			}
			
			return clone;
		}
		
		// Durum gösterimini özelleştir
		public override string GetStatusDisplay()
		{
			string status = base.GetStatusDisplay();
			
			// Garsona özel bilgiler
			status += $"\nServis Hızı: {ServiceSpeed:F1}x\n";
			status += $"Bahşiş Oranı: %{TipRate * 100:F0}\n";
			status += $"Dökme Olasılığı: %{SpillChance * 100:F0}\n";
			status += $"Masalar: {AssignedTables.Count}/{MaxTables:F1}\n";
			
			// Özel yetenekler
			if (CanMixDrinks) status += "✓ Özel İçecek Hazırlayabilir\n";
			if (HasTablet) status += "✓ Tablet Kullanıyor\n";
			
			return status;
		}
	}
}
