// src/Staff/StaffTypes/SecurityStaff.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Staff
{
	public class SecurityStaff : StaffMember
	{
		// Güvenlik personeline özel özellikler
		public float ThreatLevel { get; set; } = 1.0f;
		public float DetectionChance { get; set; } = 0.3f;
		public float FightingAbility { get; set; } = 1.0f;
		public int IncidentsResolved { get; private set; } = 0;
		public int FightsWon { get; private set; } = 0;
		public int ProblemCustomersRemoved { get; private set; } = 0;
		
		// Bölge kontrolü
		public Vector2 PatrolArea { get; set; } = Vector2.Zero;
		public float PatrolRadius { get; set; } = 50f;
		
		// Özel ekipman (upgradeable)
		public bool HasRadio { get; set; } = false;
		public bool HasTaser { get; set; } = false;
		public bool HasBodyArmor { get; set; } = false;
		
		// Constructor
		public SecurityStaff() : base()
		{
			// İş pozisyonunu ayarla
			JobTitle = "Güvenlik";
			
			// Güvenlik personeline özel özellikleri başlat
			if (!HasAttribute("Güç")) SetAttributeValue("Güç", 5f);
			if (!HasAttribute("Tehdit")) SetAttributeValue("Tehdit", 4f);
			if (!HasAttribute("Uyanıklık")) SetAttributeValue("Uyanıklık", 3f);
			if (!HasAttribute("Dikkat")) SetAttributeValue("Dikkat", 3f);
			
			// Bağımlı özellikleri hesapla
			RecalculateStats();
		}
		
		// Değerleri yeniden hesapla
		public void RecalculateStats()
		{
			// Tehdit seviyesi - müşterilerin kavga çıkarma veya sorun yaratma olasılığını düşürür
			ThreatLevel = 1.0f + (GetAttributeValue("Tehdit") / 10f) * 2.0f; // 1.0-3.0 arası
			
			// Tespit şansı - sorunlu durumları fark etme olasılığı
			DetectionChance = 0.3f + (GetAttributeValue("Uyanıklık") / 10f) * 0.5f; // 0.3-0.8 arası
			
			// Dövüş yeteneği - kavga kazanma şansını etkiler
			FightingAbility = 1.0f + (GetAttributeValue("Güç") / 10f) * 2.0f; // 1.0-3.0 arası
			
			// Ekipman bonusları
			if (HasRadio) DetectionChance += 0.1f;
			if (HasTaser) FightingAbility += 0.5f;
			if (HasBodyArmor) FightingAbility += 0.3f;
		}
		
		// Devriye gezme - olası sorunları tespit etme şansı
		public bool DetectIssue(float difficultyLevel = 1.0f)
		{
			// Zorluk seviyesi tespit şansını etkiler (1.0=normal, 2.0=zor tespit edilen)
			float chance = DetectionChance / difficultyLevel;
			
			// Dikkat özelliği de katkıda bulunur
			chance += GetAttributeValue("Dikkat") / 20f; // +0.05 to +0.5 bonus
			
			// Tespit şansını kontrol et
			bool detected = GD.Randf() < chance;
			
			if (detected)
			{
				GD.Print($"{FullName} bir sorun tespit etti!");
			}
			
			return detected;
		}
		
		// Sorunla başa çıkma (kavga, hırsızlık, vs.)
		public bool ResolveIncident(float incidentSeverity = 1.0f)
		{
			// Olay şiddeti başarı şansını etkiler (1.0=normal, 3.0=çok ciddi)
			float successChance = FightingAbility / incidentSeverity;
			
			// Sadakat da başarı şansını etkiler
			successChance += Loyalty / 200f; // +0.0 to +0.5 bonus
			
			// Maksimum %95 başarı şansı
			successChance = Mathf.Min(successChance, 0.95f);
			
			// Başarı kontrolü
			bool success = GD.Randf() < successChance;
			
			if (success)
			{
				IncidentsResolved++;
				GD.Print($"{FullName} olayı başarıyla çözdü!");
				
				// Başarılı olaylar sadakat ve deneyim kazandırır
				IncreaseLoyalty(incidentSeverity * GD.RandRange(0.5f, 1.5f));
			}
			else
			{
				GD.Print($"{FullName} olayı çözemedi veya yardım istemek zorunda kaldı!");
				
				// Başarısızlık sadakati hafif düşürebilir
				ReduceLoyalty(GD.RandRange(0.2f, 0.8f));
			}
			
			return success;
		}
		
		// Kavga - fiziksel müdahale gerektiren durumlar
		public bool HandleFight(float opponentStrength = 1.0f)
		{
			// Rakip gücü kazanma şansını etkiler (1.0=zayıf rakip, 3.0=çok güçlü rakip)
			float winChance = FightingAbility / opponentStrength;
			
			// Güvenlik personeli birden fazla rakibe karşı dezavantajlıdır
			float opponentCount = opponentStrength / 1.0f; // opponentStrength'i yaklaşık rakip sayısı olarak yorumla
			if (opponentCount > 1)
			{
				winChance /= Mathf.Sqrt(opponentCount); // Karekök fonksiyonu - dezavantaj artar ama 2 kat rakip 2 kat dezavantaj değil
			}
			
			// En fazla %90 kazanma şansı
			winChance = Mathf.Min(winChance, 0.9f);
			
			// Sonucu kontrol et
			bool won = GD.Randf() < winChance;
			
			if (won)
			{
				FightsWon++;
				GD.Print($"{FullName} kavgayı kazandı ve durumu kontrol altına aldı!");
				
				// Kazanılan kavgalar sadakat ve saygınlık kazandırır
				IncreaseLoyalty(opponentStrength * GD.RandRange(1.0f, 2.0f));
			}
			else
			{
				GD.Print($"{FullName} kavgada zorlandı ve yardım istemek zorunda kaldı!");
				
				// Kaybedilen kavgalar yaralanmaya, sadakat kaybına neden olabilir
				ReduceLoyalty(GD.RandRange(1.0f, 2.0f));
			}
			
			return won;
		}
		
		// Sorunlu müşteriyi uzaklaştırma
		public bool RemoveProblemCustomer(object customer, float difficulty = 1.0f)
		{
			if (customer == null) return false;
			
			// Zorluk, müşterinin direniş seviyesini temsil eder
			float successChance = ThreatLevel / difficulty;
			
			// İkna yeteneği varsa kullan
			if (HasAttribute("İkna"))
			{
				successChance += GetAttributeValue("İkna") / 20f;
			}
			
			// Sonucu kontrol et
			bool success = GD.Randf() < successChance;
			
			if (success)
			{
				ProblemCustomersRemoved++;
				GD.Print($"{FullName} sorunlu müşteriyi başarıyla uzaklaştırdı.");
			}
			else
			{
				GD.Print($"{FullName} müşteriyi uzaklaştırmada zorlandı, durum büyüyebilir!");
			}
			
			return success;
		}
		
		// Görev atama işlevini override et
		public override bool AssignTask(StaffTask task)
		{
			if (task.Type == "GüvenlikSağlama" || task.Type == "SorunÇözme")
			{
				// Güvenlik personeli bu görevlerde daha etkili
				return base.AssignTask(task);
			}
			else if (task.Type == "MüşteriGözlemleme")
			{
				// Güvenlik gözlem yapabilir
				return base.AssignTask(task);
			}
			
			// Diğer görevler için uygun değil
			return false;
		}
		
		// Görev performans hesaplamasını override et
		protected override float CalculateTaskPerformance(StaffTask task)
		{
			float basePerformance = base.CalculateTaskPerformance(task);
			
			// Güvenlik personeline özel performans artışları
			if (task.Type == "GüvenlikSağlama")
			{
				// Güvenlik görevinde ekstra performans
				basePerformance *= 1.2f;
				
				// Ekipman bonusları
				if (HasRadio) basePerformance *= 1.1f;
				if (HasTaser || HasBodyArmor) basePerformance *= 1.1f;
			}
			else if (task.Type == "SorunÇözme")
			{
				// Sorun çözmede ekstra performans
				basePerformance *= 1.3f;
			}
			
			return Mathf.Clamp(basePerformance, 0f, 10f);
		}
		
		// Özel yetenekleri override et
		public override string[] GetSpecialCapabilities()
		{
			List<string> capabilities = new List<string>();
			
			// Temel güvenlik yetenekleri
			capabilities.Add("Devriye Gezme");
			capabilities.Add("Kavga Önleme");
			
			// Seviyeye bağlı yetenekler
			if (Level >= 2) capabilities.Add("Tehdit Tespit Etme");
			if (Level >= 3) capabilities.Add("Kriz Yönetimi");
			if (Level >= 4) capabilities.Add("VIP Koruma");
			if (Level >= 5) capabilities.Add("Gelişmiş Güvenlik Protokolleri");
			
			// Ekipman bazlı yetenekler
			if (HasRadio) capabilities.Add("Hızlı İletişim");
			if (HasTaser) capabilities.Add("Etkisiz Hale Getirme");
			if (HasBodyArmor) capabilities.Add("Gelişmiş Koruma");
			
			return capabilities.ToArray();
		}
		
		// Clone metodunu override et
		public override StaffMember Clone()
		{
			SecurityStaff clone = new SecurityStaff
			{
				FullName = this.FullName,
				JobTitle = this.JobTitle,
				Level = this.Level,
				Salary = this.Salary,
				Loyalty = this.Loyalty,
				ThreatLevel = this.ThreatLevel,
				DetectionChance = this.DetectionChance,
				FightingAbility = this.FightingAbility,
				PatrolArea = this.PatrolArea,
				PatrolRadius = this.PatrolRadius,
				HasRadio = this.HasRadio,
				HasTaser = this.HasTaser,
				HasBodyArmor = this.HasBodyArmor
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
			
			// Güvenlik personeline özel bilgiler
			status += $"\nTehdit Seviyesi: {ThreatLevel:F1}\n";
			status += $"Tespit Şansı: %{DetectionChance * 100:F0}\n";
			status += $"Dövüş Yeteneği: {FightingAbility:F1}\n";
			status += $"Çözülen Olaylar: {IncidentsResolved}\n";
			
			// Ekipman bilgileri
			List<string> equipment = new List<string>();
			if (HasRadio) equipment.Add("Telsiz");
			if (HasTaser) equipment.Add("Şok Cihazı");
			if (HasBodyArmor) equipment.Add("Koruyucu Yelek");
			
			if (equipment.Count > 0)
			{
				status += $"Ekipman: {string.Join(", ", equipment)}\n";
			}
			
			return status;
		}
		
		// Yasadışı faaliyet riski
		public override float GetIllegalActivityRisk()
		{
			// Güvenlik personeli doğası gereği yasadışı faaliyetleri ihbar etme riski taşır
			// Sadakat düşük olsa bile temel bir risk vardır
			float baseRisk = base.GetIllegalActivityRisk();
			
			// Sadakat yüksek olsa bile minimum risk seviyesi
			return Mathf.Max(baseRisk, 3f); // En az 3/10 risk
		}
	}
}
