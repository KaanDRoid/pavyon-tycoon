using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Staff
{
	
	public partial class Musician : StaffBase
	{
		// Müzisyen özellikleri
		private float _techniqueSkill = 0.5f;        // Teknik beceri
		private float _creativitySkill = 0.5f;       // Yaratıcılık becerisi
		private float _repertoireSize = 0.5f;        // Repertuar büyüklüğü
		private float _stagePresenceSkill = 0.5f;    // Sahne hakimiyeti becerisi
		
		// Türk pavyon müzik tarzları ve seviyeleri (0-1)
		public enum MusicStyle
		{
			Arabesk,        // Duygusal/Ağır
			FanteziPop,     // Romantik/Enerjik
			OyunHavalari,   // Eğlenceli/Dans
			Taverna,        // Nostaljik/Rahat
			ModernLounge    // Lüks/Şık
		}
		
		private Dictionary<MusicStyle, float> _musicStyleLevels = new Dictionary<MusicStyle, float>();
		private MusicStyle _currentStyle = MusicStyle.Taverna; // Varsayılan başlangıç stili
		private MusicStyle _specialtyStyle = MusicStyle.Taverna; // Uzmanlık stili
		
		// Müzik enstrümanı
		public enum InstrumentType
		{
			Keman,      // Arabesk ve taverna müziklerinde yaygın
			Darbuka,    // Oyun havalarında ve oryantal eşliğinde
			Klavye,     // Taverna, pop ve arabeskte çok yönlü
			Baglama,    // Yerel tınılar ve nostalji için
			Saksafon,   // Lounge ve taverna müziklerinde
			Gitar,      // Pop/fantezi müziklerde
			KanunUd     // Daha otantik, ağır parçalar için
		}
		
		private InstrumentType _instrumentType = InstrumentType.Klavye;
		private string _instrumentName = "Klavye";
		private float _instrumentQuality = 0.5f;  // Enstrüman kalitesi (0-1)
		
		// Stil-enstrüman uyumluluk matrisi
		private Dictionary<MusicStyle, Dictionary<InstrumentType, float>> _styleInstrumentCompatibility = 
			new Dictionary<MusicStyle, Dictionary<InstrumentType, float>>();
		
		// Performans değerleri
		private bool _isPerforming = false;       // Şu anda performans sergiliyor mu
		private bool _isSoloing = false;          // Solo performans yapıyor mu
		private int _performanceQuality = 0;      // Performans kalitesi (1-10)
		private int _consecutivePerformances = 0; // Ardışık performans sayısı
		private float _audienceRating = 0.0f;     // İzleyici değerlendirmesi (0-1)
		
		// Atmosfer etkileri katsayıları
		private float _customerMoodEffect = 0.0f;     // Müşteri ruh hali etkisi (-1 to 1)
		private float _drinkOrderEffect = 0.0f;       // İçki siparişi etkisi (-1 to 1)
		private float _stayTimeEffect = 0.0f;         // Kalma süresi etkisi (-1 to 1)
		private float _konsomatrisEffect = 0.0f;      // Konsomatris performans etkisi (-1 to 1)
		private float _tipProbabilityEffect = 0.0f;   // Bahşiş olasılığı etkisi (-1 to 1)
		
		// Müzisyen grubu
		private bool _isGroupLeader = false;      // Grup lideri mi
		private int _groupSize = 1;               // 1 = Solo, 2+ = Grup
		private List<Musician> _groupMembers = new List<Musician>(); // Grup üyeleri
		
		// İstatistikler
		private int _performancesCompleted = 0;   // Tamamlanan performans sayısı
		private int _solosPerformed = 0;          // Yapılan solo sayısı
		private float _totalAudienceRating = 0.0f;// Toplam izleyici değerlendirmesi
		private int _mistakesMade = 0;            // Yapılan hata sayısı
		
		// Zaman takibi
		private float _currentPerformanceTime = 0.0f; // Mevcut performans süresi
		private float _idealPerformanceLength = 300.0f; // İdeal performans uzunluğu (5 dakika)
		
		// Müşteri demografi etkileşimi
		private Dictionary<string, float> _customerTypePreferences = new Dictionary<string, float>();
		
		// Signals
		[Signal]
		public delegate void PerformanceStartedEventHandler(string styleName, int quality);
		
		[Signal]
		public delegate void PerformanceEndedEventHandler(float audienceRating, float duration);
		
		[Signal]
		public delegate void SoloStartedEventHandler(int quality);
		
		[Signal]
		public delegate void SoloEndedEventHandler(float audienceImpression);
		
		[Signal]
		public delegate void MistakeMadeEventHandler(float severity);
		
		[Signal]
		public delegate void AtmosphereChangedEventHandler(string styleName, float moodEffect, float drinkEffect);
		
		public override void _Ready()
		{
			base._Ready();
			
			// Tipi ayarla
			Type = StaffType.Musician;
			
			// Beceri değerlerini başlat (StaffBase'deki skills sözlüğünden al)
			if (_skills.ContainsKey("technique")) _techniqueSkill = _skills["technique"];
			if (_skills.ContainsKey("creativity")) _creativitySkill = _skills["creativity"];
			if (_skills.ContainsKey("repertoire")) _repertoireSize = _skills["repertoire"];
			if (_skills.ContainsKey("stagePrescence")) _stagePresenceSkill = _skills["stagePrescence"];
			
			// Müzik stillerini başlat
			InitializeMusicStyles();
			
			// Enstrüman-stil uyumluluğunu başlat
			InitializeStyleInstrumentCompatibility();
			
			// Müşteri tipi tercihlerini başlat
			InitializeCustomerPreferences();
			
			// Rastgele enstrüman tipi ata (veya sahne yükleme sırasında gerçek verilerden al)
			AssignRandomInstrument();
			
			// Trait'lere göre değerleri düzenle
			AdjustValuesByTraits();
			
			GD.Print($"Musician {Name} initialized with instrument: {_instrumentName}, specialty: {_specialtyStyle}");
		}
		
		public override void _Process(double delta)
		{
			base._Process(delta);
			
			// Performans ölçümü
			if (_isPerforming)
			{
				_currentPerformanceTime += (float)delta;
				
				// Düzenli aralıklarla hata ihtimali kontrolü
				if (GD.Randf() < 0.01f * (1.0f - _techniqueSkill))
				{
					MakeMistake();
				}
				
				// Enerji ve ruh haline göre performans
				if (_currentPerformanceTime > _idealPerformanceLength)
				{
					// Enerji düşükse hata ihtimali artar
					if (Energy < 0.3f && GD.Randf() < 0.2f)
					{
						MakeMistake();
					}
					
					// Uzun performans enerjiyi tüketir
					AdjustEnergy(-0.005f, "Long Performance");
				}
				
				// Periyodik olarak atmosfer etkilerini güncelle
				if (GD.Randf() < 0.01f)
				{
					UpdateAtmosphereEffects();
				}
			}
		}
		
		// Türk pavyon müzik stillerini başlat
		private void InitializeMusicStyles()
		{
			// Tüm stiller için başlangıç değerlerini ayarla
			foreach (MusicStyle style in Enum.GetValues(typeof(MusicStyle)))
			{
				// Rastgele başlangıç yetenek seviyesi (0.1-0.6 arası)
				_musicStyleLevels[style] = 0.1f + GD.Randf() * 0.5f;
			}
			
			// Uzmanlık stilini rastgele belirle
			Array styles = Enum.GetValues(typeof(MusicStyle));
			_specialtyStyle = (MusicStyle)styles.GetValue(GD.RandRange(0, styles.Length - 1));
			
			// Uzmanlık stilinde daha yüksek başlangıç değeri
			_musicStyleLevels[_specialtyStyle] = 0.5f + GD.Randf() * 0.3f;
		}
		
		// Enstrüman-stil uyumluluğunu başlat
		private void InitializeStyleInstrumentCompatibility()
		{
			// Her stil için enstrüman uyumluluk değerlerini ayarla
			
			// Arabesk
			_styleInstrumentCompatibility[MusicStyle.Arabesk] = new Dictionary<InstrumentType, float>
			{
				{ InstrumentType.Keman, 0.9f },    // Arabeskte çok etkili
				{ InstrumentType.Klavye, 0.8f },   // Yaygın kullanılır
				{ InstrumentType.Baglama, 0.7f },  // İyi uyumlu
				{ InstrumentType.Gitar, 0.6f },    // Orta uyumlu
				{ InstrumentType.KanunUd, 0.8f },  // Çok iyi uyumlu
				{ InstrumentType.Saksafon, 0.5f }, // Az uyumlu
				{ InstrumentType.Darbuka, 0.3f }   // Düşük uyum
			};
			
			// Fantezi Pop
			_styleInstrumentCompatibility[MusicStyle.FanteziPop] = new Dictionary<InstrumentType, float>
			{
				{ InstrumentType.Keman, 0.7f },    // İyi uyumlu
				{ InstrumentType.Klavye, 0.9f },   // Çok iyi uyumlu
				{ InstrumentType.Baglama, 0.5f },  // Orta uyumlu
				{ InstrumentType.Gitar, 0.8f },    // Çok iyi uyumlu
				{ InstrumentType.KanunUd, 0.6f },  // Orta uyumlu
				{ InstrumentType.Saksafon, 0.7f }, // İyi uyumlu
				{ InstrumentType.Darbuka, 0.6f }   // Orta uyumlu
			};
			
			// Oyun Havaları
			_styleInstrumentCompatibility[MusicStyle.OyunHavalari] = new Dictionary<InstrumentType, float>
			{
				{ InstrumentType.Keman, 0.6f },    // Orta uyumlu
				{ InstrumentType.Klavye, 0.7f },   // İyi uyumlu
				{ InstrumentType.Baglama, 0.8f },  // Çok iyi uyumlu
				{ InstrumentType.Gitar, 0.5f },    // Orta uyumlu
				{ InstrumentType.KanunUd, 0.4f },  // Az uyumlu
				{ InstrumentType.Saksafon, 0.3f }, // Düşük uyum
				{ InstrumentType.Darbuka, 0.9f }   // Çok etkili
			};
			
			// Taverna
			_styleInstrumentCompatibility[MusicStyle.Taverna] = new Dictionary<InstrumentType, float>
			{
				{ InstrumentType.Keman, 0.8f },    // Çok iyi uyumlu
				{ InstrumentType.Klavye, 0.9f },   // Çok etkili
				{ InstrumentType.Baglama, 0.6f },  // Orta uyumlu
				{ InstrumentType.Gitar, 0.7f },    // İyi uyumlu
				{ InstrumentType.KanunUd, 0.5f },  // Orta uyumlu
				{ InstrumentType.Saksafon, 0.8f }, // Çok iyi uyumlu
				{ InstrumentType.Darbuka, 0.4f }   // Az uyumlu
			};
			
			// Modern Lounge
			_styleInstrumentCompatibility[MusicStyle.ModernLounge] = new Dictionary<InstrumentType, float>
			{
				{ InstrumentType.Keman, 0.7f },    // İyi uyumlu
				{ InstrumentType.Klavye, 0.8f },   // Çok iyi uyumlu
				{ InstrumentType.Baglama, 0.3f },  // Düşük uyum
				{ InstrumentType.Gitar, 0.7f },    // İyi uyumlu
				{ InstrumentType.KanunUd, 0.6f },  // Orta uyumlu
				{ InstrumentType.Saksafon, 0.9f }, // Çok etkili
				{ InstrumentType.Darbuka, 0.4f }   // Az uyumlu
			};
		}
		
		// Müşteri tipi tercihlerini başlat
		private void InitializeCustomerPreferences()
		{
			// Farklı müşteri tipleri için farklı müzik stili tercihleri
			_customerTypePreferences["Regular"] = 0.7f;     // Sıradan müşteri - tüm stilleri kabul eder
			_customerTypePreferences["Elite"] = 0.9f;       // Elit müşteri - Modern Lounge tercih eder
			_customerTypePreferences["Worker"] = 0.8f;      // İşçi sınıfı müşteri - Arabesk ve Oyun Havaları tercih eder
			_customerTypePreferences["Emotional"] = 0.9f;   // Duygusal müşteri - Arabesk tercih eder
			_customerTypePreferences["Nostalgic"] = 0.9f;   // Nostaljik müşteri - Taverna tercih eder
			_customerTypePreferences["Young"] = 0.8f;       // Genç müşteri - Fantezi Pop ve Modern Lounge tercih eder
		}
		
		// Rastgele enstrüman ata
		private void AssignRandomInstrument()
		{
			Array values = Enum.GetValues(typeof(InstrumentType));
			_instrumentType = (InstrumentType)values.GetValue(GD.RandRange(0, values.Length - 1));
			
			// Enstrüman adını belirle
			_instrumentName = _instrumentType.ToString();
			
			// Enstrüman kalitesini belirle (0.4-0.7 arası)
			_instrumentQuality = 0.4f + GD.Randf() * 0.3f;
		}
		
		// Trait'lere göre değerleri düzenle
		private void AdjustValuesByTraits()
		{
			foreach (string trait in GetTraits())
			{
				switch (trait)
				{
					case "Talented":
						_techniqueSkill = Mathf.Min(1.0f, _techniqueSkill + 0.2f);
						_creativitySkill = Mathf.Min(1.0f, _creativitySkill + 0.15f);
						break;
					case "ArabeskUstasi": // Özel müzisyen trait'i
						_musicStyleLevels[MusicStyle.Arabesk] = Mathf.Min(1.0f, _musicStyleLevels[MusicStyle.Arabesk] + 0.3f);
						_specialtyStyle = MusicStyle.Arabesk;
						break;
					case "OyunHavasiUstasi": // Özel müzisyen trait'i
						_musicStyleLevels[MusicStyle.OyunHavalari] = Mathf.Min(1.0f, _musicStyleLevels[MusicStyle.OyunHavalari] + 0.3f);
						_specialtyStyle = MusicStyle.OyunHavalari;
						break;
					case "TavernaUstasi": // Özel müzisyen trait'i
						_musicStyleLevels[MusicStyle.Taverna] = Mathf.Min(1.0f, _musicStyleLevels[MusicStyle.Taverna] + 0.3f);
						_specialtyStyle = MusicStyle.Taverna;
						break;
					case "PopYildizi": // Özel müzisyen trait'i
						_musicStyleLevels[MusicStyle.FanteziPop] = Mathf.Min(1.0f, _musicStyleLevels[MusicStyle.FanteziPop] + 0.3f);
						_specialtyStyle = MusicStyle.FanteziPop;
						break;
					case "ModernMuzisyen": // Özel müzisyen trait'i
						_musicStyleLevels[MusicStyle.ModernLounge] = Mathf.Min(1.0f, _musicStyleLevels[MusicStyle.ModernLounge] + 0.3f);
						_specialtyStyle = MusicStyle.ModernLounge;
						break;
					case "Experienced":
						_repertoireSize = Mathf.Min(1.0f, _repertoireSize + 0.2f);
						_techniqueSkill = Mathf.Min(1.0f, _techniqueSkill + 0.1f);
						break;
					case "Charismatic":
						_stagePresenceSkill = Mathf.Min(1.0f, _stagePresenceSkill + 0.25f);
						break;
					case "StageFright": // Özel müzisyen trait'i
						_stagePresenceSkill = Mathf.Max(0.1f, _stagePresenceSkill - 0.2f);
						break;
					case "TumTurlereHakim": // Özel müzisyen trait'i - tüm türlerde temel yetenek
						foreach (MusicStyle style in Enum.GetValues(typeof(MusicStyle)))
						{
							_musicStyleLevels[style] = Mathf.Max(_musicStyleLevels[style], 0.5f);
						}
						break;
					case "Lazy":
						_techniqueSkill = Mathf.Max(0.1f, _techniqueSkill - 0.1f);
						break;
					case "Alcoholic":
						_techniqueSkill = Mathf.Max(0.1f, _techniqueSkill - 0.15f);
						_stagePresenceSkill = Mathf.Max(0.1f, _stagePresenceSkill - 0.1f);
						break;
				}
			}
		}
		
		// Özel beceri geliştirme - StaffBase.ImproveSkills() metodunu override eder
		public override void ImproveSkills()
		{
			base.ImproveSkills();
			
			// Müzisyen-spesifik becerileri geliştir
			float baseImprovement = 0.005f; // Günlük temel gelişim
			
			// Performans sayısı gelişim hızını etkiler
			float performanceModifier = 1.0f + (Mathf.Min(_performancesCompleted, 10) * 0.03f);
			
			// Her beceri için rastgele gelişim
			if (GD.Randf() < 0.7f) // %70 ihtimalle teknik gelişimi
			{
				_techniqueSkill = Mathf.Min(1.0f, _techniqueSkill + baseImprovement * performanceModifier);
				if (_skills.ContainsKey("technique")) 
					_skills["technique"] = _techniqueSkill;
			}
			
			if (GD.Randf() < 0.5f) // %50 ihtimalle yaratıcılık gelişimi
			{
				_creativitySkill = Mathf.Min(1.0f, _creativitySkill + baseImprovement * performanceModifier);
				if (_skills.ContainsKey("creativity")) 
					_skills["creativity"] = _creativitySkill;
			}
			
			if (GD.Randf() < 0.4f) // %40 ihtimalle repertuar gelişimi
			{
				_repertoireSize = Mathf.Min(1.0f, _repertoireSize + baseImprovement * performanceModifier);
				if (_skills.ContainsKey("repertoire")) 
					_skills["repertoire"] = _repertoireSize;
			}
			
			if (GD.Randf() < 0.6f) // %60 ihtimalle sahne hakimiyeti gelişimi
			{
				_stagePresenceSkill = Mathf.Min(1.0f, _stagePresenceSkill + baseImprovement * performanceModifier);
				if (_skills.ContainsKey("stagePrescence")) 
					_skills["stagePrescence"] = _stagePresenceSkill;
			}
			
			// En çok çalınan stil daha hızlı gelişir
			if (_currentStyle != 0 && GD.Randf() < 0.8f)
			{
				_musicStyleLevels[_currentStyle] = Mathf.Min(1.0f, _musicStyleLevels[_currentStyle] + baseImprovement * 1.5f);
			}
		}
		
		// Performansı başlat
		public void StartPerformance(MusicStyle style = MusicStyle.Taverna)
		{
			if (_isPerforming) return;
			
			// Eğer stil belirtilmemişse, uzmanlık stilini kullan
			if (style == 0)
			{
				style = _specialtyStyle;
			}
			
			_isPerforming = true;
			_currentStyle = style;
			_currentPerformanceTime = 0.0f;
			
			// Performans kalitesini hesapla
			_performanceQuality = CalculatePerformanceQuality(style);
			
			// Atmosfer etkilerini güncelle
			UpdateAtmosphereEffects();
			
			// Aktiviteyi güncelle
			SetActivity(ActivityState.Special);
			
			// Performans başladı sinyali gönder
			EmitSignal(SignalName.PerformanceStarted, style.ToString(), _performanceQuality);
			
			// Performans animasyonu - enstrümana göre değişir
			PlayInstrumentAnimation();
			
			GD.Print($"Musician {Name} started a {style} performance with quality: {_performanceQuality}. Skill level: {_musicStyleLevels[style]}");
		}
		
		// Performansı başlat (string stil adı ile)
		public void StartPerformance(string styleName)
		{
			if (Enum.TryParse<MusicStyle>(styleName, out MusicStyle style))
			{
				StartPerformance(style);
			}
			else
			{
				// Stil adı geçerli değilse, uzmanlık stilini kullan
				StartPerformance(_specialtyStyle);
			}
		}
		
		// Performans kalitesi hesaplama
		private int CalculatePerformanceQuality(MusicStyle style)
		{
			// Temel beceri değerleri ile kalite hesaplama
			float baseFactor = (_techniqueSkill * 0.3f + _creativitySkill * 0.2f + 
							  _repertoireSize * 0.2f + _stagePresenceSkill * 0.3f);
			
			// Stil uzmanlık seviyesi
			float styleFactor = _musicStyleLevels[style] * 0.3f;
			
			// Enstrüman-stil uyumluluğu
			float instrumentCompatibility = _styleInstrumentCompatibility[style][_instrumentType];
			float instrumentFactor = (instrumentCompatibility * _instrumentQuality) * 0.2f;
			
			// Enerji ve ruh hali faktörü
			float conditionFactor = (Energy * 0.7f + Mood * 0.3f) * 0.2f;
			
			// Grup faktörü - grup büyüklüğüne göre bonus
			float groupFactor = 0.0f;
			if (_groupSize > 1)
			{
				groupFactor = Math.Min((_groupSize - 1) * 0.05f, 0.15f); // Maksimum %15 bonus
			}
			
			// Ardışık performans cezası
			float fatigueFactor = -Mathf.Min(_consecutivePerformances * 0.05f, 0.2f);
			
			// Toplam kalite faktörü (0-1 arası)
			float qualityFactor = baseFactor + styleFactor + instrumentFactor + conditionFactor + groupFactor + fatigueFactor;
			
			// 1-10 arası kalite puanı
			return Mathf.Clamp((int)(qualityFactor * 10f), 1, 10);
		}
		
		// Atmosfer etkilerini güncelle
		private void UpdateAtmosphereEffects()
		{
			if (!_isPerforming) return;
			
			// Stil bazlı temel etkiler
			switch (_currentStyle)
			{
				case MusicStyle.Arabesk: // Duygusal/Ağır
					_customerMoodEffect = -0.2f;        // Duygusal negatif ruh hali
					_drinkOrderEffect = 0.2f;          // İçki tüketimini artırır
					_stayTimeEffect = 0.3f;            // Duygusal müşterilerde uzun oturma süresi
					_konsomatrisEffect = 0.1f;         // Dramatik rol yapma
					_tipProbabilityEffect = 0.1f;      // Orta düzey bahşiş
					break;
				
				case MusicStyle.FanteziPop: // Romantik/Enerjik
					_customerMoodEffect = 0.2f;         // Pozitif ruh hali
					_drinkOrderEffect = 0.1f;          // Orta düzey içki tüketimi
					_stayTimeEffect = 0.1f;            // Standart oturma süresi
					_konsomatrisEffect = 0.3f;         // Konsomatris-müşteri etkileşimini artırır
					_tipProbabilityEffect = 0.15f;     // İyi düzey bahşiş
					break;
				
				case MusicStyle.OyunHavalari: // Eğlenceli/Dans
					_customerMoodEffect = 0.3f;         // Yüksek pozitif ruh hali
					_drinkOrderEffect = 0.3f;          // İçki ve meze siparişi artar
					_stayTimeEffect = 0.0f;            // Normal oturma süresi (dans nedeniyle nötr)
					_konsomatrisEffect = 0.25f;        // Dans performansı
					_tipProbabilityEffect = 0.2f;      // Yüksek bahşiş olasılığı
					break;
				
				case MusicStyle.Taverna: // Nostaljik/Rahat
					_customerMoodEffect = 0.15f;        // Orta düzey pozitif ruh hali
					_drinkOrderEffect = 0.15f;         // Orta düzey içki tüketimi
					_stayTimeEffect = 0.2f;            // Orta yaşlı müşterilerde uzun oturma süresi
					_konsomatrisEffect = 0.15f;        // Standart performans
					_tipProbabilityEffect = 0.25f;     // Yüksek bahşiş olasılığı (nostaljik müşteriler)
					break;
				
				case MusicStyle.ModernLounge: // Lüks/Şık
					_customerMoodEffect = 0.1f;         // Hafif pozitif ruh hali
					_drinkOrderEffect = 0.1f;          // Standart içki tüketimi
					_stayTimeEffect = 0.1f;            // Standart oturma süresi
					_konsomatrisEffect = 0.2f;         // Şık performans
					_tipProbabilityEffect = 0.3f;      // Çok yüksek bahşiş olasılığı (elit müşteriler)
					break;
			}
			
			// Performans kalitesine göre etkileri ayarla
			float qualityModifier = (_performanceQuality / 10.0f) * 2.0f - 1.0f; // -1 to 1
			
			// Etkileri kaliteye göre ayarla
			_customerMoodEffect *= (1.0f + qualityModifier);
			_drinkOrderEffect *= (1.0f + qualityModifier);
			_stayTimeEffect *= (1.0f + qualityModifier);
			_konsomatrisEffect *= (1.0f + qualityModifier);
			_tipProbabilityEffect *= (1.0f + qualityModifier);
			
			// Atmosfer değişimi sinyali gönder
			EmitSignal(SignalName.AtmosphereChanged, _currentStyle.ToString(), _customerMoodEffect, _drinkOrderEffect);
		}
		
		// Solo başlat
		public void StartSolo()
		{
			if (!_isPerforming) return;
			
			_isSoloing = true;
			
			// Solo kalitesi, temel performans kalitesinden biraz daha yüksek olabilir
			int soloQuality = _performanceQuality + (GD.Randf() < 0.5f ? 1 : 0);
			soloQuality = Mathf.Clamp(soloQuality, 1, 10);
			
			// Solo animasyonu
			PlayAnimation("solo");
			
			// Solo başladı sinyali gönder
			EmitSignal(SignalName.SoloStarted, soloQuality);
			
			// Solo istatistiğini artır
			_solosPerformed++;
			
			GD.Print($"Musician {Name} started a solo with quality: {soloQuality}");
		}
		
		// Solo bitir
		public void EndSolo()
		{
			if (!_isSoloing) return;
			
			_isSoloing = false;
			
			// İzleyici izlenimi
			float audienceImpression = _performanceQuality / 10.0f;
			
			// Solo bitti sinyali gönder
			EmitSignal(SignalName.SoloEnded, audienceImpression);
			
			// Deneyim puanı kazanma
			AddExperience(1);
			
			// Performans animasyonuna geri dön
			PlayInstrumentAnimation();
			
			GD.Print($"Musician {Name} ended solo. Audience impression: {audienceImpression}");
		}
		
		// Performansı bitir
		public void EndPerformance()
		{
			if (!_isPerforming) return;
			
			_isPerforming = false;
			
			// Solodaysa soloyu da bitir
			if (_isSoloing)
			{
				EndSolo();
			}
			
			// İzleyici değerlendirmesi
			_audienceRating = CalculateAudienceRating();
			
			// Performans istatistikleri
			_performancesCompleted++;
			_consecutivePerformances++;
			_totalAudienceRating += _audienceRating;
			
			// Stil becerisi geliştirme
			ImproveStyleSkill(_currentStyle);
			
			// Enerji tüketimi
			AdjustEnergy(-0.1f, "Performance");
			
			// Deneyim puanı kazanma
			AddExperience(2);
			
			// Performans süresi
			float performanceDuration = _currentPerformanceTime;
			
			// Performans bitti sinyali gönder
			EmitSignal(SignalName.PerformanceEnded, _audienceRating, performanceDuration);
			
			// Normal duruma dön
			SetActivity(ActivityState.Idle);
			
			GD.Print($"Musician {Name} ended performance. Duration: {performanceDuration}s, audience rating: {_audienceRating}");
		}
		
		// Stil becerisini geliştir
		private void ImproveStyleSkill(MusicStyle style)
		{
			// Performans kalitesine göre stil gelişimi
			float improvementAmount = 0.005f + (_performanceQuality / 10.0f) * 0.01f; // 0.005-0.015 arası
			
			// Mevcut stil seviyesi geliştir
			_musicStyleLevels[style] = Mathf.Min(1.0f, _musicStyleLevels[style] + improvementAmount);
			
			GD.Print($"Musician {Name}'s {style} skill improved to {_musicStyleLevels[style]}");
		}
		
		// İzleyici değerlendirmesi hesaplama
		private float CalculateAudienceRating()
		{
			// Temel değerlendirme puanı
			float baseRating = _performanceQuality / 10.0f;
			
			// Hata sayısı faktörü
			float mistakeFactor = -Mathf.Min(_mistakesMade * 0.05f, 0.3f);
			
			// Performans süresi faktörü
			float durationFactor = 0.0f;
			
			// İdeal süreye yakınlık
			float durationDifference = Mathf.Abs(_currentPerformanceTime - _idealPerformanceLength);
			if (durationDifference < _idealPerformanceLength * 0.2f)
			{
				durationFactor = 0.1f; // İdeal süreye yakın
			}
			else if (durationDifference > _idealPerformanceLength * 0.5f)
			{
				durationFactor = -0.1f; // Çok kısa veya çok uzun
			}
			
			// Sahne hakimiyeti faktörü
			float presenceFactor = (_stagePresenceSkill - 0.5f) * 0.2f;
			
			// Enstrüman-stil uyumluluğu faktörü
			float instrumentFactor = (_styleInstrumentCompatibility[_currentStyle][_instrumentType] - 0.5f) * 0.2f;
			
			// Toplam değerlendirme (0-1 arası)
			return Mathf.Clamp(baseRating + mistakeFactor + durationFactor + presenceFactor + instrumentFactor, 0.1f, 1.0f);
		}
		
		// Enstrüman çalma animasyonu
		private void PlayInstrumentAnimation()
		{
			// Enstrüman tipine göre farklı animasyon
			string animationName = "play_";
			
			switch (_instrumentType)
			{
				case InstrumentType.Keman:
					animationName += "keman";
					break;
				case InstrumentType.Klavye:
					animationName += "klavye";
					break;
				case InstrumentType.Baglama:
					animationName += "baglama";
					break;
				case InstrumentType.Gitar:
					animationName += "gitar";
					break;
				case InstrumentType.Darbuka:
					animationName += "darbuka";
					break;
				case InstrumentType.Saksafon:
					animationName += "saksafon";
					break;
				case InstrumentType.KanunUd:
					animationName += "kanun";
					break;
				default:
					animationName += "instrument";
					break;
			}
			
			PlayAnimation(animationName);
		}
		
		// Müzik stili öğrenme
		public void ImproveStyle(MusicStyle style, float amount)
		{
			float currentLevel = _musicStyleLevels[style];
			_musicStyleLevels[style] = Mathf.Min(1.0f, currentLevel + amount);
			
			// Öğrenme başarısı için deneyim
			AddExperience(1);
			
			// Repertuar genişliği artışı
			_repertoireSize = Mathf.Min(1.0f, _repertoireSize + 0.02f);
			
			GD.Print($"Musician {Name} improved {style} style from {currentLevel} to {_musicStyleLevels[style]}");
		}
		
		// Müzik stili öğrenme (string stil adı ile)
		public void ImproveStyle(string styleName, float amount)
		{
			if (Enum.TryParse<MusicStyle>(styleName, out MusicStyle style))
			{
				ImproveStyle(style, amount);
			}
		}
		
		// Uzmanlık türünü değiştir
		public void ChangeSpecialtyStyle(MusicStyle style)
		{
			_specialtyStyle = style;
			GD.Print($"Musician {Name} changed specialty style to: {style}");
		}
		
		// Uzmanlık türünü değiştir (string stil adı ile)
		public void ChangeSpecialtyStyle(string styleName)
		{
			if (Enum.TryParse<MusicStyle>(styleName, out MusicStyle style))
			{
				ChangeSpecialtyStyle(style);
			}
		}
		
		// Enstrüman kalitesini yükselt
		public void UpgradeInstrument(float qualityIncrease)
		{
			float oldQuality = _instrumentQuality;
			_instrumentQuality = Mathf.Min(1.0f, _instrumentQuality + qualityIncrease);
			
			// Ruh halini iyileştir
			AdjustMood(0.1f, "Instrument Upgrade");
			
			GD.Print($"Musician {Name}'s instrument upgraded from quality {oldQuality} to {_instrumentQuality}");
		}
		
		// Enstrüman tipini değiştir
		public void ChangeInstrument(InstrumentType newType, float quality)
		{
			InstrumentType oldType = _instrumentType;
			_instrumentType = newType;
			_instrumentName = newType.ToString();
			_instrumentQuality = quality;
			
			// Kısa bir adaptasyon süreci - bu sürede performans biraz düşer
			Timer adaptationTimer = new Timer
			{
				WaitTime = 60.0f, // 1 dakika
				OneShot = true
			};
			
			AddChild(adaptationTimer);
			adaptationTimer.Timeout += () => 
			{
				GD.Print($"Musician {Name} has adapted to the new instrument");
			};
			adaptationTimer.Start();
			
			GD.Print($"Musician {Name} changed instrument from {oldType} to {_instrumentName} (quality: {_instrumentQuality})");
		}
		
		// Enstrüman tipini değiştir (string enstrüman adı ile)
		public void ChangeInstrument(string instrumentName, float quality)
		{
			if (Enum.TryParse<InstrumentType>(instrumentName, out InstrumentType type))
			{
				ChangeInstrument(type, quality);
			}
		}
		
		// Hata yapma
		private void MakeMistake()
		{
			// Hata şiddeti (0-1 arası)
			float severity = 0.3f + GD.Randf() * 0.4f; // %30-%70 şiddet
			
			// Eğer teknik beceri yüksekse, hata şiddeti azalır
			severity *= (1.0f - (_techniqueSkill * 0.5f));
			
			// Çalınan stil seviyesine göre de azalır
			severity *= (1.0f - (_musicStyleLevels[_currentStyle] * 0.5f));
			
			// Hata animasyonu
			PlayAnimation("mistake");
			
			// Hatadan sonra normal çalma animasyonuna dönmek için
			Timer recoveryTimer = new Timer
			{
				WaitTime = 1.0f,
				OneShot = true
			};
			
			AddChild(recoveryTimer);
			recoveryTimer.Timeout += () => 
			{
				if (_isPerforming)
				{
					PlayInstrumentAnimation();
				}
			};
			recoveryTimer.Start();
			
			// Hata istatistiğini artır
			_mistakesMade++;
			
			// Hata sinyali gönder
			EmitSignal(SignalName.MistakeMade, severity);
			
			GD.Print($"Musician {Name} made a mistake! Severity: {severity}");
		}
		
		// Grup oluştur
		public void FormGroup(List<Musician> members)
		{
			if (members == null || members.Count == 0) return;
			
			_groupMembers.Clear();
			_groupMembers.AddRange(members);
			_groupSize = members.Count + 1; // Kendisi dahil
			_isGroupLeader = true;
			
			// Her üyeye grup bilgisini ayarla
			foreach (var member in _groupMembers)
			{
				member.JoinGroup(this);
			}
			
			GD.Print($"Musician {Name} formed a group with {_groupSize} members");
		}
		
		// Gruba katıl
		public void JoinGroup(Musician leader)
		{
			if (leader == null) return;
			
			_isGroupLeader = false;
			_groupSize = leader._groupSize;
			
			GD.Print($"Musician {Name} joined {leader.Name}'s group");
		}
		
		// Gruptan ayrıl
		public void LeaveGroup()
		{
			if (_isGroupLeader)
			{
				// Grubun diğer üyelerini bilgilendir
				foreach (var member in _groupMembers)
				{
					member._groupSize = 1;
				}
				_groupMembers.Clear();
			}
			
			_groupSize = 1;
			_isGroupLeader = false;
			
			GD.Print($"Musician {Name} left the group");
		}
		
		// Müşteri ile etkileşim - StaffBase.InteractWithCustomer() metodunu override eder
		public override void InteractWithCustomer(Node3D customer)
		{
			// Müşteriden istek parça kontrolü
			bool hasSongRequest = false;
			string requestedGenre = "";
			string customerType = "Regular";
			
			// Müşteri tipini al
			if (customer.GetType().GetProperty("CustomerType") != null)
			{
				try 
				{
					customerType = (string)customer.Get("CustomerType");
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error getting customer type: {e.Message}");
				}
			}
			
			// İstek parça kontrolü
			if (customer.GetType().GetMethod("HasSongRequest") != null)
			{
				try 
				{
					hasSongRequest = (bool)customer.Call("HasSongRequest");
					if (hasSongRequest && customer.GetType().GetMethod("GetRequestedGenre") != null)
					{
						requestedGenre = (string)customer.Call("GetRequestedGenre");
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error checking song request: {e.Message}");
				}
			}
			
			// İstek parça işleme
			if (hasSongRequest && !string.IsNullOrEmpty(requestedGenre))
			{
				// İstek parçayı çal
				if (Enum.TryParse<MusicStyle>(requestedGenre, out MusicStyle style))
				{
					float skillLevel = _musicStyleLevels[style];
					
					// Eğer performanstaysa ve bu tür biliniyorsa, parça değiştir
					if (_isPerforming && skillLevel > 0.3f)
					{
						// Mevcut performansı bitir ve yeni türde başlat
						EndPerformance();
						StartPerformance(style);
						
						// Müşteri memnuniyeti artır
						if (customer.GetType().GetMethod("AdjustSatisfaction") != null)
						{
							try 
							{
								customer.Call("AdjustSatisfaction", 0.2f, "Song Request Fulfilled");
							}
							catch (Exception e)
							{
								GD.PrintErr($"Error adjusting customer satisfaction: {e.Message}");
							}
						}
						
						GD.Print($"Musician {Name} changed performance to requested genre: {requestedGenre}");
					}
					else if (!_isPerforming && skillLevel > 0.3f)
					{
						// Yeni performans başlat
						StartPerformance(style);
						
						// Müşteri memnuniyeti artır
						if (customer.GetType().GetMethod("AdjustSatisfaction") != null)
						{
							try 
							{
								customer.Call("AdjustSatisfaction", 0.2f, "Song Request Fulfilled");
							}
							catch (Exception e)
							{
								GD.PrintErr($"Error adjusting customer satisfaction: {e.Message}");
							}
						}
						
						GD.Print($"Musician {Name} started performance of requested genre: {requestedGenre}");
					}
					else if (skillLevel <= 0.3f)
					{
						// Bilmediği veya çok zayıf olduğu bir tür - özür dile ve mevcut performansa devam et
						GD.Print($"Musician {Name} cannot play requested genre: {requestedGenre} well enough");
						
						// Müşteri memnuniyeti azalt
						if (customer.GetType().GetMethod("AdjustSatisfaction") != null)
						{
							try 
							{
								customer.Call("AdjustSatisfaction", -0.1f, "Song Request Denied");
							}
							catch (Exception e)
							{
								GD.PrintErr($"Error adjusting customer satisfaction: {e.Message}");
							}
						}
					}
				}
			}
			else
			{
				// Müşteri tipine göre tercih edilen stili çal
				MusicStyle preferredStyle = GetPreferredStyleForCustomer(customerType);
				
				if (!_isPerforming)
				{
					StartPerformance(preferredStyle);
					GD.Print($"Musician {Name} started playing {preferredStyle} for {customerType} customer");
				}
				else if (_currentStyle != preferredStyle && _performanceQuality < 7)
				{
					// Mevcut performans kalitesi düşükse, müşteri tipine göre stili değiştir
					EndPerformance();
					StartPerformance(preferredStyle);
					GD.Print($"Musician {Name} switched to {preferredStyle} for {customerType} customer");
				}
			}
			
			// Enerji tüketimi
			AdjustEnergy(-0.01f, "Customer Interaction");
		}
		
		// Müşteri tipine göre tercih edilen stili al
		private MusicStyle GetPreferredStyleForCustomer(string customerType)
		{
			switch (customerType)
			{
				case "Elite":
					return MusicStyle.ModernLounge;
				case "Worker":
					return GD.Randf() < 0.5f ? MusicStyle.Arabesk : MusicStyle.OyunHavalari;
				case "Emotional":
					return MusicStyle.Arabesk;
				case "Nostalgic":
					return MusicStyle.Taverna;
				case "Young":
					return GD.Randf() < 0.5f ? MusicStyle.FanteziPop : MusicStyle.ModernLounge;
				default:
					// "Regular" veya diğer tipler için
					return _specialtyStyle; // Uzmanlık stilini çal
			}
		}
		
		// Rotasyonlu performans (gece ilerledikçe stil değişimi)
		public void StartRotationalPerformance()
		{
			if (_isPerforming) return;
			
			// İlk stili başlat
			StartPerformance(MusicStyle.Taverna);
			
			// Periyodik stil değişimi için zamanlayıcı
			Timer rotationTimer = new Timer
			{
				WaitTime = 600.0f, // 10 dakika (gerçek oyunda ayarlanabilir)
				OneShot = false
			};
			
			AddChild(rotationTimer);
			rotationTimer.Timeout += RotateStyle;
			rotationTimer.Start();
			
			GD.Print($"Musician {Name} started rotational performance");
		}
		
		// Stil rotasyonu
		private void RotateStyle()
		{
			if (!_isPerforming) return;
			
			// Gece ilerledikçe stil değişimi
			// Taverna -> Arabesk -> Fantezi Pop -> Oyun Havaları
			MusicStyle nextStyle;
			
			switch (_currentStyle)
			{
				case MusicStyle.Taverna:
					nextStyle = MusicStyle.Arabesk;
					break;
				case MusicStyle.Arabesk:
					nextStyle = MusicStyle.FanteziPop;
					break;
				case MusicStyle.FanteziPop:
					nextStyle = MusicStyle.OyunHavalari;
					break;
				case MusicStyle.OyunHavalari:
					nextStyle = MusicStyle.ModernLounge;
					break;
				case MusicStyle.ModernLounge:
					nextStyle = MusicStyle.Taverna;
					break;
				default:
					nextStyle = MusicStyle.Taverna;
					break;
			}
			
			// Performansı bitir ve yeni stilde başlat
			EndPerformance();
			StartPerformance(nextStyle);
			
			GD.Print($"Musician {Name} rotated style to {nextStyle}");
		}
		
		// Özel performans davranışı - StaffBase.PerformSpecialBehavior() metodunu override eder
		public override void PerformSpecialBehavior()
		{
			if (_isPerforming)
			{
				// Performans sırasında özel davranış - solo başlat
				StartSolo();
			}
			else
			{
				// Performans dışında - uzmanlık stilinde yeni performans başlat
				StartPerformance(_specialtyStyle);
			}
		}
		
		// Özel animasyon - StaffBase.PlaySpecialAnimation() metodunu override eder
		protected override void PlaySpecialAnimation()
		{
			switch (_instrumentType)
			{
				case InstrumentType.Keman:
					PlayAnimation("keman_solo");
					break;
				case InstrumentType.Darbuka:
					PlayAnimation("darbuka_solo");
					break;
				case InstrumentType.Klavye:
					PlayAnimation("klavye_solo");
					break;
				case InstrumentType.Baglama:
					PlayAnimation("baglama_solo");
					break;
				case InstrumentType.Saksafon:
					PlayAnimation("saksafon_solo");
					break;
				case InstrumentType.Gitar:
					PlayAnimation("gitar_solo");
					break;
				case InstrumentType.KanunUd:
					PlayAnimation("kanun_solo");
					break;
				default:
					PlayAnimation("special_performance");
					break;
			}
		}
		
		// Seviye atlama özel efektleri - StaffBase.OnLevelUp() metodunu override eder
		protected override void OnLevelUp()
		{
			base.OnLevelUp();
			
			// Teknik beceri gelişimi
			_techniqueSkill = Mathf.Min(1.0f, _techniqueSkill + 0.05f);
			
			// Yaratıcılık becerisi gelişimi
			_creativitySkill = Mathf.Min(1.0f, _creativitySkill + 0.04f);
			
			// Repertuar gelişimi
			_repertoireSize = Mathf.Min(1.0f, _repertoireSize + 0.04f);
			
			// Sahne hakimiyeti gelişimi
			_stagePresenceSkill = Mathf.Min(1.0f, _stagePresenceSkill + 0.03f);
			
			// Uzmanlık stilinde gelişim
			_musicStyleLevels[_specialtyStyle] = Mathf.Min(1.0f, _musicStyleLevels[_specialtyStyle] + 0.05f);
			
			// Beceri değerlerini skills sözlüğüne aktar
			if (_skills.ContainsKey("technique")) _skills["technique"] = _techniqueSkill;
			if (_skills.ContainsKey("creativity")) _skills["creativity"] = _creativitySkill;
			if (_skills.ContainsKey("repertoire")) _skills["repertoire"] = _repertoireSize;
			if (_skills.ContainsKey("stagePrescence")) _skills["stagePrescence"] = _stagePresenceSkill;
			
			GD.Print($"Musician {Name} leveled up: technique {_techniqueSkill}, creativity {_creativitySkill}, {_specialtyStyle} skill {_musicStyleLevels[_specialtyStyle]}");
		}
		
		// Günlük güncelleme - StaffBase.UpdateDaily() metodunu override eder
		public override void UpdateDaily()
		{
			base.UpdateDaily();
			
			// İstatistikleri sıfırla
			_performancesCompleted = 0;
			_solosPerformed = 0;
			_totalAudienceRating = 0.0f;
			_mistakesMade = 0;
			_consecutivePerformances = 0;
			
			// Performans durumunu sıfırla
			if (_isPerforming)
			{
				EndPerformance();
			}
		}
		
		// Özel risk faktörleri - StaffBase.ApplySpecialRiskFactors() metodunu override eder
		protected override void ApplySpecialRiskFactors()
		{
			base.ApplySpecialRiskFactors();
			
			// Müzisyen-spesifik risk faktörleri
			
			// Düşük izleyici değerlendirmesi = daha fazla sadakatsizlik riski
			if (_audienceRating < 0.4f)
			{
				_disloyaltyRisk += 0.05f;
			}
			
			// Kötü enstrüman kalitesi = daha fazla sadakatsizlik riski
			if (_instrumentQuality < 0.4f)
			{
				_disloyaltyRisk += 0.03f;
			}
		}
		
		// Özel performans faktörü hesaplama - StaffBase.CalculateSpecialPerformanceFactor() metodunu override eder
		protected override float CalculateSpecialPerformanceFactor()
		{
			// Teknik, yaratıcılık ve sahne performansı faktörü
			return (_techniqueSkill * 0.3f + _creativitySkill * 0.3f + _stagePresenceSkill * 0.4f - 0.5f) * 0.2f;
		}
		
		// Müzik stilinin müşteri üzerindeki etkisini hesapla
		public float GetMusicEffectOnCustomer(string customerType)
		{
			if (!_isPerforming) return 0.0f;
			
			// Temel etki faktörü (müşteri tipine göre)
			float baseEffect = 0.0f;
			
			if (_customerTypePreferences.ContainsKey(customerType))
			{
				baseEffect = _customerTypePreferences[customerType];
			}
			
			// Stil-müşteri tipi uyumluluğu
			float styleCompatibility = 0.5f; // Varsayılan orta uyumluluk
			
			// Müşteri tipine göre stil uyumluluğu hesapla
			MusicStyle preferredStyle = GetPreferredStyleForCustomer(customerType);
			
			if (_currentStyle == preferredStyle)
			{
				styleCompatibility = 1.0f; // Tam uyumlu
			}
			else
			{
				// Stilin müşteri tipine uygun olup olmadığı
				switch (customerType)
				{
					case "Elite":
						styleCompatibility = _currentStyle == MusicStyle.ModernLounge ? 1.0f : 
											_currentStyle == MusicStyle.Taverna ? 0.7f : 
											_currentStyle == MusicStyle.FanteziPop ? 0.5f : 0.3f;
						break;
					case "Worker":
						styleCompatibility = _currentStyle == MusicStyle.Arabesk || _currentStyle == MusicStyle.OyunHavalari ? 1.0f : 
											_currentStyle == MusicStyle.Taverna ? 0.7f : 0.4f;
						break;
					case "Emotional":
						styleCompatibility = _currentStyle == MusicStyle.Arabesk ? 1.0f : 
											_currentStyle == MusicStyle.Taverna ? 0.6f : 0.4f;
						break;
					case "Nostalgic":
						styleCompatibility = _currentStyle == MusicStyle.Taverna ? 1.0f : 
											_currentStyle == MusicStyle.Arabesk ? 0.7f : 0.4f;
						break;
					case "Young":
						styleCompatibility = _currentStyle == MusicStyle.FanteziPop || _currentStyle == MusicStyle.ModernLounge ? 1.0f : 
											_currentStyle == MusicStyle.OyunHavalari ? 0.8f : 0.3f;
						break;
					default: // Regular
						styleCompatibility = 0.6f; // Ortalama değer
						break;
				}
			}
			
			// Performans kalitesi faktörü
			float qualityFactor = _performanceQuality / 10.0f;
			
			// Toplam etki (-1 ile 1 arası)
			float totalEffect = baseEffect * styleCompatibility * qualityFactor;
			
			return totalEffect;
		}
		
		// Özellik değerlerini döndür
		public new Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = base.GetStats();
			
			// Müzisyen-spesifik değerleri ekle
			stats["TechniqueSkill"] = _techniqueSkill;
			stats["CreativitySkill"] = _creativitySkill;
			stats["RepertoireSize"] = _repertoireSize;
			stats["StagePresenceSkill"] = _stagePresenceSkill;
			stats["InstrumentName"] = _instrumentName;
			stats["InstrumentQuality"] = _instrumentQuality;
			stats["PerformancesCompleted"] = _performancesCompleted;
			stats["SolosPerformed"] = _solosPerformed;
			stats["AudienceRating"] = _audienceRating;
			stats["CurrentStyle"] = _currentStyle.ToString();
			stats["SpecialtyStyle"] = _specialtyStyle.ToString();
			stats["GroupSize"] = _groupSize;
			
			// Müzik stil seviyeleri
			Dictionary<string, float> styleStats = new Dictionary<string, float>();
			foreach (var item in _musicStyleLevels)
			{
				styleStats[item.Key.ToString()] = item.Value;
			}
			stats["StyleLevels"] = styleStats;
			
			// Atmosfer etkileri
			stats["CustomerMoodEffect"] = _customerMoodEffect;
			stats["DrinkOrderEffect"] = _drinkOrderEffect;
			stats["StayTimeEffect"] = _stayTimeEffect;
			stats["KonsomatrisEffect"] = _konsomatrisEffect;
			stats["TipProbabilityEffect"] = _tipProbabilityEffect;
			
			return stats;
		}
	}
}
