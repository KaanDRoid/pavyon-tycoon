using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Staff
{
	
	public partial class Cleaner : StaffBase
	{
		// Temizlikçi özellikleri
		private float _thoroughnessSkill = 0.5f;     // Titizlik becerisi
		private float _speedSkill = 0.5f;            // Hız becerisi
		private float _discretionSkill = 0.5f;       // Gizlilik becerisi
		private float _maintenanceSkill = 0.5f;      // Bakım becerisi
		
		// Temizlik alanları
		public enum CleaningArea
		{
			Floor,          // Zemin
			Tables,         // Masalar
			Bar,            // Bar
			Bathroom,       // Tuvaletler
			Stage,          // Sahne
			VIPArea,        // VIP Alanı
			Kitchen,        // Mutfak
			Entrance,       // Giriş
			IllegalFloor,   // Kaçak Kat
			Outside         // Dış Alan
		}
		
		// Temizlik türleri
		public enum CleaningType
		{
			Regular,        // Düzenli temizlik
			DeepCleaning,   // Detaylı temizlik
			Emergency,      // Acil durum temizliği (kusma, kırık bardak, vs.)
			Maintenance,    // Küçük tamirat ve bakım
			Disposal        // Atık imhası
		}
		
		// Alan temizlik seviyeleri (0-1)
		private Dictionary<CleaningArea, float> _areaCleanlinessLevels = new Dictionary<CleaningArea, float>();
		
		// Temizlik beceri seviyeleri (0-1)
		private Dictionary<CleaningType, float> _cleaningSkillLevels = new Dictionary<CleaningType, float>();
		
		// Alan kirlilik hızları (günlük kirlenme oranı)
		private Dictionary<CleaningArea, float> _areaDirtyingRates = new Dictionary<CleaningArea, float>();
		
		// Temizlik aktivitesi
		private bool _isCleaning = false;               // Şu anda temizlik yapıyor mu
		private bool _isHandlingEmergency = false;      // Acil durum temizliği yapıyor mu
		private CleaningArea _currentArea = CleaningArea.Floor; // Şu anda temizlediği alan
		private CleaningType _currentCleaningType = CleaningType.Regular; // Mevcut temizlik türü
		private float _currentCleaningProgress = 0.0f;  // Mevcut temizlik işi ilerleme durumu
		private float _currentCleaningTotal = 100.0f;   // Mevcut temizlik işi toplam gereksinim
		
		// Temizlik talepleri
		private Queue<CleaningTask> _pendingTasks = new Queue<CleaningTask>(); // Bekleyen temizlik görevleri
		private int _maxSimultaneousTasks = 1;          // Aynı anda yapabildiği görev sayısı (genelde 1)
		private int _emergenciesHandled = 0;            // Halledilen acil durum sayısı
		
		// İşyeri temizliği etkileri
		private float _customerSatisfactionEffect = 0.0f; // Müşteri memnuniyeti etkisi
		private float _staffMoraleEffect = 0.0f;         // Personel morali etkisi
		private float _hygieneRating = 0.7f;             // Genel temizlik değerlendirmesi (0-1)
		
		// Temizlik ekipmanı ve malzemeleri
		private float _equipmentQuality = 0.5f;         // Ekipman kalitesi (0-1)
		private Dictionary<string, int> _supplies = new Dictionary<string, int>(); // Temizlik malzemeleri ve miktarları
		
		// İstatistikler
		private int _areasCleanedToday = 0;            // Bugün temizlenen alan sayısı
		private int _deepCleaningsPerformed = 0;       // Yapılan derin temizlik sayısı
		private int _tasksRejected = 0;                // Reddedilen temizlik talebi sayısı
		private int _objectsFixed = 0;                 // Tamir edilen obje sayısı
		
		// Signals
		[Signal]
		public delegate void CleaningCompletedEventHandler(string area, float newCleanliness);
		
		[Signal]
		public delegate void EmergencyHandledEventHandler(string area, float timeSpent);
		
		[Signal]
		public delegate void TaskRejectedEventHandler(string area, string reason);
		
		[Signal]
		public delegate void HygieneRatingChangedEventHandler(float previousRating, float newRating);
		
		public override void _Ready()
		{
			base._Ready();
			
			// Tipi ayarla
			Type = StaffType.Cleaner;
			
			// Beceri değerlerini başlat (StaffBase'deki skills sözlüğünden al)
			if (_skills.ContainsKey("thoroughness")) _thoroughnessSkill = _skills["thoroughness"];
			if (_skills.ContainsKey("speed")) _speedSkill = _skills["speed"];
			if (_skills.ContainsKey("discretion")) _discretionSkill = _skills["discretion"];
			if (_skills.ContainsKey("maintenance")) _maintenanceSkill = _skills["maintenance"];
			
			// Alan temizlik seviyelerini başlat
			InitializeCleanlinessLevels();
			
			// Temizlik beceri seviyelerini başlat
			InitializeCleaningSkills();
			
			// Alan kirlilik hızlarını başlat
			InitializeDirtyingRates();
			
			// Temizlik malzemelerini başlat
			InitializeSupplies();
			
			// Trait'lere göre değerleri düzenle
			AdjustValuesByTraits();
			
			// Temizlik değerlendirmesini hesapla
			CalculateHygieneRating();
			
			GD.Print($"Cleaner {Name} initialized with thoroughness: {_thoroughnessSkill}, speed: {_speedSkill}");
		}
		
		public override void _Process(double delta)
		{
			base._Process(delta);
			
			// Aktif temizlik işlerini güncelle
			if (_isCleaning)
			{
				UpdateCurrentCleaning(delta);
			}
			// Yeni temizlik işi başlat
			else if (_pendingTasks.Count > 0 && !_isHandlingEmergency)
			{
				StartNextCleaningTask();
			}
			
			// Alanların kirlenme durumunu simüle et
			UpdateAreaDirtiness(delta);
		}
		
		// Alan temizlik seviyelerini başlat
		private void InitializeCleanlinessLevels()
		{
			// Tüm alanlar için başlangıç temizlik seviyeleri
			foreach (CleaningArea area in Enum.GetValues(typeof(CleaningArea)))
			{
				// İşyeri açılışında makul temizlik (0.6-0.8 arası)
				_areaCleanlinessLevels[area] = 0.6f + GD.Randf() * 0.2f;
			}
			
			// Bazı alanların farklı başlangıç seviyesi
			_areaCleanlinessLevels[CleaningArea.Bathroom] = 0.5f + GD.Randf() * 0.3f; // Biraz daha kirli olabilir
			_areaCleanlinessLevels[CleaningArea.IllegalFloor] = 0.4f + GD.Randf() * 0.3f; // Kaçak kat daha kirli
		}
		
		// Temizlik beceri seviyelerini başlat
		private void InitializeCleaningSkills()
		{
			// Tüm temizlik türleri için başlangıç beceri seviyeleri
			foreach (CleaningType type in Enum.GetValues(typeof(CleaningType)))
			{
				// Rastgele başlangıç seviyesi (0.3-0.7 arası)
				_cleaningSkillLevels[type] = 0.3f + GD.Randf() * 0.4f;
			}
			
			// Düzenli temizlik en iyi olduğu alan
			_cleaningSkillLevels[CleaningType.Regular] = 0.5f + GD.Randf() * 0.3f;
		}
		
		// Alan kirlilik hızlarını başlat
		private void InitializeDirtyingRates()
		{
			// Her alan için kirlenme hızı (günlük temizlik kaybı)
			_areaDirtyingRates[CleaningArea.Floor] = 0.3f;        // Zemin hızlıca kirlenir
			_areaDirtyingRates[CleaningArea.Tables] = 0.4f;       // Masalar çok hızlı kirlenir
			_areaDirtyingRates[CleaningArea.Bar] = 0.35f;         // Bar hızlı kirlenir
			_areaDirtyingRates[CleaningArea.Bathroom] = 0.5f;     // Tuvaletler çok hızlı kirlenir
			_areaDirtyingRates[CleaningArea.Stage] = 0.2f;        // Sahne orta hızda kirlenir
			_areaDirtyingRates[CleaningArea.VIPArea] = 0.25f;     // VIP alanı daha az kirlenir
			_areaDirtyingRates[CleaningArea.Kitchen] = 0.45f;     // Mutfak hızlı kirlenir
			_areaDirtyingRates[CleaningArea.Entrance] = 0.3f;     // Giriş orta hızda kirlenir
			_areaDirtyingRates[CleaningArea.IllegalFloor] = 0.4f; // Kaçak kat hızlı kirlenir
			_areaDirtyingRates[CleaningArea.Outside] = 0.15f;     // Dış alan yavaş kirlenir
		}
		
		// Temizlik malzemelerini başlat
		private void InitializeSupplies()
		{
			// Temel temizlik malzemeleri
			_supplies["AllPurposeCleaner"] = 100;    // Genel temizleyici (%)
			_supplies["FloorCleaner"] = 100;         // Zemin temizleyici (%)
			_supplies["BathroomCleaner"] = 100;      // Tuvalet temizleyici (%)
			_supplies["GlassCleaner"] = 100;         // Cam temizleyici (%)
			_supplies["DisinfectantSpray"] = 100;    // Dezenfektan sprey (%)
			_supplies["Mop"] = 100;                  // Paspas durumu (%)
			_supplies["Broom"] = 100;                // Süpürge durumu (%)
			_supplies["TrashBags"] = 50;             // Çöp torbası (adet)
			_supplies["Gloves"] = 20;                // Eldiven (adet)
			_supplies["CleaningRags"] = 30;          // Temizlik bezi (adet)
		}
		
		// Trait'lere göre değerleri düzenle
		private void AdjustValuesByTraits()
		{
			foreach (string trait in GetTraits())
			{
				switch (trait)
				{
					case "Professional":
						_thoroughnessSkill = Mathf.Min(1.0f, _thoroughnessSkill + 0.2f);
						_maintenanceSkill = Mathf.Min(1.0f, _maintenanceSkill + 0.1f);
						break;
					case "Experienced":
						_thoroughnessSkill = Mathf.Min(1.0f, _thoroughnessSkill + 0.15f);
						_speedSkill = Mathf.Min(1.0f, _speedSkill + 0.1f);
						break;
					case "FastWorker": // Özel temizlikçi trait'i
						_speedSkill = Mathf.Min(1.0f, _speedSkill + 0.25f);
						_cleaningSkillLevels[CleaningType.Regular] = Mathf.Min(1.0f, _cleaningSkillLevels[CleaningType.Regular] + 0.1f);
						break;
					case "Thorough": // Özel temizlikçi trait'i
						_thoroughnessSkill = Mathf.Min(1.0f, _thoroughnessSkill + 0.25f);
						_cleaningSkillLevels[CleaningType.DeepCleaning] = Mathf.Min(1.0f, _cleaningSkillLevels[CleaningType.DeepCleaning] + 0.2f);
						break;
					case "Resourceful": // Özel temizlikçi trait'i
						_maintenanceSkill = Mathf.Min(1.0f, _maintenanceSkill + 0.25f);
						_cleaningSkillLevels[CleaningType.Maintenance] = Mathf.Min(1.0f, _cleaningSkillLevels[CleaningType.Maintenance] + 0.2f);
						break;
					case "Discreet": // Özel temizlikçi trait'i
						_discretionSkill = Mathf.Min(1.0f, _discretionSkill + 0.3f);
						_cleaningSkillLevels[CleaningType.Disposal] = Mathf.Min(1.0f, _cleaningSkillLevels[CleaningType.Disposal] + 0.2f);
						break;
					case "Handy": // Özel temizlikçi trait'i - tamir uzmanı
						_maintenanceSkill = Mathf.Min(1.0f, _maintenanceSkill + 0.3f);
						break;
					case "QuickResponse": // Özel temizlikçi trait'i - acil durum uzmanı
						_speedSkill = Mathf.Min(1.0f, _speedSkill + 0.15f);
						_cleaningSkillLevels[CleaningType.Emergency] = Mathf.Min(1.0f, _cleaningSkillLevels[CleaningType.Emergency] + 0.3f);
						break;
					case "EconomicalCleaner": // Özel temizlikçi trait'i - malzeme tasarrufu
						// Malzeme tasarrufu sağlayan bir özellik ekleyebiliriz
						break;
					case "Lazy":
						_speedSkill = Mathf.Max(0.1f, _speedSkill - 0.15f);
						_thoroughnessSkill = Mathf.Max(0.1f, _thoroughnessSkill - 0.1f);
						break;
					case "Alcoholic":
						_thoroughnessSkill = Mathf.Max(0.1f, _thoroughnessSkill - 0.1f);
						_maintenanceSkill = Mathf.Max(0.1f, _maintenanceSkill - 0.1f);
						break;
				}
			}
		}
		
		// Özel beceri geliştirme - StaffBase.ImproveSkills() metodunu override eder
		public override void ImproveSkills()
		{
			base.ImproveSkills();
			
			// Temizlikçi-spesifik becerileri geliştir
			float baseImprovement = 0.005f; // Günlük temel gelişim
			
			// Temizlenen alan sayısı gelişim hızını etkiler
			float areaModifier = 1.0f + (Mathf.Min(_areasCleanedToday, 10) * 0.02f);
			
			// Her beceri için rastgele gelişim
			if (GD.Randf() < 0.7f) // %70 ihtimalle titizlik gelişimi
			{
				_thoroughnessSkill = Mathf.Min(1.0f, _thoroughnessSkill + baseImprovement * areaModifier);
				if (_skills.ContainsKey("thoroughness")) 
					_skills["thoroughness"] = _thoroughnessSkill;
			}
			
			if (GD.Randf() < 0.6f) // %60 ihtimalle hız gelişimi
			{
				_speedSkill = Mathf.Min(1.0f, _speedSkill + baseImprovement * areaModifier);
				if (_skills.ContainsKey("speed")) 
					_skills["speed"] = _speedSkill;
			}
			
			if (GD.Randf() < 0.5f) // %50 ihtimalle gizlilik gelişimi
			{
				_discretionSkill = Mathf.Min(1.0f, _discretionSkill + baseImprovement * areaModifier);
				if (_skills.ContainsKey("discretion")) 
					_skills["discretion"] = _discretionSkill;
			}
			
			if (GD.Randf() < 0.4f) // %40 ihtimalle bakım gelişimi
			{
				_maintenanceSkill = Mathf.Min(1.0f, _maintenanceSkill + baseImprovement * areaModifier);
				if (_skills.ContainsKey("maintenance")) 
					_skills["maintenance"] = _maintenanceSkill;
			}
			
			// En sık yaptığı temizlik türünde gelişim
			if (_pendingTasks.Count > 0 && GD.Randf() < 0.8f)
			{
				CleaningType mostUsedType = _pendingTasks.Peek().Type;
				_cleaningSkillLevels[mostUsedType] = Mathf.Min(1.0f, _cleaningSkillLevels[mostUsedType] + baseImprovement * 1.5f);
				
				GD.Print($"Cleaner {Name} improved skill in {mostUsedType} cleaning to {_cleaningSkillLevels[mostUsedType]}");
			}
		}
		
		// Temizlik görev sınıfı
		private class CleaningTask
		{
			public CleaningArea Area { get; set; }
			public CleaningType Type { get; set; }
			public float TimeRequired { get; set; }
			public float TimeSpent { get; set; }
			public bool IsHighPriority { get; set; }
			public string RequesterId { get; set; } // İsteyen kişi/sistem ID'si
			
			public CleaningTask(CleaningArea area, CleaningType type, float timeRequired, string requesterId = "", bool isHighPriority = false)
			{
				Area = area;
				Type = type;
				TimeRequired = timeRequired;
				TimeSpent = 0.0f;
				IsHighPriority = isHighPriority;
				RequesterId = requesterId;
			}
			
			public float GetProgress()
			{
				if (TimeRequired <= 0) return 1.0f;
				return Mathf.Clamp(TimeSpent / TimeRequired, 0.0f, 1.0f);
			}
			
			public bool IsComplete()
			{
				return TimeSpent >= TimeRequired;
			}
		}
		
		// Temizlik görevi al
		public bool TakeCleaningTask(CleaningArea area, CleaningType type, string requesterId = "", bool isHighPriority = false)
		{
			// Acil durum görevi ise ve şu anda acil durum görevi yapıyorsa reddet
			if (type == CleaningType.Emergency && _isHandlingEmergency)
			{
				EmitSignal(SignalName.TaskRejected, area.ToString(), "Already handling an emergency");
				_tasksRejected++;
				GD.Print($"Cleaner {Name} rejected emergency cleaning task for {area} - already handling emergency");
				return false;
			}
			
			// Gerekli temizlik süresi
			float timeRequired = CalculateCleaningTime(area, type);
			
			// Temizlik görevi oluştur
			CleaningTask task = new CleaningTask(area, type, timeRequired, requesterId, isHighPriority);
			
			// Acil durum veya yüksek öncelikli görevler kuyruğun başına
			if (type == CleaningType.Emergency || isHighPriority)
			{
				List<CleaningTask> tempList = new List<CleaningTask>();
				
				// Mevcut acil durum veya yüksek öncelikli görevleri koru
				while (_pendingTasks.Count > 0 && (_pendingTasks.Peek().Type == CleaningType.Emergency || _pendingTasks.Peek().IsHighPriority))
				{
					tempList.Add(_pendingTasks.Dequeue());
				}
				
				// Yeni görevi ekle
				tempList.Add(task);
				
				// Kalan görevleri ekle
				while (_pendingTasks.Count > 0)
				{
					tempList.Add(_pendingTasks.Dequeue());
				}
				
				// Kuyruğu yeniden oluştur
				foreach (var item in tempList)
				{
					_pendingTasks.Enqueue(item);
				}
				
				// Acil durumsa, mevcut işi bırak ve acil duruma geç
				if (type == CleaningType.Emergency && _isCleaning)
				{
					InterruptCurrentTask();
					_isHandlingEmergency = true;
				}
			}
			else
			{
				_pendingTasks.Enqueue(task);
			}
			
			// İşi hemen başlat
			if (!_isCleaning && !_isHandlingEmergency)
			{
				StartNextCleaningTask();
			}
			
			GD.Print($"Cleaner {Name} took cleaning task for {area} (type: {type}) from {requesterId}. ETA: {timeRequired} seconds");
			return true;
		}
		
		// Temizlik görevi al (string alan adı ve türü ile)
		public bool TakeCleaningTask(string areaName, string typeName, string requesterId = "", bool isHighPriority = false)
		{
			if (Enum.TryParse<CleaningArea>(areaName, out CleaningArea area) && 
				Enum.TryParse<CleaningType>(typeName, out CleaningType type))
			{
				return TakeCleaningTask(area, type, requesterId, isHighPriority);
			}
			
			GD.Print($"Invalid cleaning area or type: {areaName}, {typeName}");
			return false;
		}
		
		// Temizlik süresini hesapla
		private float CalculateCleaningTime(CleaningArea area, CleaningType type)
		{
			// Temel süre (saniye)
			float baseTime = 0.0f;
			
			// Alan büyüklüğüne göre temel süre
			switch (area)
			{
				case CleaningArea.Floor:
					baseTime = 300.0f; // 5 dakika
					break;
				case CleaningArea.Tables:
					baseTime = 180.0f; // 3 dakika
					break;
				case CleaningArea.Bar:
					baseTime = 180.0f; // 3 dakika
					break;
				case CleaningArea.Bathroom:
					baseTime = 300.0f; // 5 dakika
					break;
				case CleaningArea.Stage:
					baseTime = 240.0f; // 4 dakika
					break;
				case CleaningArea.VIPArea:
					baseTime = 240.0f; // 4 dakika
					break;
				case CleaningArea.Kitchen:
					baseTime = 360.0f; // 6 dakika
					break;
				case CleaningArea.Entrance:
					baseTime = 180.0f; // 3 dakika
					break;
				case CleaningArea.IllegalFloor:
					baseTime = 360.0f; // 6 dakika
					break;
				case CleaningArea.Outside:
					baseTime = 300.0f; // 5 dakika
					break;
				default:
					baseTime = 240.0f; // 4 dakika (varsayılan)
					break;
			}
			
			// Temizlik türüne göre çarpan
			float typeMultiplier = 1.0f;
			
			switch (type)
			{
				case CleaningType.Regular:
					typeMultiplier = 1.0f; // Normal temizlik
					break;
				case CleaningType.DeepCleaning:
					typeMultiplier = 2.5f; // Derin temizlik çok daha uzun sürer
					break;
				case CleaningType.Emergency:
					typeMultiplier = 0.5f; // Acil durum temizliği daha hızlı yapılır
					break;
				case CleaningType.Maintenance:
					typeMultiplier = 1.5f; // Bakım temizlikten biraz uzun sürer
					break;
				case CleaningType.Disposal:
					typeMultiplier = 0.7f; // Atık imhası nispeten hızlıdır
					break;
				default:
					typeMultiplier = 1.0f;
					break;
			}
			
			// Alanın mevcut temizlik seviyesi faktörü
			float cleanlinessMultiplier = 1.0f;
			
			// Alan temizlik seviyesi düşükse, temizlenmesi daha uzun sürer
			if (_areaCleanlinessLevels.ContainsKey(area))
			{
				float cleanliness = _areaCleanlinessLevels[area];
				cleanlinessMultiplier = 1.0f + Mathf.Max(0.0f, (0.7f - cleanliness)) * 2.0f; // 0.7'nin altında temizlik için daha uzun süre
			}
			
			// Beceri faktörleri
			float skillMultiplier = 1.0f;
			
			// Temizlik türüne göre beceri etkisi
			float typeSkill = _cleaningSkillLevels[type];
			float speedFactor = _speedSkill;
			
			// Beceri yükseldikçe süre kısalır
			skillMultiplier = 1.0f - ((typeSkill + speedFactor) / 4.0f); // En fazla %50 azaltma
			
			// Temizlik süresi
			float cleaningTime = baseTime * typeMultiplier * cleanlinessMultiplier * skillMultiplier;
			
			// Minimum süre
			cleaningTime = Mathf.Max(cleaningTime, baseTime * 0.3f); // En az temel sürenin %30'u kadar sürmeli
			
			return cleaningTime;
		}
		
		// Mevcut görevi güncelle
		private void UpdateCurrentCleaning(double delta)
		{
			// Eğer görev yoksa veya temizlik durumu yanlışsa düzelt
			if (_pendingTasks.Count == 0 || !_isCleaning)
			{
				_isCleaning = false;
				SetActivity(ActivityState.Idle);
				return;
			}
			
			// Mevcut görevi al
			CleaningTask currentTask = _pendingTasks.Peek();
			
			// Zamanı güncelle
			currentTask.TimeSpent += (float)delta * GetCleaningSpeed();
			
			// Temizlik ilerleme yüzdesini güncelle
			_currentCleaningProgress = currentTask.GetProgress() * 100.0f;
			
			// Görev tamamlandı mı kontrol et
			if (currentTask.IsComplete())
			{
				// Görevi tamamla
				CompleteCleaningTask(_pendingTasks.Dequeue());
			}
		}
		
		// Temizlik hızını hesapla
		private float GetCleaningSpeed()
		{
			// Temel hız
			float baseSpeed = 1.0f;
			
			// Enerji faktörü
			float energyFactor = Energy;
			
			// Ruh hali faktörü
			float moodFactor = Mood;
			
			// Hız becerisi faktörü
			float speedFactor = _speedSkill;
			
			// Ekipman kalitesi faktörü
			float equipmentFactor = 0.7f + (_equipmentQuality * 0.3f); // Ekipman kalitesi %30 etkiler
			
			// Malzeme yetersizliği faktörü
			float suppliesFactor = 1.0f;
			
			// Mevcut görevin malzeme durumunu kontrol et
			if (_pendingTasks.Count > 0)
			{
				CleaningTask currentTask = _pendingTasks.Peek();
				suppliesFactor = GetSuppliesFactor(currentTask.Area, currentTask.Type);
			}
			
			// Nihai hız
			return baseSpeed * energyFactor * moodFactor * speedFactor * equipmentFactor * suppliesFactor;
		}
		
		// Malzeme faktörünü hesapla
		private float GetSuppliesFactor(CleaningArea area, CleaningType type)
		{
			// Kullanılan malzemeler
			string primarySupply = "";
			string secondarySupply = "";
			
			// Alana göre kullanılan birincil malzeme
			switch (area)
			{
				case CleaningArea.Floor:
					primarySupply = "FloorCleaner";
					secondarySupply = "Mop";
					break;
				case CleaningArea.Tables:
				case CleaningArea.Bar:
				case CleaningArea.Stage:
				case CleaningArea.VIPArea:
					primarySupply = "AllPurposeCleaner";
					secondarySupply = "CleaningRags";
					break;
				case CleaningArea.Bathroom:
					primarySupply = "BathroomCleaner";
					secondarySupply = "DisinfectantSpray";
					break;
				case CleaningArea.Kitchen:
					primarySupply = "AllPurposeCleaner";
					secondarySupply = "DisinfectantSpray";
					break;
				case CleaningArea.Entrance:
				case CleaningArea.Outside:
					primarySupply = "Broom";
					secondarySupply = "FloorCleaner";
					break;
				case CleaningArea.IllegalFloor:
					primarySupply = "AllPurposeCleaner";
					secondarySupply = "Gloves";
					break;
				default:
					primarySupply = "AllPurposeCleaner";
					break;
			}
			
			// Tipe göre ek malzeme
			if (type == CleaningType.Emergency || type == CleaningType.Disposal)
			{
				secondarySupply = "Gloves";
			}
			
			// Malzeme durumunu kontrol et
			float factor = 1.0f;
			
			if (!string.IsNullOrEmpty(primarySupply) && _supplies.ContainsKey(primarySupply))
			{
				int supplyLevel = _supplies[primarySupply];
				if (supplyLevel < 10) // %10'un altında hız büyük oranda düşer
				{
					factor *= 0.5f;
				}
				else if (supplyLevel < 30) // %30'un altında hız düşer
				{
					factor *= 0.8f;
				}
			}
			
			if (!string.IsNullOrEmpty(secondarySupply) && _supplies.ContainsKey(secondarySupply))
			{
				int supplyLevel = _supplies[secondarySupply];
				if (supplyLevel < 10) // %10'un altında hız hafif düşer
				{
					factor *= 0.7f;
				}
				else if (supplyLevel < 30) // %30'un altında hız biraz düşer
				{
					factor *= 0.9f;
				}
			}
			
			return factor;
		}
		
		// Malzeme kullan
		private void UseSupplies(CleaningArea area, CleaningType type)
		{
			// Kullanılan malzemeler
			string primarySupply = "";
			string secondarySupply = "";
			
			// Alana göre kullanılan birincil malzeme
			switch (area)
			{
				case CleaningArea.Floor:
					primarySupply = "FloorCleaner";
					secondarySupply = "Mop";
					break;
				case CleaningArea.Tables:
				case CleaningArea.Bar:
				case CleaningArea.Stage:
				case CleaningArea.VIPArea:
					primarySupply = "AllPurposeCleaner";
					secondarySupply = "CleaningRags";
					break;
				case CleaningArea.Bathroom:
					primarySupply = "BathroomCleaner";
					secondarySupply = "DisinfectantSpray";
					break;
				case CleaningArea.Kitchen:
					primarySupply = "AllPurposeCleaner";
					secondarySupply = "DisinfectantSpray";
					break;
				case CleaningArea.Entrance:
				case CleaningArea.Outside:
					primarySupply = "Broom";
					secondarySupply = "FloorCleaner";
					break;
				case CleaningArea.IllegalFloor:
					primarySupply = "AllPurposeCleaner";
					secondarySupply = "Gloves";
					break;
				default:
					primarySupply = "AllPurposeCleaner";
					break;
			}
			
			// Tipe göre ek malzeme
			if (type == CleaningType.Emergency || type == CleaningType.Disposal)
			{
				secondarySupply = "Gloves";
			}
			
			// Malzeme kullanımı
			int usageAmount = 1; // Temel kullanım miktarı
			
			// Temizlik türüne göre kullanım miktarı
			switch (type)
			{
				case CleaningType.Regular:
					usageAmount = 1;
					break;
				case CleaningType.DeepCleaning:
					usageAmount = 3; // Derin temizlik daha çok malzeme kullanır
					break;
				case CleaningType.Emergency:
					usageAmount = 2; // Acil durumlar ekstra malzeme gerektirir
					break;
				case CleaningType.Maintenance:
					usageAmount = 1;
					break;
				case CleaningType.Disposal:
					usageAmount = 2; // Atık imhası ekstra malzeme gerektirir
					break;
			}
			
			// Tasarruflu temizlik trait'i varsa kullanım azalır
			if (GetTraits().Contains("EconomicalCleaner"))
			{
				usageAmount = Mathf.Max(1, usageAmount - 1);
			}
			
			// Birincil malzeme kullanımı
			if (!string.IsNullOrEmpty(primarySupply) && _supplies.ContainsKey(primarySupply))
			{
				_supplies[primarySupply] = Mathf.Max(0, _supplies[primarySupply] - usageAmount);
				
				// Malzeme az kaldıysa uyarı
				if (_supplies[primarySupply] < 20)
				{
					GD.Print($"Warning: {primarySupply} is running low ({_supplies[primarySupply]}%)");
				}
			}
			
			// İkincil malzeme kullanımı
			if (!string.IsNullOrEmpty(secondarySupply) && _supplies.ContainsKey(secondarySupply))
			{
				_supplies[secondarySupply] = Mathf.Max(0, _supplies[secondarySupply] - usageAmount);
				
				// Malzeme az kaldıysa uyarı
				if (_supplies[secondarySupply] < 20)
				{
					GD.Print($"Warning: {secondarySupply} is running low ({_supplies[secondarySupply]}%)");
				}
			}
			
			// TrashBags kullanımı (temizlik sırasında çöp toplarken)
			if (_supplies.ContainsKey("TrashBags") && GD.Randf() < 0.3f) // %30 ihtimalle çöp torbası kullanılır
			{
				_supplies["TrashBags"] = Mathf.Max(0, _supplies["TrashBags"] - 1);
				
				// Malzeme az kaldıysa uyarı
				if (_supplies["TrashBags"] < 10)
				{
					GD.Print($"Warning: TrashBags are running low ({_supplies["TrashBags"]} left)");
				}
			}
		}
		
		// Bir sonraki temizlik görevini başlat
		private void StartNextCleaningTask()
		{
			if (_pendingTasks.Count == 0) return;
			
			// İlk görevi al
			CleaningTask nextTask = _pendingTasks.Peek();
			
			// Acil durum kontrolü
			_isHandlingEmergency = (nextTask.Type == CleaningType.Emergency);
			
			// Görev değişkenlerini ayarla
			_currentArea = nextTask.Area;
			_currentCleaningType = nextTask.Type;
			_currentCleaningTotal = nextTask.TimeRequired;
			_currentCleaningProgress = 0.0f;
			
			// Temizlik durumunu başlat
			_isCleaning = true;
			
			// Aktiviteyi güncelle ve uygun animasyonu oynat
			SetActivity(ActivityState.Working);
			PlayCleaningAnimation(nextTask.Area, nextTask.Type);
			
			GD.Print($"Cleaner {Name} started cleaning {nextTask.Area} (type: {nextTask.Type})");
		}
		
		// Temizlik animasyonunu oynat
		private void PlayCleaningAnimation(CleaningArea area, CleaningType type)
		{
			string anim = "clean_";
			
			// Temizlik tipine göre animasyon
			switch (type)
			{
				case CleaningType.Regular:
					anim += "regular";
					break;
				case CleaningType.DeepCleaning:
					anim += "deep";
					break;
				case CleaningType.Emergency:
					anim += "emergency";
					break;
				case CleaningType.Maintenance:
					anim += "maintenance";
					break;
				case CleaningType.Disposal:
					anim += "disposal";
					break;
				default:
					anim += "regular";
					break;
			}
			
			// Basit durumlarda alan özelliğini de ekle
			if (area == CleaningArea.Floor)
				anim = "mop_floor";
			else if (area == CleaningArea.Bathroom)
				anim = "clean_bathroom";
			
			PlayAnimation(anim);
		}
		
		// Mevcut görevi yarıda kes
		private void InterruptCurrentTask()
		{
			if (!_isCleaning || _pendingTasks.Count == 0) return;
			
			// Mevcut görevi al
			CleaningTask currentTask = _pendingTasks.Peek();
			
			// Yarıda kalan görevi kuyruğun sonuna eklemek için tüm görevleri geçici listeye al
			List<CleaningTask> tempList = new List<CleaningTask>();
			CleaningTask interruptedTask = _pendingTasks.Dequeue(); // Mevcut görev
			
			// Diğer görevleri geçici listeye ekle
			while (_pendingTasks.Count > 0)
			{
				tempList.Add(_pendingTasks.Dequeue());
			}
			
			// Yarıda kalan görevi sona ekle (yüksek öncelikli değilse)
			if (!interruptedTask.IsHighPriority)
			{
				tempList.Add(interruptedTask);
			}
			
			// Kuyruğu yeniden oluştur
			foreach (var task in tempList)
			{
				_pendingTasks.Enqueue(task);
			}
			
			// Temizlik durumunu sıfırla
			_isCleaning = false;
			
			GD.Print($"Cleaner {Name} interrupted cleaning task for {interruptedTask.Area}");
		}
		
		// Görevi tamamla
		private void CompleteCleaningTask(CleaningTask task)
		{
			// Temizlik durumunu sıfırla
			_isCleaning = false;
			
			// Alan temizlik seviyesini güncelle
			float oldCleanliness = _areaCleanlinessLevels[task.Area];
			float newCleanliness = CalculateNewCleanliness(task.Area, task.Type);
			
			_areaCleanlinessLevels[task.Area] = newCleanliness;
			
			// Malzeme kullan
			UseSupplies(task.Area, task.Type);
			
			// İstatistikleri güncelle
			_areasCleanedToday++;
			
			if (task.Type == CleaningType.DeepCleaning)
				_deepCleaningsPerformed++;
			
			if (task.Type == CleaningType.Emergency)
			{
				_emergenciesHandled++;
				_isHandlingEmergency = false;
			}
			
			if (task.Type == CleaningType.Maintenance)
				_objectsFixed++;
			
			// Deneyim kazan
			AddExperience(1);
			
			// Temizlik tamamlandı sinyali gönder
			EmitSignal(SignalName.CleaningCompletedEventHandler, task.Area.ToString(), newCleanliness);
			
			// Acil durum ise özel sinyal
			if (task.Type == CleaningType.Emergency)
			{
				EmitSignal(SignalName.EmergencyHandledEventHandler, task.Area.ToString(), task.TimeSpent);
			}
			
			// Genel temizlik değerlendirmesini güncelle
			CalculateHygieneRating();
			
			// Aktiviteyi güncelle
			SetActivity(ActivityState.Idle);
			
			// Yeni görev varsa başlat
			if (_pendingTasks.Count > 0)
			{
				StartNextCleaningTask();
			}
			
			GD.Print($"Cleaner {Name} completed cleaning {task.Area}. Cleanliness improved from {oldCleanliness:F2} to {newCleanliness:F2}");
		}
		
		// Yeni temizlik seviyesini hesapla
		private float CalculateNewCleanliness(CleaningArea area, CleaningType type)
		{
			// Mevcut temizlik seviyesi
			float currentCleanliness = _areaCleanlinessLevels[area];
			
			// Temizlik iyileştirme faktörleri
			float baseImprovement = 0.0f;
			
			// Temizlik türüne göre iyileştirme miktarı
			switch (type)
			{
				case CleaningType.Regular:
					baseImprovement = 0.3f; // Normal temizlik %30 iyileştirir
					break;
				case CleaningType.DeepCleaning:
					baseImprovement = 0.7f; // Derin temizlik %70 iyileştirir
					break;
				case CleaningType.Emergency:
					baseImprovement = 0.2f; // Acil durum temizliği %20 iyileştirir
					break;
				case CleaningType.Maintenance:
					baseImprovement = 0.25f; // Bakım %25 iyileştirir
					break;
				case CleaningType.Disposal:
					baseImprovement = 0.15f; // Atık imhası %15 iyileştirir
					break;
				default:
					baseImprovement = 0.2f; // Varsayılan
					break;
			}
			
			// Titizlik becerisi faktörü
			float skillFactor = _thoroughnessSkill;
			
			// Temizlik türüne özel beceri faktörü
			float typeSkillFactor = _cleaningSkillLevels[type];
			
			// Malzeme faktörü
			float suppliesFactor = GetSuppliesFactor(area, type);
			
			// Mevcut temizlik seviyesi faktörü (düşük seviyede daha fazla iyileşme)
			float currentCleanlinessModifier = 1.0f - (currentCleanliness * 0.5f);
			
			// Nihai iyileştirme miktarı
			float improvement = baseImprovement * skillFactor * typeSkillFactor * suppliesFactor * currentCleanlinessModifier;
			
			// Azalan getiri - çok temiz bir alanda iyileştirme daha zordur
			if (currentCleanliness > 0.8f)
			{
				improvement *= 0.5f; // Yüksek temizlik seviyesindeki iyileştirme yarıya düşer
			}
			
			// Yeni temizlik seviyesi (0-1 arası)
			float newCleanliness = Mathf.Clamp(currentCleanliness + improvement, 0.0f, 1.0f);
			
			return newCleanliness;
		}
		
		// Alanların kirlenme durumunu güncelle
		private void UpdateAreaDirtiness(double delta)
		{
			// Kirlilik simülasyonu saatte bir çalışır (her 0.01'lik delta = 36 saniye)
			if ((delta - Mathf.Floor(delta / 0.01f) * 0.01f) > 0.005f) return;
			
			// Her alan için kirlenme hesapla
			foreach (CleaningArea area in Enum.GetValues(typeof(CleaningArea)))
			{
				if (_areaDirtyingRates.ContainsKey(area))
				{
					float dirtyingRate = _areaDirtyingRates[area];
					
					// Müşteri yoğunluğuna göre kirlenme hızını ayarla
					float customerModifier = GetCustomerDensityModifier(area);
					
					// Saatlik kirlenme hesapla (günlük oran / 24)
					float hourlyDirtying = (dirtyingRate / 24.0f) * customerModifier;
					
					// Temizlik azalması (36 saniye için oranla)
					float cleanlinessDecrease = hourlyDirtying * 0.01f;
					
					// Alanı kirlet
					_areaCleanlinessLevels[area] = Mathf.Max(0.0f, _areaCleanlinessLevels[area] - cleanlinessDecrease);
					
					// Kirlilik kritik seviyeye düştüyse uyarı ver
					if (_areaCleanlinessLevels[area] < 0.3f)
					{
						NotifyCriticalDirtiness(area);
					}
				}
			}
		}
		
		// Müşteri yoğunluğu modifikatörü
		private float GetCustomerDensityModifier(CleaningArea area)
		{
			// Gerçek uygulamada CustomerManager'dan alınacak
			float customerDensity = 0.5f; // 0-1 arası (varsayılan yarı dolu)
			
			// Mekan yöneticisinden müşteri yoğunluğunu al
			if (GetTree().Root.HasNode("GameManager/CustomerManager"))
			{
				var customerManager = GetTree().Root.GetNode("GameManager/CustomerManager");
				
				if (customerManager.HasMethod("GetCustomerDensity"))
				{
					try
					{
						customerDensity = (float)customerManager.Call("GetCustomerDensity");
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error getting customer density: {e.Message}");
					}
				}
				
				// Belirli alanların müşteri yoğunluğunu al
				if (customerManager.HasMethod("GetAreaCustomerDensity"))
				{
					try
					{
						customerDensity = (float)customerManager.Call("GetAreaCustomerDensity", area.ToString());
					}
					catch (Exception e)
					{
						// Özel alan yoğunluğu alınamazsa genel yoğunluk kullanılır
					}
				}
			}
			
			// Yoğunluk modifikatörü (0.5-2.0 arası)
			return 0.5f + (customerDensity * 1.5f);
		}
		
		// Kritik kirlilik bildirimi
		private void NotifyCriticalDirtiness(CleaningArea area)
		{
			// BuildingManager'a kritik kirlilik bildirimi gönder
			if (GetTree().Root.HasNode("GameManager/BuildingManager"))
			{
				var buildingManager = GetTree().Root.GetNode("GameManager/BuildingManager");
				
				if (buildingManager.HasMethod("NotifyCriticalDirtiness"))
				{
					try
					{
						buildingManager.Call("NotifyCriticalDirtiness", area.ToString(), _areaCleanlinessLevels[area]);
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error notifying critical dirtiness: {e.Message}");
					}
				}
			}
			
			// Gerçek uygulamada: Bu uyarıyı üstteki yöneticiye iletebilir
			GD.Print($"Critical dirtiness in {area}! Cleanliness level: {_areaCleanlinessLevels[area]:F2}");
		}
		
		// Genel temizlik değerlendirmesini hesapla
		private void CalculateHygieneRating()
		{
			float previousRating = _hygieneRating;
			
			// Alan ağırlıkları (önem sırasına göre)
			Dictionary<CleaningArea, float> areaWeights = new Dictionary<CleaningArea, float>
			{
				{ CleaningArea.Bathroom, 0.20f },    // Tuvaletler çok önemli
				{ CleaningArea.Kitchen, 0.15f },     // Mutfak çok önemli
				{ CleaningArea.Tables, 0.12f },      // Masalar önemli
				{ CleaningArea.Floor, 0.10f },       // Zemin önemli
				{ CleaningArea.Bar, 0.10f },         // Bar önemli
				{ CleaningArea.VIPArea, 0.08f },     // VIP alanı orta düzeyde önemli
				{ CleaningArea.Stage, 0.07f },       // Sahne orta düzeyde önemli
				{ CleaningArea.Entrance, 0.08f },    // Giriş orta düzeyde önemli
				{ CleaningArea.IllegalFloor, 0.05f },// Kaçak kat daha az önemli
				{ CleaningArea.Outside, 0.05f }      // Dış alan daha az önemli
			};
			
			// Ağırlıklı ortalama hesapla
			float totalWeight = 0.0f;
			float weightedSum = 0.0f;
			
			foreach (var area in areaWeights.Keys)
			{
				float weight = areaWeights[area];
				float cleanliness = _areaCleanlinessLevels[area];
				
				weightedSum += cleanliness * weight;
				totalWeight += weight;
			}
			
			// Yeni hijyen değerlendirmesi
			_hygieneRating = totalWeight > 0 ? weightedSum / totalWeight : 0.5f;
			
			// Değişim varsa sinyal gönder
			if (Mathf.Abs(_hygieneRating - previousRating) > 0.01f)
			{
				EmitSignal(SignalName.HygieneRatingChangedEventHandler, previousRating, _hygieneRating);
				
				// Müşteri memnuniyeti ve personel morali etkisini güncelle
				UpdateHygieneEffects();
				
				GD.Print($"Hygiene rating changed from {previousRating:F2} to {_hygieneRating:F2}");
			}
		}
		
		// Temizlik etkilerini güncelle
		private void UpdateHygieneEffects()
		{
			// Temizliğin müşteri memnuniyetine etkisi
			_customerSatisfactionEffect = (_hygieneRating - 0.5f) * 0.3f; // -0.15 ile +0.15 arası etki
			
			// Temizliğin personel moraline etkisi
			_staffMoraleEffect = (_hygieneRating - 0.5f) * 0.2f; // -0.1 ile +0.1 arası etki
			
			// Etkileri diğer sistemlere bildir
			UpdateCustomerSatisfaction();
			UpdateStaffMorale();
		}
		
		// Müşteri memnuniyetini güncelle
		private void UpdateCustomerSatisfaction()
		{
			// CustomerManager'a temizlik etkisini bildir
			if (GetTree().Root.HasNode("GameManager/CustomerManager"))
			{
				var customerManager = GetTree().Root.GetNode("GameManager/CustomerManager");
				
				if (customerManager.HasMethod("SetCleanlinessEffect"))
				{
					try
					{
						customerManager.Call("SetCleanlinessEffect", _customerSatisfactionEffect);
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error updating customer satisfaction: {e.Message}");
					}
				}
			}
		}
		
		// Personel moralini güncelle
		private void UpdateStaffMorale()
		{
			// StaffManager'a temizlik etkisini bildir
			if (GetTree().Root.HasNode("GameManager/StaffManager"))
			{
				var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
				
				if (staffManager.HasMethod("SetCleanlinessEffect"))
				{
					try
					{
						staffManager.Call("SetCleanlinessEffect", _staffMoraleEffect);
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error updating staff morale: {e.Message}");
					}
				}
			}
		}
		
		// Ekipman kalitesini yükselt
		public void UpgradeEquipment(float qualityIncrease)
		{
			float oldQuality = _equipmentQuality;
			_equipmentQuality = Mathf.Min(1.0f, _equipmentQuality + qualityIncrease);
			
			// Ruh halini iyileştir
			AdjustMood(0.1f, "Equipment Upgrade");
			
			GD.Print($"Cleaner {Name}'s equipment upgraded from quality {oldQuality:F2} to {_equipmentQuality:F2}");
		}
		
		// Malzeme stokunu artır
		public void RestockSupplies(string supplyName, int amount)
		{
			if (_supplies.ContainsKey(supplyName))
			{
				_supplies[supplyName] = Mathf.Min(100, _supplies[supplyName] + amount);
				GD.Print($"Restocked {supplyName} by {amount}. New level: {_supplies[supplyName]}");
			}
			else
			{
				GD.Print($"Unknown supply: {supplyName}");
			}
		}
		
		// Tüm malzemeleri stokla
		public void RestockAllSupplies()
		{
			foreach (var supply in _supplies.Keys.ToArray())
			{
				int currentLevel = _supplies[supply];
				int addAmount = 100 - currentLevel;
				
				if (addAmount > 0)
				{
					_supplies[supply] = 100;
					GD.Print($"Restocked {supply} by {addAmount}. Now at full capacity.");
				}
			}
			
			GD.Print("All cleaning supplies restocked to full capacity.");
		}
		
		// Müşteri ile etkileşim - StaffBase.InteractWithCustomer() metodunu override eder
		public override void InteractWithCustomer(Node3D customer)
		{
			if (customer == null) return;
			
			string customerId = customer.Name;
			
			// Müşteriden temizlik talebi kontrolü
			bool hasCleaningRequest = false;
			string areaRequest = "";
			bool isEmergency = false;
			
			// Talep kontrolü
			if (customer.GetType().GetMethod("HasCleaningRequest") != null)
			{
				try 
				{
					hasCleaningRequest = (bool)customer.Call("HasCleaningRequest");
					if (hasCleaningRequest)
					{
						if (customer.GetType().GetMethod("GetCleaningRequestArea") != null)
						{
							areaRequest = (string)customer.Call("GetCleaningRequestArea");
						}
						
						if (customer.GetType().GetMethod("IsCleaningEmergency") != null)
						{
							isEmergency = (bool)customer.Call("IsCleaningEmergency");
						}
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error checking cleaning request: {e.Message}");
				}
			}
			
			// Temizlik talebi işleme
			if (hasCleaningRequest && !string.IsNullOrEmpty(areaRequest))
			{
				CleaningType requestType = isEmergency ? CleaningType.Emergency : CleaningType.Regular;
				
				bool taskAccepted = TakeCleaningTask(areaRequest, requestType.ToString(), customerId, isEmergency);
				
				if (!taskAccepted)
				{
					// Talebi reddet - müşteriye bildir
					if (customer.GetType().GetMethod("CleaningRequestRejected") != null)
					{
						try 
						{
							customer.Call("CleaningRequestRejected", "Cleaner is busy");
						}
						catch (Exception e)
						{
							GD.PrintErr($"Error notifying request rejection: {e.Message}");
						}
					}
				}
				else
				{
					// Talebi kabul et - müşteriye bildir
					if (customer.GetType().GetMethod("CleaningRequestAccepted") != null)
					{
						try 
						{
							customer.Call("CleaningRequestAccepted");
						}
						catch (Exception e)
						{
							GD.PrintErr($"Error notifying request acceptance: {e.Message}");
						}
					}
				}
			}
			else
			{
				// Temizlik talebi yok, genel durumu bildir
				int pendingTasksCount = _pendingTasks.Count;
				
				// Müşteriye bilgi ver
				if (customer.GetType().GetMethod("ReceiveCleanerStatus") != null)
				{
					try 
					{
						customer.Call("ReceiveCleanerStatus", pendingTasksCount, _isCleaning, _hygieneRating);
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error sharing cleaner status: {e.Message}");
					}
				}
			}
			
			// Enerji tüketimi
			AdjustEnergy(-0.01f, "Customer Interaction");
		}
		
		// Özel performans davranışı - StaffBase.PerformSpecialBehavior() metodunu override eder
		public override void PerformSpecialBehavior()
		{
			// Özel performans: Tüm alanları hızlı temizleme
			PerformSpeedCleaning();
		}
		
		// Hızlı temizleme
		private void PerformSpeedCleaning()
		{
			// Hızlı temizleme modu
			SetActivity(ActivityState.Special);
			
			// Hızlı temizleme animasyonu
			PlayAnimation("speed_cleaning");
			
			// Süreyi belirle
			float duration = 60.0f; // 1 dakika
			
			// Geçici hız bonusu
			float originalSpeed = _speedSkill;
			_speedSkill = Mathf.Min(1.0f, _speedSkill + 0.3f);
			
			// Timer ile normal moda dönüş
			Timer timer = new Timer
			{
				WaitTime = duration,
				OneShot = true
			};
			
			AddChild(timer);
			timer.Timeout += () => 
			{
				_speedSkill = originalSpeed;
				
				if (_isCleaning)
				{
					SetActivity(ActivityState.Working);
					CleaningTask currentTask = _pendingTasks.Peek();
					PlayCleaningAnimation(currentTask.Area, currentTask.Type);
				}
				else
				{
					SetActivity(ActivityState.Idle);
				}
				
				GD.Print($"Cleaner {Name}'s speed cleaning mode ended");
			};
			timer.Start();
			
			// Hızlı temizleme etkisiyle tüm alanların temizliğini hafif artır
			foreach (CleaningArea area in Enum.GetValues(typeof(CleaningArea)))
			{
				_areaCleanlinessLevels[area] = Mathf.Min(1.0f, _areaCleanlinessLevels[area] + 0.1f);
			}
			
			// Genel temizlik değerlendirmesini güncelle
			CalculateHygieneRating();
			
			// Deneyim kazanma
			AddExperience(2);
			
			GD.Print($"Cleaner {Name} activated speed cleaning mode! All areas got a small cleanliness boost.");
		}
		
		// Özel animasyon - StaffBase.PlaySpecialAnimation() metodunu override eder
		protected override void PlaySpecialAnimation()
		{
			PlayAnimation("speed_cleaning");
		}
		
		// Seviye atlama özel efektleri - StaffBase.OnLevelUp() metodunu override eder
		protected override void OnLevelUp()
		{
			base.OnLevelUp();
			
			// Titizlik becerisi gelişimi
			_thoroughnessSkill = Mathf.Min(1.0f, _thoroughnessSkill + 0.05f);
			
			// Hız becerisi gelişimi
			_speedSkill = Mathf.Min(1.0f, _speedSkill + 0.04f);
			
			// Gizlilik becerisi gelişimi
			_discretionSkill = Mathf.Min(1.0f, _discretionSkill + 0.03f);
			
			// Bakım becerisi gelişimi
			_maintenanceSkill = Mathf.Min(1.0f, _maintenanceSkill + 0.04f);
			
			// Bir temizlik türünde rastgele gelişim
			Array cleaningTypes = Enum.GetValues(typeof(CleaningType));
			CleaningType randomType = (CleaningType)cleaningTypes.GetValue(GD.RandRange(0, cleaningTypes.Length - 1));
			_cleaningSkillLevels[randomType] = Mathf.Min(1.0f, _cleaningSkillLevels[randomType] + 0.1f);
			
			// Beceri değerlerini skills sözlüğüne aktar
			if (_skills.ContainsKey("thoroughness")) _skills["thoroughness"] = _thoroughnessSkill;
			if (_skills.ContainsKey("speed")) _skills["speed"] = _speedSkill;
			if (_skills.ContainsKey("discretion")) _skills["discretion"] = _discretionSkill;
			if (_skills.ContainsKey("maintenance")) _skills["maintenance"] = _maintenanceSkill;
			
			GD.Print($"Cleaner {Name} leveled up: thoroughness {_thoroughnessSkill}, speed {_speedSkill}, {randomType} cleaning {_cleaningSkillLevels[randomType]}");
		}
		
		// Günlük güncelleme - StaffBase.UpdateDaily() metodunu override eder
		public override void UpdateDaily()
		{
			base.UpdateDaily();
			
			// İstatistikleri sıfırla
			_areasCleanedToday = 0;
			_deepCleaningsPerformed = 0;
			_emergenciesHandled = 0;
			_tasksRejected = 0;
			_objectsFixed = 0;
			
			// Ekipman aşınması
			_equipmentQuality = Mathf.Max(0.3f, _equipmentQuality - 0.02f); // Ekipman günde %2 aşınır, %30'un altına düşmez
			
			// Görevleri sıfırla
			if (_isHandlingEmergency)
			{
				_isHandlingEmergency = false;
			}
		}
		
		// Özel risk faktörleri - StaffBase.ApplySpecialRiskFactors() metodunu override eder
		protected override void ApplySpecialRiskFactors()
		{
			base.ApplySpecialRiskFactors();
			
			// Temizlikçi-spesifik risk faktörleri
			
			// Düşük genel temizlik = daha fazla sadakatsizlik riski
			if (_hygieneRating < 0.4f)
			{
				_disloyaltyRisk += 0.05f;
			}
			
			// Ekipman kalitesi düşük = daha fazla sadakatsizlik riski
			if (_equipmentQuality < 0.4f)
			{
				_disloyaltyRisk += 0.03f;
			}
			
			// Malzeme yetersizliği = daha fazla sadakatsizlik riski
			bool lowSupplies = false;
			foreach (var supply in _supplies)
			{
				if (supply.Value < 20) // %20'nin altı malzeme yetersiz
				{
					lowSupplies = true;
					break;
				}
			}
			
			if (lowSupplies)
			{
				_disloyaltyRisk += 0.02f;
			}
		}
		
		// Özel performans faktörü hesaplama - StaffBase.CalculateSpecialPerformanceFactor() metodunu override eder
		protected override float CalculateSpecialPerformanceFactor()
		{
			// Titizlik, hız ve bakım faktörü
			return (_thoroughnessSkill * 0.4f + _speedSkill * 0.3f + _maintenanceSkill * 0.3f - 0.5f) * 0.2f;
		}
		
		// Tüm görevleri iptal et
		public void CancelAllTasks()
		{
			// Tüm bekleyen görevleri temizle
			_pendingTasks.Clear();
			
			// Çalışma modunu sıfırla
			_isCleaning = false;
			_isHandlingEmergency = false;
			SetActivity(ActivityState.Idle);
			
			GD.Print($"Cleaner {Name} canceled all pending tasks");
		}
		
		// Özellik değerlerini döndür
		public new Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = base.GetStats();
			
			// Temizlikçi-spesifik değerleri ekle
			stats["ThoroughnessSkill"] = _thoroughnessSkill;
			stats["SpeedSkill"] = _speedSkill;
			stats["DiscretionSkill"] = _discretionSkill;
			stats["MaintenanceSkill"] = _maintenanceSkill;
			stats["HygieneRating"] = _hygieneRating;
			stats["AreasCleanedToday"] = _areasCleanedToday;
			stats["DeepCleaningsPerformed"] = _deepCleaningsPerformed;
			stats["EmergenciesHandled"] = _emergenciesHandled;
			stats["TasksRejected"] = _tasksRejected;
			stats["ObjectsFixed"] = _objectsFixed;
			stats["EquipmentQuality"] = _equipmentQuality;
			stats["CustomerSatisfactionEffect"] = _customerSatisfactionEffect;
			stats["StaffMoraleEffect"] = _staffMoraleEffect;
			
			// Temizlik beceri seviyeleri
			Dictionary<string, float> cleaningSkills = new Dictionary<string, float>();
			foreach (var item in _cleaningSkillLevels)
			{
				cleaningSkills[item.Key.ToString()] = item.Value;
			}
			stats["CleaningSkills"] = cleaningSkills;
			
			// Alan temizlik seviyeleri
			Dictionary<string, float> areaCleanlinessStats = new Dictionary<string, float>();
			foreach (var item in _areaCleanlinessLevels)
			{
				areaCleanlinessStats[item.Key.ToString()] = item.Value;
			}
			stats["AreaCleanliness"] = areaCleanlinessStats;
			
			// Malzeme durumları
			stats["Supplies"] = _supplies;
			
			return stats;
		}
	}
}
