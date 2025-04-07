using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Staff
{
	
	public partial class Konsomatris : StaffBase
	{
		// Kons özellikleri
		private float _danceSkill = 0.5f;       // Dans becerisi
		private float _conversationSkill = 0.5f; // Konuşma becerisi
		private float _attractiveness = 0.5f;    // Çekicilik seviyesi
		private float _persuasionSkill = 0.5f;   // İkna becerisi

		// Müşteri takibi
		private Node3D _currentCustomer = null;  // Şu anda ilgilenilen müşteri
		private float _customerSatisfactionBonus = 0f; // Müşteri memnuniyeti bonusu
		private float _tipBonus = 0f;            // Bahşiş bonusu
		
		// Kıskanç davranış özellikleri 
		private float _jealousy = 0f;           // Kıskançlık seviyesi (0-1)
		private float _ambition = 0.5f;         // Hırs seviyesi (0-1)
		
		// Sahne ve performans
		private bool _isPerforming = false;     // Şu anda performans sergiliyor mu
		private int _performanceQuality = 0;    // Performans kalitesi
		
		// Diğer konslarla ilişkiler
		private Dictionary<string, float> _relationWithOtherKons = new Dictionary<string, float>();
		
		// Toplam kazanılan bahşiş miktarı
		private float _totalTipsEarned = 0f;
		
		// Servis açmak verilen içki sayısı
		private int _drinksSold = 0;
		
		// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ
		private float _fatigue = 0f;            // Yorgunluk seviyesi (0-1)
		private float _fatigueResistance = 0.5f; // Yorgunluğa direnç (0-1)
		private float _stressLevel = 0f;        // Stres seviyesi (0-1)
		
		// ÖZELLİK 7: PERFORMANS VE İSTATİSTİK TAKİBİ
		private int _totalPerformancesGiven = 0;  // Toplam verilen performans sayısı
		private float _totalPerformanceTime = 0f; // Toplam performans süresi (dakika)
		private float _averagePerformanceRating = 0f; // Ortalama performans puanı
		private int _totalCustomersServed = 0;    // Toplam hizmet edilen müşteri sayısı
		private int _jointPerformancesGiven = 0;  // Ortak performans sayısı
		
		// ÖZELLİK 4: ÖZEL ETKİLEŞİM VE MİNİ OYUNLAR
		private bool _isMiniGameActive = false;   // Mini oyun aktif mi
		private string _currentMiniGameType = ""; // Aktif mini oyun tipi
		private float _miniGameScore = 0f;        // Mini oyun puanı
				
		// Signals
		[Signal]
		public delegate void PerformanceStartedEventHandler(int quality);
		
		[Signal]
		public delegate void PerformanceEndedEventHandler(float customerSatisfaction);
		
		// ÖZELLİK 1: DİNAMİK İLİŞKİ VE İŞBİRLİĞİ YÖNETİMİ
		[Signal]
		public delegate void JointPerformanceStartedEventHandler(string otherKonsId, int combinedQuality);
		
		// ÖZELLİK 3: EK ANİMASYON VE EFEKTLER
		[Signal]
		public delegate void SpecialEffectTriggeredEventHandler(string effectType, int intensity);
		
		// ÖZELLİK 4: ÖZEL ETKİLEŞİM VE MİNİ OYUNLAR
		[Signal]
		public delegate void MiniGameStartedEventHandler(string gameType);
		
		[Signal]
		public delegate void MiniGameEndedEventHandler(float score, bool success);
		
		public override void _Ready()
		{
			base._Ready();
			
			// Tipi ayarla
			Type = StaffType.Konsomatris;
			
			// Beceri değerlerini başlat (StaffBase'deki skills sözlüğünden al)
			if (_skills.ContainsKey("charm")) _danceSkill = _skills["charm"];
			if (_skills.ContainsKey("conversation")) _conversationSkill = _skills["conversation"];
			if (_skills.ContainsKey("persuasion")) _persuasionSkill = _skills["persuasion"];
			
			// Çekicilik değeri karizma ile ilişkili
			_attractiveness = Charisma * 0.8f + GD.Randf() * 0.2f;
			
			// Kıskançlık ve hırs değerlerini başlat
			_jealousy = GD.Randf() * 0.7f; // 0-0.7 arası rastgele değer
			_ambition = 0.3f + GD.Randf() * 0.7f; // 0.3-1.0 arası rastgele değer
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Başlangıç değerleri
			_fatigueResistance = 0.3f + GD.Randf() * 0.4f; // 0.3-0.7 arası rastgele değer
			
			// Trait'lere göre değerleri düzenle
			AdjustValuesByTraits();
			
			GD.Print($"Konsomatris {Name} initialized with attractiveness: {_attractiveness}, jealousy: {_jealousy}, ambition: {_ambition}");
		}
		
		// Trait'lere göre değerleri düzenle
		private void AdjustValuesByTraits()
		{
			foreach (string trait in GetTraits())
			{
				switch (trait)
				{
					case "Beautiful":
						_attractiveness = Mathf.Min(1.0f, _attractiveness + 0.2f);
						break;
					case "Charismatic":
						_conversationSkill = Mathf.Min(1.0f, _conversationSkill + 0.15f);
						_persuasionSkill = Mathf.Min(1.0f, _persuasionSkill + 0.15f);
						break;
					case "Jealous": // Özel kons trait'i
						_jealousy = Mathf.Min(1.0f, _jealousy + 0.3f);
						break;
					case "Ambitious": // Özel kons trait'i
						_ambition = Mathf.Min(1.0f, _ambition + 0.3f);
						break;
					case "Talented":
						_danceSkill = Mathf.Min(1.0f, _danceSkill + 0.2f);
						break;
					case "Lazy":
						_ambition = Mathf.Max(0.1f, _ambition - 0.2f);
						break;
					case "Alcoholic":
						_persuasionSkill = Mathf.Max(0.1f, _persuasionSkill - 0.1f);
						_conversationSkill = Mathf.Max(0.1f, _conversationSkill - 0.1f);
						break;
					// ÖZELLİK 5: GELİŞMİŞ TRAIT ETKİLEŞİMLERİ - Yeni trait'ler
					case "Empathetic": // Yeni trait: Empatik
						_conversationSkill = Mathf.Min(1.0f, _conversationSkill + 0.2f);
						_fatigueResistance = Mathf.Min(1.0f, _fatigueResistance + 0.1f);
						break;
					case "Rebellious": // Yeni trait: Asi
						_jealousy = Mathf.Min(1.0f, _jealousy + 0.15f);
						_ambition = Mathf.Min(1.0f, _ambition + 0.2f);
						Loyalty = Mathf.Max(0.1f, Loyalty - 0.15f);
						break;
					case "Athletic": // Yeni trait: Atletik
						_danceSkill = Mathf.Min(1.0f, _danceSkill + 0.15f);
						_fatigueResistance = Mathf.Min(1.0f, _fatigueResistance + 0.2f);
						break;
					case "Extrovert": // Yeni trait: Dışa dönük
						_conversationSkill = Mathf.Min(1.0f, _conversationSkill + 0.2f);
						_stressLevel = Mathf.Max(0f, _stressLevel - 0.2f);
						break;
					case "Introvert": // Yeni trait: İçe dönük
						_danceSkill = Mathf.Min(1.0f, _danceSkill + 0.1f); // Dans ile kendini ifade eder
						_stressLevel = Mathf.Min(1.0f, _stressLevel + 0.2f); // Sosyal ortamda stres yaşar
						break;
				}
			}
		}
		
		// Özel beceri geliştirme - StaffBase.ImproveSkills() metodunu override eder
		public override void ImproveSkills()
		{
			base.ImproveSkills();
			
			// Kons-spesifik becerileri geliştir
			float baseImprovement = 0.005f; // Günlük temel gelişim
			
			// Hırs seviyesi beceri gelişimini etkiler
			float improvementMultiplier = 1.0f + (_ambition - 0.5f);
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Yorgunluk faktörü
			float fatigueModifier = 1.0f - (_fatigue * 0.5f); // Yorgunluk öğrenmeyi %50'ye kadar azaltabilir
			improvementMultiplier *= fatigueModifier;
			
			// Her beceri için rastgele gelişim
			if (GD.Randf() < 0.6f) // %60 ihtimalle dans gelişimi
			{
				_danceSkill = Mathf.Min(1.0f, _danceSkill + baseImprovement * improvementMultiplier);
				if (_skills.ContainsKey("dancing")) 
					_skills["dancing"] = _danceSkill;
			}
			
			if (GD.Randf() < 0.7f) // %70 ihtimalle konuşma gelişimi
			{
				_conversationSkill = Mathf.Min(1.0f, _conversationSkill + baseImprovement * improvementMultiplier);
				if (_skills.ContainsKey("conversation")) 
					_skills["conversation"] = _conversationSkill;
			}
			
			if (GD.Randf() < 0.5f) // %50 ihtimalle ikna gelişimi
			{
				_persuasionSkill = Mathf.Min(1.0f, _persuasionSkill + baseImprovement * improvementMultiplier);
				if (_skills.ContainsKey("persuasion")) 
					_skills["persuasion"] = _persuasionSkill;
			}
		}
		
		// Müşteri ile etkileşim - StaffBase.InteractWithCustomer() metodunu override eder
		public override void InteractWithCustomer(Node3D customer)
		{
			// ÖZELLİK 6: İŞLEM GÜVENLİĞİ VE HATA YÖNETİMİ
			if (customer == null)
			{
				GD.PrintErr($"Konsomatris {Name}: Null customer reference in InteractWithCustomer");
				return;
			}
			
			_currentCustomer = customer;
			
			// Etkileşim durumuna geç
			SetActivity(ActivityState.Talking);
			
			// Müşteri memnuniyeti hesapla
			float satisfactionBonus = CalculateCustomerSatisfaction(customer);
			_customerSatisfactionBonus = satisfactionBonus;
			
			// Potansiyel bahşiş hesaplama
			_tipBonus = CalculateTipBonus(customer);
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ
			AdjustEnergy(-0.02f, "Customer Interaction");
			IncreaseFatigue(0.01f); // Her müşteri etkileşiminde yorgunluk artar
			IncreaseStress(0.005f); // Hafif stres artışı
			
			// ÖZELLİK 7: PERFORMANS VE İSTATİSTİK TAKİBİ
			_totalCustomersServed++;
			
			// Satılan içki sayısını artırma şansı
			if (GD.Randf() < _persuasionSkill)
			{
				_drinksSold++;
			}
			
			// ÖZELLİK 4: ÖZEL ETKİLEŞİM VE MİNİ OYUNLAR - Rastgele mini oyun başlatma şansı
			TryStartMiniGame();
			
			GD.Print($"Konsomatris {Name} is serving a customer. Satisfaction bonus: {satisfactionBonus}, tip bonus: {_tipBonus}");
		}
		
		// Müşteri memnuniyeti hesaplama
		private float CalculateCustomerSatisfaction(Node3D customer)
		{
			// ÖZELLİK 6: İŞLEM GÜVENLİĞİ VE HATA YÖNETİMİ
			if (customer == null) return 0f;
			
			// Beceri ve özelliklere dayalı memnuniyet hesaplama
			float baseSatisfaction = (_conversationSkill + _danceSkill + _attractiveness + _persuasionSkill) / 4.0f;
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ
			// Enerji, ruh hali ve yorgunluk faktörü
			float conditionFactor = (Energy + Mood - _fatigue - _stressLevel * 0.5f) / 2.0f;
			conditionFactor = Mathf.Clamp(conditionFactor, 0.2f, 1.5f); // Çok düşük veya yüksek değerlere izin verme
			
			// Müşteri talebi faktörü (gerçek uygulamada müşteri sınıfından alınacak)
			float customerPreferenceFactor = 1.0f; // Varsayılan
			
			// ÖZELLİK 6: İŞLEM GÜVENLİĞİ VE HATA YÖNETİMİ
			// Customer sınıfında PreferenceLevel metodu varsa çağır
			try 
			{
				if (customer.HasMethod("GetPreferenceLevel"))
				{
					customerPreferenceFactor = (float)customer.Call("GetPreferenceLevel", StaffTypeString);
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"Konsomatris {Name}: Error calling GetPreferenceLevel: {e.Message}");
				// Hata durumunda varsayılan değeri kullan
			}
			
			// Son memnuniyet puanı
			return baseSatisfaction * conditionFactor * customerPreferenceFactor;
		}
		
		// Bahşiş bonusu hesaplama
		private float CalculateTipBonus(Node3D customer)
		{
			// ÖZELLİK 6: İŞLEM GÜVENLİĞİ VE HATA YÖNETİMİ
			if (customer == null) return 0f;
			
			// Beceri ve özelliklere dayalı bahşiş faktörü
			float baseTipFactor = (_conversationSkill * 0.3f + _danceSkill * 0.2f + _attractiveness * 0.4f + _persuasionSkill * 0.3f) / 1.2f;
			
			// Müşteri cömertlik faktörü (gerçek uygulamada müşteri sınıfından alınacak)
			float customerGenerosity = 1.0f; // Varsayılan
			
			// ÖZELLİK 6: İŞLEM GÜVENLİĞİ VE HATA YÖNETİMİ
			try 
			{
				// Customer sınıfında GetGenerosity metodu varsa çağır
				if (customer.HasMethod("GetGenerosity"))
				{
					customerGenerosity = (float)customer.Call("GetGenerosity");
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"Konsomatris {Name}: Error calling GetGenerosity: {e.Message}");
				// Hata durumunda varsayılan değeri kullan
			}
			
			// Müşteri bütçesi faktörü
			float customerBudget = 1000.0f; // Varsayılan
			
			// ÖZELLİK 6: İŞLEM GÜVENLİĞİ VE HATA YÖNETİMİ
			try 
			{
				// Customer sınıfında GetBudget metodu varsa çağır
				if (customer.HasMethod("GetBudget"))
				{
					customerBudget = (float)customer.Call("GetBudget");
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"Konsomatris {Name}: Error calling GetBudget: {e.Message}");
				// Hata durumunda varsayılan değeri kullan
			}
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Yorgunluk bahşişi etkiler
			float fatigueModifier = 1.0f - (_fatigue * 0.3f);
			
			// Son bahşiş bonusu (müşteri bütçesinin bir yüzdesi)
			return customerBudget * baseTipFactor * customerGenerosity * 0.1f * fatigueModifier; // Maksimum bütçenin %10'u
		}
		
		// Bahşiş toplama
		public void CollectTip(float amount)
		{
			if (amount <= 0) return;
			
			float tipShare = amount * TipPercentage;
			_totalTipsEarned += tipShare;
			
			// Bahşiş toplamı
			ReceiveTips(tipShare);
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Bahşiş moralini yükseltir
			float moodBoost = Mathf.Min(tipShare / 500, 0.1f); // Maksimum 0.1 moral artışı
			AdjustMood(moodBoost, "Got Tips");
			
			GD.Print($"Konsomatris {Name} collected {tipShare} tip from total {amount}");
		}
		
		// Özel performans davranışı - StaffBase.PerformSpecialBehavior() metodunu override eder
		public override void PerformSpecialBehavior()
		{
			// Dans performansı başlat
			StartPerformance();
		}
		
		// Dans performansı başlat
		public void StartPerformance()
		{
			if (_isPerforming) return;
			
			_isPerforming = true;
			SetActivity(ActivityState.Special);
			
			// Performans kalitesini hesapla
			_performanceQuality = CalculatePerformanceQuality();
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ
			IncreaseFatigue(0.05f); // Performans başlatma yorgunluk artışı
			
			// ÖZELLİK 3: EK ANİMASYON VE EFEKTLER
			TriggerPerformanceEffects();
			
			// ÖZELLİK 7: PERFORMANS VE İSTATİSTİK TAKİBİ
			_totalPerformancesGiven++;
			
			// Performans başladı sinyali gönder
			EmitSignal(SignalName.PerformanceStarted, _performanceQuality);
			
			GD.Print($"Konsomatris {Name} started a dance performance with quality: {_performanceQuality}");
		}
		
		// ÖZELLİK 1: DİNAMİK İLİŞKİ VE İŞBİRLİĞİ YÖNETİMİ
		// Ortak performans başlatma
		public void StartJointPerformance(Konsomatris otherKons)
		{
			if (_isPerforming || otherKons == null || otherKons._isPerforming) return;
			
			// İlişki seviyesi kontrol et
			string otherKonsId = otherKons.StaffID;
			if (!_relationWithOtherKons.ContainsKey(otherKonsId))
			{
				_relationWithOtherKons[otherKonsId] = 0.5f; // Başlangıç değeri
			}
			
			float relationLevel = _relationWithOtherKons[otherKonsId];
			
			// İlişki seviyesi yeterince yüksekse ortak performans yapabilir
			if (relationLevel >= 0.7f)
			{
				_isPerforming = true;
				otherKons._isPerforming = true;
				
				SetActivity(ActivityState.Special);
				otherKons.SetActivity(ActivityState.Special);
				
				// Performans kalitelerini hesapla ve birleştir
				int myQuality = CalculatePerformanceQuality();
				int otherQuality = otherKons.CalculatePerformanceQuality();
				int combinedQuality = (int)((myQuality + otherQuality) * 1.2f / 2); // Ortak performans bonusu
				
				_performanceQuality = combinedQuality;
				otherKons._performanceQuality = combinedQuality;
				
				// ÖZELLİK 3: EK ANİMASYON VE EFEKTLER - Ortak performans efektleri
				TriggerJointPerformanceEffects(otherKons);
				
				// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ
				IncreaseFatigue(0.03f); // Ortak performans normal performanstan daha az yorucu
				otherKons.IncreaseFatigue(0.03f);
				
				// İlişkiyi iyileştir
				_relationWithOtherKons[otherKonsId] = Mathf.Min(1.0f, relationLevel + 0.05f);
				if (otherKons._relationWithOtherKons.ContainsKey(StaffID))
				{
					otherKons._relationWithOtherKons[StaffID] = Mathf.Min(1.0f, otherKons._relationWithOtherKons[StaffID] + 0.05f);
				}
				else
				{
					otherKons._relationWithOtherKons[StaffID] = Mathf.Min(1.0f, 0.7f + 0.05f);
				}
				
				// ÖZELLİK 7: PERFORMANS VE İSTATİSTİK TAKİBİ
				_jointPerformancesGiven++;
				otherKons._jointPerformancesGiven++;
				
				// Sinyal gönder
				EmitSignal(SignalName.JointPerformanceStarted, otherKonsId, combinedQuality);
				
				GD.Print($"Konsomatris {Name} and {otherKons.Name} started a joint performance with quality: {combinedQuality}");
			}
			else
			{
				GD.Print($"Konsomatris {Name} and {otherKons.Name} relation level too low for joint performance: {relationLevel}");
			}
		}
		
		// Müşteri paylaşımı - ÖZELLİK 1: DİNAMİK İLİŞKİ VE İŞBİRLİĞİ YÖNETİMİ
		public void ShareCustomerWith(Konsomatris otherKons)
		{
			if (otherKons == null || _currentCustomer == null) return;
			
			string otherKonsId = otherKons.StaffID;
			if (!_relationWithOtherKons.ContainsKey(otherKonsId))
			{
				_relationWithOtherKons[otherKonsId] = 0.5f;
			}
			
			float relationLevel = _relationWithOtherKons[otherKonsId];
			
			// İlişki seviyesi yeterince yüksekse müşteri paylaşımı yapabilir
			if (relationLevel >= 0.6f)
			{
				// Müşteri her iki konsa da hizmet ücreti ödeyecek, ikisi de bahşiş alacak
				otherKons._currentCustomer = _currentCustomer;
				
				// İşbirliği bonusu - her iki kons da ekstra bahşiş alır
				float collaborationBonus = 0.2f; // %20 bonus
				_tipBonus *= (1 + collaborationBonus);
				otherKons._tipBonus = _tipBonus * 0.8f; // Biraz daha az
				
				// İlişkiyi iyileştir
				_relationWithOtherKons[otherKonsId] = Mathf.Min(1.0f, relationLevel + 0.02f);
				if (otherKons._relationWithOtherKons.ContainsKey(StaffID))
				{
					otherKons._relationWithOtherKons[StaffID] = Mathf.Min(1.0f, otherKons._relationWithOtherKons[StaffID] + 0.02f);
				}
				else
				{
					otherKons._relationWithOtherKons[StaffID] = Mathf.Min(1.0f, 0.6f + 0.02f);
				}
				
				// Moral artışı
				AdjustMood(0.02f, "Customer Sharing");
				otherKons.AdjustMood(0.03f, "Got Shared Customer");
				
				GD.Print($"Konsomatris {Name} shared customer with {otherKons.Name}, bonus: {collaborationBonus}");
			}
			else
			{
				GD.Print($"Konsomatris {Name} and {otherKons.Name} relation level too low for customer sharing: {relationLevel}");
			}
		}
		
		// Performans kalitesi hesaplama
		private int CalculatePerformanceQuality()
		{
			// Dans becerisi, çekicilik, enerji ve ruh haline dayalı performans kalitesi
			float qualityBase = (_danceSkill * 0.5f + _attractiveness * 0.2f + Energy * 0.15f + Mood * 0.15f);
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Yorgunluk ve stres faktörü
			float fatigueModifier = 1.0f - (_fatigue * 0.5f); // Yorgunluk performansı %50'ye kadar azaltabilir
			float stressModifier = 1.0f - (_stressLevel * 0.3f); // Stres performansı %30'a kadar azaltabilir
			
			qualityBase *= fatigueModifier * stressModifier;
			
			// 1-5 arası kalite puanı
			return (int)(qualityBase * 5f) + 1;
		}
		
		// Dans performansı bitir
		public void EndPerformance()
		{
			if (!_isPerforming) return;
			
			_isPerforming = false;
			SetActivity(ActivityState.Idle);
			
			// Müşteri memnuniyeti hesapla
			float customerSatisfaction = _performanceQuality / 5.0f;
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ
			AdjustEnergy(-0.1f, "Dance Performance");
			IncreaseFatigue(0.1f); // Performans sonu yorgunluk artışı
			
			// ÖZELLİK 7: PERFORMANS VE İSTATİSTİK TAKİBİ
			float performanceTime = 5f + _performanceQuality * 2; // Performans süresi 5-15 dakika arası
			_totalPerformanceTime += performanceTime;
			
			// Ortalama performans puanını güncelle
			_averagePerformanceRating = (_averagePerformanceRating * (_totalPerformancesGiven - 1) + customerSatisfaction) / _totalPerformancesGiven;
			
			// Deneyim puanı kazan
			AddExperience(2);
			
			// Performans bitti sinyali gönder
			EmitSignal(SignalName.PerformanceEnded, customerSatisfaction);
			
			GD.Print($"Konsomatris {Name} ended dance performance. Customer satisfaction: {customerSatisfaction}, duration: {performanceTime} minutes");
		}
		
		// Kıskanç davranış - diğer konslarla rekabet
		public void CompeteWithOtherKons(Konsomatris otherKons)
		{
			if (otherKons == null) return;
			
			// Kıskançlık seviyesi rekabet ihtimalini belirler
			if (GD.Randf() > _jealousy) return;
			
			string otherKonsId = otherKons.StaffID;
			
			// Diğer konsla ilişki seviyesini belirle veya başlat
			if (!_relationWithOtherKons.ContainsKey(otherKonsId))
			{
				_relationWithOtherKons[otherKonsId] = 0.5f; // Nötr başlangıç
			}
			
			// İlişki seviyesi rekabet davranışını etkiler
			float relationLevel = _relationWithOtherKons[otherKonsId];
			
			// ÖZELLİK 1: DİNAMİK İLİŞKİ VE İŞBİRLİĞİ YÖNETİMİ
			if (relationLevel > 0.7f)
			{
				// Yüksek ilişki = rekabet yerine işbirliği yap
				float randomChoice = GD.Randf();
				if (randomChoice < 0.4f && _currentCustomer != null)
				{
					ShareCustomerWith(otherKons);
				}
				else if (randomChoice < 0.7f && !_isPerforming && !otherKons._isPerforming)
				{
					StartJointPerformance(otherKons);
				}
			}
			// Düşük ilişki = daha agresif rekabet
			else if (relationLevel < 0.3f)
			{
				// Agresif rekabet - müşteri çalmaya çalışma
				StealCustomer(otherKons);
			}
			else if (relationLevel < 0.7f)
			{
				// Hafif rekabet - sadece gösteri ile dikkat çekme
				StartPerformance();
			}
			
			GD.Print($"Konsomatris {Name} competing with {otherKons.Name}. Relation level: {relationLevel}");
		}
		
		// Başka konstan müşteri çalma
		private void StealCustomer(Konsomatris otherKons)
		{
			// ÖZELLİK 6: İŞLEM GÜVENLİĞİ VE HATA YÖNETİMİ
			if (otherKons == null) return;
			
			// Bayrımdaki müşteri
			Node3D otherCustomer = otherKons._currentCustomer;
			if (otherCustomer == null) return;
			
			// Çalma başarı şansı: ikna becerisi + hırs + çekicilik vs. diğer konsun sadakat + ikna
			float myScore = _persuasionSkill * 0.4f + _ambition * 0.3f + _attractiveness * 0.3f;
			float otherScore = otherKons._persuasionSkill * 0.3f + otherKons.Loyalty * 0.4f + otherKons._attractiveness * 0.3f;
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Yorgunluk ve stres faktörü
			myScore *= (1.0f - _fatigue * 0.3f) * (1.0f - _stressLevel * 0.2f);
			otherScore *= (1.0f - otherKons._fatigue * 0.3f) * (1.0f - otherKons._stressLevel * 0.2f);
			
			// Rastgele başarı faktörü
			float successFactor = GD.Randf();
			
			// Başarı kontrolü
			if (myScore * successFactor > otherScore)
			{
				// Müşteriyi çalma başarılı
				otherKons._currentCustomer = null;
				_currentCustomer = otherCustomer;
				
				// İlişkiyi kötüleştir
				_relationWithOtherKons[otherKons.StaffID] = Mathf.Max(0.0f, _relationWithOtherKons[otherKons.StaffID] - 0.1f);
				if (otherKons._relationWithOtherKons.ContainsKey(StaffID))
				{
					otherKons._relationWithOtherKons[StaffID] = Mathf.Max(0.0f, otherKons._relationWithOtherKons[StaffID] - 0.15f);
				}
				
				// Ruh halini iyileştir
				AdjustMood(0.05f, "Successful Competition");
				// Diğer konsun ruh halini kötüleştir
				otherKons.AdjustMood(-0.1f, "Customer Stolen");
				
				// Stres azalt, diğer konsun stresini artır
				ReduceStress(0.05f);
				otherKons.IncreaseStress(0.1f);
				
				GD.Print($"Konsomatris {Name} successfully stole a customer from {otherKons.Name}!");
			}
			else
			{
				// Başarısız çalma girişimi
				// Ruh halini kötüleştir
				AdjustMood(-0.05f, "Failed Competition");
				// Stres artır
				IncreaseStress(0.05f);
				
				GD.Print($"Konsomatris {Name} failed to steal a customer from {otherKons.Name}.");
			}
		}
		
		// Çıkışta tahsil edilmek üzere kons masası açma
		public void OpenKonsTable(float minimumCharge)
		{
			// ÖZELLİK 6: İŞLEM GÜVENLİĞİ VE HATA YÖNETİMİ
			if (_currentCustomer == null)
			{
				GD.PrintErr($"Konsomatris {Name}: Cannot open kons table - no current customer");
				return;
			}
			
			// Müşteri ödeme yeteneği kontrolü
			bool canPay = true;
			
			try
			{
				// Customer sınıfında CanAfford metodu varsa çağır
				if (_currentCustomer.HasMethod("CanAfford"))
				{
					canPay = (bool)_currentCustomer.Call("CanAfford", minimumCharge);
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"Konsomatris {Name}: Error calling CanAfford: {e.Message}");
				// Hata durumunda varsayılan değeri kullan
			}
			
			if (canPay)
			{
				try
				{
					// Müşteriye masa açma
					if (_currentCustomer.HasMethod("AddKonsTableCharge"))
					{
						_currentCustomer.Call("AddKonsTableCharge", minimumCharge, StaffID);
						
						// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ
						IncreaseFatigue(0.02f); // Masa açmak az da olsa yorar
						
						GD.Print($"Konsomatris {Name} opened a kons table for {minimumCharge}");
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"Konsomatris {Name}: Error calling AddKonsTableCharge: {e.Message}");
				}
			}
			else
			{
				GD.Print($"Customer cannot afford kons table minimum charge of {minimumCharge}");
				
				// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Başarısız masa açma stres yaratır
				IncreaseStress(0.05f);
				AdjustMood(-0.02f, "Table Opening Failed");
			}
		}
		
		// Özel animasyon - StaffBase.PlaySpecialAnimation() metodunu override eder
		protected override void PlaySpecialAnimation()
		{
			// Dans animasyonu oynat
			PlayAnimation("dance");
			
			// ÖZELLİK 3: EK ANİMASYON VE EFEKTLER
			if (_isPerforming && _performanceQuality >= 4)
			{
				// Yüksek kaliteli performans ek animasyonları
				PlayAnimation("dance_special");
			}
		}
		
		// ÖZELLİK 3: EK ANİMASYON VE EFEKTLER - Performans efektleri tetikleme
		private void TriggerPerformanceEffects()
		{
			// Performans kalitesine bağlı efektler
			string effectType = "standard";
			int intensity = _performanceQuality;
			
			if (_performanceQuality >= 4)
			{
				effectType = "highlight";
				intensity += 1;
			}
			
			if (_performanceQuality == 5)
			{
				effectType = "spectacular";
				intensity += 2;
			}
			
			// Efekt sinyali gönder
			EmitSignal(SignalName.SpecialEffectTriggered, effectType, intensity);
			
			GD.Print($"Konsomatris {Name} triggered {effectType} effect with intensity {intensity}");
		}
		
		// ÖZELLİK 3: EK ANİMASYON VE EFEKTLER - Ortak performans efektleri
		private void TriggerJointPerformanceEffects(Konsomatris otherKons)
		{
			// Ortak performans için özel efektler
			string effectType = "joint_dance";
			int intensity = _performanceQuality;
			
			// Performans kalitesi 4 ve üstüyse daha yoğun efektler
			if (_performanceQuality >= 4)
			{
				effectType = "joint_spectacular";
				intensity += 2;
			}
			
			// Efekt sinyali gönder
			EmitSignal(SignalName.SpecialEffectTriggered, effectType, intensity);
			otherKons.EmitSignal(SignalName.SpecialEffectTriggered, effectType, intensity);
			
			GD.Print($"Konsomatris {Name} and {otherKons.Name} triggered {effectType} effect with intensity {intensity}");
		}
		
		// ÖZELLİK 4: ÖZEL ETKİLEŞİM VE MİNİ OYUNLAR - Mini oyun başlatma denemesi
		private void TryStartMiniGame()
		{
			if (_currentCustomer == null || _isMiniGameActive || GD.Randf() > 0.3f) return; // %30 şans
			
			string[] miniGameTypes = {"dance_rhythm", "conversation_challenge", "drink_mixing"};
			int randomIndex = (int)(GD.Randf() * miniGameTypes.Length);
			_currentMiniGameType = miniGameTypes[randomIndex];
			
			_isMiniGameActive = true;
			_miniGameScore = 0f;
			
			// Mini oyun başlatma sinyali gönder
			EmitSignal(SignalName.MiniGameStarted, _currentMiniGameType);
			
			GD.Print($"Konsomatris {Name} started mini-game: {_currentMiniGameType}");
		}
		
		// ÖZELLİK 4: ÖZEL ETKİLEŞİM VE MİNİ OYUNLAR - Mini oyun sonlandırma
		public void EndMiniGame(float score)
		{
			if (!_isMiniGameActive) return;
			
			_isMiniGameActive = false;
			_miniGameScore = score;
			
			// Başarı kontrolü
			bool success = score >= 0.6f; // %60 ve üzeri başarılı
			
			// Başarı durumuna göre bonuslar
			if (success)
			{
				// Beceri bonusu
				float skillBonus = score * 0.01f; // Maksimum 0.01 beceri artışı
				
				switch (_currentMiniGameType)
				{
					case "dance_rhythm":
						_danceSkill = Mathf.Min(1.0f, _danceSkill + skillBonus);
						break;
					case "conversation_challenge":
						_conversationSkill = Mathf.Min(1.0f, _conversationSkill + skillBonus);
						break;
					case "drink_mixing":
						_persuasionSkill = Mathf.Min(1.0f, _persuasionSkill + skillBonus);
						_drinksSold++; // Ekstra içki satışı
						break;
				}
				
				// Moral artışı
				AdjustMood(0.05f, "Mini-Game Success");
				
				// Müşteri memnuniyeti bonusu
				_customerSatisfactionBonus += score * 0.2f;
				
				// Tip bonusu
				_tipBonus *= (1.0f + score * 0.3f);
			}
			else
			{
				// Başarısızlık durumu - hafif moral düşüşü
				AdjustMood(-0.02f, "Mini-Game Failure");
			}
			
			// Mini oyun bitti sinyali gönder
			EmitSignal(SignalName.MiniGameEnded, score, success);
			
			GD.Print($"Konsomatris {Name} ended mini-game: {_currentMiniGameType}, score: {score}, success: {success}");
		}
		
		// Seviye atlama özel efektleri - StaffBase.OnLevelUp() metodunu override eder
		protected override void OnLevelUp()
		{
			base.OnLevelUp();
			
			// Dans becerisi gelişimi
			_danceSkill = Mathf.Min(1.0f, _danceSkill + 0.05f);
			
			// Konuşma becerisi gelişimi
			_conversationSkill = Mathf.Min(1.0f, _conversationSkill + 0.04f);
			
			// İkna becerisi gelişimi
			_persuasionSkill = Mathf.Min(1.0f, _persuasionSkill + 0.03f);
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Yorgunluğa direnç artışı
			_fatigueResistance = Mathf.Min(1.0f, _fatigueResistance + 0.02f);
			
			// Beceri değerlerini skills sözlüğüne aktar
			if (_skills.ContainsKey("dancing")) _skills["dancing"] = _danceSkill;
			if (_skills.ContainsKey("conversation")) _skills["conversation"] = _conversationSkill;
			if (_skills.ContainsKey("persuasion")) _skills["persuasion"] = _persuasionSkill;
			
			// ÖZELLİK 3: EK ANİMASYON VE EFEKTLER - Seviye atlama efekti
			EmitSignal(SignalName.SpecialEffectTriggered, "level_up", Level);
			
			GD.Print($"Konsomatris {Name} leveled up: dance {_danceSkill}, conversation {_conversationSkill}, persuasion {_persuasionSkill}, fatigue resistance {_fatigueResistance}");
		}
		
		// Günlük güncelleme - StaffBase.UpdateDaily() metodunu override eder
		public override void UpdateDaily()
		{
			base.UpdateDaily();
			
			// İstatistik sıfırlama
			_drinksSold = 0;
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Günlük yorgunluk iyileşmesi
			float fatigueRecovery = 0.5f + _fatigueResistance * 0.3f; // 0.5-0.8 arası iyileşme
			_fatigue = Mathf.Max(0f, _fatigue - fatigueRecovery);
			
			// Stres azaltma
			ReduceStress(0.3f); // Günlük stres azalması
			
			GD.Print($"Konsomatris {Name} daily update: fatigue {_fatigue}, stress {_stressLevel}");
		}
		
		// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Yorgunluk artırma
		public void IncreaseFatigue(float amount)
		{
			if (amount <= 0) return;
			
			// Yorgunluğa direnç faktörü
			float resistanceFactor = 1.0f - _fatigueResistance * 0.5f; // Direnç yorgunluğu %50'ye kadar azaltabilir
			float actualIncrease = amount * resistanceFactor;
			
			_fatigue = Mathf.Min(1.0f, _fatigue + actualIncrease);
			
			// Çok yüksek yorgunluk ruh halini etkiler
			if (_fatigue > 0.7f && GD.Randf() < 0.3f) // %30 şans
			{
				AdjustMood(-0.05f, "Extreme Fatigue");
			}
			
			// Yorgunluk seviyesine göre log
			if (_fatigue > 0.8f)
			{
				GD.Print($"Konsomatris {Name} is extremely fatigued: {_fatigue}");
			}
		}
		
		// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Stres artırma
		public void IncreaseStress(float amount)
		{
			if (amount <= 0) return;
			
			// Stres direnci için karakter trait/özellikleri buraya eklenebilir
			_stressLevel = Mathf.Min(1.0f, _stressLevel + amount);
			
			// Yüksek stres ruh halini olumsuz etkiler
			if (_stressLevel > 0.7f && GD.Randf() < 0.4f) // %40 şans
			{
				AdjustMood(-0.08f, "High Stress");
			}
			
			if (_stressLevel > 0.9f)
			{
				GD.Print($"Konsomatris {Name} is under severe stress: {_stressLevel}");
			}
		}
		
		// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Stres azaltma
		public void ReduceStress(float amount)
		{
			if (amount <= 0) return;
			
			_stressLevel = Mathf.Max(0f, _stressLevel - amount);
			
			// Stres azalması ruh halini iyileştirebilir
			if (amount > 0.2f) // Önemli miktarda stres azalması
			{
				AdjustMood(0.05f, "Stress Relief");
			}
		}
		
		// Özel risk faktörleri - StaffBase.ApplySpecialRiskFactors() metodunu override eder
		protected override void ApplySpecialRiskFactors()
		{
			base.ApplySpecialRiskFactors();
			
			// Kons-spesifik risk faktörleri
			
			// Yüksek hırs ve düşük bağlılık = rakip işletmelere gitme riski
			if (_ambition > 0.7f && Loyalty < 0.4f)
			{
				_disloyaltyRisk += 0.1f;
			}
			
			// Yüksek kıskançlık = daha fazla sorun çıkarma riski
			if (_jealousy > 0.7f)
			{
				_disloyaltyRisk += 0.05f;
			}
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Kronik stres ve yorgunluk riski artırır
			if (_fatigue > 0.8f && _stressLevel > 0.7f)
			{
				_disloyaltyRisk += 0.15f;
				GD.Print($"Konsomatris {Name} disloyalty risk increased due to fatigue and stress");
			}
		}
		
		// Özel performans faktörü hesaplama - StaffBase.CalculateSpecialPerformanceFactor() metodunu override eder
		protected override float CalculateSpecialPerformanceFactor()
		{
			// Dans ve çekicilik yetenek faktörü
			float baseFactor = (_danceSkill * 0.5f + _attractiveness * 0.5f - 0.5f) * 0.2f;
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ - Yorgunluk ve stres faktörü
			float conditionModifier = 1.0f - (_fatigue * 0.4f) - (_stressLevel * 0.2f);
			conditionModifier = Mathf.Clamp(conditionModifier, 0.5f, 1.2f);
			
			return baseFactor * conditionModifier;
		}
		
		// Özellik değerlerini döndür
		public new Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = base.GetStats();
			
			// Kons-spesifik değerleri ekle
			stats["DanceSkill"] = _danceSkill;
			stats["ConversationSkill"] = _conversationSkill; 
			stats["Attractiveness"] = _attractiveness;
			stats["PersuasionSkill"] = _persuasionSkill;
			stats["Jealousy"] = _jealousy;
			stats["Ambition"] = _ambition;
			stats["TotalTipsEarned"] = _totalTipsEarned;
			stats["DrinksSold"] = _drinksSold;
			
			// ÖZELLİK 2: YORGUNLUK VE ENERJİ YÖNETİMİ
			stats["Fatigue"] = _fatigue;
			stats["FatigueResistance"] = _fatigueResistance;
			stats["StressLevel"] = _stressLevel;
			
			// ÖZELLİK 7: PERFORMANS VE İSTATİSTİK TAKİBİ
			stats["TotalPerformances"] = _totalPerformancesGiven;
			stats["JointPerformances"] = _jointPerformancesGiven;
			stats["TotalPerformanceTime"] = _totalPerformanceTime;
			stats["AveragePerformanceRating"] = _averagePerformanceRating;
			stats["TotalCustomersServed"] = _totalCustomersServed;
			
			return stats;
		}
	}
}
