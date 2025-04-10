using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	
	public partial class RegularCustomer : CustomerBase
	{
		// Düzenli müşteri özellikleri
		protected float _loyaltyLevel = 0.0f;               // Pavyona sadakat seviyesi (0-1)
		protected float _alcoholTolerance = 0.5f;           // Alkol toleransı (0-1)
		protected int _visitCount = 0;                      // Ziyaret sayısı
		protected bool _isHereForFood = false;              // Yemek için mi geldi
		protected Dictionary<string, float> _drinkPreferences = new Dictionary<string, float>();  // İçki tercihleri (0-1)
		protected Dictionary<string, float> _foodPreferences = new Dictionary<string, float>();   // Yemek tercihleri (0-1)
		protected Dictionary<string, float> _musicPreferences = new Dictionary<string, float>();  // Müzik tercihleri (0-1)
		
		// Ek durum değişkenleri
		protected bool _isBeingServed = false;              // Şu anda servis alıyor mu
		protected float _waitingTime = 0.0f;                // Servis bekleme süresi
		protected float _servicePatienceThreshold = 300.0f; // Sabır eşiği (saniye)
		protected bool _wantsToOrder = false;               // Sipariş vermek istiyor mu
		protected bool _needsAttention = false;             // İlgi istiyor mu

		// Sipariş verileri
		protected string _currentDrinkOrder = "";           // Mevcut içki siparişi
		protected string _currentFoodOrder = "";            // Mevcut yemek siparişi
		protected Dictionary<string, int> _drinkHistory = new Dictionary<string, int>();  // İçki geçmişi (isim, adet)
		protected Dictionary<string, int> _foodHistory = new Dictionary<string, int>();   // Yemek geçmişi (isim, adet)

		// Müzik ve atmosfer kaynaklı durum değişiklikleri
		protected float _musicMoodEffect = 0.0f;            // Müziğin ruh haline etkisi (-1 to 1)
		protected float _ambientMoodEffect = 0.0f;          // Ortamın ruh haline etkisi (-1 to 1)
		
		// Bu müşteri tipine özel konuşma metinleri
		protected List<string> _greetings = new List<string>();           // Selamlaşma cümleleri
		protected List<string> _orderPhrases = new List<string>();        // Sipariş verme cümleleri
		protected List<string> _complainPhrases = new List<string>();     // Şikayet cümleleri
		protected List<string> _complimentPhrases = new List<string>();   // İltifat cümleleri
		protected List<string> _drunkPhrases = new List<string>();        // Sarhoş cümleleri
		
		public override void _Ready()
		{
			base._Ready();
			
			// Varsayılan müşteri tipi
			CustomerType = CustomerType.Regular;
			
			// İçki tercihleri
			InitializeDrinkPreferences();
			
			// Yemek tercihleri
			InitializeFoodPreferences();
			
			// Müzik tercihleri
			InitializeMusicPreferences();
			
			// Konuşma metinleri
			InitializeDialogueOptions();
			
			// Sadakat ve alkol toleransı - rastgele başlangıç değerleri
			_loyaltyLevel = GD.Randf() * 0.3f;  // 0-0.3 arası rastgele başlangıç sadakati
			_alcoholTolerance = 0.3f + GD.Randf() * 0.4f;  // 0.3-0.7 arası rastgele alkol toleransı
			
			// Yemek siparişi verme ihtimali
			_isHereForFood = GD.Randf() < 0.4f;  // %40 ihtimalle yemek de isteyecek
			
			// Hemen sipariş vermek için hazır
			_wantsToOrder = true;
			_needsAttention = true;
			
			// Sabır süresi (4-8 dakika arası)
			_servicePatienceThreshold = 240.0f + GD.Randf() * 240.0f;
			
			// İsim ve demografik özellikler, CustomerBase sınıfından gelir
			
			GD.Print($"Regular customer initialized: {FullName}, Age: {Age}, District: {District}");
		}
		
		public override void _Process(double delta)
		{
			base._Process(delta);
			
			// Eğer sipariş vermek istiyorsa ve servis beklemiyorsa
			if (_wantsToOrder && !_isBeingServed)
			{
				// Bekleme süresini artır
				_waitingTime += (float)delta;
				
				// Belirli aralıklarla garson çağır
				if (_waitingTime > 30.0f && (_waitingTime % 30.0f) < 1.0f)
				{
					CallWaiter();
				}
				
				// Sabır kontrolü
				if (_waitingTime > _servicePatienceThreshold)
				{
					// Sabır taştı - memnuniyeti düşür
					_satisfaction -= 0.2f;
					
					// Durumu güncelle
					UpdateMoodBasedOnSatisfaction();
					
					// Sipariş vermekten vazgeç, sonra tekrar dene
					_wantsToOrder = false;
					_needsAttention = false;
					_waitingTime = 0.0f;
					
					// Şikayet et
					Complain("LongWait");
					
					// Belirli bir süre sonra tekrar sipariş vermeyi dene
					float retryTime = 120.0f + GD.Randf() * 120.0f;  // 2-4 dakika arası
					
					Timer timer = new Timer
					{
						WaitTime = retryTime,
						OneShot = true
					};
					
					AddChild(timer);
					timer.Timeout += () => 
					{
						_wantsToOrder = true;
						_needsAttention = true;
					};
					timer.Start();
				}
			}
			
			// Eğer şu anda dikkate ihtiyacı varsa (sipariş dışında diğer nedenler için)
			if (_needsAttention && !_isBeingServed)
			{
				// Düzenli müşteri için standart davranış
				AttractionBehavior();
			}
			
			// Düzenli ruh hali güncellemesi
			if (GD.Randf() < 0.01f * (float)delta)  // Düşük olasılıkla (nadiren) güncelle
			{
				UpdateMoodBasedOnEnvironment();
			}
			
			// Sarhoşluk seviyesi artışı - sarhoş olma eğilimi
			if (!string.IsNullOrEmpty(_currentDrinkOrder) && _currentDrinkOrder.Contains("Alcohol") && _drunkennessLevel < 1.0f)
			{
				float increment = (float)delta * 0.002f / _alcoholTolerance;  // Alkol toleransı düşükse daha hızlı sarhoş olur
				IncreaseDrunkenness(increment);
			}
		}
		
		// İçki tercihlerini başlat
		protected virtual void InitializeDrinkPreferences()
		{
			// Temel içki tercihleri (0.0 - 1.0 arası değerler)
			_drinkPreferences["Beer"] = 0.6f;         // Bira - orta-yüksek tercih
			_drinkPreferences["Raki"] = 0.5f;         // Rakı - orta tercih
			_drinkPreferences["Wine"] = 0.3f;         // Şarap - düşük-orta tercih
			_drinkPreferences["Whiskey"] = 0.4f;      // Viski - orta tercih
			_drinkPreferences["Vodka"] = 0.4f;        // Votka - orta tercih
			_drinkPreferences["Gin"] = 0.2f;          // Cin - düşük tercih
			_drinkPreferences["Cognac"] = 0.2f;       // Konyak - düşük tercih
			_drinkPreferences["Cocktail"] = 0.3f;     // Kokteyl - düşük-orta tercih
			_drinkPreferences["SoftDrink"] = 0.4f;    // Alkolsüz içecek - orta tercih
			_drinkPreferences["Water"] = 0.5f;        // Su - orta tercih
			_drinkPreferences["Tea"] = 0.3f;          // Çay - düşük-orta tercih
			_drinkPreferences["Coffee"] = 0.2f;       // Kahve - düşük tercih
			
			// Rastgele varyasyon ekle - her müşteri biraz farklı
			RandomizeDrinkPreferences();
		}
		
		// Rastgele içki tercihleri varyasyonu
		protected virtual void RandomizeDrinkPreferences()
		{
			// Her tercih için +/- 0.2 arası rastgele değişim
			foreach (var key in _drinkPreferences.Keys.ToArray())
			{
				float variation = (GD.Randf() * 0.4f) - 0.2f;
				_drinkPreferences[key] = Mathf.Clamp(_drinkPreferences[key] + variation, 0.0f, 1.0f);
			}
		}
		
		// Yemek tercihlerini başlat
		protected virtual void InitializeFoodPreferences()
		{
			// Temel yemek tercihleri
			_foodPreferences["Kofte"] = 0.6f;         // Köfte - orta-yüksek tercih
			_foodPreferences["Balik"] = 0.4f;         // Balık - orta tercih
			_foodPreferences["Ciger"] = 0.3f;         // Ciğer - düşük-orta tercih
			_foodPreferences["Cacik"] = 0.5f;         // Cacık - orta tercih
			_foodPreferences["Patlican"] = 0.4f;      // Patlıcan salatası - orta tercih
			_foodPreferences["Humus"] = 0.3f;         // Humus - düşük-orta tercih
			_foodPreferences["Lahmacun"] = 0.5f;      // Lahmacun - orta tercih
			_foodPreferences["AciliEzme"] = 0.4f;     // Acılı ezme - orta tercih
			_foodPreferences["Patates"] = 0.7f;       // Kızartma patates - yüksek tercih
			_foodPreferences["Borek"] = 0.5f;         // Çeşitli börekler - orta tercih
			_foodPreferences["Dolma"] = 0.3f;         // Dolma - düşük-orta tercih
			_foodPreferences["Kavun"] = 0.4f;         // Kavun - orta tercih
			
			// Rastgele varyasyon ekle
			RandomizeFoodPreferences();
		}
		
		// Rastgele yemek tercihleri varyasyonu
		protected virtual void RandomizeFoodPreferences()
		{
			// Her tercih için +/- 0.2 arası rastgele değişim
			foreach (var key in _foodPreferences.Keys.ToArray())
			{
				float variation = (GD.Randf() * 0.4f) - 0.2f;
				_foodPreferences[key] = Mathf.Clamp(_foodPreferences[key] + variation, 0.0f, 1.0f);
			}
		}
		
		// Müzik tercihlerini başlat
		protected virtual void InitializeMusicPreferences()
		{
			// Temel müzik tercihleri (Türk pavyon müzik türleri)
			_musicPreferences["Arabesk"] = 0.5f;          // Arabesk - orta tercih
			_musicPreferences["FanteziPop"] = 0.5f;       // Fantezi Pop - orta tercih
			_musicPreferences["OyunHavasi"] = 0.6f;       // Oyun Havaları - orta-yüksek tercih
			_musicPreferences["Taverna"] = 0.5f;          // Taverna - orta tercih
			_musicPreferences["ModernLounge"] = 0.3f;     // Modern Lounge - düşük-orta tercih
			_musicPreferences["TurkishClassic"] = 0.2f;   // Türk Sanat Müziği - düşük tercih
			
			// Rastgele varyasyon ekle
			RandomizeMusicPreferences();
		}
		
		// Rastgele müzik tercihleri varyasyonu
		protected virtual void RandomizeMusicPreferences()
		{
			// Her tercih için +/- 0.3 arası rastgele değişim
			foreach (var key in _musicPreferences.Keys.ToArray())
			{
				float variation = (GD.Randf() * 0.6f) - 0.3f;
				_musicPreferences[key] = Mathf.Clamp(_musicPreferences[key] + variation, 0.0f, 1.0f);
			}
		}
		
		// Konuşma metinlerini başlat
		protected virtual void InitializeDialogueOptions()
		{
			// Selamlar
			_greetings.Add("İyi akşamlar.");
			_greetings.Add("Selamlar.");
			_greetings.Add("Merhaba, bir masanız var mı?");
			_greetings.Add("Hoş bir yer burası.");
			_greetings.Add("Bugün çok yorucu bir gündü.");
			
			// Sipariş cümleleri
			_orderPhrases.Add("Bir bira alabilir miyim?");
			_orderPhrases.Add("Bir tek rakı lütfen.");
			_orderPhrases.Add("Bir duble rakı ve yanına biraz meze.");
			_orderPhrases.Add("Bana bir bardak su getirir misiniz?");
			_orderPhrases.Add("Bir şişe açalım.");
			_orderPhrases.Add("Bugünün tavsiyesi nedir?");
			
			// Şikayet cümleleri
			_complainPhrases.Add("Servis neden bu kadar yavaş?");
			_complainPhrases.Add("Bu içki biraz sıcak olmuş.");
			_complainPhrases.Add("Hesapta bir yanlışlık var gibi.");
			_complainPhrases.Add("Buranın müziği biraz fazla gürültülü.");
			_complainPhrases.Add("Burada hep böyle uzun sürer mi sipariş?");
			
			// İltifat cümleleri
			_complimentPhrases.Add("Mezeniz gerçekten lezzetli.");
			_complimentPhrases.Add("Müzik çok güzel.");
			_complimentPhrases.Add("Atmosferi sevdim.");
			_complimentPhrases.Add("Burası rahat bir yer.");
			_complimentPhrases.Add("Servis için teşekkürler.");
			
			// Sarhoş cümleleri
			_drunkPhrases.Add("Bir *hık* daha getir...");
			_drunkPhrases.Add("Bu gece çok güzel geçiyor...");
			_drunkPhrases.Add("Şuraya bak... *hık* ne güzel dönüyor dünya...");
			_drunkPhrases.Add("Sen... *hık* ...sen iyi bir insansın, biliyor musun?");
			_drunkPhrases.Add("Hayat zor be kardeşim... *hık*");
		}
		
		// Garson çağır
		protected virtual void CallWaiter()
		{
			// Düzenli müşteri için standart garson çağırma davranışı
			if (GetTree().Root.HasNode("GameManager/StaffManager"))
			{
				var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
				
				if (staffManager.HasMethod("NotifyWaiterNeeded"))
				{
					try 
					{
						staffManager.Call("NotifyWaiterNeeded", CustomerId, GlobalPosition);
						EmitSignal(SignalName.CustomerActionTaken, CustomerId, "CallWaiter");
						GD.Print($"Customer {FullName} is calling for a waiter.");
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error calling waiter: {e.Message}");
					}
				}
			}
		}
		
		// Dikkat çekici davranış
		protected virtual void AttractionBehavior()
		{
			// Düzenli müşteri için dikkat çekme davranışı
			// Düzenli olarak garson çağırma, kons çağırma, vs.
			
			// Belirli aralıklarla uygun personeli çağır
			if ((_waitingTime % 30.0f) < 0.1f)
			{
				// Öncelikle sarhoşluk seviyesine göre davranış değişimi
				if (_drunkennessLevel > 0.7f)
				{
					// Çok sarhoş - rastgele davranış
					float chance = GD.Randf();
					
					if (chance < 0.4f)
					{
						// Kons çağırma olasılığı
						CallKonsomatris();
					}
					else if (chance < 0.7f)
					{
						// Garson çağırma olasılığı
						CallWaiter();
					}
					else
					{
						// Rastgele sarhoş konuşma
						Say(GetRandomDrunkPhrase());
					}
				}
				else
				{
					// Normal davranış - garson veya kons çağırma
					if (_wantsToOrder)
					{
						CallWaiter();
					}
					else if (GD.Randf() < 0.3f)
					{
						CallKonsomatris();
					}
				}
			}
		}
		
		// Kons çağır
		protected virtual void CallKonsomatris()
		{
			// Kons çağırma davranışı
			if (GetTree().Root.HasNode("GameManager/StaffManager"))
			{
				var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
				
				if (staffManager.HasMethod("NotifyKonsNeeded"))
				{
					try 
					{
						staffManager.Call("NotifyKonsNeeded", CustomerId, GlobalPosition);
						EmitSignal(SignalName.CustomerActionTaken, CustomerId, "CallKons");
						GD.Print($"Customer {FullName} is calling for a kons.");
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error calling kons: {e.Message}");
					}
				}
			}
		}
		
		// Garson tarafından sipariş alındı
		public override void TakeOrder(Node3D waiter)
		{
			if (waiter == null) return;
			
			_isBeingServed = true;
			_wantsToOrder = false;
			_waitingTime = 0.0f;
			
			// Siparişi belirle
			if (string.IsNullOrEmpty(_currentDrinkOrder))
			{
				_currentDrinkOrder = SelectDrink();
			}
			
			// Memnuniyeti biraz arttır
			_satisfaction += 0.05f;
			UpdateMoodBasedOnSatisfaction();
			
			// Rastgele bir sipariş cümlesi söyle
			Say(GetRandomOrderPhrase());
			
			// Siparişi garsona ilet
			if (waiter.GetType().GetMethod("TakeOrder") != null)
			{
				try 
				{
					waiter.Call("TakeOrder", this, _currentDrinkOrder, _isHereForFood);
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error giving order to waiter: {e.Message}");
				}
			}
			
			GD.Print($"Customer {FullName} ordered: {_currentDrinkOrder}");
			
			// Belirli bir süre sonra servisi bitir
			Timer timer = new Timer
			{
				WaitTime = 3.0f,
				OneShot = true
			};
			
			AddChild(timer);
			timer.Timeout += () => 
			{
				_isBeingServed = false;
				_needsAttention = false;
			};
			timer.Start();
		}
		
		// Yiyecek siparişi al
		public override void TakeFoodOrder(Node3D waiter)
		{
			if (waiter == null || !_isHereForFood) return;
			
			_isBeingServed = true;
			
			// Siparişi belirle
			if (string.IsNullOrEmpty(_currentFoodOrder))
			{
				_currentFoodOrder = SelectFood();
			}
			
			// Memnuniyeti biraz arttır
			_satisfaction += 0.05f;
			UpdateMoodBasedOnSatisfaction();
			
			// Siparişi garsona ilet
			if (waiter.GetType().GetMethod("TakeFoodOrder") != null)
			{
				try 
				{
					waiter.Call("TakeFoodOrder", this, _currentFoodOrder);
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error giving food order to waiter: {e.Message}");
				}
			}
			
			GD.Print($"Customer {FullName} ordered food: {_currentFoodOrder}");
			
			// Belirli bir süre sonra servisi bitir
			Timer timer = new Timer
			{
				WaitTime = 3.0f,
				OneShot = true
			};
			
			AddChild(timer);
			timer.Timeout += () => 
			{
				_isBeingServed = false;
			};
			timer.Start();
		}
		
		// İçki tercihleri arasından en yükseğini seç
		protected virtual string SelectDrink()
		{
			// Yüksek tercihli içkileri listele
			List<string> possibleDrinks = new List<string>();
			
			// Her içki türü için tercih değerini kullanarak seçim olasılığını hesapla
			foreach (var drink in _drinkPreferences)
			{
				// Tercih değeri yüksek olanlar daha çok eklenir (0.5'ten büyükse)
				if (drink.Value > 0.5f)
				{
					// Tercih değeri kadar ekleme yapılır
					int count = (int)(drink.Value * 10.0f);
					for (int i = 0; i < count; i++)
					{
						possibleDrinks.Add(drink.Key);
					}
				}
				else if (drink.Value > 0.0f)
				{
					// Düşük tercihler de bir şans verilir
					if (GD.Randf() < drink.Value)
					{
						possibleDrinks.Add(drink.Key);
					}
				}
			}
			
			// Eğer seçenek yoksa, varsayılan olarak su
			if (possibleDrinks.Count == 0)
			{
				possibleDrinks.Add("Water");
			}
			
			// Rastgele bir içki seç
			string selectedDrink = possibleDrinks[GD.RandRange(0, possibleDrinks.Count - 1)];
			
			// İçki geçmişini güncelle
			if (!_drinkHistory.ContainsKey(selectedDrink))
			{
				_drinkHistory[selectedDrink] = 0;
			}
			_drinkHistory[selectedDrink]++;
			
			return selectedDrink;
		}
		
		// Yemek tercihleri arasından en yükseğini seç
		protected virtual string SelectFood()
		{
			// Yüksek tercihli yemekleri listele
			List<string> possibleFoods = new List<string>();
			
			// Her yemek türü için tercih değerini kullanarak seçim olasılığını hesapla
			foreach (var food in _foodPreferences)
			{
				// Tercih değeri yüksek olanlar daha çok eklenir (0.5'ten büyükse)
				if (food.Value > 0.5f)
				{
					// Tercih değeri kadar ekleme yapılır
					int count = (int)(food.Value * 10.0f);
					for (int i = 0; i < count; i++)
					{
						possibleFoods.Add(food.Key);
					}
				}
				else if (food.Value > 0.0f)
				{
					// Düşük tercihler de bir şans verilir
					if (GD.Randf() < food.Value)
					{
						possibleFoods.Add(food.Key);
					}
				}
			}
			
			// Eğer seçenek yoksa, varsayılan olarak patates
			if (possibleFoods.Count == 0)
			{
				possibleFoods.Add("Patates");
			}
			
			// Rastgele bir yemek seç
			string selectedFood = possibleFoods[GD.RandRange(0, possibleFoods.Count - 1)];
			
			// Yemek geçmişini güncelle
			if (!_foodHistory.ContainsKey(selectedFood))
			{
				_foodHistory[selectedFood] = 0;
			}
			_foodHistory[selectedFood]++;
			
			return selectedFood;
		}
		
		// Sipariş edilen içkiyi al
		public override void ReceiveDrink(string drinkName)
		{
			// İçki alındı
			GD.Print($"Customer {FullName} received drink: {drinkName}");
			
			// Memnuniyeti artır
			float satisfactionIncrease = 0.1f;
			
			// Tercih edilen içkiyse daha çok memnuniyet
			if (_drinkPreferences.ContainsKey(drinkName))
			{
				satisfactionIncrease += _drinkPreferences[drinkName] * 0.1f;
			}
			
			_satisfaction += satisfactionIncrease;
			UpdateMoodBasedOnSatisfaction();
			
			// Mevcut siparişi temizle - sonraki sipariş için hazır
			_currentDrinkOrder = drinkName; // Son alınan içki olarak tut
			
			// İçki için ödeme yap
			float price = GetDrinkPrice(drinkName);
			SpendMoney(price, "Drink");
			
			// İltifat et
			if (GD.Randf() < 0.3f)
			{
				Say(GetRandomComplimentPhrase());
			}
			
			// Belirli bir süre sonra yeni içki isteyecek
			ScheduleNextOrder();
		}
		
		// Sipariş edilen yemeği al
		public override void ReceiveFood(string foodName)
		{
			// Yemek alındı
			GD.Print($"Customer {FullName} received food: {foodName}");
			
			// Memnuniyeti artır
			float satisfactionIncrease = 0.15f; // Yemek içkiden daha fazla memnuniyet sağlar
			
			// Tercih edilen yemekse daha çok memnuniyet
			if (_foodPreferences.ContainsKey(foodName))
			{
				satisfactionIncrease += _foodPreferences[foodName] * 0.15f;
			}
			
			_satisfaction += satisfactionIncrease;
			UpdateMoodBasedOnSatisfaction();
			
			// Mevcut siparişi temizle - yemek siparişi verildi
			_currentFoodOrder = foodName; // Son alınan yemek olarak tut
			_isHereForFood = false; // Artık yemek siparişi vermeyecek
			
			// Yemek için ödeme yap
			float price = GetFoodPrice(foodName);
			SpendMoney(price, "Food");
			
			// İltifat et
			if (GD.Randf() < 0.5f) // Yemek için iltifat olasılığı daha yüksek
			{
				Say(GetRandomComplimentPhrase());
			}
		}
		
		// Sonraki siparişi planla
		protected virtual void ScheduleNextOrder()
		{
			// Bir sonraki siparişi beklemek için bir süre belirle
			float waitTime = 300.0f + GD.Randf() * 300.0f; // 5-10 dakika arası
			
			// Sarhoşluk seviyesine göre ayarla
			if (_drunkennessLevel > 0.5f)
			{
				waitTime *= 0.6f; // Sarhoşsa daha hızlı içer
			}
			
			Timer timer = new Timer
			{
				WaitTime = waitTime,
				OneShot = true
			};
			
			AddChild(timer);
			timer.Timeout += () => 
			{
				// Bütçe kontrolü
				if (_remainingBudget > GetDrinkPrice("Beer")) // En azından bira alabilecek kadar para var mı
				{
					_wantsToOrder = true;
					_needsAttention = true;
					_currentDrinkOrder = ""; // Yeni bir içki seçebilmek için temizle
				}
				else
				{
					// Para bitti, gitmeye hazırlan
					PrepareToLeave();
				}
			};
			timer.Start();
		}
		
		// İçki fiyatını hesapla
		protected virtual float GetDrinkPrice(string drinkName)
		{
			// Basit fiyat listesi - gerçek uygulamada EconomyManager'dan alınır
			Dictionary<string, float> drinkPrices = new Dictionary<string, float>
			{
				{ "Beer", 100.0f },
				{ "Raki", 180.0f },
				{ "Wine", 250.0f },
				{ "Whiskey", 300.0f },
				{ "Vodka", 250.0f },
				{ "Gin", 270.0f },
				{ "Cognac", 350.0f },
				{ "Cocktail", 200.0f },
				{ "SoftDrink", 50.0f },
				{ "Water", 20.0f },
				{ "Tea", 30.0f },
				{ "Coffee", 40.0f }
			};
			
			// Fiyat bulunamazsa varsayılan
			if (!drinkPrices.ContainsKey(drinkName))
			{
				return 100.0f;
			}
			
			return drinkPrices[drinkName];
		}
		
		// Yemek fiyatını hesapla
		protected virtual float GetFoodPrice(string foodName)
		{
			// Basit fiyat listesi - gerçek uygulamada EconomyManager'dan alınır
			Dictionary<string, float> foodPrices = new Dictionary<string, float>
			{
				{ "Kofte", 200.0f },
				{ "Balik", 280.0f },
				{ "Ciger", 180.0f },
				{ "Cacik", 80.0f },
				{ "Patlican", 100.0f },
				{ "Humus", 120.0f },
				{ "Lahmacun", 150.0f },
				{ "AciliEzme", 90.0f },
				{ "Patates", 80.0f },
				{ "Borek", 120.0f },
				{ "Dolma", 150.0f },
				{ "Kavun", 70.0f }
			};
			
			// Fiyat bulunamazsa varsayılan
			if (!foodPrices.ContainsKey(foodName))
			{
				return 120.0f;
			}
			
			return foodPrices[foodName];
		}
		
		// Şikayet et
		protected virtual void Complain(string reason)
		{
			// Şikayet metni seç
			string complaint = GetRandomComplaintPhrase();
			
			// Şikayeti dile getir
			Say(complaint);
			
			// Şikayet sinyali gönder
			EmitSignal(SignalName.CustomerComplained, CustomerId, reason);
			
			GD.Print($"Customer {FullName} complained: {complaint}");
		}
		
		// Bir şey söyle
		protected virtual void Say(string text)
		{
			// Konuşma balonu göster
			ShowSpeechBubble(text);
			
			// Konuşma sinyali gönder
			EmitSignal(SignalName.CustomerActionTaken, CustomerId, "Speak", text);
		}
		
		// Konuşma balonu göster
		protected virtual void ShowSpeechBubble(string text)
		{
			// Bu fonksiyon override edildiğinde, karakterin üstünde konuşma balonu gösterir
			// Temel uygulamada sadece konsola yazar
			GD.Print($"[{FullName}]: {text}");
		}
		
		// Müzik ve ortamdan etkilenme
		public override void ReactToMusic(string musicStyle, float intensity)
		{
			// Müzik tercihlerine göre ruh halini etkile
			if (_musicPreferences.ContainsKey(musicStyle))
			{
				// Müzik tercih düzeyi
				float preference = _musicPreferences[musicStyle];
				
				// Etki hesapla (-0.2 to 0.2 arası)
				float effect = (preference - 0.5f) * 0.4f;
				
				// Müziğin yoğunluğuna göre etkiyi ayarla
				effect *= intensity;
				
				// Müzik etkisini uygula
				_musicMoodEffect = Mathf.Clamp(_musicMoodEffect + effect, -0.5f, 0.5f);
				
				// Ruh halini güncelle
				UpdateMoodBasedOnEnvironment();
				
				GD.Print($"Customer {FullName} reacted to {musicStyle} music with effect: {effect:F2}");
			}
		}
		
		// Ortam atmosferine tepki (ışık, dekor, vb.)
		public override void ReactToAmbiance(string ambianceType, float intensity)
		{
			// Düzenli müşteri için standart tepki
			// Basit ortam etkileri, alt sınıflar özelleştirebilir
			
			float effect = 0.0f;
			
			switch (ambianceType)
			{
				case "Dim":
					effect = 0.1f; // Loş ışık genel olarak pozitif
					break;
				case "Bright":
					effect = -0.1f; // Parlak ışık genel olarak negatif
					break;
				case "Loud":
					effect = -0.15f; // Gürültülü ortam genel olarak negatif
					break;
				case "Quiet":
					effect = 0.1f; // Sessiz ortam genel olarak pozitif
					break;
				case "Elegant":
					effect = 0.05f; // Şık ortam hafif pozitif
					break;
				case "Crowded":
					effect = -0.05f; // Kalabalık ortam hafif negatif
					break;
			}
			
			// Yoğunluğa göre etkiyi ayarla
			effect *= intensity;
			
			// Ortam etkisini uygula
			_ambientMoodEffect = Mathf.Clamp(_ambientMoodEffect + effect, -0.5f, 0.5f);
			
			// Ruh halini güncelle
			UpdateMoodBasedOnEnvironment();
		}
		
		// Çevre faktörlerine göre ruh halini güncelle
		protected virtual void UpdateMoodBasedOnEnvironment()
		{
			// Müzik ve ortam etkilerini birleştir
			float combinedEffect = _musicMoodEffect + _ambientMoodEffect;
			
			// Memnuniyeti etkile (küçük bir etki, -0.1 ile 0.1 arası)
			_satisfaction = Mathf.Clamp(_satisfaction + (combinedEffect * 0.01f), 0.0f, 1.0f);
			
			// Ruh halini güncelle
			UpdateMoodBasedOnSatisfaction();
		}
		
		// Memnuniyete göre ruh halini güncelle
		protected virtual void UpdateMoodBasedOnSatisfaction()
		{
			// Memnuniyet ruh haline doğrudan etki eder
			_mood = Mathf.Clamp(_satisfaction - 0.1f + (GD.Randf() * 0.2f), 0.0f, 1.0f);
			
			// Memnuniyet düşükse gitmeyi düşünme ihtimali
			if (_satisfaction < 0.3f && GD.Randf() < 0.1f)
			{
				// Çok memnuniyetsiz, gitmeyi düşün
				ConsiderLeaving();
			}
		}
		
		// Gitmeyi düşün
		protected virtual void ConsiderLeaving()
		{
			// Sadakat faktörü - sadakat yüksekse, memnuniyetsizliğe rağmen kalma ihtimali artar
			float stayChance = _loyaltyLevel * 0.5f;
			
			// Sarhoşluk faktörü - sarhoşsa, memnuniyetsizliğe rağmen kalma ihtimali artar
			if (_drunkennessLevel > 0.5f)
			{
				stayChance += 0.2f;
			}
			
			// Kalma kararı
			if (GD.Randf() < stayChance)
			{
				// Kalmaya karar verdi
				GD.Print($"Customer {FullName} is unhappy but decided to stay due to loyalty or drunkenness.");
				
				// Belki bir şikayet ekleyebiliriz
				if (GD.Randf() < 0.5f)
				{
					Complain("Dissatisfaction");
				}
			}
			else
			{
				// Gitmeye karar verdi
				GD.Print($"Customer {FullName} is unhappy and decided to leave.");
				PrepareToLeave();
			}
		}
		
		// Gitmeye hazırlan
		public override void PrepareToLeave()
		{
			// Zaten gitmeye hazırlanıyorsa tekrarlama
			if (_isPreparingToLeave) return;
			
			GD.Print($"Customer {FullName} is preparing to leave. Spent: {_totalSpent}, Remaining: {_remainingBudget}");
			
			// Gitme durumunu ayarla
			_isPreparingToLeave = true;
			
			// Son ödemeyi hesapla ve yap
			MakeFinalPayment();
			
			// Masadan kalk
			LeaveTable();
			
			// Çıkışa yönel
			GoToExit();
		}
		
		// Son ödemeyi yap
		protected virtual void MakeFinalPayment()
		{
			// Son ödeme - bahşiş vb.
			float tipAmount = 0.0f;
			
			// Memnuniyet yüksekse bahşiş oranı da yüksek
			if (_satisfaction > 0.7f)
			{
				tipAmount = _totalSpent * (0.1f + (_satisfaction - 0.7f)); // Bahşiş %10-%40 arası
			}
			else if (_satisfaction > 0.5f)
			{
				tipAmount = _totalSpent * 0.05f; // Minimum %5 bahşiş
			}
			
			// Bahşiş bırak (bütçede kaldıysa)
			if (tipAmount > 0 && _remainingBudget >= tipAmount)
			{
				// Bahşiş bırak
				TipStaff(tipAmount);
			}
		}
		
		// Bahşiş ver
		protected virtual void TipStaff(float amount)
		{
			// Bütçeden bahşiş miktarını harca
			SpendMoney(amount, "Tip");
			
			// Personele bahşiş bildir
			if (GetTree().Root.HasNode("GameManager/StaffManager"))
			{
				var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
				
				if (staffManager.HasMethod("DistributeTipsToStaff"))
				{
					try 
					{
						staffManager.Call("DistributeTipsToStaff", amount);
						GD.Print($"Customer {FullName} left a tip of {amount}");
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error giving tip: {e.Message}");
					}
				}
			}
		}
		
		// Rasrtgele şikayet cümlesi al
		protected virtual string GetRandomComplaintPhrase()
		{
			if (_complainPhrases.Count == 0) return "Bu hizmet çok kötü.";
			
			return _complainPhrases[GD.RandRange(0, _complainPhrases.Count - 1)];
		}
		
		// Rastgele iltifat cümlesi al
		protected virtual string GetRandomComplimentPhrase()
		{
			if (_complimentPhrases.Count == 0) return "Teşekkürler, çok güzel.";
			
			return _complimentPhrases[GD.RandRange(0, _complimentPhrases.Count - 1)];
		}
		
		// Rastgele sipariş cümlesi al
		protected virtual string GetRandomOrderPhrase()
		{
			if (_orderPhrases.Count == 0) return "Bir içki alabilir miyim?";
			
			return _orderPhrases[GD.RandRange(0, _orderPhrases.Count - 1)];
		}
		
		// Rastgele sarhoş cümlesi al
		protected virtual string GetRandomDrunkPhrase()
		{
			if (_drunkPhrases.Count == 0) return "Bir daha... *hık*";
			
			return _drunkPhrases[GD.RandRange(0, _drunkPhrases.Count - 1)];
		}
		
		// Rastgele selamlama cümlesi al
		protected virtual string GetRandomGreeting()
		{
			if (_greetings.Count == 0) return "Merhaba.";
			
			return _greetings[GD.RandRange(0, _greetings.Count - 1)];
		}
		
		// Müşteri bilgisi - müşteri manager için
		public override Dictionary<string, object> GetCustomerInfo()
		{
			Dictionary<string, object> info = base.GetCustomerInfo();
			
			// Regular müşteriye özel bilgiler
			info["VisitCount"] = _visitCount;
			info["LoyaltyLevel"] = _loyaltyLevel;
			info["AlcoholTolerance"] = _alcoholTolerance;
			info["WantsToOrder"] = _wantsToOrder;
			info["CurrentDrinkOrder"] = _currentDrinkOrder;
			info["CurrentFoodOrder"] = _currentFoodOrder;
			info["IsHereForFood"] = _isHereForFood;
			info["MusicMoodEffect"] = _musicMoodEffect;
			info["AmbientMoodEffect"] = _ambientMoodEffect;
			
			return info;
		}
	}
}
