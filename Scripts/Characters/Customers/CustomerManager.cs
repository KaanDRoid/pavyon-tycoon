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
		
		// Müşteri demografisi ve gelirinin dinamik dağılımı
		private Dictionary<CustomerBase.CustomerType, float> _customerTypeDistribution = new Dictionary<CustomerBase.CustomerType, float>();
		
		// Özel müşteri grupları
		private Dictionary<string, List<CustomerInfo>> _specialCustomerGroups = new Dictionary<string, List<CustomerInfo>>();
		
		// Müşteri memnuniyeti ve etki faktörleri
		private float _averageCustomerSatisfaction = 0.7f;   // Ortalama memnuniyet (0-1 arası)
		private Dictionary<string, float> _satisfactionModifiers = new Dictionary<string, float>();
		
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
		
		// Pavyonu aç veya kapat
		public void SetPavyonOpen(bool isOpen)
		{
			_isPavyonOpen = isOpen;
			
			if (isOpen)
			{
				GD.Print("Pavyon opened for customers");
				_isGeneratingCustomers = true;
			}
			else
			{
				GD.Print("Pavyon closed");
				_isGeneratingCustomers = false;
				
				// Tüm müşterilere gitme sinyali gönder
				foreach (var customer in _allCustomers.ToList())
				{
					customer.PrepareToLeave();
				}
			}
		}
		
		// Rastgele müşteri oluştur
		private CustomerBase GenerateRandomCustomer()
		{
			// Boş masa kontrolü
			if (_availableTables.Count == 0)
			{
				return null;
			}
			
			// Rastgele müşteri tipi seç
			CustomerBase.CustomerType customerType = SelectRandomCustomerType();
			
			// Rastgele isim ve demografik özellikleri seç
			bool isMale = GD.Randf() < 0.8f; // Pavyon müşterilerinin çoğu erkek
			string gender = isMale ? "Male" : "Female";
			
			string firstName = isMale 
				? _maleFirstNames[GD.RandRange(0, _maleFirstNames.Count - 1)]
				: _femaleFirstNames[GD.RandRange(0, _femaleFirstNames.Count - 1)];
				
			string lastName = _lastNames[GD.RandRange(0, _lastNames.Count - 1)];
			string fullName = $"{firstName} {lastName}";
			
			// Yaş 
			int age = GenerateAgeByCustomerType(customerType);
			
			// Bütçe - müşteri tipine göre
			float budget = GenerateBudgetByCustomerType(customerType);
			
			// Müşteri sahnesini yükle ve oluştur
			PackedScene customerScene = ResourceLoader.Load<PackedScene>(_customerScenePath);
			CustomerBase customer = customerScene.Instantiate<CustomerBase>();
			
			// Müşteri özelliklerini ayarla
			customer.FullName = fullName;
			customer.Age = age;
			customer.Gender = gender;
			customer.SetCustomerType(customerType);
			
			// Özel bütçe ata
			SetCustomerBudget(customer, budget);
			
			// Pavyon konumlarını bildir
			customer.SetPavyonLocations(_bathroomPosition, _exitPosition);
			
			// Müşteri başlangıç konumunu ayarla
			customer.GlobalPosition = _entrancePosition;
			
			// Müşteri oluşturma tamamlandıktan sonra sahneye ekle
			AddChild(customer);
			
			// Listelere ekle
			_allCustomers.Add(customer);
			_customersById[customer.CustomerId] = customer;
			
			// Müşteri tipine göre listeye ekle
			if (!_customersByType.ContainsKey(customerType))
			{
				_customersByType[customerType] = new List<CustomerBase>();
			}
			_customersByType[customerType].Add(customer);
			
			// VIP ise vip listesine ekle
			if (customer.IsVIP)
			{
				_vipCustomers.Add(customer);
				
				// VIP müşteri geldi sinyali
				EmitSignal(SignalName.VIPCustomerEnteredEventHandler, customer.CustomerId, customerType.ToString());
			}
			
			// Müşteri geldi sinyali
			EmitSignal(SignalName.CustomerEnteredEventHandler, customer.CustomerId, customerType.ToString());
			
			// Masa ata
			AssignTableToCustomer(customer);
			
			GD.Print($"Generated new customer: {fullName}, Type: {customerType}, Budget: {budget}");
			
			return customer;
		}
		
		// Müşteri tipine göre rastgele yaş üret
		private int GenerateAgeByCustomerType(CustomerBase.CustomerType type)
		{
			switch (type)
			{
				case CustomerBase.CustomerType.Young:
					return GD.RandRange(20, 30);
				case CustomerBase.CustomerType.Worker:
					return GD.RandRange(30, 50);
				case CustomerBase.CustomerType.Elite:
					return GD.RandRange(35, 60);
				case CustomerBase.CustomerType.Nostalgic:
					return GD.RandRange(50, 70);
				case CustomerBase.CustomerType.Emotional:
					return GD.RandRange(25, 55);
				case CustomerBase.CustomerType.Bureaucrat:
					return GD.RandRange(40, 60);
				case CustomerBase.CustomerType.UnderCover:
					return GD.RandRange(30, 45);
				case CustomerBase.CustomerType.Sapkali:
					return GD.RandRange(45, 65);
				case CustomerBase.CustomerType.Gangster:
					return GD.RandRange(25, 50);
				case CustomerBase.CustomerType.Foreigner:
					return GD.RandRange(25, 55);
				default: // Regular
					return GD.RandRange(25, 65);
			}
		}
		
		// Müşteri tipine göre rastgele bütçe üret
		private float GenerateBudgetByCustomerType(CustomerBase.CustomerType type)
		{
			float baseBudget = 0.0f;
			float randomVariance = 0.0f;
			
			switch (type)
			{
				case CustomerBase.CustomerType.Young:
					baseBudget = 1000.0f;
					randomVariance = 500.0f;
					break;
				case CustomerBase.CustomerType.Worker:
					baseBudget = 1500.0f;
					randomVariance = 800.0f;
					break;
				case CustomerBase.CustomerType.Elite:
					baseBudget = 10000.0f;
					randomVariance = 5000.0f;
					break;
				case CustomerBase.CustomerType.Nostalgic:
					baseBudget = 3000.0f;
					randomVariance = 1500.0f;
					break;
				case CustomerBase.CustomerType.Emotional:
					baseBudget = 2500.0f;
					randomVariance = 1000.0f;
					break;
				case CustomerBase.CustomerType.Bureaucrat:
					baseBudget = 5000.0f;
					randomVariance = 2000.0f;
					break;
				case CustomerBase.CustomerType.UnderCover:
					baseBudget = 2000.0f;
					randomVariance = 500.0f;
					break;
				case CustomerBase.CustomerType.Sapkali:
					baseBudget = 30000.0f;
					randomVariance = 15000.0f;
					break;
				case CustomerBase.CustomerType.Gangster:
					baseBudget = 8000.0f;
					randomVariance = 4000.0f;
					break;
				case CustomerBase.CustomerType.Foreigner:
					baseBudget = 5000.0f;
					randomVariance = 3000.0f;
					break;
				default: // Regular
					baseBudget = 1800.0f;
					randomVariance = 1000.0f;
					break;
			}
			
			return baseBudget + (GD.Randf() * randomVariance);
