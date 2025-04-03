// src/Staff/StaffTypes/Cook.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Staff
{
	public class Cook : StaffMember
	{
		// Yemek türleri
		public enum CuisineType { 
			Meze, 
			Izgara, 
			Ocakbaşı, 
			Kebap, 
			FastFood, 
			Ev_Yemeği 
		}
		
		public CuisineType Specialty { get; set; } = CuisineType.Meze;
		
		// Aşçıya özel özellikler
		public float CookingQuality { get; set; } = 1.0f;
		public float EfficiencyRate { get; set; } = 1.0f;
		public float WasteReduction { get; set; } = 0.2f; // 0.0-1.0 arası, malzeme tasarrufu
		public float CreativityLevel { get; set; } = 0.5f; // 0.0-1.0 arası, yeni tarifler yaratabilme
		
		// Yemek istatistikleri
		public int DishesPrepared { get; private set; } = 0;
		public int SpecialDishesPrepared { get; private set; } = 0;
		public int CustomerCompliments { get; private set; } = 0;
		
		// Tarif koleksiyonu
		public int RecipeCount => 10 + (Level * 5); // Her seviye için 5 tarif daha
		
		// Mutfak ekipmanları
		public bool HasChefKnives { get; set; } = false; // Daha hızlı ve kaliteli hazırlık
		public bool HasSpecialSpices { get; set; } = false; // Daha lezzetli yemekler
		public bool HasModernEquipment { get; set; } = false; // Daha verimli pişirme
		
		// Constructor
		public Cook() : base()
		{
			// İş pozisyonunu ayarla
			JobTitle = "Aşçı";
			
			// Aşçıya özel özellikleri başlat
			if (!HasAttribute("Yemek")) SetAttributeValue("Yemek", 5f);
			if (!HasAttribute("Dikkat")) SetAttributeValue("Dikkat", 4f);
			if (!HasAttribute("Yaratıcılık")) SetAttributeValue("Yaratıcılık", 3f);
			if (!HasAttribute("Hız")) SetAttributeValue("Hız", 3f);
			
			// Rastgele bir mutfak uzmanlığı seç
			int cuisineCount = Enum.GetValues(typeof(CuisineType)).Length;
			Specialty = (CuisineType)GD.RandRange(0, cuisineCount - 1);
			
			// Bağımlı değerleri hesapla
			RecalculateStats();
		}
		
		// Değerleri yeniden hesapla
		public void RecalculateStats()
		{
			// Yemek kalitesi - ana yemek yeteneğini yansıtır
			CookingQuality = 1.0f + (GetAttributeValue("Yemek") / 10f) * 2.0f; // 1.0-3.0 arası
			
			// Verimlilik oranı - hız ve organizasyon etkiler
			float speedBonus = GetAttributeValue("Hız") / 10f;
			float organizationBonus = HasAttribute("Organizasyon") ? GetAttributeValue("Organizasyon") / 10f : 0f;
			EfficiencyRate = 1.0f + (speedBonus + organizationBonus) * 1.5f; // 1.0-2.5 arası
			
			// Malzeme tasarrufu - dikkat ve deneyim etkiler
			float attentionBonus = GetAttributeValue("Dikkat") / 10f;
			WasteReduction = 0.2f + (attentionBonus * 0.3f) + (Level * 0.05f); // 0.2-0.8 arası
			WasteReduction = Mathf.Clamp(WasteReduction, 0.2f, 0.8f);
			
			// Yaratıcılık seviyesi - yeni tarifler üretebilme
			CreativityLevel = 0.3f + (GetAttributeValue("Yaratıcılık") / 10f) * 0.6f; // 0.3-0.9 arası
			
			// Ekipman bonusları
			if (HasChefKnives) EfficiencyRate *= 1.2f;
			if (HasSpecialSpices) CookingQuality *= 1.15f;
			if (HasModernEquipment) {
				EfficiencyRate *= 1.1f;
				WasteReduction += 0.1f;
			}
		}
		
		// Yemek hazırlama
		public float PrepareDish(string dishName, int quantity, float baseCost)
		{
			if (quantity <= 0) return 0f;
			
			// Temel yemek kalitesi
			float qualityScore = CookingQuality;
			
			// Uzmanlık alanındaki yemeklerde bonus
			if (IsDishInSpecialty(dishName))
			{
				qualityScore *= 1.2f;
				GD.Print($"{FullName} uzmanı olduğu {dishName} yemeğini hazırlıyor!");
			}
			
			// Maliyeti hesapla (tasarruf faktörünü uygula)
			float actualCost = baseCost * quantity * (1f - WasteReduction);
			
			// Başarı şansı - kompleks yemeklerde daha düşük
			float complexity = 1.0f; // Basit yemekler için
			if (dishName.Contains("Özel") || dishName.Contains("Lüks"))
			{
				complexity = 1.5f; // Daha kompleks yemekler
			}
			
			float successRate = Mathf.Min(0.95f, (GetAttributeValue("Yemek") / 10f) / complexity);
			
			// Başarılı porsiyonları hesapla
			int successfulDishes = 0;
			for (int i = 0; i < quantity; i++)
			{
				if (GD.Randf() < successRate)
				{
					successfulDishes++;
				}
			}
			
			// İstatistikleri güncelle
			DishesPrepared += successfulDishes;
			
			if (dishName.Contains("Özel") || dishName.Contains("Lüks"))
			{
				SpecialDishesPrepared += successfulDishes;
			}
			
			// Müşteri beğeni şansı (kaliteye bağlı)
			float complimentChance = qualityScore / 5f; // 0.2-0.6 arası
			
			for (int i = 0; i < successfulDishes; i++)
			{
				if (GD.Randf() < complimentChance)
				{
					CustomerCompliments++;
					
					// Beğeniler sadakati artırır
					IncreaseLoyalty(GD.RandRange(0.1f, 0.3f));
				}
			}
			
			// Yemek başarısına göre sadakat değişimi
			float successPercentage = (float)successfulDishes / quantity;
			if (successPercentage > 0.8f)
			{
				// Yüksek başarı sadakati artırır
				IncreaseLoyalty(GD.RandRange(0.2f, 0.5f));
			}
			else if (successPercentage < 0.5f)
			{
				// Düşük başarı sadakati azaltır
				ReduceLoyalty(GD.RandRange(0.1f, 0.4f));
			}
			
			GD.Print($"{FullName} {quantity} porsiyon {dishName} hazırladı. Başarılı: {successfulDishes}, Kalite: {qualityScore:F1}/5");
			
			// Yemeğin satış değeri (maliyetin üzerine kalite bazlı kar)
			float dishValue = actualCost * (1.0f + (qualityScore / 5f));
			
			return dishValue;
		}
		
		// Yeni tarif geliştirme
		public bool DevelopNewRecipe(string recipeName, float difficultyLevel = 1.0f)
		{
			// Zorluk seviyesi başarı şansını etkiler (1.0=kolay, 3.0=zor)
			float successChance = CreativityLevel / difficultyLevel;
			
			// Yaratıcılık ve yemek yeteneği etkiler
			successChance *= (GetAttributeValue("Yaratıcılık") + GetAttributeValue("Yemek")) / 20f;
			
			// Maksimum %80 başarı şansı
			successChance = Mathf.Clamp(successChance, 0.1f, 0.8f);
			
			// Başarı kontrolü
			bool success = GD.Randf() < successChance;
			
			if (success)
			{
				GD.Print($"🌟 {FullName} yeni bir tarif geliştirdi: {recipeName}");
				
				// Yeni tarifler sadakati ve yaratıcılığı artırır
				IncreaseLoyalty(GD.RandRange(1.0f, 2.0f));
				if (HasAttribute("Yaratıcılık"))
				{
					SetAttributeValue("Yaratıcılık", GetAttributeValue("Yaratıcılık") + GD.RandRange(0.1f, 0.3f));
				}
			}
			else
			{
				GD.Print($"{FullName} \"{recipeName}\" tarifini geliştirmeyi başaramadı.");
			}
			
			return success;
		}
		
		// Stok yönetimi
		public float ManageInventory(float inventoryValue)
		{
			// Malzeme tasarrufu sayesinde elde edilen tasarruf
			float savedAmount = inventoryValue * WasteReduction;
			
			// Dikkat yüksekse ek tasarruf sağlar
			if (GetAttributeValue("Dikkat") > 7f)
			{
				savedAmount *= 1.2f;
			}
			
			GD.Print($"{FullName} malzeme yönetimi ile {savedAmount:F0}₺ tasarruf sağladı.");
			
			return savedAmount;
		}
		
		// Mutfak personelini eğitme (daha yüksek seviyedeki aşçılar yapabilir)
		public bool TrainKitchenStaff(StaffMember staff, string attribute)
		{
			if (staff == null || Level < 3) return false;
			
			// Sadece belirli özellikleri eğitebilir
			if (attribute != "Yemek" && attribute != "Dikkat" && attribute != "Hız") return false;
			
			// Kendinden daha yüksek seviyedeki personeli eğitemez
			if (staff is Cook && staff.Level >= Level) return false;
			
			// Eğitim başarı şansı
			float successChance = (GetAttributeValue(attribute) / 10f) * 0.8f;
			bool success = GD.Randf() < successChance;
			
			if (success)
			{
				// Eğitim alan personelin özelliğini geliştir
				float currentValue = staff.GetAttributeValue(attribute);
				float improvement = GD.RandRange(0.2f, 0.5f);
				staff.SetAttributeValue(attribute, currentValue + improvement);
				
				GD.Print($"{FullName} {staff.FullName}'e {attribute} eğitimi verdi. +{improvement:F1} gelişim.");
				
				// Eğitim vermek sadakati artırır
				IncreaseLoyalty(GD.RandRange(0.3f, 0.7f));
			}
			else
			{
				GD.Print($"{FullName}'in eğitim girişimi başarısız oldu.");
			}
			
			return success;
		}
		
		// Görev atama işlevini override et
		public override bool AssignTask(StaffTask task)
		{
			if (task.Type == "YemekHazırlama")
			{
				// Aşçılar bu görevde çok etkili
				return base.AssignTask(task);
			}
			else if (task.Type == "TemizlikYapma")
			{
				// Aşçılar temizlik yapabilir (mutfak temizliği)
				return base.AssignTask(task);
			}
			
			// Diğer görevler için uygun değil
			return false;
		}
		
		// Görev performans hesaplamasını override et
		protected override float CalculateTaskPerformance(StaffTask task)
		{
			float basePerformance = base.CalculateTaskPerformance(task);
			
			// Aşçıya özel performans artışları
			if (task.Type == "YemekHazırlama")
			{
				// Yemek hazırlamada ekstra performans
				basePerformance *= 1.5f;
				
				// Ekipman bonusları
				if (HasChefKnives) basePerformance *= 1.1f;
				if (HasSpecialSpices) basePerformance *= 1.1f;
				if (HasModernEquipment) basePerformance *= 1.1f;
			}
			else if (task.Type == "TemizlikYapma")
			{
				// Temizlikte normal performans (mutfak temizliği)
				basePerformance *= 1.0f;
			}
			
			return Mathf.Clamp(basePerformance, 0f, 10f);
		}
		
		// Özel yetenekleri override et
		public override string[] GetSpecialCapabilities()
		{
			List<string> capabilities = new List<string>();
			
			// Temel aşçı yetenekleri
			capabilities.Add("Yemek Hazırlama");
			capabilities.Add($"{Specialty} Uzmanlığı");
			
			// Seviyeye bağlı yetenekler
			if (Level >= 2) capabilities.Add("Malzeme Yönetimi");
			if (Level >= 3) capabilities.Add("Personel Eğitimi");
			if (Level >= 4) capabilities.Add("Özel Menü Hazırlama");
			if (Level >= 5) capabilities.Add("Şef Kontrolü");
			
			// Özel yetenekler
			if (CookingQuality >= 2.5f) capabilities.Add("Üstün Lezzet");
			if (WasteReduction >= 0.6f) capabilities.Add("Minimum Atık");
			if (CreativityLevel >= 0.7f) capabilities.Add("Yaratıcı Tarifler");
			
			return capabilities.ToArray();
		}
		
		// Clone metodunu override et
		public override StaffMember Clone()
		{
			Cook clone = new Cook
			{
				FullName = this.FullName,
				JobTitle = this.JobTitle,
				Level = this.Level,
				Salary = this.Salary,
				Loyalty = this.Loyalty,
				Specialty = this.Specialty,
				CookingQuality = this.CookingQuality,
				EfficiencyRate = this.EfficiencyRate,
				WasteReduction = this.WasteReduction,
				CreativityLevel = this.CreativityLevel,
				HasChefKnives = this.HasChefKnives,
				HasSpecialSpices = this.HasSpecialSpices,
				HasModernEquipment = this.HasModernEquipment
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
			
			// Aşçıya özel bilgiler
			status += $"\nUzmanlık: {Specialty}\n";
			status += $"Yemek Kalitesi: {CookingQuality:F1}/5\n";
			status += $"Verimlilik: {EfficiencyRate:F1}x\n";
			status += $"Malzeme Tasarrufu: %{WasteReduction * 100:F0}\n";
			status += $"Yaratıcılık: %{CreativityLevel * 100:F0}\n";
			status += $"Tarif Sayısı: {RecipeCount}\n";
			
			// Ekipman bilgileri
			List<string> equipment = new List<string>();
			if (HasChefKnives) equipment.Add("Şef Bıçakları");
			if (HasSpecialSpices) equipment.Add("Özel Baharatlar");
			if (HasModernEquipment) equipment.Add("Modern Ekipman");
			
			if (equipment.Count > 0)
			{
				status += $"Ekipman: {string.Join(", ", equipment)}\n";
			}
			
			return status;
		}
		
		// Yemek türünü döndür
		public string GetSpecialtyName()
		{
			return Enum.GetName(typeof(CuisineType), Specialty);
		}
		
		// Bir yemeğin uzmanlık alanında olup olmadığını kontrol et
		private bool IsDishInSpecialty(string dishName)
		{
			// Yemek ismi uzmanlık türü içeriyorsa
			string specialtyName = GetSpecialtyName().Replace("_", " ");
			return dishName.Contains(specialtyName);
		}
	}
}
