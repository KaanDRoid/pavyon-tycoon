using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PavyonTycoon.Characters.Customers
{
	public partial class CustomerManager : Node
	{
		// Singleton instance
		private static CustomerManager _instance;
		public static CustomerManager Instance => _instance;

		// Mevcut müşteriler
		private List<CustomerBase> _allCustomers = new List<CustomerBase>();
		private Dictionary<string, CustomerBase> _customersById = new Dictionary<string, CustomerBase>();
		
		// Müşteri tipi bazlı müşteri listeleri
		private Dictionary<CustomerBase.CustomerType, List<CustomerBase>> _customersByType = new Dictionary<CustomerBase.CustomerType, List<CustomerBase>>();
		
		// VIP müşteriler
		private List<CustomerBase> _vipCustomers = new List<CustomerBase>();
		
		// Müşteri sahne yolu
		private string _customerScenePath = "res://Scenes/Characters/Customers/Customer.tscn";
		
		// İsim ve demografik veri listeleri
		private List<string> _maleFirstNames = new List<string>();
		private List<string> _femaleFirstNames = new List<string>();
		private List<string> _lastNames = new List<string>();
		
		// Ankara mahalleleri (müşteri arka planı için)
		private List<string> _ankaraDistricts = new List<string>();
		
		// Müşteri oluşturma parametreleri
		private int _maxCustomers = 50;         // Aynı anda maksimum müşteri sayısı
		private float _customerSpawnRate = 0.5f; // Dakikada ortalama müşteri oluşturma oranı
		private float _spawnTimer = 0.0f;
		private bool _isGeneratingCustomers = false;
		
		// Masa yönetimi
		private List<Node3D> _availableTables = new List<Node3D>();
		private Dictionary<string, Node3D> _occupiedTables = new Dictionary<string, Node3D>();
		
		// Pavyon durumu
		private bool _isPavyonOpen = false;
		private Vector3 _entrancePosition;
		private Vector3 _exitPosition;
		private Vector3 _bathroomPosition;
		private Vector3 _illegalFloorEntrancePosition; // Kaçak kat girişi
		
		// Müşteri demografisi ve gelirinin dinamik dağılımı
		private Dictionary<CustomerBase.CustomerType, float> _customerTypeDistribution = new Dictionary<CustomerBase.CustomerType, float>();
		
		// Özel müşteri grupları
		private Dictionary<string, List<CustomerInfo>> _specialCustomerGroups = new Dictionary<string, List<CustomerInfo>>();
		
		// Müşteri memnuniyeti ve etki faktörleri
		private float _averageCustomerSatisfaction = 0.7f;   // Ortalama memnuniyet (0-1 arası)
		private Dictionary<string, float> _satisfactionModifiers = new Dictionary<string, float>();

		// EKLENEN - Derin müşteri ilişkileri
		private Dictionary<string, CustomerRelationship> _regularCustomers = new Dictionary<string, CustomerRelationship>();
		private const int REGULAR_CUSTOMER_VISIT_THRESHOLD = 3; // Düzenli müşteri sayılması için ziyaret eşiği
		
		// EKLENEN - Zaman bazlı müşteri yönetimi
		private Dictionary<int, Dictionary<CustomerBase.CustomerType, float>> _hourlyDistributions = new Dictionary<int, Dictionary<CustomerBase.CustomerType, float>>();
		private Dictionary<DayOfWeek, Dictionary<CustomerBase.CustomerType, float>> _dailyDistributions = new Dictionary<DayOfWeek, Dictionary<CustomerBase.CustomerType, float>>();
		private DateTime _lastTimeDistributionUpdate = DateTime.Now;
		
		// EKLENEN - Karmaşık müşteri davranışları
		private float _conflictProbability = 0.05f; // Genel çatışma olasılığı
		private List<CustomerConflict> _activeConflicts = new List<CustomerConflict>();
		private float _conflictCheckTimer = 0f;
		private const float CONFLICT_CHECK_INTERVAL = 300f; // 5 dakikada bir çatışma kontrolü
		
		// EKLENEN - Ekonomik entegrasyon
		private Dictionary<CustomerBase.CustomerType, Dictionary<string, float>> _spendingPatterns = new Dictionary<CustomerBase.CustomerType, Dictionary<string, float>>();
		private float _totalRevenue = 0f;
		private Dictionary<string, float> _revenueByCategory = new Dictionary<string, float>();
		private Dictionary<CustomerBase.CustomerType, float> _revenueByCustomerType = new Dictionary<CustomerBase.CustomerType, float>();
		
		// EKLENEN - Olay sistemi entegrasyonu
		private bool _isSpecialEventActive = false;
		private string _activeEventType = "";
		private float _eventSatisfactionMultiplier = 1.0f;
		private Dictionary<string, Dictionary<CustomerBase.CustomerType, float>> _eventTypeBonus = new Dictionary<string, Dictionary<CustomerBase.CustomerType, float>>();
		
		// EKLENEN - İllegal kat etkileşimleri
		private bool _isIllegalFloorOpen = false;
		private HashSet<string> _customersInIllegalFloor = new HashSet<string>();
		private Dictionary<CustomerBase.CustomerType, float> _illegalFloorAccessProbability = new Dictionary<CustomerBase.CustomerType, float>();
		private float _illegalFloorRaidProbability = 0.001f; // Baskın olasılığı (düşük)
		
		// EKLENEN - Personel-Müşteri etkileşim derinliği
		private Dictionary<string, Dictionary<string, float>> _staffCustomerRelationships = new Dictionary<string, Dictionary<string, float>>();
		private Dictionary<string, string> _assignedKons = new Dictionary<string, string>(); // MüşteriID -> KonsID eşleşmesi
		
		// EKLENEN - Rekabet sistemi 
		private float _customerLoyalty = 0.7f; // Genel müşteri sadakati (0-1)
		private Dictionary<string, float> _regularCustomerLoyalty = new Dictionary<string, float>(); // Düzenli müşteri sadakati
		private float _competitorAttractionRate = 0.05f; // Rakiplerin müşteri çekme olasılığı
		private float _competitionTimer = 0f;
		private const float COMPETITION_CHECK_INTERVAL = 600f; // 10 dakikada bir rekabet kontrolü
		
		// Signals
		[Signal]
		public delegate void CustomerEnteredEventHandler(string customerId, string customerType);
		
		[Signal]
		public delegate void CustomerLeftEventHandler(string customerId, float spentAmount, float satisfaction);
		
		[Signal]
		public delegate void VIPCustomerEnteredEventHandler(string customerId, string customerType);
		
		[Signal]
		public delegate void CustomerSatisfactionChangedEventHandler(float newAverageSatisfaction);
		
		[Signal]
		public delegate void SpecialGroupArrivedEventHandler(string groupName, int customerCount);
		
		[Signal]
		public delegate void TableAssignedEventHandler(string customerId, string tableId);
		
		// EKLENEN - Yeni sinyaller
		[Signal]
		public delegate void RegularCustomerRecognizedEventHandler(string customerId, int visitCount);
		
		[Signal]
		public delegate void CustomerConflictStartedEventHandler(string instigatorId, string targetId, string conflictType);
		
		[Signal]
		public delegate void CustomerConflictResolvedEventHandler(string conflictId, bool peacefulResolution);
		
		[Signal]
		public delegate void CustomerEnteredIllegalFloorEventHandler(string customerId, string customerType);
		
		[Signal]
		public delegate void PoliceRaidTriggeredEventHandler(string reason, int customerCount);
		
		[Signal]
		public delegate void CustomerLostToCompetitorEventHandler(string customerId, string reason);
		
		[Signal]
		public delegate void KonsAssignedToCustomerEventHandler(string konsId, string customerId, float initialRelationship);
		
		// Müşteri bilgi sınıfı (özel grup müşterileri için)
		private class CustomerInfo
		{
			public string Name { get; set; }
			public int Age { get; set; }
			public string Gender { get; set; }
			public CustomerBase.CustomerType Type { get; set; }
			public float Budget { get; set; }
			public bool IsVIP { get; set; }
			public string Signature { get; set; }
			public Dictionary<string, float> CustomPreferences { get; set; }
			
			public CustomerInfo(string name, int age, string gender, CustomerBase.CustomerType type, 
						 float budget, bool isVIP = false, string signature = "", Dictionary<string, float> customPreferences = null)
			{
				Name = name;
				Age = age;
				Gender = gender;
				Type = type;
				Budget = budget;
				IsVIP = isVIP;
				Signature = signature;
				CustomPreferences = customPreferences ?? new Dictionary<string, float>();
			}
		}

		// EKLENEN - Düzenli müşteri ilişki sınıfı
		private class CustomerRelationship
		{
			public string CustomerId { get; set; }
			public string FullName { get; set; }
			public CustomerBase.CustomerType Type { get; set; }
			public int VisitCount { get; set; }
			public DateTime LastVisit { get; set; }
			public float Loyalty { get; set; } // 0-1 arası
			public Dictionary<string, float> Preferences { get; set; } // Tercih edilen içecek, müzik, vb.
			public Dictionary<string, float> StaffRelationships { get; set; } // Staff ID -> İlişki değeri
			public bool HasVisitedIllegalFloor { get; set; }
			public float TotalSpent { get; set; }
			public bool IsTroublesome { get; set; } // Sorun çıkarma eğilimi
			public int ConflictCount { get; set; }
			
			public CustomerRelationship(string id, string name, CustomerBase.CustomerType type)
			{
				CustomerId = id;
				FullName = name;
				Type = type;
				VisitCount = 1;
				LastVisit = DateTime.Now;
				Loyalty = 0.5f;
				Preferences = new Dictionary<string, float>();
				StaffRelationships = new Dictionary<string, float>();
				HasVisitedIllegalFloor = false;
				TotalSpent = 0f;
				IsTroublesome = false;
				ConflictCount = 0;
			}
			
			public void UpdateVisit()
			{
				VisitCount++;
				LastVisit = DateTime.Now;
				// Sadakat artışı (ziyaret sayısı arttıkça daha az artar)
				Loyalty = Mathf.Min(1.0f, Loyalty + (0.05f / Mathf.Sqrt(VisitCount)));
			}
		}
		
		// EKLENEN - Çatışma sınıfı
		private class CustomerConflict
		{
			public string ConflictId { get; set; }
			public string InstigatorId { get; set; } // Başlatan müşteri
			public string TargetId { get; set; } // Hedef müşteri
			public string ConflictType { get; set; } // Sözlü tartışma, kavga, vb.
			public float Severity { get; set; } // 0-1 arası şiddet
			public float Duration { get; set; } // Saniye cinsinden süre
			public float ElapsedTime { get; set; }
			public bool IsResolved { get; set; }
			
			public CustomerConflict(string instigatorId, string targetId, string conflictType, float severity)
			{
				ConflictId = Guid.NewGuid().ToString();
				InstigatorId = instigatorId;
				TargetId = targetId;
				ConflictType = conflictType;
				Severity = Mathf.Clamp(severity, 0.1f, 1.0f);
				Duration = 60f + (severity * 180f); // 1-4 dakika arası
				ElapsedTime = 0f;
				IsResolved = false;
			}
			
			public void Update(float delta)
			{
				ElapsedTime += delta;
				
				if (ElapsedTime >= Duration)
				{
					IsResolved = true;
				}
			}
		}
		
		public override void _Ready()
		{
			// Singleton kurulumu
			if (_instance != null)
			{
				QueueFree();
				return;
			}
			_instance = this;
			
			// Veri listelerini yükle
			LoadDataLists();
			
			// Müşteri tipi dağılımını ayarla
			InitializeCustomerDistribution();
			
			// Özel grupları tanımla
			DefineSpecialGroups();
			
			// Masa pozisyonlarını bul
			FindTables();
			
			// Pavyon konum noktalarını bul
			FindPavyonLocations();
			
			// Memnuniyet modifikatörlerini sıfırla
			ResetSatisfactionModifiers();
			
			// EKLENEN - Zamana dayalı dağılımları başlat
			InitializeTimeBasedDistributions();
			
			// EKLENEN - Harcama kalıplarını başlat
			InitializeSpendingPatterns();
			
			// EKLENEN - Özel etkinlik bonuslarını başlat
			InitializeEventBonuses();
			
			// EKLENEN - İllegal kat erişim olasılıklarını başlat
			InitializeIllegalFloorAccess();
			
			GD.Print("CustomerManager initialized.");
		}
		
		public override void _Process(double delta)
		{
			// Müşteri oluşturma
			if (_isPavyonOpen && _isGeneratingCustomers)
			{
				// Müşteri sayısı kontrolü
				if (_allCustomers.Count < _maxCustomers && _availableTables.Count > 0)
				{
					// Müşteri oluşturma zamanı geldi mi?
					_spawnTimer += (float)delta * 60.0f; // Saniye -> dakika dönüşümü
					
					if (_spawnTimer >= (1.0f / _customerSpawnRate))
					{
						_spawnTimer = 0.0f;
						
						// Yeni müşteri oluştur
						GenerateRandomCustomer();
					}
				}
			}
			
			// Müşteri yönetimi ve güncellemesi
			// (Godot zaten _Process methodu ile tüm müşterilerin kendi _Process metodlarını çağıracak)
			
			// Ortalama müşteri memnuniyetini periyodik olarak güncelle
			if (GD.Randf() < 0.001f) // Düşük olasılıkla (nadiren) güncelle
			{
				UpdateAverageCustomerSatisfaction();
			}
			
			// EKLENEN - Zaman bazlı dağılım güncelleme kontrolü
			CheckTimeDistributionUpdate();
			
			// EKLENEN - Çatışma kontrolü
			UpdateConflicts((float)delta);
			
			// EKLENEN - İllegal kat baskın kontrolü
			CheckIllegalFloorRaid((float)delta);
			
			// EKLENEN - Rekabet kontrolü
			CheckCompetition((float)delta);
		}
		
		// Veri listelerini yükle
		private void LoadDataLists()
		{
			// Erkek isimleri
			_maleFirstNames = new List<string>
			{
				"Ali", "Mehmet", "Ahmet", "Mustafa", "İbrahim", "Hasan", "Hüseyin", 
				"Murat", "Emre", "Kemal", "Oğuz", "Serkan", "Burak", "Selim", "Kaan",
				"Cengiz", "Tolga", "Orhan", "Yaşar", "Cem", "Ercan", "Sinan", "Erdem",
				"Baran", "Cihan", "Deniz", "Eren", "Ferhat", "Gökhan", "Hakan"
			};
			
			// Kadın isimleri
			_femaleFirstNames = new List<string>
			{
				"Ayşe", "Fatma", "Zeynep", "Merve", "Ebru", "Esra", "Derya", 
				"Serap", "Melis", "Buse", "Selin", "Emine", "Gül", "Sevgi", "Nur",
				"Canan", "Tuğçe", "Özge", "Pınar", "Rana", "Sibel", "Tülay", "Yeliz",
				"Aslı", "Burcu", "Ceren", "Damla", "Ece", "Feride", "Gamze"
			};
			
			// Soyadları
			_lastNames = new List<string>
			{
				"Yılmaz", "Kaya", "Demir", "Çelik", "Şahin", "Yıldız", "Yıldırım", 
				"Öztürk", "Aydın", "Özdemir", "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin",
				"Tanrıverdi", "Koç", "Kurt", "Özkan", "Şeker", "Akar", "Alp", "Ateş", "Bulut",
				"Çakır", "Çakmak", "Çiçek", "Duman", "Erbil", "Erdoğan", "Ergün", "Erkuş",
				"Kaplan", "Kartal", "Keskin", "Korkmaz", "Korkut", "Kuru", "Polat", "Şener"
			};
			
			// Ankara mahalleleri
			_ankaraDistricts = new List<string>
			{
				"Bahçelievler", "Emek", "Çankaya", "Kızılay", "Ulus", "Keçiören", 
				"Batıkent", "Sincan", "Mamak", "Cebeci", "Dikmen", "Etimesgut",
				"Yenimahalle", "Demetevler", "Ayrancı", "Balgat", "Tunalı", "Sıhhiye",
				"Altındağ", "Çubuk", "Esat", "Maltepe", "Eryaman", "Pursaklar", "Gölbaşı"
			};
		}
		
		// Müşteri tipi dağılımını başlat
		private void InitializeCustomerDistribution()
		{
			// Varsayılan dağılım - pavyonun seviyesi, yeri vb. faktörlere göre değişebilir
			_customerTypeDistribution[CustomerBase.CustomerType.Regular] = 0.30f;
			_customerTypeDistribution[CustomerBase.CustomerType.Worker] = 0.25f;
			_customerTypeDistribution[CustomerBase.CustomerType.Elite] = 0.08f;
			_customerTypeDistribution[CustomerBase.CustomerType.Nostalgic] = 0.10f;
			_customerTypeDistribution[CustomerBase.CustomerType.Emotional] = 0.10f;
			_customerTypeDistribution[CustomerBase.CustomerType.Young] = 0.05f;
			_customerTypeDistribution[CustomerBase.CustomerType.Bureaucrat] = 0.05f;
			_customerTypeDistribution[CustomerBase.CustomerType.UnderCover] = 0.02f;
			_customerTypeDistribution[CustomerBase.CustomerType.Sapkali] = 0.02f;
			_customerTypeDistribution[CustomerBase.CustomerType.Gangster] = 0.02f;
			_customerTypeDistribution[CustomerBase.CustomerType.Foreigner] = 0.01f;
		}

		// EKLENEN - Zamana dayalı dağılımları başlat
		private void InitializeTimeBasedDistributions()
		{
			// Saatlik dağılımlar
			// Erken saatler (18:00-20:00)
			Dictionary<CustomerBase.CustomerType, float> earlyHoursDistribution = new Dictionary<CustomerBase.CustomerType, float>();
			earlyHoursDistribution[CustomerBase.CustomerType.Regular] = 0.35f;
			earlyHoursDistribution[CustomerBase.CustomerType.Worker] = 0.30f;
			earlyHoursDistribution[CustomerBase.CustomerType.Nostalgic] = 0.15f;
			earlyHoursDistribution[CustomerBase.CustomerType.Elite] = 0.05f;
			earlyHoursDistribution[CustomerBase.CustomerType.Young] = 0.03f;
			earlyHoursDistribution[CustomerBase.CustomerType.Bureaucrat] = 0.05f;
			earlyHoursDistribution[CustomerBase.CustomerType.Emotional] = 0.04f;
			earlyHoursDistribution[CustomerBase.CustomerType.UnderCover] = 0.01f;
			earlyHoursDistribution[CustomerBase.CustomerType.Sapkali] = 0.01f;
			earlyHoursDistribution[CustomerBase.CustomerType.Gangster] = 0.01f;
			
			for (int hour = 18; hour <= 20; hour++)
			{
				_hourlyDistributions[hour] = new Dictionary<CustomerBase.CustomerType, float>(earlyHoursDistribution);
			}
			
			// Prime time (21:00-00:00)
			Dictionary<CustomerBase.CustomerType, float> primeTimeDistribution = new Dictionary<CustomerBase.CustomerType, float>();
			primeTimeDistribution[CustomerBase.CustomerType.Regular] = 0.25f;
			primeTimeDistribution[CustomerBase.CustomerType.Worker] = 0.20f;
			primeTimeDistribution[CustomerBase.CustomerType.Nostalgic] = 0.10f;
			primeTimeDistribution[CustomerBase.CustomerType.Elite] = 0.15f;
			primeTimeDistribution[CustomerBase.CustomerType.Young] = 0.10f;
			primeTimeDistribution[CustomerBase.CustomerType.Bureaucrat] = 0.08f;
			primeTimeDistribution[CustomerBase.CustomerType.Emotional] = 0.05f;
			primeTimeDistribution[CustomerBase.CustomerType.UnderCover] = 0.02f;
			primeTimeDistribution[CustomerBase.CustomerType.Sapkali] = 0.02f;
			primeTimeDistribution[CustomerBase.CustomerType.Gangster] = 0.02f;
			primeTimeDistribution[CustomerBase.CustomerType.Foreigner] = 0.01f;
			
			for (int hour = 21; hour <= 23; hour++)
			{
				_hourlyDistributions[hour] = new Dictionary<CustomerBase.CustomerType, float>(primeTimeDistribution);
			}
			_hourlyDistributions[0] = new Dictionary<CustomerBase.CustomerType, float>(primeTimeDistribution);
			
			// Geç saatler (01:00-04:00)
			Dictionary<CustomerBase.CustomerType, float> lateHoursDistribution = new Dictionary<CustomerBase.CustomerType, float>();
			lateHoursDistribution[CustomerBase.CustomerType.Regular] = 0.20f;
			lateHoursDistribution[CustomerBase.CustomerType.Worker] = 0.15f;
			lateHoursDistribution[CustomerBase.CustomerType.Nostalgic] = 0.05f;
			lateHoursDistribution[CustomerBase.CustomerType.Elite] = 0.20f;
			lateHoursDistribution[CustomerBase.CustomerType.Young] = 0.15f;
			lateHoursDistribution[CustomerBase.CustomerType.Bureaucrat] = 0.05f;
			lateHoursDistribution[CustomerBase.CustomerType.Emotional] = 0.08f;
			lateHoursDistribution[CustomerBase.CustomerType.UnderCover] = 0.02f;
			lateHoursDistribution[CustomerBase.CustomerType.Sapkali] = 0.03f;
			lateHoursDistribution[CustomerBase.CustomerType.Gangster] = 0.05f;
			lateHoursDistribution[CustomerBase.CustomerType.Foreigner] = 0.02f;
			
			for (int hour = 1; hour <= 4; hour++)
			{
				_hourlyDistributions[hour] = new Dictionary<CustomerBase.CustomerType, float>(lateHoursDistribution);
			}
			
			// Günlük dağılımlar
			// Hafta içi (Pazartesi-Perşembe)
			Dictionary<CustomerBase.CustomerType, float> weekdayDistribution = new Dictionary<CustomerBase.CustomerType, float>();
			weekdayDistribution[CustomerBase.CustomerType.Regular] = 0.30f;
			weekdayDistribution[CustomerBase.CustomerType.Worker] = 0.25f;
			weekdayDistribution[CustomerBase.CustomerType.Nostalgic] = 0.12f;
			weekdayDistribution[CustomerBase.CustomerType.Elite] = 0.08f;
			weekdayDistribution[CustomerBase.CustomerType.Young] = 0.05f;
			weekdayDistribution[CustomerBase.CustomerType.Bureaucrat] = 0.08f;
			weekdayDistribution[CustomerBase.CustomerType.Emotional] = 0.07f;
			weekdayDistribution[CustomerBase.CustomerType.UnderCover] = 0.02f;
			weekdayDistribution[CustomerBase.CustomerType.Sapkali] = 0.01f;
			weekdayDistribution[CustomerBase.CustomerType.Gangster] = 0.01f;
			weekdayDistribution[CustomerBase.CustomerType.Foreigner] = 0.01f;
			
			_dailyDistributions[DayOfWeek.Monday] = new Dictionary<CustomerBase.CustomerType, float>(weekdayDistribution);
			_dailyDistributions[DayOfWeek.Tuesday] = new Dictionary<CustomerBase.CustomerType, float>(weekdayDistribution);
			_dailyDistributions[DayOfWeek.Wednesday] = new Dictionary<CustomerBase.CustomerType, float>(weekdayDistribution);
			_dailyDistributions[DayOfWeek.Thursday] = new Dictionary<CustomerBase.CustomerType, float>(weekdayDistribution);
			
			// Cuma
			Dictionary<CustomerBase.CustomerType, float> fridayDistribution = new Dictionary<CustomerBase.CustomerType, float>();
			fridayDistribution[CustomerBase.CustomerType.Regular] = 0.25f;
			fridayDistribution[CustomerBase.CustomerType.Worker] = 0.20f;
			fridayDistribution[CustomerBase.CustomerType.Nostalgic] = 0.10f;
			fridayDistribution[CustomerBase.CustomerType.Elite] = 0.12f;
			fridayDistribution[CustomerBase.CustomerType.Young] = 0.12f;
			fridayDistribution[CustomerBase.CustomerType.Bureaucrat] = 0.06f;
			fridayDistribution[CustomerBase.CustomerType.Emotional] = 0.08f;
			fridayDistribution[CustomerBase.CustomerType.UnderCover] = 0.02f;
			fridayDistribution[CustomerBase.CustomerType.Sapkali] = 0.02f;
			fridayDistribution[CustomerBase.CustomerType.Gangster] = 0.02f;
			fridayDistribution[CustomerBase.CustomerType.Foreigner] = 0.01f;
			
			_dailyDistributions[DayOfWeek.Friday] = fridayDistribution;
			
			// Hafta sonu (Cumartesi-Pazar)
			Dictionary<CustomerBase.CustomerType, float> weekendDistribution = new Dictionary<CustomerBase.CustomerType, float>();
			weekendDistribution[CustomerBase.CustomerType.Regular] = 0.20f;
			weekendDistribution[CustomerBase.CustomerType.Worker] = 0.15f;
			weekendDistribution[CustomerBase.CustomerType.Nostalgic] = 0.08f;
			weekendDistribution[CustomerBase.CustomerType.Elite] = 0.15f;
			weekendDistribution[CustomerBase.CustomerType.Young] = 0.18f;
			weekendDistribution[CustomerBase.CustomerType.Bureaucrat] = 0.04f;
			weekendDistribution[CustomerBase.CustomerType.Emotional] = 0.10f;
			weekendDistribution[CustomerBase.CustomerType.UnderCover] = 0.03f;
			weekendDistribution[CustomerBase.CustomerType.Sapkali] = 0.03f;
			weekendDistribution[CustomerBase.CustomerType.Gangster] = 0.03f;
			weekendDistribution[CustomerBase.CustomerType.Foreigner] = 0.01f;
			
			_dailyDistributions[DayOfWeek.Saturday] = new Dictionary<CustomerBase.CustomerType, float>(weekendDistribution);
			_dailyDistributions[DayOfWeek.Sunday] = new Dictionary<CustomerBase.CustomerType, float>(weekendDistribution);
		}
		
		// EKLENEN - Harcama kalıplarını başlat
		private void InitializeSpendingPatterns()
		{
			// Düzenli müşteri
			Dictionary<string, float> regularSpending = new Dictionary<string, float>();
			regularSpending["drinks"] = 0.6f;
			regularSpending["food"] = 0.3f;
			regularSpending["entertainment"] = 0.1f;
			_spendingPatterns[CustomerBase.CustomerType.Regular] = regularSpending;
			
			// İşçi
			Dictionary<string, float> workerSpending = new Dictionary<string, float>();
			workerSpending["drinks"] = 0.7f;
			workerSpending["food"] = 0.25f;
			workerSpending["entertainment"] = 0.05f;
			_spendingPatterns[CustomerBase.CustomerType.Worker] = workerSpending;
			
			// Elit müşteri
			Dictionary<string, float> eliteSpending = new Dictionary<string, float>();
			eliteSpending["drinks"] = 0.4f;
			eliteSpending["food"] = 0.2f;
			eliteSpending["entertainment"] = 0.4f;
			eliteSpending["vip_services"] = 0.4f;
			_spendingPatterns[CustomerBase.CustomerType.Elite] = eliteSpending;
			
			// Nostaljik müşteri
			Dictionary<string, float> nostalgicSpending = new Dictionary<string, float>();
			nostalgicSpending["drinks"] = 0.5f;
			nostalgicSpending["food"] = 0.4f;
			nostalgicSpending["entertainment"] = 0.1f;
			_spendingPatterns[CustomerBase.CustomerType.Nostalgic] = nostalgicSpending;
			
			// Duygusal müşteri
			Dictionary<string, float> emotionalSpending = new Dictionary<string, float>();
			emotionalSpending["drinks"] = 0.8f;
			emotionalSpending["food"] = 0.1f;
			emotionalSpending["entertainment"] = 0.1f;
			_spendingPatterns[CustomerBase.CustomerType.Emotional] = emotionalSpending;
			
			// Genç müşteri
			Dictionary<string, float> youngSpending = new Dictionary<string, float>();
			youngSpending["drinks"] = 0.7f;
			youngSpending["food"] = 0.1f;
			youngSpending["entertainment"] = 0.2f;
			_spendingPatterns[CustomerBase.CustomerType.Young] = youngSpending;
			
			// Bürokrat
			Dictionary<string, float> bureaucratSpending = new Dictionary<string, float>();
			bureaucratSpending["drinks"] = 0.5f;
			bureaucratSpending["food"] = 0.3f;
			bureaucratSpending["entertainment"] = 0.2f;
			bureaucratSpending["vip_services"] = 0.3f;
			_spendingPatterns[CustomerBase.CustomerType.Bureaucrat] = bureaucratSpending;
			
			// Sivil polis
			Dictionary<string, float> underCoverSpending = new Dictionary<string, float>();
			underCoverSpending["drinks"] = 0.7f;
			underCoverSpending["food"] = 0.2f;
			underCoverSpending["entertainment"] = 0.1f;
			_spendingPatterns[CustomerBase.CustomerType.UnderCover] = underCoverSpending;
			
			// Şapkalı müşteri
			Dictionary<string, float> sapkaliSpending = new Dictionary<string, float>();
			sapkaliSpending["drinks"] = 0.4f;
			sapkaliSpending["food"] = 0.2f;
			sapkaliSpending["entertainment"] = 0.4f;
			sapkaliSpending["vip_services"] = 0.5f;
			_spendingPatterns[CustomerBase.CustomerType.Sapkali] = sapkaliSpending;
			
			// Gangster
			Dictionary<string, float> gangsterSpending = new Dictionary<string, float>();
			gangsterSpending["drinks"] = 0.6f;
			gangsterSpending["food"] = 0.2f;
			gangsterSpending["entertainment"] = 0.2f;
			gangsterSpending["illegal"] = 0.3f;
			_spendingPatterns[CustomerBase.CustomerType.Gangster] = gangsterSpending;
			
			// Yabancı
			Dictionary<string, float> foreignerSpending = new Dictionary<string, float>();
			foreignerSpending["drinks"] = 0.6f;
			foreignerSpending["food"] = 0.3f;
			foreignerSpending["entertainment"] = 0.1f;
			_spendingPatterns[CustomerBase.CustomerType.Foreigner] = foreignerSpending;
			
			// Gelir kategorilerini başlat
			_revenueByCategory["drinks"] = 0f;
			_revenueByCategory["food"] = 0f;
			_revenueByCategory["entertainment"] = 0f;
			_revenueByCategory["vip_services"] = 0f;
			_revenueByCategory["illegal"] = 0f;
			
			// Müşteri tipine göre gelir kategorilerini başlat
			foreach (CustomerBase.CustomerType type in Enum.GetValues(typeof(CustomerBase.CustomerType)))
			{
				_revenueByCustomerType[type] = 0f;
			}
		}
		
		// EKLENEN - Özel etkinlik bonuslarını başlat
		private void InitializeEventBonuses()
		{
			// VIP Gecesi
			Dictionary<CustomerBase.CustomerType, float> vipNightBonus = new Dictionary<CustomerBase.CustomerType, float>();
			vipNightBonus[CustomerBase.CustomerType.Elite] = 0.4f;
			vipNightBonus[CustomerBase.CustomerType.Bureaucrat] = 0.3f;
			vipNightBonus[CustomerBase.CustomerType.Sapkali] = 0.3f;
			vipNightBonus[CustomerBase.CustomerType.Gangster] = 0.2f;
			_eventTypeBonus["vip_night"] = vipNightBonus;
			
			// Arabesk Gecesi
			Dictionary<CustomerBase.CustomerType, float> arabeskNightBonus = new Dictionary<CustomerBase.CustomerType, float>();
			arabeskNightBonus[CustomerBase.CustomerType.Emotional] = 0.4f;
			arabeskNightBonus[CustomerBase.CustomerType.Worker] = 0.3f;
			arabeskNightBonus[CustomerBase.CustomerType.Nostalgic] = 0.2f;
			_eventTypeBonus["arabesk_night"] = arabeskNightBonus;
			
			// Dans Gecesi
			Dictionary<CustomerBase.CustomerType, float> danceNightBonus = new Dictionary<CustomerBase.CustomerType, float>();
			danceNightBonus[CustomerBase.CustomerType.Young] = 0.4f;
			danceNightBonus[CustomerBase.CustomerType.Regular] = 0.2f;
			danceNightBonus[CustomerBase.CustomerType.Elite] = 0.2f;
			_eventTypeBonus["dance_night"] = danceNightBonus;
			
			// Pavyon Yıldönümü
			Dictionary<CustomerBase.CustomerType, float> anniversaryBonus = new Dictionary<CustomerBase.CustomerType, float>();
			// Tüm müşteri tipleri için bonus
			foreach (CustomerBase.CustomerType type in Enum.GetValues(typeof(CustomerBase.CustomerType)))
			{
				anniversaryBonus[type] = 0.25f;
			}
			_eventTypeBonus["anniversary"] = anniversaryBonus;
			
			// Yılbaşı Özel
			Dictionary<CustomerBase.CustomerType, float> newYearBonus = new Dictionary<CustomerBase.CustomerType, float>();
			// Tüm müşteri tipleri için yüksek bonus
			foreach (CustomerBase.CustomerType type in Enum.GetValues(typeof(CustomerBase.CustomerType)))
			{
				newYearBonus[type] = 0.35f;
			}
			_eventTypeBonus["new_year"] = newYearBonus;
		}
		
		// EKLENEN - İllegal kat erişim olasılıklarını başlat
		private void InitializeIllegalFloorAccess()
		{
			_illegalFloorAccessProbability[CustomerBase.CustomerType.Regular] = 0.1f;
			_illegalFloorAccessProbability[CustomerBase.CustomerType.Worker] = 0.15f;
			_illegalFloorAccessProbability[CustomerBase.CustomerType.Elite] = 0.4f;
			_illegalFloorAccessProbability[CustomerBase.CustomerType.Nostalgic] = 0.1f;
			_illegalFloorAccessProbability[CustomerBase.CustomerType.Emotional] = 0.15f;
			_illegalFloorAccessProbability[CustomerBase.CustomerType.Young] = 0.2f;
			_illegalFloorAccessProbability[CustomerBase.CustomerType.Bureaucrat] = 0.3f;
			_illegalFloorAccessProbability[CustomerBase.CustomerType.UnderCover] = 0.5f; // Yüksek olasılık çünkü görevleri
			_illegalFloorAccessProbability[CustomerBase.CustomerType.Sapkali] = 0.6f;
			_illegalFloorAccessProbability[CustomerBase.CustomerType.Gangster] = 0.7f;
			_illegalFloorAccessProbability[CustomerBase.CustomerType.Foreigner] = 0.05f;
		}
		
		// Özel müşteri gruplarını tanımla
		private void DefineSpecialGroups()
		{
			// Behzat Ç. ekibi özel grup - sivil polisler
			List<CustomerInfo> behzatTeam = new List<CustomerInfo>
			{
				new CustomerInfo("Behzat Ç.", 45, "Male", CustomerBase.CustomerType.UnderCover, 3000f, true, 
								"Dağınık giyimli, sinirli bakış, sürekli sigara"),
								
				new CustomerInfo("Harun Sinanoğlu", 40, "Male", CustomerBase.CustomerType.UnderCover, 2500f, false, 
								"Takım elbise, düzenli görünüm, keskin zeka"),
								
				new CustomerInfo("Hayalet", 35, "Male", CustomerBase.CustomerType.UnderCover, 2000f, false, 
								"İnce yapılı, tedirgin bakışlar, gizli konuşma"),
								
				new CustomerInfo("Akbaba", 38, "Male", CustomerBase.CustomerType.UnderCover, 2200f, false, 
								"İri yapılı, sert görünüm, kısa saçlar")
			};
			
			// Behzat Ç. ekibine özel tercihler ekle
			foreach (var member in behzatTeam)
			{
				member.CustomPreferences = new Dictionary<string, float>
				{
					{ "drink_raki", 0.8f },        // Rakı tercihi yüksek
					{ "drink_beer", 0.7f },        // Bira tercihi yüksek
					{ "music_arabesk", 0.6f },     // Arabesk müzik tercihi orta-yüksek
					{ "ambiance_intimate", 0.8f }, // Samimi ortam tercihi yüksek
					{ "ambiance_loud", 0.2f }      // Gürültülü ortam tercihi düşük
				};
			}
			
			_specialCustomerGroups["BehzatTeam"] = behzatTeam;
			
			// Şapkalılar grubu - Tarlasını satıp pavyonda harcayanlar
			List<CustomerInfo> sapkaliGroup = new List<CustomerInfo>
			{
				new CustomerInfo("Recep Dayı", 55, "Male", CustomerBase.CustomerType.Sapkali, 40000f, true, 
								"Kasket, köy kıyafetleri, elinde para destesi"),
								
				new CustomerInfo("İsmail Ağa", 60, "Male", CustomerBase.CustomerType.Sapkali, 35000f, true, 
								"Fötr şapka, eski usul giyim, bol altın"),
								
				new CustomerInfo("Musa Emmi", 58, "Male", CustomerBase.CustomerType.Sapkali, 45000f, true, 
								"Hasır şapka, yeni aldığı takım elbise, kalın bıyık")
			};
			
			foreach (var sapkali in sapkaliGroup)
			{
				sapkali.CustomPreferences = new Dictionary<string, float>
				{
					{ "drink_raki", 0.9f },          // Rakı tercihi çok yüksek
					{ "music_oyunHavasi", 0.9f },    // Oyun havası tercihi çok yüksek
					{ "staff_kons", 0.95f },         // Kons tercihi çok yüksek
					{ "generosity", 0.9f }           // Cömertlik çok yüksek
				};
			}
			
			_specialCustomerGroups["Sapkalilar"] = sapkaliGroup;
			
			// Bürokratlar grubu
			List<CustomerInfo> bureaucratGroup = new List<CustomerInfo>
			{
				new CustomerInfo("Faruk Bey", 50, "Male", CustomerBase.CustomerType.Bureaucrat, 8000f, true, 
								"Pahalı takım elbise, iyi bakımlı, disiplinli duruş"),
								
				new CustomerInfo("Turgut Bey", 48, "Male", CustomerBase.CustomerType.Bureaucrat, 7000f, true, 
								"Klasik giyim, dikkatli bakışlar, ölçülü konuşma"),
								
				new CustomerInfo("Necati Bey", 52, "Male", CustomerBase.CustomerType.Bureaucrat, 7500f, true, 
								"Resmi giyim, sürekli etrafı izleyen, dikkatli"),
								
				new CustomerInfo("Sadettin Bey", 55, "Male", CustomerBase.CustomerType.Bureaucrat, 8500f, true, 
								"Altın kol düğmeli takım elbise, güçlü duruş")
			};
			
			foreach (var bureaucrat in bureaucratGroup)
			{
				bureaucrat.CustomPreferences = new Dictionary<string, float>
				{
					{ "drink_whiskey", 0.8f },       // Viski tercihi yüksek
					{ "drink_raki", 0.7f },          // Rakı tercihi yüksek
					{ "ambiance_intimate", 0.9f },   // Samimi ortam tercihi çok yüksek
					{ "ambiance_luxurious", 0.8f },  // Lüks ortam tercihi yüksek
					{ "staff_kons", 0.8f }           // Kons tercihi yüksek
				};
			}
			
			_specialCustomerGroups["Burokratlar"] = bureaucratGroup;
			
			// İş adamları grubu
			List<CustomerInfo> businessmenGroup = new List<CustomerInfo>
			{
				new CustomerInfo("Nazım Bey", 45, "Male", CustomerBase.CustomerType.Elite, 15000f, true, 
								"Armani takım elbise, Rolex saat, kendinden emin duruş"),
								
				new CustomerInfo("Ferit Bey", 50, "Male", CustomerBase.CustomerType.Elite, 18000f, true, 
								"Özel dikim takım, ağır parfüm, keskin bakışlar"),
								
				new CustomerInfo("Selim Bey", 42, "Male", CustomerBase.CustomerType.Elite, 20000f, true, 
								"İtalyan ayakkabılar, son model telefon, tarz gözlük")
			};
			
			foreach (var businessman in businessmenGroup)
			{
				businessman.CustomPreferences = new Dictionary<string, float>
				{
					{ "drink_whiskey", 0.9f },       // Viski tercihi çok yüksek
					{ "drink_special", 0.85f },      // Özel kokteyl tercihi çok yüksek
					{ "ambiance_luxurious", 0.95f }, // Lüks ortam tercihi çok yüksek
					{ "staff_kons", 0.9f },          // Kons tercihi çok yüksek
					{ "music_modern", 0.8f }         // Modern müzik tercihi yüksek
				};
			}
			
			_specialCustomerGroups["IsAdamlari"] = businessmenGroup;
			
			// Disco Elysium karakterleri (Kim Kitsuragi ve Harry Du Bois)
			List<CustomerInfo> discoElysiumTeam = new List<CustomerInfo>
			{
				new CustomerInfo("Kim Kitsuragi", 43, "Male", CustomerBase.CustomerType.Foreigner, 4000f, true, 
								"Pilot ceketi, dikkatli bakış, not defteri, metodolojik tavır"),
								
				new CustomerInfo("Harry Du Bois", 44, "Male", CustomerBase.CustomerType.Foreigner, 3000f, true, 
								"Dağınık görünüm, alışılmadık kravat, bazen unutkan bakışlar")
			};
			
			// Kim Kitsuragi ve Harry Du Bois'a özel tercihler ekle
			discoElysiumTeam[0].CustomPreferences = new Dictionary<string, float> // Kim Kitsuragi
			{
				{ "drink_beer", 0.7f },           // Bira tercihi yüksek
				{ "drink_whiskey", 0.5f },        // Viski tercihi orta
				{ "drink_special", 0.3f },        // Kokteyl tercihi düşük
				{ "music_taverna", 0.6f },        // Nostaljik müzik tercihi orta-yüksek
				{ "ambiance_intimate", 0.8f },    // Samimi ortam tercihi yüksek
				{ "ambiance_loud", 0.2f },        // Gürültülü ortam tercihi düşük
				{ "generosity", 0.6f },           // Cömertlik orta düzey
				{ "observation", 0.95f }          // Gözlem yeteneği çok yüksek
			};
			
			discoElysiumTeam[1].CustomPreferences = new Dictionary<string, float> // Harry Du Bois
			{
				{ "drink_beer", 0.9f },           // Bira tercihi çok yüksek
				{ "drink_whiskey", 0.95f },       // Viski tercihi çok yüksek
				{ "drink_raki", 0.8f },           // Rakı tercihi yüksek (yeni içkiler denemeyi sever)
				{ "music_arabesk", 0.8f },        // Arabesk müzik tercihi yüksek (duygusal müzik sever)
				{ "music_taverna", 0.7f },        // Taverna müzik tercihi yüksek
				{ "ambiance_loud", 0.7f },        // Gürültülü ortam tercihi yüksek
				{ "generosity", 0.8f },           // Cömertlik yüksek (özellikle sarhoşken)
				{ "unpredictability", 0.95f }     // Tahmin edilemezlik çok yüksek
			};
			
			_specialCustomerGroups["DiscoElysiumTeam"] = discoElysiumTeam;

			// EKLENEN - Gangster grubu
			List<CustomerInfo> gangsterGroup = new List<CustomerInfo>
			{
				new CustomerInfo("Deli Yavuz", 38, "Male", CustomerBase.CustomerType.Gangster, 12000f, true, 
								"Deri ceket, omuz üstü kesim saç, gümüş kolye"),
				
				new CustomerInfo("Kasap Cemal", 45, "Male", CustomerBase.CustomerType.Gangster, 15000f, true, 
								"Geniş vücut, boğa boyun, kırık burun"),
				
				new CustomerInfo("İnce Suat", 32, "Male", CustomerBase.CustomerType.Gangster, 10000f, false, 
								"Zarif giyimli, tehlikeli bakışlar, sabit gülümseme")
			};
			
			foreach (var gangster in gangsterGroup)
			{
				gangster.CustomPreferences = new Dictionary<string, float>
				{
					{ "drink_raki", 0.8f },           // Rakı tercihi yüksek
					{ "drink_whiskey", 0.7f },        // Viski tercihi yüksek
					{ "music_arabesk", 0.9f },        // Arabesk müzik tercihi çok yüksek
					{ "staff_kons", 0.8f },           // Kons tercihi yüksek
					{ "illegal_activities", 0.9f },   // Yasadışı etkinliklere ilgi çok yüksek
					{ "aggression", 0.7f }            // Saldırganlık potansiyeli yüksek
				};
			}
			
			_specialCustomerGroups["Gangsterler"] = gangsterGroup;
		}
		
		// Masa pozisyonlarını bul
		private void FindTables()
		{
			var currentScene = GetTree().CurrentScene;
			
			if (currentScene != null)
			{
				if (currentScene.HasNode("Tables"))
				{
					var tablesNode = currentScene.GetNode("Tables");
					
					foreach (Node child in tablesNode.GetChildren())
					{
						if (child is Node3D table)
						{
							_availableTables.Add(table);
						}
					}
				}
			}
			
			GD.Print($"Found {_availableTables.Count} available tables");
		}
		
		// Pavyon konum noktalarını bul
		private void FindPavyonLocations()
		{
			var currentScene = GetTree().CurrentScene;
			
			if (currentScene != null)
			{
				if (currentScene.HasNode("EntrancePosition"))
				{
					_entrancePosition = currentScene.GetNode<Node3D>("EntrancePosition").GlobalPosition;
				}
				
				if (currentScene.HasNode("ExitPosition"))
				{
					_exitPosition = currentScene.GetNode<Node3D>("ExitPosition").GlobalPosition;
				}
				
				if (currentScene.HasNode("BathroomPosition"))
				{
					_bathroomPosition = currentScene.GetNode<Node3D>("BathroomPosition").GlobalPosition;
				}
				
				// EKLENEN - İllegal kat girişini bul
				if (currentScene.HasNode("IllegalFloorEntrance"))
				{
					_illegalFloorEntrancePosition = currentScene.GetNode<Node3D>("IllegalFloorEntrance").GlobalPosition;
				}
			}
			
			// Giriş ve çıkış pozisyonları yoksa varsayılan değerleri ayarla
			if (_entrancePosition == Vector3.Zero)
			{
				_entrancePosition = new Vector3(0, 0, 10);
			}
			
			if (_exitPosition == Vector3.Zero)
			{
				_exitPosition = new Vector3(0, 0, -10);
			}
			
			if (_bathroomPosition == Vector3.Zero)
			{
				_bathroomPosition = new Vector3(10, 0, 0);
			}
			
			if (_illegalFloorEntrancePosition == Vector3.Zero)
			{
				_illegalFloorEntrancePosition = new Vector3(-10, 0, 0);
			}
		}
		
		// Memnuniyet modifikatörlerini sıfırla
		private void ResetSatisfactionModifiers()
		{
			_satisfactionModifiers.Clear();
			
			// Temel memnuniyet modifikatörleri
			_satisfactionModifiers["decor"] = 0.0f;         // Dekorasyon etkisi
			_satisfactionModifiers["music"] = 0.0f;         // Müzik etkisi
			_satisfactionModifiers["service"] = 0.0f;       // Servis kalitesi etkisi
			_satisfactionModifiers["drink_quality"] = 0.0f; // İçki kalitesi etkisi
			_satisfactionModifiers["food_quality"] = 0.0f;  // Yemek kalitesi etkisi
			_satisfactionModifiers["cleanliness"] = 0.0f;   // Temizlik etkisi
			_satisfactionModifiers["entertainment"] = 0.0f; // Eğlence etkisi
			_satisfactionModifiers["crowd"] = 0.0f;         // Kalabalık etkisi
			_satisfactionModifiers["price"] = 0.0f;         // Fiyat etkisi
			_satisfactionModifiers["ambiance"] = 0.0f;      // Ambiyans etkisi
		}

		// EKLENEN - Zaman bazlı dağılım kontrolü
		private void CheckTimeDistributionUpdate()
		{
			// Oyun saatini al
			int currentHour = GetCurrentHour();
			DayOfWeek currentDayOfWeek = GetCurrentDayOfWeek();
			
			// Son güncellemeden beri 15 dakika geçmiş mi kontrol et (gerçek zaman)
			TimeSpan timeSinceLastUpdate = DateTime.Now - _lastTimeDistributionUpdate;
			if (timeSinceLastUpdate.TotalMinutes >= 15)
			{
				// Saatlik dağılımı güncelle
				if (_hourlyDistributions.ContainsKey(currentHour))
				{
					// Mevcut dağılımı _customerTypeDistribution ile %70-%30 karıştır
					Dictionary<CustomerBase.CustomerType, float> hourlyDist = _hourlyDistributions[currentHour];
					Dictionary<CustomerBase.CustomerType, float> dailyDist = _dailyDistributions.ContainsKey(currentDayOfWeek) ? 
																		   _dailyDistributions[currentDayOfWeek] : null;
					
					foreach (var type in _customerTypeDistribution.Keys.ToList())
					{
						float hourlyFactor = hourlyDist.ContainsKey(type) ? hourlyDist[type] : _customerTypeDistribution[type];
						float dailyFactor = dailyDist != null && dailyDist.ContainsKey(type) ? dailyDist[type] : _customerTypeDistribution[type];
						
						// Saatlik dağılım %50, günlük dağılım %30, temel dağılım %20 etkili
						_customerTypeDistribution[type] = (hourlyFactor * 0.5f) + (dailyFactor * 0.3f) + (_customerTypeDistribution[type] * 0.2f);
					}
					
					// Normalizasyon - toplamı 1.0 yap
					NormalizeCustomerDistribution();
				}
				
				// Zamanı güncelle
				_lastTimeDistributionUpdate = DateTime.Now;
			}
		}
		
		// EKLENEN - Müşteri dağılımını normalize et
		private void NormalizeCustomerDistribution()
		{
			float sum = 0f;
			foreach (var value in _customerTypeDistribution.Values)
			{
				sum += value;
			}
			
			if (sum > 0f)
			{
				foreach (var type in _customerTypeDistribution.Keys.ToList())
				{
					_customerTypeDistribution[type] /= sum;
				}
			}
		}
		
		// EKLENEN - Oyun saatini al
		private int GetCurrentHour()
		{
			if (GetTree().Root.HasNode("TimeManager"))
			{
				var timeManager = GetTree().Root.GetNode("TimeManager");
				if (timeManager.HasMethod("GetCurrentHour"))
				{
					return (int)timeManager.Call("GetCurrentHour");
				}
			}
			return DateTime.Now.Hour; // Varsayılan olarak gerçek saati kullan
		}
		
		// EKLENEN - Oyun gününü al
		private DayOfWeek GetCurrentDayOfWeek()
		{
			if (GetTree().Root.HasNode("TimeManager"))
			{
				var timeManager = GetTree().Root.GetNode("TimeManager");
				if (timeManager.HasMethod("GetCurrentDayOfWeekEnum"))
				{
					return (DayOfWeek)timeManager.Call("GetCurrentDayOfWeekEnum");
				}
			}
			return DateTime.Now.DayOfWeek; // Varsayılan olarak gerçek günü kullan
		}
		
		// EKLENEN - Çatışma güncelleme
		private void UpdateConflicts(float delta)
		{
			// Çatışma zamanlayıcısını artır
			_conflictCheckTimer += delta;
			
			// Mevcut çatışmaları güncelle
			for (int i = _activeConflicts.Count - 1; i >= 0; i--)
			{
				CustomerConflict conflict = _activeConflicts[i];
				conflict.Update(delta);
				
				if (conflict.IsResolved)
				{
					// Çatışmayı çöz
					ResolveConflict(conflict);
					_activeConflicts.RemoveAt(i);
				}
			}
			
			// Belirli aralıklarla yeni çatışma olasılığı kontrolü
			if (_conflictCheckTimer >= CONFLICT_CHECK_INTERVAL)
			{
				_conflictCheckTimer = 0f;
				CheckForNewConflict();
			}
		}
		
		// EKLENEN - Yeni çatışma kontrolü
		private void CheckForNewConflict()
		{
			// İki müşteriden az varsa çatışma olmaz
			if (_allCustomers.Count < 2 || _activeConflicts.Count > 0)
				return;
			
			// Çatışma olasılığı
			float baseConflictChance = _conflictProbability;
			
			// Saat ve kalabalık faktörlerini hesapla
			int hour = GetCurrentHour();
			float timeFactor = 1.0f;
			
			// Gece ilerledikçe çatışma olasılığı artar
			if (hour >= 0 && hour < 4) // 00:00-04:00
			{
				timeFactor = 1.5f;
			}
			else if (hour >= 22) // 22:00-00:00
			{
				timeFactor = 1.2f;
			}
			
			// Kalabalık faktörü
			float crowdFactor = 1.0f + (GetCustomerDensity() * 0.5f);
			
			// Toplam çatışma olasılığı
			float conflictChance = baseConflictChance * timeFactor * crowdFactor;
			
			// Çatışma kontrolü
			if (GD.Randf() < conflictChance)
			{
				StartRandomConflict();
			}
		}
		
		// EKLENEN - Rastgele çatışma başlat
		private void StartRandomConflict()
{
	// Çatışma başlatabilecek müşterileri filtrele
	// Sarhoşluk, saldırganlık veya huzursuzluk gibi özelliğe sahip müşteriler öncelikli
	List<CustomerBase> potentialInstigators = _allCustomers
		.Where(c => GetCustomerAggressionLevel(c) > 0.6f || GetCustomerDrunkennessLevel(c) > 0.8f)
		.ToList();
	
	// Potansiyel başlatıcı yoksa rastgele bir müşteri seç
	if (potentialInstigators.Count == 0)
	{
		potentialInstigators = _allCustomers.Where(c => 
			c.CustomerTypeValue != CustomerBase.CustomerType.Elite &&
			c.CustomerTypeValue != CustomerBase.CustomerType.Bureaucrat &&
			c.CustomerTypeValue != CustomerBase.CustomerType.Foreigner
		).ToList();
		
		// Hala yok ise çatışma başlatma
		if (potentialInstigators.Count == 0)
			return;
	}
	
	// Potansiyel başlatıcılardan bir tanesini seç
	CustomerBase instigator = potentialInstigators[GD.RandRange(0, potentialInstigators.Count - 1)];
	
	// Düzenli müşterileri kontrol et, daha önce sorun çıkarmış mı?
	if (_regularCustomers.ContainsKey(instigator.ID))
	{
		// Sorun çıkaran bir düzenli müşteri olarak işaretle
		_regularCustomers[instigator.ID].IsTroublesome = true;
		_regularCustomers[instigator.ID].ConflictCount++;
	}
	
	// Hedef olabilecek müşterileri filtrele (başlatıcı hariç)
	List<CustomerBase> potentialTargets = _allCustomers
		.Where(c => c.ID != instigator.ID && 
					!c.IsLeaving && 
					Vector3.Distance(c.GlobalPosition, instigator.GlobalPosition) < 15f)
		.ToList();
	
	if (potentialTargets.Count == 0)
		return;
	
	// Hedef müşteriyi seç
	CustomerBase target = potentialTargets[GD.RandRange(0, potentialTargets.Count - 1)];
	
	// Çatışma tipini belirle
	string conflictType = "verbal"; // Varsayılan sözlü tartışma
	float severity = GD.Randf() * 0.7f + 0.3f; // 0.3-1.0 arası
	
	// Sarhoşluk ve saldırganlık seviyelerini kontrol et
	float instigatorDrunkLevel = GetCustomerDrunkennessLevel(instigator);
	float instigatorAggrLevel = GetCustomerAggressionLevel(instigator);
	
	// Yüksek sarhoşluk ve saldırganlık fiziksel çatışmaya yol açabilir
	if (instigatorDrunkLevel > 0.8f && instigatorAggrLevel > 0.7f)
	{
		conflictType = "physical";
		severity += 0.2f;
	}
	// Orta düzey sarhoşluk ve yüksek saldırganlık şiddetli sözel kavgaya yol açabilir
	else if (instigatorDrunkLevel > 0.6f && instigatorAggrLevel > 0.6f)
	{
		conflictType = "heated_verbal";
		severity += 0.1f;
	}
	
	// Şiddet seviyesini sınırla
	severity = Mathf.Clamp(severity, 0.3f, 1.0f);
	
	// Çatışmayı oluştur ve aktif çatışmalara ekle
	CustomerConflict conflict = new CustomerConflict(instigator.ID, target.ID, conflictType, severity);
	_activeConflicts.Add(conflict);
	
	// Çatışma sinyali gönder
	EmitSignal(SignalName.CustomerConflictStartedEventHandler, instigator.ID, target.ID, conflictType);
	
	// Güvenlik görevlisine bildirim
	NotifySecurityAboutConflict(conflict);
	
	// Çevredeki müşterileri etkile (korku, rahatsızlık)
	AffectSurroundingCustomers(instigator.GlobalPosition, severity);
	
	GD.Print($"Conflict started: {instigator.Name} vs {target.Name}, type: {conflictType}, severity: {severity}");
}

// EKLENEN - Çatışma çözme
private void ResolveConflict(CustomerConflict conflict)
{
	bool peacefulResolution = false;
	
	// Çatışma şiddetine göre barışçıl çözüm olasılığı
	float peacefulChance = 0.7f - (conflict.Severity * 0.5f);
	
	// Güvenlik görevlisi müdahale ettiyse barışçıl çözüm şansı artar
	if (GetSecurityIntervention())
	{
		peacefulChance += 0.3f;
	}
	
	// Barışçıl çözüm kontrolü
	if (GD.Randf() < peacefulChance)
	{
		peacefulResolution = true;
	}
	
	// Sonuca göre müşterileri etkile
	CustomerBase instigator = GetCustomerById(conflict.InstigatorId);
	CustomerBase target = GetCustomerById(conflict.TargetId);
	
	if (instigator != null)
	{
		// Barışçıl çözümde veya güvenlik müdahalesinde müşteri kalmaya devam edebilir
		if (peacefulResolution)
		{
			AdjustCustomerSatisfaction(instigator, -0.2f, "conflict_resolved");
		}
		// Barışçıl olmayan çözüm - müşterinin kavga sonrası davranışını belirle
		else
		{
			AdjustCustomerSatisfaction(instigator, -0.4f, "conflict_escalated");
			
			// Çatışma şiddetliyse müşteri pavyondan ayrılabilir
			if (conflict.Severity > 0.7f && GD.Randf() < 0.8f)
			{
				CustomerLeave(instigator, "escorted_out");
			}
		}
	}
	
	if (target != null)
	{
		// Hedef müşterinin memnuniyeti düşer
		float satisfactionImpact = peacefulResolution ? -0.15f : -0.3f;
		AdjustCustomerSatisfaction(target, satisfactionImpact, "involved_in_conflict");
		
		// Barışçıl olmayan çözümde ve şiddetli çatışmada hedef de ayrılabilir
		if (!peacefulResolution && conflict.Severity > 0.7f && GD.Randf() < 0.5f)
		{
			CustomerLeave(target, "escorted_out");
		}
	}
	
	// Çatışma çözüm sinyali gönder
	EmitSignal(SignalName.CustomerConflictResolvedEventHandler, conflict.ConflictId, peacefulResolution);
	
	GD.Print($"Conflict resolved: {conflict.ConflictId}, peaceful: {peacefulResolution}");
}

// EKLENEN - Müşteri saldırganlık seviyesini al
private float GetCustomerAggressionLevel(CustomerBase customer)
{
	if (customer == null) return 0f;
	
	// Temel saldırganlık seviyesi
	float baseAggression = 0.3f;
	
	// Müşteri tipine göre temel saldırganlık
	switch (customer.CustomerTypeValue)
	{
		case CustomerBase.CustomerType.Gangster:
			baseAggression = 0.7f;
			break;
		case CustomerBase.CustomerType.Young:
			baseAggression = 0.5f;
			break;
		case CustomerBase.CustomerType.Emotional:
			baseAggression = 0.6f;
			break;
		case CustomerBase.CustomerType.Worker:
			baseAggression = 0.5f;
			break;
		case CustomerBase.CustomerType.Elite:
			baseAggression = 0.3f;
			break;
		case CustomerBase.CustomerType.Bureaucrat:
			baseAggression = 0.2f;
			break;
		case CustomerBase.CustomerType.UnderCover:
			baseAggression = 0.4f;
			break;
	}
	
	// AggressionLevel özelliği varsa kullan
	if (customer.HasProperty("AggressionLevel"))
	{
		return (float)customer.GetProperty("AggressionLevel");
	}
	
	// Sarhoşluk saldırganlığı artırır
	float drunkennessBonus = GetCustomerDrunkennessLevel(customer) * 0.3f;
	
	// Memnuniyetsizlik saldırganlığı artırır
	float satisfactionPenalty = (1f - customer.Satisfaction) * 0.2f;
	
	// Düzenli müşteri ise önceki çatışma geçmişini kontrol et
	if (_regularCustomers.ContainsKey(customer.ID) && _regularCustomers[customer.ID].IsTroublesome)
	{
		baseAggression += 0.1f * Mathf.Min(_regularCustomers[customer.ID].ConflictCount, 3);
	}
	
	return Mathf.Clamp(baseAggression + drunkennessBonus + satisfactionPenalty, 0f, 1f);
}

// EKLENEN - Müşteri sarhoşluk seviyesini al
private float GetCustomerDrunkennessLevel(CustomerBase customer)
{
	if (customer == null) return 0f;
	
	// DrunkennessLevel özelliği varsa kullan
	if (customer.HasProperty("DrunkennessLevel"))
	{
		return (float)customer.GetProperty("DrunkennessLevel");
	}
	
	// Varsayılan değer
	return customer.AlcoholConsumed / customer.AlcoholTolerance;
}

// EKLENEN - Güvenlik görevlisi müdahale ettimi
private bool GetSecurityIntervention()
{
	// Aktif güvenlik görevlisi kontrolü
	if (GetTree().Root.HasNode("GameManager/StaffManager"))
	{
		var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
		
		if (staffManager.HasMethod("GetSecurityResponse"))
		{
			return (bool)staffManager.Call("GetSecurityResponse");
		}
	}
	
	return GD.Randf() < 0.5f; // Varsayılan olarak %50 şans
}

// EKLENEN - Güvenlik görevlisini çatışma hakkında uyar
private void NotifySecurityAboutConflict(CustomerConflict conflict)
{
	if (GetTree().Root.HasNode("GameManager/StaffManager"))
	{
		var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
		
		if (staffManager.HasMethod("HandleCustomerConflict"))
		{
			CustomerBase instigator = GetCustomerById(conflict.InstigatorId);
			CustomerBase target = GetCustomerById(conflict.TargetId);
			
			if (instigator != null && target != null)
			{
				List<Node3D> participants = new List<Node3D> { instigator, target };
				staffManager.Call("HandleCustomerConflict", instigator.GlobalPosition, participants, conflict.Severity);
			}
		}
	}
}

// EKLENEN - Çevredeki müşterileri etkile
private void AffectSurroundingCustomers(Vector3 conflictPosition, float severity)
{
	float effectRadius = 10f + (severity * 15f); // 10-25 birim arası etki mesafesi
	
	foreach (var customer in _allCustomers)
	{
		float distance = Vector3.Distance(customer.GlobalPosition, conflictPosition);
		
		if (distance <= effectRadius)
		{
			// Mesafeye bağlı olarak etki gücünü hesapla
			float effectStrength = 1f - (distance / effectRadius);
			float impact = -0.1f * severity * effectStrength;
			
			// Çatışmadan etkilenme
			AdjustCustomerSatisfaction(customer, impact, "nearby_conflict");
			
			// Elite veya Bureaucrat müşteriler daha hassas
			if ((customer.CustomerTypeValue == CustomerBase.CustomerType.Elite || 
				 customer.CustomerTypeValue == CustomerBase.CustomerType.Bureaucrat) && 
				severity > 0.7f && effectStrength > 0.5f)
			{
				// Yüksek şiddetli çatışma ve yakın mesafe - müşteri ayrılabilir
				if (GD.Randf() < 0.3f)
				{
					CustomerLeave(customer, "disturbed_by_conflict");
				}
			}
		}
	}
}

// EKLENEN - İllegal kat baskın kontrolü
private void CheckIllegalFloorRaid(float delta)
{
	// İllegal kat açık değilse kontrol etme
	if (!_isIllegalFloorOpen || _customersInIllegalFloor.Count == 0)
		return;
	
	// Sivil polis varlığı baskın olasılığını artırır
	float raidModifier = 1.0f;
	int underCoverCount = _allCustomers.Count(c => c.CustomerTypeValue == CustomerBase.CustomerType.UnderCover);
	
	if (underCoverCount > 0)
	{
		raidModifier = 1.0f + (underCoverCount * 0.5f);
	}
	
	// İllegal kattaki müşteri sayısı fazla olduğunda dikkat çeker
	if (_customersInIllegalFloor.Count > 10)
	{
		raidModifier *= 1.5f;
	}
	
	// Baskın olasılığı kontrolü
	float raidChance = _illegalFloorRaidProbability * raidModifier * delta;
	
	if (GD.Randf() < raidChance)
	{
		TriggerPoliceRaid();
	}
}

// EKLENEN - Polis baskını tetikle
private void TriggerPoliceRaid()
{
	// İllegal kattaki müşteri sayısını al
	int customersInIllegalFloor = _customersInIllegalFloor.Count;
	
	// Raid sinyali gönder
	EmitSignal(SignalName.PoliceRaidTriggeredEventHandler, "illegal_floor_activities", customersInIllegalFloor);
	
	// Yakalanabilecek müşterileri belirle (kaçamayacak olanlar)
	List<CustomerBase> caughtCustomers = new List<CustomerBase>();
	
	foreach (string customerId in _customersInIllegalFloor)
	{
		CustomerBase customer = GetCustomerById(customerId);
		if (customer != null)
		{
			// Chance to get caught based on customer type
			float catchChance = 0.8f; // Default high chance
			
			switch (customer.CustomerTypeValue)
			{
				case CustomerBase.CustomerType.Elite:
				case CustomerBase.CustomerType.Bureaucrat:
					catchChance = 0.3f; // Influential customers have better chance to escape
					break;
				case CustomerBase.CustomerType.Gangster:
					catchChance = 0.5f; // Gangsters know escape routes
					break;
				case CustomerBase.CustomerType.UnderCover:
					catchChance = 0.0f; // Undercover police don't get caught
					break;
			}
			
			if (GD.Randf() < catchChance)
			{
				caughtCustomers.Add(customer);
			}
		}
	}
	
	// Yakalanan müşterileri pavyondan çıkar
	foreach (var customer in caughtCustomers)
	{
		CustomerLeave(customer, "caught_in_raid");
		
		// Düzenli müşteriyse itibarına zarar ver
		if (_regularCustomers.ContainsKey(customer.ID))
		{
			_regularCustomers[customer.ID].Loyalty = Mathf.Max(0.1f, _regularCustomers[customer.ID].Loyalty - 0.3f);
			_regularCustomers[customer.ID].HasVisitedIllegalFloor = true;
		}
	}
	
	// İllegal katın kapatılması
	_isIllegalFloorOpen = false;
	_customersInIllegalFloor.Clear();
	
	// Diğer müşterilerin rahatsız olması
	foreach (var customer in _allCustomers)
	{
		AdjustCustomerSatisfaction(customer, -0.2f, "police_raid");
		
		// Belirli müşteri tipleri baskın sırasında ayrılmayı tercih edebilir
		if ((customer.CustomerTypeValue == CustomerBase.CustomerType.Elite || 
			 customer.CustomerTypeValue == CustomerBase.CustomerType.Bureaucrat || 
			 customer.CustomerTypeValue == CustomerBase.CustomerType.Gangster) &&
			GD.Randf() < 0.7f)
		{
			CustomerLeave(customer, "police_raid_escape");
		}
	}
	
	// Mekan itibarı düşüşü için EventManager'a bildir
	if (GetTree().Root.HasNode("GameManager/ReputationManager"))
	{
		var reputationManager = GetTree().Root.GetNode("GameManager/ReputationManager");
		
		if (reputationManager.HasMethod("DecreaseReputation"))
		{
			reputationManager.Call("DecreaseReputation", 20, "police_raid"); // Ciddi itibar kaybı
		}
	}
	
	GD.Print($"Police raid on illegal floor! {caughtCustomers.Count} customers caught, {customersInIllegalFloor} total in illegal floor.");
}

// EKLENEN - Rekabet kontrolü
private void CheckCompetition(float delta)
{
	// Rekabet kontrolü için zamanı artır
	_competitionTimer += delta;
	
	// Belirli aralıklarla rekabet kontrolü
	if (_competitionTimer >= COMPETITION_CHECK_INTERVAL)
	{
		_competitionTimer = 0f;
		
		// Düzenli müşteriler arasından rakiplere gidebilecekleri seç
		List<string> potentialDefectors = new List<string>();
		
		foreach (var entry in _regularCustomers)
		{
			string customerId = entry.Key;
			CustomerRelationship relationship = entry.Value;
			
			// Güncel müşteri değilse (pavyonda olmayan)
			if (!_customersById.ContainsKey(customerId))
			{
				// Sadakat düşükse potansiyel kayıp
				if (relationship.Loyalty < 0.5f)
				{
					potentialDefectors.Add(customerId);
				}
				// Sadakat orta seviyede ise daha düşük olasılıkla kayıp
				else if (relationship.Loyalty < 0.7f && GD.Randf() < 0.3f)
				{
					potentialDefectors.Add(customerId);
				}
			}
		}
		
		// Rakip çekim oranı ve potansiyel kaçaklar varsa
		if (potentialDefectors.Count > 0 && GD.Randf() < _competitorAttractionRate)
		{
			// Rastgele bir müşteriyi seç
			string defectorId = potentialDefectors[GD.RandRange(0, potentialDefectors.Count - 1)];
			
			// Rakibe gitme sebebi
			string reason = DetermineDefectionReason(_regularCustomers[defectorId]);
			
			// Müşteriyi rakibe kaybetme
			LoseCustomerToCompetitor(defectorId, reason);
		}
	}
}

// EKLENEN - Müşteri kaybetme sebebini belirle
private string DetermineDefectionReason(CustomerRelationship relationship)
{
	// En düşük memnuniyet faktörüne sahip kategoriyi bul
	string worstCategory = "general";
	float lowestScore = float.MaxValue;
	
	if (relationship.Preferences.Count > 0)
	{
		foreach (var pref in relationship.Preferences)
		{
			if (pref.Value < lowestScore)
			{
				lowestScore = pref.Value;
				worstCategory = pref.Key;
			}
		}
	}
	
	// Kategoriye göre sebep belirle
	switch (worstCategory)
	{
		case "drink_quality":
		case "drink_selection":
			return "better_drinks";
		case "service_quality":
		case "service_speed":
			return "better_service";
		case "ambiance":
		case "music":
			return "better_atmosphere";
		case "cleanliness":
			return "cleaner_venue";
		case "security":
			return "safer_venue";
		case "entertainment":
			return "better_entertainment";
		case "price":
			return "cheaper_prices";
		case "food_quality":
			return "better_food";
		case "staff_relationship":
			return "better_staff_relations";
		default:
			return "general_preference";
	}
}

// EKLENEN - Müşteriyi rakibe kaybet
private void LoseCustomerToCompetitor(string customerId, string reason)
{
	if (_regularCustomers.ContainsKey(customerId))
	{
		// Sadakati düşür
		_regularCustomers[customerId].Loyalty = Mathf.Max(0.1f, _regularCustomers[customerId].Loyalty - 0.2f);
		
		// Kayıp sinyali gönder
		EmitSignal(SignalName.CustomerLostToCompetitorEventHandler, customerId, reason);
		
		// İstatistiklere ekle
		// Burada rakibe giden müşteri sayısı takip edilebilir
		
		GD.Print($"Regular customer {_regularCustomers[customerId].FullName} lost to competitor. Reason: {reason}");
	}
}

// Rastgele müşteri oluştur
private void GenerateRandomCustomer()
{
	// Yeni müşteri oluştur
	bool isExistingRegular = false;
	CustomerBase customer = null;
	
	// EKLENEN - Düzenli bir müşterinin tekrar gelmesi kontrolü
	if (_regularCustomers.Count > 0 && GD.Randf() < 0.3f)
	{
		// Rastgele bir düzenli müşteri seç
		List<string> regularIds = new List<string>(_regularCustomers.Keys);
		string regularId = regularIds[GD.RandRange(0, regularIds.Count - 1)];
		
		// Mevcut müşteri listesinde olmadığından emin ol
		if (!_customersById.ContainsKey(regularId))
		{
			// Düzenli müşteri verilerini al
			CustomerRelationship relationship = _regularCustomers[regularId];
			
			// Müşteri tipini belirle
			CustomerBase.CustomerType customerType = relationship.Type;
			
			// Müşteriyi oluştur
			customer = CreateCustomer(relationship.FullName, regularId, customerType);
			
			if (customer != null)
			{
				// Müşteri özelliklerini ayarla (sadakat, vb.)
				customer.FriendlinessModifier = relationship.Loyalty;
				customer.SpendingModifier = Mathf.Max(0.8f, relationship.Loyalty);
				customer.SetProperty("IsRegularCustomer", true);
				customer.SetProperty("VisitCount", relationship.VisitCount);
				
				// İllegal kat ziyaret geçmişi
				if (relationship.HasVisitedIllegalFloor)
				{
					customer.SetProperty("HasVisitedIllegalFloor", true);
				}
				
				// Tercihler varsa ayarla
				if (relationship.Preferences.Count > 0)
				{
					foreach (var pref in relationship.Preferences)
					{
						customer.SetPreference(pref.Key, pref.Value);
					}
				}
				
				isExistingRegular = true;
				
				// Ziyareti güncelle
				relationship.UpdateVisit();
			}
		}
	}
	
	// Yeni normal müşteri oluşturma
	if (customer == null)
	{
		// Müşteri tipini belirle
		CustomerBase.CustomerType customerType = DetermineRandomCustomerType();
		
		// Özel grup kontrolü
		if (GD.Randf() < 0.05f) // %5 olasılıkla özel grup
		{
			ProcessSpecialGroup();
			return;
		}
		
		// Rastgele ad oluştur
		string gender = GD.Randf() < 0.8f ? "Male" : "Female"; // %80 Erkek, %20 Kadın
		string fullName = GenerateRandomName(gender);
		
		// Müşteriyi oluştur
		customer = CreateCustomer(fullName, null, customerType);
	}
	
	// Müşteri oluşturulamadıysa çık
	if (customer == null)
		return;
	
	// Takip listelerine ekle
	_allCustomers.Add(customer);
	_customersById[customer.ID] = customer;
	
	// Tür bazlı listeye ekle
	if (!_customersByType.ContainsKey(customer.CustomerTypeValue))
	{
		_customersByType[customer.CustomerTypeValue] = new List<CustomerBase>();
	}
	_customersByType[customer.CustomerTypeValue].Add(customer);
	
	// VIP müşteri kontrolü
	if (customer.IsVIP)
	{
		_vipCustomers.Add(customer);
		EmitSignal(SignalName.VIPCustomerEnteredEventHandler, customer.ID, customer.CustomerTypeString);
	}
	
	// Masa atama
	AssignTable(customer);
	
	// Düzenli müşteri tanıma
	if (!isExistingRegular && customer.SetProperty("IsRegularCustomer", false))
	{
		customer.SetProperty("VisitCount", 1);
	}
	
	// EKLENEN - Düzenli müşteri ilişkisini başlat veya güncelle
	if (!isExistingRegular)
	{
		// Yeni müşteri ilişkisini başlat
		if (!_regularCustomers.ContainsKey(customer.ID))
		{
			_regularCustomers[customer.ID] = new CustomerRelationship(
				customer.ID, 
				customer.Name, 
				customer.CustomerTypeValue
			);
			
			// Müşteri tipine göre başlangıç tercihleri
			InitializeCustomerPreferences(_regularCustomers[customer.ID]);
		}
	}
	
	// EKLENEN - Kons atama kontrolü (özellikle Elite, Sapkali ve Bureaucrat için)
	if (customer.CustomerTypeValue == CustomerBase.CustomerType.Elite || 
		customer.CustomerTypeValue == CustomerBase.CustomerType.Sapkali || 
		customer.CustomerTypeValue == CustomerBase.CustomerType.Bureaucrat ||
		GD.Randf() < 0.2f) // Diğer tipler için %20 şans
	{
		AssignKonsToCustomer(customer);
	}
	
	// Müşteri girdi sinyali gönder
	EmitSignal(SignalName.CustomerEnteredEventHandler, customer.ID, customer.CustomerTypeString);
	
	GD.Print($"New customer generated: {customer.Name}, Type: {customer.CustomerTypeString}, Table: {customer.AssignedTable}");
}

// EKLENEN - Düzenli müşteri tercihlerini başlat
private void InitializeCustomerPreferences(CustomerRelationship relationship)
{
	Dictionary<string, float> preferences = new Dictionary<string, float>();
	
	// Müşteri tipine göre tercihler
	switch (relationship.Type)
	{
		case CustomerBase.CustomerType.Regular:
			preferences["drink_beer"] = 0.6f + GD.Randf() * 0.3f;
			preferences["drink_raki"] = 0.5f + GD.Randf() * 0.3f;
			preferences["music_taverna"] = 0.5f + GD.Randf() * 0.4f;
			preferences["service_speed"] = 0.6f + GD.Randf() * 0.3f;
			break;
		
		case CustomerBase.CustomerType.Worker:
			preferences["drink_beer"] = 0.7f + GD.Randf() * 0.2f;
			preferences["drink_raki"] = 0.6f + GD.Randf() * 0.3f;
			preferences["music_arabesk"] = 0.7f + GD.Randf() * 0.3f;
			preferences["price"] = 0.8f + GD.Randf() * 0.2f; // Fiyat duyarlılığı
			break;
		
		case CustomerBase.CustomerType.Elite:
			preferences["drink_whiskey"] = 0.8f + GD.Randf() * 0.2f;
			preferences["drink_special"] = 0.7f + GD.Randf() * 0.3f;
			preferences["music_modern"] = 0.7f + GD.Randf() * 0.3f;
			preferences["ambiance_luxurious"] = 0.9f + GD.Randf() * 0.1f;
			preferences["staff_kons"] = 0.8f + GD.Randf() * 0.2f;
			break;
			
		case CustomerBase.CustomerType.Nostalgic:
			preferences["drink_raki"] = 0.8f + GD.Randf() * 0.2f;
			preferences["music_taverna"] = 0.9f + GD.Randf() * 0.1f;
			preferences["ambiance_traditional"] = 0.8f + GD.Randf() * 0.2f;
			preferences["food_traditional"] = 0.7f + GD.Randf() * 0.3f;
			break;
			
		case CustomerBase.CustomerType.Emotional:
			preferences["drink_raki"] = 0.9f + GD.Randf() * 0.1f;
			preferences["music_arabesk"] = 0.9f + GD.Randf() * 0.1f;
			preferences["ambiance_intimate"] = 0.7f + GD.Randf() * 0.3f;
			preferences["staff_kons"] = 0.6f + GD.Randf() * 0.3f;
			break;
			
		case CustomerBase.CustomerType.Young:
			preferences["drink_cocktail"] = 0.7f + GD.Randf() * 0.3f;
			preferences["drink_vodka"] = 0.6f + GD.Randf() * 0.3f;
			preferences["music_modern"] = 0.8f + GD.Randf() * 0.2f;
			preferences["music_dance"] = 0.7f + GD.Randf() * 0.3f;
			preferences["ambiance_energetic"] = 0.8f + GD.Randf() * 0.2f;
			break;
			
		case CustomerBase.CustomerType.Bureaucrat:
			preferences["drink_whiskey"] = 0.8f + GD.Randf() * 0.2f;
			preferences["drink_raki"] = 0.7f + GD.Randf() * 0.2f;
			preferences["music_taverna"] = 0.6f + GD.Randf() * 0.3f;
			preferences["ambiance_intimate"] = 0.9f + GD.Randf() * 0.1f;
			preferences["staff_kons"] = 0.8f + GD.Randf() * 0.2f;
			preferences["privacy"] = 0.9f + GD.Randf() * 0.1f;
			break;
			
		case CustomerBase.CustomerType.Sapkali:
			preferences["drink_raki"] = 0.9f + GD.Randf() * 0.1f;
			preferences["music_oyunHavasi"] = 0.8f + GD.Randf() * 0.2f;
			preferences["staff_kons"] = 0.9f + GD.Randf() * 0.1f;
			preferences["entertainment"] = 0.8f + GD.Randf() * 0.2f;
			preferences["generosity"] = 0.8f + GD.Randf() * 0.2f;
			break;
			
		case CustomerBase.CustomerType.Gangster:
			preferences["drink_whiskey"] = 0.7f + GD.Randf() * 0.3f;
			preferences["drink_raki"] = 0.8f + GD.Randf() * 0.2f;
			preferences["music_arabesk"] = 0.8f + GD.Randf() * 0.2f;
			preferences["privacy"] = 0.8f + GD.Randf() * 0.2f;
			preferences["staff_kons"] = 0.7f + GD.Randf() * 0.3f;
			preferences["illegal_activities"] = 0.7f + GD.Randf() * 0.3f;
			break;
			
		case CustomerBase.CustomerType.UnderCover:
			preferences["drink_beer"] = 0.6f + GD.Randf() * 0.3f;
			preferences["drink_raki"] = 0.5f + GD.Randf() * 0.3f;
			preferences["observation"] = 0.9f + GD.Randf() * 0.1f;
			preferences["privacy"] = 0.7f + GD.Randf() * 0.2f;
			break;
			
		case CustomerBase.CustomerType.Foreigner:
			preferences["drink_beer"] = 0.7f + GD.Randf() * 0.2f;
			preferences["drink_cocktail"] = 0.6f + GD.Randf() * 0.3f;
			preferences["music_modern"] = 0.6f + GD.Randf() * 0.3f;
			preferences["ambiance_traditional"] = 0.7f + GD.Randf() * 0.2f;
			preferences["food_traditional"] = 0.8f + GD.Randf() * 0.2f;
			break;
	}
	
	// Rastgele bazı bireysel tercihler ekle
	AddRandomPreferences(preferences);
	
	// Tercihleri ilişkiye ata
	relationship.Preferences = preferences;
}

// EKLENEN - Rastgele tercihler ekle
private void AddRandomPreferences(Dictionary<string, float> preferences)
{
	// Rastgele birkaç özel tercih ekleme
	string[] possiblePreferences = {
		"staff_appearance", "air_conditioning", "lighting", "noise_level", 
		"seating_comfort", "view", "smoke_level", "special_attention",
		"food_spiciness", "drink_variety", "entertainment_variety"
	};
	
	// 2-4 adet rastgele tercih ekle
	int extraPrefs = GD.RandRange(2, 4);
	for (int i = 0; i < extraPrefs; i++)
	{
		string pref = possiblePreferences[GD.RandRange(0, possiblePreferences.Length - 1)];
		
		// Eğer bu tercih zaten eklenmemişse
		if (!preferences.ContainsKey(pref))
		{
			preferences[pref] = 0.4f + GD.Randf() * 0.5f; // 0.4 - 0.9 arası değer
		}
	}
}

// EKLENEN - Kons atama
private void AssignKonsToCustomer(CustomerBase customer)
{
	// Uygun kons personeli bul
	if (GetTree().Root.HasNode("GameManager/StaffManager"))
	{
		var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
		
		if (staffManager.HasMethod("GetAvailableKons"))
		{
			try
			{
				// Müşteri tipine uygun kons al
				Node3D kons = (Node3D)staffManager.Call("GetAvailableKons", customer.CustomerTypeValue.ToString());
				
				if (kons != null)
				{
					string konsId = kons.Name;
					
					// Kons-müşteri ataması yap
					_assignedKons[customer.ID] = konsId;
					
					// Personel-müşteri ilişkisini başlat
					if (!_staffCustomerRelationships.ContainsKey(konsId))
					{
						_staffCustomerRelationships[konsId] = new Dictionary<string, float>();
					}
					
					// İlişki başlangıç değeri
					float initialRelationship = 0.5f;
					
					// Düzenli müşteri ise önceki ilişkiyi kontrol et
					if (_regularCustomers.ContainsKey(customer.ID) && 
						_regularCustomers[customer.ID].StaffRelationships.ContainsKey(konsId))
					{
						initialRelationship = _regularCustomers[customer.ID].StaffRelationships[konsId];
					}
					// İlk defa ilişki kuruluyor
					else if (_regularCustomers.ContainsKey(customer.ID))
					{
						_regularCustomers[customer.ID].StaffRelationships[konsId] = initialRelationship;
					}
					
					_staffCustomerRelationships[konsId][customer.ID] = initialRelationship;
					
					// Kons-müşteri etkileşimi başlasın
					if (kons.GetType().GetMethod("ServeCustomer") != null)
					{
						kons.Call("ServeCustomer", customer);
					}
					
					// Kons atama sinyali gönder
					EmitSignal(SignalName.KonsAssignedToCustomerEventHandler, konsId, customer.ID, initialRelationship);
					
					GD.Print($"Kons {konsId} assigned to customer {customer.Name} with relationship level: {initialRelationship}");
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"Error assigning kons to customer: {e.Message}");
			}
		}
	}
}

// Rastgele müşteri tipi belirle
private CustomerBase.CustomerType DetermineRandomCustomerType()
{
	// Zamana bağlı dağılım güncellemeleri zaten yapıldı, şimdi rastgele seçim yap
	float randomValue = GD.Randf();
	float cumulativeProbability = 0.0f;
	
	foreach (var entry in _customerTypeDistribution)
	{
		cumulativeProbability += entry.Value;
		if (randomValue <= cumulativeProbability)
		{
			return entry.Key;
		}
	}
	
	// Varsayılan olarak Regular döndür
	return CustomerBase.CustomerType.Regular;
}

// Özel müşteri grubu işle
private void ProcessSpecialGroup()
{
	// Özel bir grup için aday listesi oluştur
	List<string> candidates = new List<string>(_specialCustomerGroups.Keys);
	
	// Özel grup yok
	if (candidates.Count == 0)
		return;
	
	// Rastgele bir grup seç
	string groupName = candidates[GD.RandRange(0, candidates.Count - 1)];
	List<CustomerInfo> group = _specialCustomerGroups[groupName];
	
	// Masa kapasitesi kontrolü
	if (_availableTables.Count < group.Count)
	{
		GD.Print($"Not enough tables for special group {groupName}. Needed: {group.Count}, Available: {_availableTables.Count}");
		return;
	}
	
	// Grup üyelerini oluştur
	List<CustomerBase> groupMembers = new List<CustomerBase>();
	
	foreach (var info in group)
	{
		CustomerBase customer = CreateCustomer(info.Name, null, info.Type, info.Budget, info.IsVIP);
		
		if (customer != null)
		{
			// Özel tercihleri ayarla
			if (info.CustomPreferences != null)
			{
				foreach (var pref in info.CustomPreferences)
				{
					customer.SetPreference(pref.Key, pref.Value);
				}
			}
			
			// İmza özelliği
			if (!string.IsNullOrEmpty(info.Signature))
			{
				customer.SetProperty("Signature", info.Signature);
			}
			
			// Müşteri listelere ekle
			_allCustomers.Add(customer);
			_customersById[customer.ID] = customer;
			
			// Tür bazlı listeye ekle
			if (!_customersByType.ContainsKey(customer.CustomerTypeValue))
			{
				_customersByType[customer.CustomerTypeValue] = new List<CustomerBase>();
			}
			_customersByType[customer.CustomerTypeValue].Add(customer);
			
			// VIP kontrolü
			if (customer.IsVIP)
			{
				_vipCustomers.Add(customer);
				EmitSignal(SignalName.VIPCustomerEnteredEventHandler, customer.ID, customer.CustomerTypeString);
			}
			
			// Masa ata
			AssignTable(customer);
			
			// Grup üyelerine ekle
			groupMembers.Add(customer);
			
			// Müşteri girdi sinyali gönder
			EmitSignal(SignalName.CustomerEnteredEventHandler, customer.ID, customer.CustomerTypeString);
		}
	}
	
	// Özel grup sinyali gönder
	if (groupMembers.Count > 0)
	{
		EmitSignal(SignalName.SpecialGroupArrivedEventHandler, groupName, groupMembers.Count);
		GD.Print($"Special group arrived: {groupName} with {groupMembers.Count} members");
		
		// Olay tetikle
		if (GetTree().Root.HasNode("GameManager/EventManager"))
		{
			var eventManager = GetTree().Root.GetNode("GameManager/EventManager");
			
			if (eventManager.HasMethod("TriggerSpecialGroupEvent"))
			{
				eventManager.Call("TriggerSpecialGroupEvent", groupName, groupMembers.Count);
			}
		}
	}
}

// İsim oluştur
private string GenerateRandomName(string gender)
{
	string firstName = "";
	
	if (gender == "Male" && _maleFirstNames.Count > 0)
	{
		firstName = _maleFirstNames[GD.RandRange(0, _maleFirstNames.Count - 1)];
	}
	else if (gender == "Female" && _femaleFirstNames.Count > 0)
	{
		firstName = _femaleFirstNames[GD.RandRange(0, _femaleFirstNames.Count - 1)];
	}
	else
	{
		firstName = "Müşteri";
	}
	
	string lastName = "";
	if (_lastNames.Count > 0)
	{
		lastName = _lastNames[GD.RandRange(0, _lastNames.Count - 1)];
	}
	
	return $"{firstName} {lastName}";
}

// Customer nesnesi oluştur
private CustomerBase CreateCustomer(string fullName, string id = null, CustomerBase.CustomerType type = CustomerBase.CustomerType.Regular, float budget = 0f, bool isVIP = false)
{
	// Sahneden customer prefabı yükle
	PackedScene customerScene = ResourceLoader.Load<PackedScene>(_customerScenePath);
	
	if (customerScene == null)
	{
		GD.PrintErr($"Failed to load customer scene: {_customerScenePath}");
		return null;
	}
	
	// Yeni müşteri oluştur
	CustomerBase customer = customerScene.Instantiate<CustomerBase>();
	
	if (customer != null)
	{
		// Temel özellikleri ayarla
		customer.Name = id ?? Guid.NewGuid().ToString();
		customer.Initialize(fullName, type, isVIP);
		
		// Giriş pozisyonunu ayarla
		customer.GlobalPosition = _entrancePosition;
		
		// Müşterinin standart giriş rotasını ayarla
		customer.SetEntryPath(_entrancePosition);
		
		// Diğer ilgili konumları bildir
		customer.SetExitPosition(_exitPosition);
		customer.SetBathroomPosition(_bathroomPosition);
		
		// EKLENEN - İllegal kat pozisyonunu ayarla
		if (_isIllegalFloorOpen)
		{
			customer.SetIllegalFloorPosition(_illegalFloorEntrancePosition);
		}
		
		// Bütçe
		if (budget <= 0)
		{
			// Rastgele bütçe - müşteri tipine göre
			switch (type)
			{
				case CustomerBase.CustomerType.Elite:
					budget = 5000f + GD.Randf() * 15000f; // 5000-20000
					break;
				case CustomerBase.CustomerType.Bureaucrat:
					budget = 3000f + GD.Randf() * 7000f; // 3000-10000
					break;
				case CustomerBase.CustomerType.Sapkali:
					budget = 10000f + GD.Randf() * 30000f; // 10000-40000
					break;
				case CustomerBase.CustomerType.Gangster:
					budget = 5000f + GD.Randf() * 10000f; // 5000-15000
					break;
				case CustomerBase.CustomerType.Worker:
					budget = 800f + GD.Randf() * 1200f; // 800-2000
					break;
				case CustomerBase.CustomerType.Nostalgic:
					budget = 1500f + GD.Randf() * 1500f; // 1500-3000
					break;
				case CustomerBase.CustomerType.Emotional:
					budget = 1000f + GD.Randf() * 1000f; // 1000-2000
					break;
				case CustomerBase.CustomerType.Young:
					budget = 700f + GD.Randf() * 1300f; // 700-2000
					break;
				case CustomerBase.CustomerType.Foreigner:
					budget = 2000f + GD.Randf() * 3000f; // 2000-5000
					break;
				case CustomerBase.CustomerType.UnderCover:
					budget = 1500f + GD.Randf() * 1500f; // 1500-3000
					break;
				default: // Regular
					budget = 1000f + GD.Randf() * 2000f; // 1000-3000
					break;
			}
		}
		
		customer.Budget = budget;
		
		// EKLENEN - Müşteri tipine göre harcama kalıplarını ayarla
		if (_spendingPatterns.ContainsKey(type))
		{
			customer.SetSpendingPattern(_spendingPatterns[type]);
		}
		
		// Pavyona ekle
		AddChild(customer);
	}
	
	return customer;
}

// Müşteriye masa ata
private void AssignTable(CustomerBase customer)
{
	if (_availableTables.Count == 0)
	{
		GD.Print($"No available tables for customer {customer.Name}");
		return;
	}
	
	// VIP müşteriler için özel VIP masası kontrolü
	if (customer.IsVIP)
	{
		// VIP masa kontrolü
		Node3D vipTable = _availableTables.FirstOrDefault(t => t.Name.Contains("VIP"));
		if (vipTable != null)
		{
			_availableTables.Remove(vipTable);
			_occupiedTables[customer.ID] = vipTable;
			
			customer.AssignedTable = vipTable.Name;
			customer.TablePosition = vipTable.GlobalPosition;
			
			EmitSignal(SignalName.TableAssignedEventHandler, customer.ID, vipTable.Name);
			return;
		}
	}
	
	// Rastgele bir masa seç
	int tableIndex = GD.RandRange(0, _availableTables.Count - 1);
	Node3D table = _availableTables[tableIndex];
	
	_availableTables.RemoveAt(tableIndex);
	_occupiedTables[customer.ID] = table;
	
	customer.AssignedTable = table.Name;
	customer.TablePosition = table.GlobalPosition;
	
	EmitSignal(SignalName.TableAssignedEventHandler, customer.ID, table.Name);
}

// Müşteri ayrılma
public void CustomerLeave(CustomerBase customer, string reason = "")
{
	if (customer == null) return;
	
	// EKLENEN - İllegal kattaysa listeden çıkar
	if (_customersInIllegalFloor.Contains(customer.ID))
	{
		_customersInIllegalFloor.Remove(customer.ID);
	}
	
	// EKLENEN - Konstan ayrıl
	if (_assignedKons.ContainsKey(customer.ID))
	{
		string konsId = _assignedKons[customer.ID];
		
		// Kons ilişkisini güncelle
		if (_staffCustomerRelationships.ContainsKey(konsId) && 
			_staffCustomerRelationships[konsId].ContainsKey(customer.ID))
		{
			float relationship = _staffCustomerRelationships[konsId][customer.ID];
			
			// Düzenli müşteri ilişkisini güncelle
			if (_regularCustomers.ContainsKey(customer.ID))
			{
				_regularCustomers[customer.ID].StaffRelationships[konsId] = relationship;
			}
		}
		
		// Kons atamasını kaldır
		_assignedKons.Remove(customer.ID);
		
		// Konsa bilgi ver
		if (GetTree().Root.HasNode("GameManager/StaffManager"))
		{
			var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
			
			if (staffManager.HasMethod("NotifyKonsCustomerLeft"))
			{
				staffManager.Call("NotifyKonsCustomerLeft", konsId, customer.ID);
			}
		}
	}
	
	// EKLENEN - Harcama kayıtları
	float spent = customer.GetTotalSpent();
	if (spent > 0)
	{
		_totalRevenue += spent;
		
		// Kategori bazlı gelir takibi
		Dictionary<string, float> spendingBreakdown = customer.GetSpendingBreakdown();
		foreach (var entry in spendingBreakdown)
		{
			string category = entry.Key;
			float amount = entry.Value;
			
			if (_revenueByCategory.ContainsKey(category))
			{
				_revenueByCategory[category] += amount;
			}
			else
			{
				_revenueByCategory[category] = amount;
			}
		}
		
		// Müşteri tipi bazlı gelir takibi
		if (_revenueByCustomerType.ContainsKey(customer.CustomerTypeValue))
		{
			_revenueByCustomerType[customer.CustomerTypeValue] += spent;
		}
		else
		{
			_revenueByCustomerType[customer.CustomerTypeValue] = spent;
		}
		
		// Düzenli müşteri kaydı güncelle
		if (_regularCustomers.ContainsKey(customer.ID))
		{
			_regularCustomers[customer.ID].TotalSpent += spent;
		}
	}
	
	// Masa serbest bırak
	if (_occupiedTables.ContainsKey(customer.ID))
	{
		Node3D table = _occupiedTables[customer.ID];
		_availableTables.Add(table);
		_occupiedTables.Remove(customer.ID);
	}
	
	// Müşteri listelerinden çıkar
	if (_customersById.ContainsKey(customer.ID))
	{
		_customersById.Remove(customer.ID);
	}
	
	_allCustomers.Remove(customer);
	
	if (_customersByType.ContainsKey(customer.CustomerTypeValue))
	{
		_customersByType[customer.CustomerTypeValue].Remove(customer);
	}
	
	if (customer.IsVIP)
	{
		_vipCustomers.Remove(customer);
	}
	
	// Düzenli müşteri kontrolü
	bool isRegularCustomer = false;
	int visitCount = 1;
	
	if (customer.HasProperty("IsRegularCustomer"))
	{
		isRegularCustomer = (bool)customer.GetProperty("IsRegularCustomer");
	}
	
	if (customer.HasProperty("VisitCount"))
	{
		visitCount = (int)customer.GetProperty("VisitCount");
	}
	
	// Düzenli müşteri değilse, ziyaret sayısını kontrol et
	if (!isRegularCustomer && visitCount >= REGULAR_CUSTOMER_VISIT_THRESHOLD)
	{
		// Artık düzenli müşteri olarak tanınıyor
		if (_regularCustomers.ContainsKey(customer.ID))
		{
			_regularCustomers[customer.ID].VisitCount = visitCount;
			
			// Düzenli müşteri tanıma sinyali
			EmitSignal(SignalName.RegularCustomerRecognizedEventHandler, customer.ID, visitCount);
			
			GD.Print($"{customer.Name} recognized as a regular customer after {visitCount} visits!");
		}
	}
	
	// Ayrılma sinyali gönder
	EmitSignal(SignalName.CustomerLeftEventHandler, customer.ID, customer.GetTotalSpent(), customer.Satisfaction);
	
	// Ayrılma animasyonu
	customer.Leave();
	
	GD.Print($"Customer {customer.Name} left. Reason: {reason}, Spent: {customer.GetTotalSpent()}, Satisfaction: {customer.Satisfaction}");
}

// ID'ye göre müşteri bulma
public CustomerBase GetCustomerById(string id)
{
	if (_customersById.ContainsKey(id))
	{
		return _customersById[id];
	}
	
	return null;
}

// Tipi verilen rastgele müşteri döndür
public CustomerBase GetRandomCustomerByType(CustomerBase.CustomerType type)
{
	if (_customersByType.ContainsKey(type) && _customersByType[type].Count > 0)
	{
		int index = GD.RandRange(0, _customersByType[type].Count - 1);
		return _customersByType[type][index];
	}
	
	return null;
}

// VIP müşteri sayısını al
public int GetVIPCustomerCount()
{
	return _vipCustomers.Count;
}

// Müşteri memnuniyetini ayarla
public void AdjustCustomerSatisfaction(CustomerBase customer, float amount, string reason = "")
{
	if (customer == null) return;
	
	customer.AdjustSatisfaction(amount, reason);
	
	// Ortalama müşteri memnuniyetini güncelle
	UpdateAverageCustomerSatisfaction();
}

// Ortalama müşteri memnuniyetini güncelle
private void UpdateAverageCustomerSatisfaction()
{
	if (_allCustomers.Count == 0) return;
	
	float totalSatisfaction = 0f;
	
	foreach (var customer in _allCustomers)
	{
		totalSatisfaction += customer.Satisfaction;
	}
	
	float newAverage = totalSatisfaction / _allCustomers.Count;
	
	// Eğer değişim önemli ise güncelle ve sinyal gönder
	if (Mathf.Abs(newAverage - _averageCustomerSatisfaction) > 0.05f)
	{
		_averageCustomerSatisfaction = newAverage;
		EmitSignal(SignalName.CustomerSatisfactionChangedEventHandler, _averageCustomerSatisfaction);
		
		GD.Print($"Average customer satisfaction updated: {_averageCustomerSatisfaction}");
	}
}

// Tüm müşterilerin memnuniyetini etkile
public void ModifyAllCustomerSatisfaction(float amount, string reason = "")
{
	foreach (var customer in _allCustomers)
	{
		customer.AdjustSatisfaction(amount, reason);
	}
	
	// Ortalama müşteri memnuniyetini güncelle
	UpdateAverageCustomerSatisfaction();
}

// Sadece belirli tipteki müşterilerin memnuniyetini etkile
public void ModifyCustomerSatisfactionByType(CustomerBase.CustomerType type, float amount, string reason = "")
{
	if (!_customersByType.ContainsKey(type)) return;
	
	foreach (var customer in _customersByType[type])
	{
		customer.AdjustSatisfaction(amount, reason);
	}
	
	// Ortalama müşteri memnuniyetini güncelle
	UpdateAverageCustomerSatisfaction();
}

// Müşteri memnuniyet modifikatörünü ayarla
public void SetSatisfactionModifier(string category, float value)
{
	if (_satisfactionModifiers.ContainsKey(category))
	{
		float previousValue = _satisfactionModifiers[category];
		_satisfactionModifiers[category] = value;
		
		// Değişim farkı
		float diff = value - previousValue;
		
		// Tüm müşterileri etkileyecek kadar önemli bir değişiklik
		if (Mathf.Abs(diff) >= 0.1f)
		{
			// Her müşterinin memnuniyetini güncelle
			foreach (var customer in _allCustomers)
			{
				// Müşteri tipine veya tercihlerine göre etkiyi ayarlayabiliriz
				float impact = diff * 0.5f; // Örnek olarak, değişikliğin yarısı kadar etki
				customer.AdjustSatisfaction(impact, $"Change in {category}");
			}
			
			// Ortalama memnuniyeti güncelle
			UpdateAverageCustomerSatisfaction();
			//economymanager ile bağlantı 
		private void TransferRevenueToEconomyManager(float amount, Dictionary<string, float> categoryBreakdown, CustomerBase.CustomerType customerType)
{
	if (GetTree().Root.HasNode("GameManager/EconomyManager"))
	{
		var economyManager = GetTree().Root.GetNode("GameManager/EconomyManager");
		if (economyManager.HasMethod("AddRevenue"))
		{
			economyManager.Call("AddRevenue", amount, categoryBreakdown, customerType.ToString());
		}
	}
}
