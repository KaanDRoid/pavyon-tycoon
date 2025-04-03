// src/Staff/StaffTypes/IllegalFloorStaff.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Staff
{
	public class IllegalFloorStaff : StaffMember
	{
		// Kaçak kat faaliyet türleri
		public enum ActivityType { 
			Kumar, 
			Şantaj, 
			Kaçak_İçki, 
			Uyuşturucu, 
			Bilgi_Toplama, 
			VIP_Koruma 
		}
		
		public ActivityType PrimaryActivity { get; set; } = ActivityType.Kumar;
		public ActivityType SecondaryActivity { get; set; } = ActivityType.Kaçak_İçki;
		
		// Kaçak kat personeline özel özellikler
		public float DiscretionLevel { get; set; } = 1.0f; // 1.0-5.0 arası, fark edilmeme yeteneği
		public float ProfitMargin { get; set; } = 1.0f; // 1.0-3.0 arası, yasadışı işlerde kâr marjı
		public float RiskReduction { get; set; } = 0.2f; // 0.0-1.0 arası, risk azaltma yeteneği
		public float ConnectionsLevel { get; set; } = 0.5f; // 0.0-1.0 arası, faydalı bağlantılar
		
		// Faaliyet istatistikleri
		public float TotalProfitGenerated { get; private set; } = 0f;
		public int OperationsCompleted { get; private set; } = 0;
		public int CloseCallsAvoided { get; private set; } = 0;
		
		// Etkinliğe bağlı değerler
		public float DetectionRisk => 0.5f - (DiscretionLevel / 10f); // Yakalanma riski
		public int MaxVipContacts => 2 + Level; // Önemli kişilerle bağlantılar
		
		// Özel ekipman
		public bool HasFakeID { get; set; } = false; // Kimlik gizleme
		public bool HasCustomSecuritySystem { get; set; } = false; // Erken uyarı sistemi
		public bool HasVIPContactList { get; set; } = false; // Önemli kişilere erişim
		
		// Constructor
		public IllegalFloorStaff() : base()
		{
			// İş pozisyonunu ayarla
			JobTitle = "Kaçak Kat Görevlisi";
			
			// Kaçak kat personeline özel özellikleri başlat
			if (!HasAttribute("Gizlilik")) SetAttributeValue("Gizlilik", 5f);
			if (!HasAttribute("Sadakat")) SetAttributeValue("Sadakat", 5f);
			if (!HasAttribute("Uyanıklık")) SetAttributeValue("Uyanıklık", 4f);
			if (!HasAttribute("İkna")) SetAttributeValue("İkna", 3f);
			
			// Rastgele bir faaliyet türü seç
			int activityCount = Enum.GetValues(typeof(ActivityType)).Length;
			PrimaryActivity = (ActivityType)GD.RandRange(0, activityCount - 1);
			
			// İkinci bir faaliyet seç (farklı olmalı)
			do {
				SecondaryActivity = (ActivityType)GD.RandRange(0, activityCount - 1);
			} while (SecondaryActivity == PrimaryActivity);
			
			// Bağımlı değerleri hesapla
			RecalculateStats();
		}
		
		// Değerleri yeniden hesapla
		public void RecalculateStats()
		{
			// Gizlilik seviyesi - fark edilmeme yeteneği
			DiscretionLevel = 1.0f + (GetAttributeValue("Gizlilik") / 10f) * 4.0f; // 1.0-5.0 arası
			
			// Kâr marjı - yasadışı faaliyetlerden elde edilen kârı etkiler
			float persuasionBonus = HasAttribute("İkna") ? GetAttributeValue("İkna") / 10f : 0f;
			ProfitMargin = 1.0f + (persuasionBonus + (Level * 0.1f)) * 2.0f; // 1.0-3.0 arası
			
			// Risk azaltma - denetimden kaçma, müdahale yetenekleri
			float awarenessBonus = GetAttributeValue("Uyanıklık") / 10f;
			RiskReduction = 0.2f + (awarenessBonus * 0.3f) + (Level * 0.05f); // 0.2-0.8 arası
			RiskReduction = Mathf.Clamp(RiskReduction, 0.2f, 0.8f);
			
			// Bağlantılar - önemli kişilere erişim, bilgi toplama
			ConnectionsLevel = 0.2f + (Level * 0.1f);
			if (HasAttribute("Karizma")) ConnectionsLevel += GetAttributeValue("Karizma") / 20f;
			ConnectionsLevel = Mathf.Clamp(ConnectionsLevel, 0.2f, 0.9f);
			
			// Ekipman bonusları
			if (HasFakeID) DiscretionLevel *= 1.2f;
			if (HasCustomSecuritySystem) RiskReduction += 0.1f;
			if (HasVIPContactList) ConnectionsLevel += 0.15f;
		}
		
		// Kaçak kat faaliyeti yürütme
		public float RunIllegalOperation(ActivityType activity, float basePotential, float riskLevel)
		{
			// Aktivite uzmanlık alanında mı kontrol et
			bool isPrimaryActivity = (activity == PrimaryActivity);
			bool isSecondaryActivity = (activity == SecondaryActivity);
			
			// Başarı şansı hesaplama
			float successChance = 0.6f; // Temel başarı şansı
			
			// Uzmanlık alanı bonus verir
			if (isPrimaryActivity) 
				successChance += 0.2f;
			else if (isSecondaryActivity)
				successChance += 0.1f;
			
			// Sadakat önemli - sadakat düşükse başarısız olma riski
			if (Loyalty < 50f)
			{
				successChance -= (50f - Loyalty) / 100f; // Sadakat eksikliği başarı şansını düşürür
			}
			
			// Risk seviyesi başarı şansını etkiler (1.0=düşük risk, 3.0=yüksek risk)
			float adjustedRiskLevel = riskLevel * (1f - RiskReduction);
			successChance -= (adjustedRiskLevel - 1.0f) * 0.1f;
			
			// Şansı sınırla
			successChance = Mathf.Clamp(successChance, 0.2f, 0.95f);
			
			// Başarı kontrolü
			bool success = GD.Randf() < successChance;
			
			// Tespit edilme riski
			float detectionChance = DetectionRisk * adjustedRiskLevel;
			bool detected = GD.Randf() < detectionChance;
			
			// Sonuçları hesapla
			float profit = 0f;
			
			if (success)
			{
				// Başarılı olursa kâr elde edilir
				float activityBonus = 1.0f;
				if (isPrimaryActivity) activityBonus = 1.3f;
				else if (isSecondaryActivity) activityBonus = 1.15f;
				
				profit = basePotential * ProfitMargin * activityBonus;
				
				// İstatistikleri güncelle
				TotalProfitGenerated += profit;
				OperationsCompleted++;
				
				GD.Print($"💰 {FullName} kaçak katta başarılı bir {activity} operasyonu yürüttü! Kazanç: {profit:F0}₺");
				
				// Başarılı operasyonlar sadakati artırır
				IncreaseLoyalty(GD.RandRange(0.5f, 1.5f));
			}
			else
			{
				GD.Print($"❌ {FullName} {activity} operasyonunu başarısız oldu. Kazanç yok.");
				
				// Başarısızlık sadakati azaltabilir
				ReduceLoyalty(GD.RandRange(0.2f, 0.8f));
			}
			
			// Tespit edilme durumu
			if (detected)
			{
				// Tehlikeli bir şekilde tespit edilme durumu
				GD.Print($"⚠️ DİKKAT: {FullName} {activity} operasyonu sırasında neredeyse yakalanıyordu!");
				
				// Korku ve sadakat kaybı
				ReduceLoyalty(GD.RandRange(3.0f, 10.0f));
				
				// Risk azaltılabilirse
				bool avoidedTrouble = GD.Randf() < RiskReduction;
				if (avoidedTrouble)
				{
					CloseCallsAvoided++;
					GD.Print($"😅 {FullName} durumu ustaca kontrol altına aldı!");
					
					// Tehlikeden kurtulmak sadakati biraz düzeltir
					IncreaseLoyalty(GD.RandRange(1.0f, 3.0f));
				}
				else
				{
					// Tespit sonrası potansiyel sonuçlar (ceza, rüşvet, vs.)
					profit = 0; // Kâr kaybı
					GD.Print($"🚨 {FullName} yakalanma tehlikesi atlattı, ama bu operasyondan kazanç elde edilemedi!");
					
					// Ekstra sadakat kaybı
					ReduceLoyalty(GD.RandRange(2.0f, 5.0f));
				}
			}
			
			return profit;
		}
		
		// Şantaj materyali toplama
		public bool CollectBlackmailMaterial(object target, float difficulty = 1.0f)
		{
			// Hedef null ise işlem yapılmaz
			if (target == null) return false;
			
			// Zorluk seviyesi başarı şansını etkiler (1.0=kolay hedef, 3.0=çok zor hedef)
			float successChance = (DiscretionLevel / 5f) / difficulty;
			
			// Bağlantılar başarı şansını artırır
			successChance += ConnectionsLevel * 0.3f;
			
			// Şansı sınırla
			successChance = Mathf.Clamp(successChance, 0.1f, 0.9f);
			
			// Başarı kontrolü
			bool success = GD.Randf() < successChance;
			
			if (success)
			{
				GD.Print($"🔍 {FullName} önemli bir kişi hakkında kullanılabilir bilgi topladı!");
				
				// Blackmail başarısı sadakati artırır
				IncreaseLoyalty(GD.RandRange(1.0f, 2.0f));
			}
			else
			{
				GD.Print($"❌ {FullName} hedef hakkında kullanılabilir bilgi toplayamadı.");
				
				// Başarısızlık hafif sadakat düşüşü yaratabilir
				ReduceLoyalty(GD.RandRange(0.1f, 0.5f));
			}
			
			return success;
		}
		
		// VIP müşteriyi ağırlama (özel illegal servisler)
		public float HandleVipClient(object vipClient, float spendingPotential)
		{
			if (vipClient == null) return 0f;
			
			// VIP müşterinin harcama potansiyelini hesapla
			float actualSpending = spendingPotential;
			
			// İkna yeteneği yüksekse daha fazla satış yapabilir
			if (HasAttribute("İkna"))
			{
				float persuasionFactor = 1.0f + (GetAttributeValue("İkna") / 10f);
				actualSpending *= persuasionFactor;
			}
			
			// VIP listesi varsa ekstra bonus
			if (HasVIPContactList)
			{
				actualSpending *= 1.2f;
			}
			
			// Kâr marjı uygulanır
			float profit = actualSpending * ProfitMargin;
			
			// İstatistikleri güncelle
			TotalProfitGenerated += profit;
			
			GD.Print($"💎 {FullName} bir VIP müşteriyi kaçak katta ağırladı! Kazanç: {profit:F0}₺");
			
			// VIP müşterilerle ilgilenmek sadakati artırır
			IncreaseLoyalty(GD.RandRange(0.5f, 1.5f));
			
			return profit;
		}
		
		// Mafya bağlantılarını kullanma
		public bool UseMafiaConnections(string purpose, float riskLevel = 1.0f)
		{
			// Risk seviyesi başarı şansını etkiler (1.0=düşük risk, 3.0=yüksek risk)
			float successChance = ConnectionsLevel - (riskLevel * 0.1f);
			
			// Sadakat önemli
			if (Loyalty < 70f)
			{
				successChance -= (70f - Loyalty) / 100f; // Sadakat eksikliği başarı şansını düşürür
			}
			
			// Şansı sınırla
			successChance = Mathf.Clamp(successChance, 0.1f, 0.8f);
			
			// Başarı kontrolü
			bool success = GD.Randf() < successChance;
			
			if (success)
			{
				GD.Print($"🤝 {FullName} mafya bağlantılarını kullanarak '{purpose}' amacına ulaştı!");
				
				// Başarılı mafya etkileşimi sadakati artırır
				IncreaseLoyalty(GD.RandRange(1.0f, 3.0f));
			}
			else
			{
				GD.Print($"❌ {FullName} mafya bağlantılarını kullanamadı: {purpose}");
				
				// Başarısızlık sadakat düşüşü yaratabilir
				ReduceLoyalty(GD.RandRange(0.5f, 2.0f));
			}
			
			return success;
		}
		
		// Polis veya devlet memuruna rüşvet verme
		public bool BribeOfficial(string officialType, float amount)
		{
			if (amount <= 0f) return false;
			
			// Rüşvet miktarı başarı şansını etkiler
			float successChance = Mathf.Min(0.8f, amount / 1000f); // Maksimum %80 şans
			
			// Bağlantılar şansı artırır
			successChance += ConnectionsLevel * 0.2f;
			
			// İkna yeteneği varsa bonus
			if (HasAttribute("İkna"))
			{
				successChance += GetAttributeValue("İkna") / 20f;
			}
			
			// Şansı sınırla
			successChance = Mathf.Clamp(successChance, 0.2f, 0.95f);
			
			// Başarı kontrolü
			bool success = GD.Randf() < successChance;
			
			if (success)
			{
				GD.Print($"💰 {FullName} bir {officialType}'ye başarıyla rüşvet verdi ({amount:F0}₺)");
				
				// Başarılı rüşvet sadakati artırır
				IncreaseLoyalty(GD.RandRange(0.5f, 1.5f));
			}
			else
			{
				GD.Print($"⚠️ {FullName}'nin {officialType}'ye rüşvet verme girişimi başarısız oldu! Dikkatli olun!");
				
				// Başarısız rüşvet girişimi tehlikelidir - sadakat çok düşer
				ReduceLoyalty(GD.RandRange(5.0f, 15.0f));
			}
			
			return success;
		}
		
		// Görev atama işlevini override et
		public override bool AssignTask(StaffTask task)
		{
			if (task.Type == "YasadışıFaaliyet" || task.Type == "MafyaBağlantısı")
			{
				// Kaçak kat personeli bu görevlerde çok etkili
				return base.AssignTask(task);
			}
			else if (task.Type == "MüşteriGözlemleme")
			{
				// Bilgi toplama aktivitesinde iyilerdir
				return base.AssignTask(task);
			}
			
			// Diğer görevler için uygun değil
			return false;
		}
		
		// Görev performans hesaplamasını override et
		protected override float CalculateTaskPerformance(StaffTask task)
		{
			float basePerformance = base.CalculateTaskPerformance(task);
			
			// Kaçak kat personeline özel performans artışları
			if (task.Type == "YasadışıFaaliyet")
			{
				// Yasadışı faaliyetlerde ekstra performans
				basePerformance *= 1.5f;
				
				// Ekipman bonusları
				if (HasFakeID) basePerformance *= 1.1f;
				if (HasCustomSecuritySystem) basePerformance *= 1.1f;
			}
			else if (task.Type == "MafyaBağlantısı")
			{
				// Mafya bağlantılarında ekstra performans
				basePerformance *= 1.3f;
				
				// VIP bağlantıları varsa bonus
				if (HasVIPContactList) basePerformance *= 1.2f;
			}
			else if (task.Type == "MüşteriGözlemleme")
			{
				// Müşteri gözlemlemede iyi performans
				basePerformance *= 1.2f;
			}
			
			return Mathf.Clamp(basePerformance, 0f, 10f);
		}
		
		// Özel yetenekleri override et
		public override string[] GetSpecialCapabilities()
		{
			List<string> capabilities = new List<string>();
			
			// Temel kaçak kat personeli yetenekleri
			capabilities.Add("Gizli Operasyon");
			capabilities.Add($"{PrimaryActivity} Uzmanlığı");
			capabilities.Add($"{SecondaryActivity} Alt Uzmanlığı");
			
			// Seviyeye bağlı yetenekler
			if (Level >= 2) capabilities.Add("Bağlantı Ağı");
			if (Level >= 3) capabilities.Add("Risk Yönetimi");
			if (Level >= 4) capabilities.Add("VIP Müşteri Kazanma");
			if (Level >= 5) capabilities.Add("Mafya Arabuluculuğu");
			
			// Özel yetenekler
			if (DiscretionLevel >= 4.0f) capabilities.Add("Hayalet Operatör");
			if (ProfitMargin >= 2.5f) capabilities.Add("Yüksek Kâr Marjı");
			if (ConnectionsLevel >= 0.7f) capabilities.Add("Geniş Bağlantı Ağı");
			
			return capabilities.ToArray();
		}
		
		// Clone metodunu override et
		public override StaffMember Clone()
		{
			IllegalFloorStaff clone = new IllegalFloorStaff
			{
				FullName = this.FullName,
				JobTitle = this.JobTitle,
				Level = this.Level,
				Salary = this.Salary,
				Loyalty = this.Loyalty,
				PrimaryActivity = this.PrimaryActivity,
				SecondaryActivity = this.SecondaryActivity,
				DiscretionLevel = this.DiscretionLevel,
				ProfitMargin = this.ProfitMargin,
				RiskReduction = this.RiskReduction,
				ConnectionsLevel = this.ConnectionsLevel,
				HasFakeID = this.HasFakeID,
				HasCustomSecuritySystem = this.HasCustomSecuritySystem,
				HasVIPContactList = this.HasVIPContactList
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
			
			// Kaçak kat personeline özel bilgiler
			status += $"\nAna Faaliyet: {PrimaryActivity}\n";
			status += $"Alt Faaliyet: {SecondaryActivity}\n";
			status += $"Gizlilik: {DiscretionLevel:F1}/5\n";
			status += $"Kâr Marjı: {ProfitMargin:F1}x\n";
			status += $"Risk Azaltma: %{RiskReduction * 100:F0}\n";
			status += $"Bağlantılar: %{ConnectionsLevel * 100:F0}\n";
			status += $"Tespit Riski: %{DetectionRisk * 100:F0}\n";
			
			// Ekipman bilgileri
			List<string> equipment = new List<string>();
			if (HasFakeID) equipment.Add("Sahte Kimlik");
			if (HasCustomSecuritySystem) equipment.Add("Özel Güvenlik Sistemi");
			if (HasVIPContactList) equipment.Add("VIP Bağlantı Listesi");
			
			if (equipment.Count > 0)
			{
				status += $"Ekipman: {string.Join(", ", equipment)}\n";
			}
			
			return status;
		}
		
		// Yasadışı faaliyet riski (Override - bu personel tipi için daha farklı davranır)
		public override float GetIllegalActivityRisk()
		{
			// Kaçak kat personeli, yasadışı faaliyetler için özel olarak işe alınmıştır
			// ve bu tür faaliyetlerde risk seviyesi sadakatle ters orantılıdır
			
			// Sadakat düşükse, ihanet riski yüksek
			if (Loyalty < 40f)
			{
				return 9f; // Çok yüksek risk (0-10 skalasında)
			}
			else if (Loyalty < 60f)
			{
				return 7f; // Yüksek risk
			}
			else if (Loyalty < 80f)
			{
				return 4f; // Orta risk
			}
			else
			{
				return 2f; // Düşük risk
			}
		}
		
		// Aktivite adını döndür
		public string GetActivityName(ActivityType activity)
		{
			return Enum.GetName(typeof(ActivityType), activity).Replace("_", " ");
		}
	}
}
