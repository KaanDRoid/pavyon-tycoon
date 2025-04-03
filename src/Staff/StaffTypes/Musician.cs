// src/Staff/StaffTypes/Musician.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Staff
{
	public class Musician : StaffMember
	{
		// Enstrüman ve tür bilgisi
		public enum InstrumentType { 
			Kanun, 
			Ud, 
			Keman, 
			Klarnet, 
			Darbuka, 
			Vokal, 
			Bağlama, 
			Tambur, 
			Zurna, 
			Davul 
		}
		
		public InstrumentType Instrument { get; set; } = InstrumentType.Vokal;
		
		// Müzik türü uzmanlığı
		public enum MusicGenre { 
			Klasik, 
			HalkMüziği, 
			Fantezi, 
			Arabesk, 
			Taverna, 
			Pop
		}
		
		public MusicGenre Specialty { get; set; } = MusicGenre.Taverna;
		
		// Müzisyene özel özellikler
		public float PerformanceQuality { get; set; } = 1.0f;
		public float CrowdExcitementFactor { get; set; } = 1.0f;
		public float StaminaLevel { get; set; } = 100f; // 0-100 arası, performans süresini etkiler
		public float RequestKnowledge { get; set; } = 0.5f; // 0.0-1.0 arası, istek parçaları bilme oranı
		
		// Performans istatistikleri
		public int PerformancesCompleted { get; private set; } = 0;
		public int SongRequestsHandled { get; private set; } = 0;
		public int StandingOvations { get; private set; } = 0;
		
		// Repertuar büyüklüğü
		public int RepertoireSize => 20 + (Level * 10); // Her seviye için 10 şarkı daha
		
		// Özel ekipman
		public bool HasOwnInstrument { get; set; } = true; // Kendi enstrümanı daha kaliteli sonuç verir
		public bool HasWirelessMic { get; set; } = false; // Sahnede daha rahat hareket etmesini sağlar
		public bool HasCustomOutfit { get; set; } = false; // Görselliği artırır
		
		// Constructor
		public Musician() : base()
		{
			// İş pozisyonunu ayarla
			JobTitle = "Müzisyen";
			
			// Müzisyene özel özellikleri başlat
			if (!HasAttribute("Müzik")) SetAttributeValue("Müzik", 5f);
			if (!HasAttribute("Performans")) SetAttributeValue("Performans", 4f);
			if (!HasAttribute("Karizma")) SetAttributeValue("Karizma", 3f);
			if (!HasAttribute("Dayanıklılık")) SetAttributeValue("Dayanıklılık", 3f);
			
			// Rastgele bir enstrüman ve tür seç
			int instrumentCount = Enum.GetValues(typeof(InstrumentType)).Length;
			Instrument = (InstrumentType)GD.RandRange(0, instrumentCount - 1);
			
			int genreCount = Enum.GetValues(typeof(MusicGenre)).Length;
			Specialty = (MusicGenre)GD.RandRange(0, genreCount - 1);
			
			// Bağımlı değerleri hesapla
			RecalculateStats();
		}
		
		// Değerleri yeniden hesapla
		public void RecalculateStats()
		{
			// Performans kalitesi - temel müzik yeteneğini yansıtır
			PerformanceQuality = 1.0f + (GetAttributeValue("Müzik") / 10f) * 2.0f; // 1.0-3.0 arası
			
			// Kalabalığı coşturma faktörü - performans ve karizma etkiler
			float performanceBonus = GetAttributeValue("Performans") / 10f;
			float charismaBonus = HasAttribute("Karizma") ? GetAttributeValue("Karizma") / 10f : 0f;
			CrowdExcitementFactor = 1.0f + (performanceBonus + charismaBonus) * 1.5f; // 1.0-2.5 arası
			
			// Dayanıklılık - uzun performanslar için
			float staminaBonus = HasAttribute("Dayanıklılık") ? GetAttributeValue("Dayanıklılık") / 10f : 0f;
			StaminaLevel = 70f + (staminaBonus * 30f); // 70-100 arası
			
			// İstek şarkıları bilme oranı - seviye ve repertuar etkiler
			RequestKnowledge = 0.5f + (Level * 0.1f); // Her seviye %10 daha fazla şarkı bilir
			RequestKnowledge = Mathf.Clamp(RequestKnowledge, 0.5f, 0.9f); // %50-%90 arası
			
			// Ekipman bonusları
			if (HasOwnInstrument) PerformanceQuality *= 1.2f;
			if (HasWirelessMic) CrowdExcitementFactor *= 1.15f;
			if (HasCustomOutfit) CrowdExcitementFactor *= 1.1f;
		}
		
		// Performans gösterisi yapma
		public float PerformShow(int audienceSize, int duration)
		{
			if (audienceSize <= 0 || duration <= 0) return 0f;
			
			// Performans kalitesi hesaplama
			float qualityScore = PerformanceQuality;
			
			// Dayanıklılık etkisi - uzun gösteriler yorar
			float staminaFactor = 1.0f;
			if (duration > 30) // 30 dakikadan uzun gösteriler
			{
				staminaFactor = StaminaLevel / 100f;
				qualityScore *= staminaFactor;
				
				// Dayanıklılığı azalt (sonraki performansları etkiler)
				StaminaLevel = Mathf.Max(10f, StaminaLevel - (duration / 10f));
				GD.Print($"{FullName} yoruldu! Dayanıklılık: {StaminaLevel:F0}/100");
			}
			
			// Kalabalık etkisi hesaplama
			float crowdFactor = CrowdExcitementFactor;
			
			// Toplam müşteri memnuniyeti ve harcama artışı
			float satisfactionBonus = qualityScore * crowdFactor / 10f; // 0.0-1.0 arası
			
			// Ayakta alkış olasılığı
			float ovationChance = satisfactionBonus * 0.5f; // Maksimum %50 şans
			if (GD.Randf() < ovationChance)
			{
				StandingOvations++;
				satisfactionBonus *= 1.5f; // Ekstra bonus
				GD.Print($"🌟 {FullName} ayakta alkışlandı!");
				
				// Sadakati artır
				IncreaseLoyalty(GD.RandRange(1.0f, 3.0f));
			}
			
			// İstatistikleri güncelle
			PerformancesCompleted++;
			
			// Başarılı performans sadakati artırır
			IncreaseLoyalty(satisfactionBonus * GD.RandRange(0.5f, 1.0f));
			
			GD.Print($"{FullName} performansı tamamladı! Kalite: {qualityScore:F1}/5, Coşku: {satisfactionBonus:F2}");
			
			// Performans sonucu (müşteri memnuniyeti ve harcama artışı) döndür
			return satisfactionBonus;
		}
		
		// İstek parça çalma
		public bool PlayRequest(string songRequest, MusicGenre requestGenre)
		{
			// İstek parçayı bilme olasılığı
			float requestChance = RequestKnowledge;
			
			// Uzmanlık alanındaki parçaları daha iyi bilir
			if (requestGenre == Specialty)
			{
				requestChance += 0.2f; // +%20 şans
				requestChance = Mathf.Min(requestChance, 0.99f); // Maksimum %99
			}
			
			// Sonucu kontrol et
			bool canPlay = GD.Randf() < requestChance;
			
			if (canPlay)
			{
				SongRequestsHandled++;
				GD.Print($"{FullName} istek parçayı çalabildi: \"{songRequest}\"");
				
				// İstek parçaları çalabilmek sadakati artırır
				IncreaseLoyalty(GD.RandRange(0.2f, 0.5f));
			}
			else
			{
				GD.Print($"{FullName} istek parçayı çalamadı: \"{songRequest}\"");
				
				// Başarısızlık hafif sadakat düşüşü yaratabilir
				ReduceLoyalty(GD.RandRange(0.1f, 0.3f));
			}
			
			return canPlay;
		}
		
		// Parçayı transpozisyon yapabilme (farklı tonda çalabilme)
		public bool TransposeMusic(float difficulty = 1.0f)
		{
			// Zorluk seviyesi başarı şansını etkiler (1.0=kolay, 3.0=zor)
			float successChance = (GetAttributeValue("Müzik") / 10f) / difficulty;
			successChance = Mathf.Clamp(successChance, 0.3f, 0.9f);
			
			return GD.Randf() < successChance;
		}
		
		// Dayanıklılığı yenileme (dinlenme)
		public void Rest(int minutes)
		{
			float recoveryRate = 0.5f; // Dakika başına %0.5 yenilenme
			float recovery = minutes * recoveryRate;
			
			// Dayanıklılık özelliği iyileşme hızını etkiler
			if (HasAttribute("Dayanıklılık"))
			{
				recovery *= 1.0f + (GetAttributeValue("Dayanıklılık") / 10f);
			}
			
			// Dayanıklılığı artır (maksimum 100)
			StaminaLevel = Mathf.Min(100f, StaminaLevel + recovery);
			GD.Print($"{FullName} dinlendi. Dayanıklılık: {StaminaLevel:F0}/100");
		}
		
		// Görev atama işlevini override et
		public override bool AssignTask(StaffTask task)
		{
			if (task.Type == "MüzikPerformansı")
			{
				// Müzisyenler bu görevde çok etkili
				return base.AssignTask(task);
			}
			else if (task.Type == "MüşteriEğlendirme")
			{
				// Müzisyenler müşteri eğlendirebilir
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
			
			// Müzisyene özel performans artışları
			if (task.Type == "MüzikPerformansı")
			{
				// Müzik performansında ekstra performans
				basePerformance *= 1.5f;
				
				// Dayanıklılık faktörü
				basePerformance *= (StaminaLevel / 100f);
				
				// Ekipman bonusları
				if (HasOwnInstrument) basePerformance *= 1.2f;
				if (HasWirelessMic || HasCustomOutfit) basePerformance *= 1.1f;
			}
			else if (task.Type == "MüşteriEğlendirme")
			{
				// Karizma yüksekse müşteri eğlendirmede bonus
				if (HasAttribute("Karizma") && GetAttributeValue("Karizma") >= 5f)
				{
					basePerformance *= 1.2f;
				}
			}
			
			return Mathf.Clamp(basePerformance, 0f, 10f);
		}
		
		// Özel yetenekleri override et
		public override string[] GetSpecialCapabilities()
		{
			List<string> capabilities = new List<string>();
			
			// Temel müzisyen yetenekleri
			capabilities.Add("Müzik Performansı");
			capabilities.Add($"{Instrument} Çalma");
			capabilities.Add($"{Specialty} Uzmanlığı");
			
			// Seviyeye bağlı yetenekler
			if (Level >= 2) capabilities.Add("Büyük Repertuar");
			if (Level >= 3) capabilities.Add("Doğaçlama Yeteneği");
			if (Level >= 4) capabilities.Add("Müzik Adaptasyonu");
			if (Level >= 5) capabilities.Add("Virtüöz Performans");
			
			// Özel yetenekler
			if (RepertoireSize >= 50) capabilities.Add("Geniş Repertuar");
			if (PerformanceQuality >= 2.5f) capabilities.Add("Üstün Performans");
			
			return capabilities.ToArray();
		}
		
		// Clone metodunu override et
		public override StaffMember Clone()
		{
			Musician clone = new Musician
			{
				FullName = this.FullName,
				JobTitle = this.JobTitle,
				Level = this.Level,
				Salary = this.Salary,
				Loyalty = this.Loyalty,
				Instrument = this.Instrument,
				Specialty = this.Specialty,
				PerformanceQuality = this.PerformanceQuality,
				CrowdExcitementFactor = this.CrowdExcitementFactor,
				StaminaLevel = this.StaminaLevel,
				RequestKnowledge = this.RequestKnowledge,
				HasOwnInstrument = this.HasOwnInstrument,
				HasWirelessMic = this.HasWirelessMic,
				HasCustomOutfit = this.HasCustomOutfit
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
			
			// Müzisyene özel bilgiler
			status += $"\nEnstrüman: {Instrument}\n";
			status += $"Uzmanlık: {Specialty}\n";
			status += $"Performans Kalitesi: {PerformanceQuality:F1}/5\n";
			status += $"Kalabalık Etkisi: {CrowdExcitementFactor:F1}x\n";
			status += $"Dayanıklılık: {StaminaLevel:F0}/100\n";
			status += $"Repertuar: {RepertoireSize} parça\n";
			
			// Ekipman bilgileri
			List<string> equipment = new List<string>();
			if (HasOwnInstrument) equipment.Add("Özel Enstrüman");
			if (HasWirelessMic) equipment.Add("Kablosuz Mikrofon");
			if (HasCustomOutfit) equipment.Add("Özel Sahne Kıyafeti");
			
			if (equipment.Count > 0)
			{
				status += $"Ekipman: {string.Join(", ", equipment)}\n";
			}
			
			return status;
		}
		
		// Enstrüman adını döndür
		public string GetInstrumentName()
		{
			return Enum.GetName(typeof(InstrumentType), Instrument);
		}
		
		// Tür adını döndür
		public string GetGenreName()
		{
			return Enum.GetName(typeof(MusicGenre), Specialty);
		}
	}
}
