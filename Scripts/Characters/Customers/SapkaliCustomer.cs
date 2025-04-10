// Scripts/Characters/Customers/SapkaliCustomer.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	public partial class SapkaliCustomer : CustomerBase
	{
		// Unique Sapkali characteristics
		private float _moneyFlashFrequency = 0.0f;    // How often the customer flashes money
		private float _sinceLastFlash = 0.0f;         // Timer since last money flash
		private float _remainingLandMoney;            // Money from land sale, separate from budget
		private string _hatType;                      // Type of hat (Kasket, Fötr, Hasır şapka, etc.)
		private string _homeVillage;                  // Village they come from
		private int _consecutiveDrinks = 0;           // Track drink count in a single session
		private bool _hasMadeGrandGesture = false;    // Has made a grand spending gesture yet?

		// Ankara region villages
		private static readonly string[] _ankaraVillages = {
			"Çubuk", "Kalecik", "Kızılcahamam", "Bala", "Haymana", 
			"Şereflikoçhisar", "Nallıhan", "Beypazarı", "Güdül", "Ayaş", 
			"Polatlı", "Elmadağ", "Kazan", "Sincan", "Gölbaşı"
		};

		// Hat types with descriptions
		private static readonly Dictionary<string, string> _hatTypes = new Dictionary<string, string>
		{
			{ "Kasket", "Klasik köylü kasketi, eskimiş fakat temiz" },
			{ "Fötr", "Eskiden şehre inince takılan özel fötr şapka" },
			{ "Hasır", "Yazlık hasır şapka, tarlada çalışırken giyilen türden" },
			{ "Kalpak", "Eskiden kalma siyah kalpak, aile yadigarı" },
			{ "Şapka", "Cumhuriyet döneminden kalma resmi tarz şapka" }
		};

		// Sapkali-specific dialogue phrases
		private string[] _specificGreetings = {
			"Selamınaleyküm, mekan sahibi nerde?",
			"Ula burası şehrin en iyi yeri dediler, doğru mu?",
			"Çok methettiler burayı köyde, geldik bakalım!",
			"Bizim köyün yarısının tarlasını aldı müteahhit, hepimiz burdayık bu akşam!",
			"Şehire inince ilk iş buraya gel dediler!"
		};

		private string[] _moneyFlashPhrases = {
			"Bak yeğenim, para bu köyün ağzına sıçayım!",
			"Anam babam, bu para bitecek gibi değil, valla!",
			"Al bakayım şu paraları, akşama kadar masama bak sen!",
			"Müteahhit verdi parayı, harcamasını da biliriz!",
			"Bizim oranın tarlası altın değerindeymiş meğer, al bak!"
		};

		private string[] _drunkPhrases = {
			"Köyde... *hık*... öküz bile bu kadar içki içmez vallaha!",
			"Babamın tarlası... *hık*... şimdi AVM olacakmış, içelim bakalım!",
			"Dedim dayıya... *hık*... satma tarlayı. Dinlemedi beni!",
			"Bir kasa rakıyı... *hık*... tek başıma götürürdüm eskiden!",
			"Şu konsların... *hık*... hepsine tarla alırım ben valla!"
		};

		private string[] _specialRequests = {
			"Bana en büyük rakıdan getir! Hem de AÇTIRMA bak!",
			"Şu muganniye söyle biraz köy türküsü söylesin!",
			"PAVYONU SATTIN ALIYORUM bu akşam, söyle patrona!",
			"Bugün sadece bizim masaya bakacak konslar, anlaştık mı?",
			"Patron gelsin şuraya, özel bir anlaşma yapacağım!"
		};

		// Constructor with override for CustomerBase
		public SapkaliCustomer() : base()
		{
			// Base constructor already created CustomerId
		}

		public SapkaliCustomer(string fullName, int age, string gender, float budget)
			: base(fullName, age, gender, CustomerType.Sapkali, budget)
		{
			// Additional initialization
			InitializeSapkaliTraits();
		}

		public override void _Ready()
		{
			base._Ready();
			
			// If this is a newly instantiated customer without custom initialization
			if (_hatType == null)
			{
				InitializeSapkaliTraits();
			}

			// Introduce self with Sapkali-specific greeting
			int greetingIndex = (int)(GD.Randf() * _specificGreetings.Length);
			Say(_specificGreetings[greetingIndex], 4.0f);
		}

		// Initialize Sapkali-specific traits
		private void InitializeSapkaliTraits()
		{
			// Select a random village
			_homeVillage = _ankaraVillages[GD.RandRange(0, _ankaraVillages.Length - 1)];
			
			// Select a random hat type
			string[] hatKeys = new string[_hatTypes.Keys.Count];
			_hatTypes.Keys.CopyTo(hatKeys, 0);
			_hatType = hatKeys[GD.RandRange(0, hatKeys.Length - 1)];
			
			// Set land money (separate budget just for "showing off")
			_remainingLandMoney = _budget * 0.7f; // 70% of budget is land money for showing off
			
			// Adjust generosity even higher than base initialized it
			_generosity = Mathf.Min(1.0f, _generosity + 0.15f);
			
			// Flash frequency - how often they show their money
			_moneyFlashFrequency = 180.0f + GD.Randf() * 300.0f; // 3-8 minutes between flashes
			
			// Custom signature combining hat and village
			_signature = $"{_hatType} şapkalı, {_homeVillage}'lı çiftçi";
			
			// Ensure VIP status - Sapkali customers are always VIP
			IsVIP = true;
		}

		public override void _Process(double delta)
		{
			base._Process(delta);
			
			// Process Sapkali-specific behaviors
			ProcessMoneyFlashing((float)delta);
			
			// Check for grand gesture opportunities
			if (!_hasMadeGrandGesture && _timeInPavyon > 60.0f && GD.Randf() < 0.001f)
			{
				MakeGrandGesture();
			}
		}

		// Process money flashing behavior - showing off wealth
		private void ProcessMoneyFlashing(float delta)
		{
			_sinceLastFlash += delta * 60.0f; // Convert to minutes
			
			// If enough time has passed and the customer has land money to flash
			if (_sinceLastFlash >= _moneyFlashFrequency && _remainingLandMoney > 1000.0f && _currentState == CustomerState.Sitting)
			{
				FlashMoney();
				_sinceLastFlash = 0.0f;
				
				// Adjust next flash frequency based on drunkenness
				_moneyFlashFrequency = Mathf.Max(60.0f, 180.0f - (_drunkennessLevel * 120.0f));
			}
		}

		// Money flashing behavior
		private void FlashMoney()
		{
			// Select a random phrase
			int phraseIndex = (int)(GD.Randf() * _moneyFlashPhrases.Length);
			Say(_moneyFlashPhrases[phraseIndex], 4.0f);
			
			// Play animation
			PlayAnimation("flash_money");
			
			// Calculate amount to flash (5-20% of remaining land money)
			float flashAmount = _remainingLandMoney * (0.05f + (GD.Randf() * 0.15f));
			flashAmount = Mathf.Min(flashAmount, _remainingLandMoney);
			
			// Update remaining land money
			_remainingLandMoney -= flashAmount;
			
			// Notify event system about money flashing
			if (GetTree().Root.HasNode("GameManager/EventManager"))
			{
				var eventManager = GetTree().Root.GetNode("GameManager/EventManager");
				if (eventManager.HasMethod("OnCustomerFlashedMoney"))
				{
					eventManager.Call("OnCustomerFlashedMoney", CustomerId, flashAmount, Position);
				}
			}
			
			// Increase chance of kons interaction
			if (_assignedKonsId != null)
			{
				ChangeState(CustomerState.TalkingToKons);
			}
			
			GD.Print($"Sapkali customer {FullName} flashed {flashAmount} TL from village money!");
		}

		// Override drink ordering with Sapkali-specific behavior
		public override string OrderDrink()
		{
			// Farmers typically prefer rakı above all else
			if (GD.Randf() < 0.8f + (_drunkennessLevel * 0.1f))
			{
				// Rakı preference increases with drunkenness
				string drinkType = "Rakı";
				
				// Update order history
				if (_orderHistory.ContainsKey(drinkType))
					_orderHistory[drinkType]++;
				else
					_orderHistory[drinkType] = 1;
				
				_drinkCount++;
				_consecutiveDrinks++;
				
				// Specific ordering animations and phrases depending on how many drinks
				if (_consecutiveDrinks <= 2)
				{
					PlayAnimation("order_drink");
					Say("Rakı getir hemşerim, büyük olsun!");
				}
				else if (_consecutiveDrinks <= 5)
				{
					PlayAnimation("order_drink_excited");
					Say("Çiftçinin içtiği rakıdır, büyükten doldur!");
				}
				else
				{
					PlayAnimation("order_drink_very_drunk");
					Say("AÇTIĞRRRRR RAKIYI GETIR BURAYA!");
				}
				
				// 30% chance of ordering for the whole table when drunk
				if (_drunkennessLevel > 0.6f && GD.Randf() < 0.3f)
				{
					OrderDrinksForOthers();
				}
				
				return drinkType;
			}
			else
			{
				// Occasionally order something else
				return base.OrderDrink();
			}
		}

		// Special function for ordering drinks for others
		private void OrderDrinksForOthers()
		{
			Say("Tüm masaya doldur, ben ödüyorum! KÖYÜN AĞASI GELDİ BURAYA!");
			
			// Notify nearby customers that drinks are on this customer
			var customerManager = GetTree().Root.GetNode("GameManager/CustomerManager");
			if (customerManager != null && customerManager.HasMethod("NotifyDrinksOnCustomer"))
			{
				customerManager.Call("NotifyDrinksOnCustomer", CustomerId, Position, 5.0f); // 5m radius
			}
			
			// Spend a larger amount
			float amount = 1000.0f + (GD.Randf() * 1000.0f);
			SpendMoney(amount, "drinks_for_others");
			
			// Enhanced reputation within the pavyon
			AdjustSatisfaction(0.1f, "Showing generosity");
			
			GD.Print($"Sapkali customer {FullName} ordered drinks for everyone nearby, spending {amount} TL!");
		}

		// Grand gesture - a big show of wealth (usually once per visit)
		private void MakeGrandGesture()
		{
			if (_remainingLandMoney < 5000.0f) return;
			
			// Mark as made gesture
			_hasMadeGrandGesture = true;
			
			// Random selection of grand gesture type
			float random = GD.Randf();
			
			if (random < 0.4f)
			{
				// Big tip to musician
				GrandGestureMusician();
			}
			else if (random < 0.8f)
			{
				// Pay for everyone's tab
				GrandGestureEveryoneTab();
			}
			else
			{
				// Try to buy something in the pavyon
				GrandGestureBuyItem();
			}
		}

		// Grand gesture types
		private void GrandGestureMusician()
		{
			float amount = _remainingLandMoney * 0.3f;
			_remainingLandMoney -= amount;
			
			Say("MÜZİSYENE YOLLAAAA! ÖZEL ŞARKI İSTİYORUM! " + _homeVillage + " TÜRKÜSÜ ÇALSIN!");
			
			// Notify musician manager
			var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
			if (staffManager != null && staffManager.HasMethod("RequestSpecialPerformance"))
			{
				staffManager.Call("RequestSpecialPerformance", "Musician", amount, CustomerId, _homeVillage + " Türküsü");
			}
			
			// Create a special event
			var eventManager = GetTree().Root.GetNode("GameManager/EventManager");
			if (eventManager != null && eventManager.HasMethod("TriggerEvent"))
			{
				eventManager.Call("TriggerEvent", "SapkaliMusicRequest", new Dictionary<string, object>
				{
					{ "customerId", CustomerId },
					{ "amount", amount },
					{ "village", _homeVillage }
				});
			}
			
			GD.Print($"Sapkali customer {FullName} made a grand gesture to musician, spending {amount} TL!");
		}

		private void GrandGestureEveryoneTab()
		{
			float amount = _remainingLandMoney * 0.4f;
			_remainingLandMoney -= amount;
			
			Say("HERKES DİNLESİN! BU GECE HERKESIN HESABI BENDEN! KÖYÜN TARLALARI PARA ETTİ!");
			
			// Notify customer manager
			var customerManager = GetTree().Root.GetNode("GameManager/CustomerManager");
			if (customerManager != null && customerManager.HasMethod("NotifyTabsPaidBy"))
			{
				customerManager.Call("NotifyTabsPaidBy", CustomerId, amount);
			}
			
			// Create a special event
			var eventManager = GetTree().Root.GetNode("GameManager/EventManager");
			if (eventManager != null && eventManager.HasMethod("TriggerEvent"))
			{
				eventManager.Call("TriggerEvent", "SapkaliPaidAllTabs", new Dictionary<string, object>
				{
					{ "customerId", CustomerId },
					{ "amount", amount }
				});
			}
			
			GD.Print($"Sapkali customer {FullName} paid everyone's tab as a grand gesture, spending {amount} TL!");
		}

		private void GrandGestureBuyItem()
		{
			float amount = _remainingLandMoney * 0.5f;
			_remainingLandMoney -= amount;
			
			// Select a random pavyon item to try to buy
			string[] items = {
				"En büyük rakı şişesi", "Müzisyenin sazı", "Pavyon tabelası",
				"Barmenin yeleği", "Özel VIP masası", "Pavyondaki en büyük avize"
			};
			
			string item = items[GD.RandRange(0, items.Length - 1)];
			
			Say($"ŞU {item.ToUpper()}'NI SATTIN ALIYORUM! AL PARANIZI!");
			
			// Notify manager
			var gameManager = GetTree().Root.GetNode("GameManager");
			if (gameManager != null && gameManager.HasMethod("HandleItemPurchaseAttempt"))
			{
				gameManager.Call("HandleItemPurchaseAttempt", CustomerId, item, amount);
			}
			
			// Create a special event
			var eventManager = GetTree().Root.GetNode("GameManager/EventManager");
			if (eventManager != null && eventManager.HasMethod("TriggerEvent"))
			{
				eventManager.Call("TriggerEvent", "SapkaliBuyAttempt", new Dictionary<string, object>
				{
					{ "customerId", CustomerId },
					{ "item", item },
					{ "amount", amount }
				});
			}
			
			GD.Print($"Sapkali customer {FullName} tried to buy {item} for {amount} TL as a grand gesture!");
		}

		// Override sarhoş lafları with Sapkali-specific drunk phrases
		protected override void SayRandomDrunkPhrase()
		{
			// If very drunk, use Sapkali-specific phrases
			if (_drunkennessLevel > 0.6f && GD.Randf() < 0.7f)
			{
				int index = (int)(GD.Randf() * _drunkPhrases.Length);
				Say(_drunkPhrases[index], 3.0f);
			}
			else
			{
				// Fall back to base behavior sometimes
				base.SayRandomDrunkPhrase();
			}
		}

		// Override state change to add Sapkali-specific behaviors
		protected override void OnStateChanged(CustomerState previousState, CustomerState newState)
		{
			base.OnStateChanged(previousState, newState);
			
			// Sapkali-specific state transitions
			if (newState == CustomerState.Sitting && previousState == CustomerState.WaitingToSit)
			{
				// Show off when first sitting down
				_sinceLastFlash = _moneyFlashFrequency; // Force a money flash soon
			}
			else if (newState == CustomerState.OrderingDrink && _drunkennessLevel > 0.5f)
			{
				// Special requests when ordering while drunk
				if (GD.Randf() < 0.3f)
				{
					int requestIndex = (int)(GD.Randf() * _specialRequests.Length);
					Say(_specialRequests[requestIndex], 4.0f);
				}
			}
			else if (newState == CustomerState.Leaving)
			{
				// Special goodbye when leaving
				Say("Benden bu kadar, köye dönme vakti geldi! HESABI GETİR HEMŞERIM!");
			}
		}

		// Override drink reception for Sapkali-specific behavior
		public override void ReceiveDrink(string drinkType, float quality)
		{
			// Call base implementation first
			base.ReceiveDrink(drinkType, quality);
			
			// Additional Sapkali-specific behavior
			if (drinkType == "Rakı" && _drunkennessLevel > 0.4f)
			{
				// Special rakı toast
				string[] rakiToasts = {
					"Tarla gitti ama KEYIF BIZDE!",
					"Köyün şerefine!",
					"Ankara'nın şerefine!",
					"Çiftçinin alın terine!",
					"Müteahhidin cebi doldu, bizim keyif de yerinde!"
				};
				
				int toastIndex = (int)(GD.Randf() * rakiToasts.Length);
				Say(rakiToasts[toastIndex], 3.0f);
				
				// Chance to flash money with each rakı at higher drunkenness
				if (_drunkennessLevel > 0.6f && GD.Randf() < 0.3f && _remainingLandMoney > 500.0f)
				{
					FlashMoney();
				}
			}
		}

		// Override payment behavior to include more generous tipping
		protected override void ProcessPayment()
		{
			// Bonus tip percentage for Sapkali from land money
			float standardTipPercentage = _generosity * 0.2f + (_satisfaction - 0.5f) * 0.1f;
			
			// Add land money bonus if any remains
			float landMoneyTipPercentage = 0.0f;
			if (_remainingLandMoney > 0)
			{
				landMoneyTipPercentage = 0.3f; // Extra 30% tip from land money
				
				// Use some of remaining land money as extra tip
				float extraTip = _remainingLandMoney * 0.2f;
				
				// If assigned kons, give tip
				if (_assignedKonsId != null)
				{
					GiveTip(extraTip, _assignedKonsId);
				}
				// Otherwise spread among staff
				else if (_interactedStaffIds.Count > 0)
				{
					float tipPerStaff = extraTip / _interactedStaffIds.Count;
					foreach (string staffId in _interactedStaffIds)
					{
						GiveTip(tipPerStaff, staffId);
					}
				}
				
				_remainingLandMoney = 0; // All land money now spent
			}
			
			// If very drunk, might overtip dramatically
			if (_drunkennessLevel > 0.8f && GD.Randf() < 0.5f)
			{
				standardTipPercentage += 0.2f; // Additional 20% when very drunk
				Say("PARA BENDE ÇOK! AL HEPSİNİ! *Hık*");
			}
			
			// Modify base behavior to include land money bonus
			float tipAmount = _totalSpent * (standardTipPercentage + landMoneyTipPercentage);
			
			// Ensure tip doesn't exceed remaining budget
			tipAmount = Mathf.Min(tipAmount, _remainingBudget);
			
			// Give tip to staff
			if (_assignedKonsId != null)
			{
				GiveTip(tipAmount, _assignedKonsId);
			}
			else if (_interactedStaffIds.Count > 0)
			{
				// Distribute among staff
				int randomIndex = (int)(GD.Randf() * _interactedStaffIds.Count);
				GiveTip(tipAmount, _interactedStaffIds[randomIndex]);
			}
			
			GD.Print($"Sapkali customer {FullName} paid total: {_totalSpent}, tip: {tipAmount} (including land money bonus)");
		}

		// Override GetStats to include Sapkali-specific properties
		public new Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = base.GetStats();
			
			// Add Sapkali-specific stats
			stats["HatType"] = _hatType;
			stats["HomeVillage"] = _homeVillage;
			stats["RemainingLandMoney"] = _remainingLandMoney;
			stats["HasMadeGrandGesture"] = _hasMadeGrandGesture;
			stats["ConsecutiveDrinks"] = _consecutiveDrinks;
			
			return stats;
		}
	}
}
