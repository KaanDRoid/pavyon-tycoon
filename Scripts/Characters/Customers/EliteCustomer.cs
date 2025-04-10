// Scripts/Characters/Customers/EliteCustomer.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	public partial class EliteCustomer : CustomerBase
	{
		// Elite-specific attributes
		private float _statusConsciousness = 0.8f;      // How concerned they are about being treated like a VIP (0-1)
		private float _businessInfluence = 0.7f;        // Their influence on local business/politics (0-1)
		private bool _hasSpecialTable = false;          // Whether they have a reserved VIP table
		private int _entourageSize = 0;                 // How many people they brought with them
		private float _conspicuousSpending = 0.8f;      // Tendency to spend visibly to impress others (0-1)
		private float _connectionLevel = 0.6f;          // Connection to powerful figures (0-1)
		
		// Elite customer preferences
		private Dictionary<string, float> _elitePreferences = new Dictionary<string, float>();
		
		// Entry style (flash, quiet, with security, etc.)
		private string _entryStyle = "Standard";
		
		// Recent insult tracking (for drama creation)
		private bool _recentlyInsulted = false;
		private string _insultSource = "";
		private float _insultSeverity = 0f;
		
		// Premium service expectations
		private float _serviceExpectation = 0.9f;       // Very high expectations (0-1)
		private float _attentionRequirement = 0.8f;     // Need for staff attention (0-1)
		
		// Special requests and accommodations
		private List<string> _specialRequests = new List<string>();
		
		// Business connections and networking
		private Dictionary<string, float> _businessContacts = new Dictionary<string, float>();
		
		public EliteCustomer() : base()
		{
			// Default constructor
		}
		
		// Constructor with parameters
		public EliteCustomer(string fullName, int age, string gender, float budget) 
			: base(fullName, age, gender, CustomerType.Elite, budget)
		{
			// Initialize Elite-specific attributes and behavior patterns
			InitializeEliteAttributes();
		}
		
		public override void _Ready()
		{
			base._Ready();
			
			// Elite-specific setup
			if (_customerType != CustomerType.Elite)
			{
				_customerType = CustomerType.Elite;
				InitializeByCustomerType();
			}
			
			// Initialize Elite attributes if not done
			if (_statusConsciousness == 0)
			{
				InitializeEliteAttributes();
			}
			
			// Set appropriate appearance and animations
			SetEliteAppearance();
		}
		
		// Initialize Elite-specific attributes and preferences
		private void InitializeEliteAttributes()
		{
			// Basic Elite characteristics
			IsVIP = true;
			_budget = Mathf.Max(_budget, 10000f);          // Ensure minimum elite budget
			_remainingBudget = _budget;
			_generosity = 0.6f + GD.Randf() * 0.3f;        // 0.6-0.9 generosity range
			_statusConsciousness = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0 status consciousness
			_businessInfluence = 0.5f + GD.Randf() * 0.4f; // 0.5-0.9 business influence
			_entourageSize = GD.RandRange(0, 3);           // 0-3 people in entourage
			_serviceExpectation = 0.8f + GD.Randf() * 0.2f; // Very high service expectations
			
			// Signature appearance based on subtype
			SetEliteSubtype();
			
			// Special preferences
			InitializeElitePreferences();
			
			// Generate random special requests
			GenerateSpecialRequests();
			
			// Create some business connections
			GenerateBusinessContacts();
			
			// Adjust base stats
			_kargaLevel = 0.01f;                         // Very unlikely to leave without paying
			_aggressionLevel = 0.2f + GD.Randf() * 0.3f; // Can be assertive but rarely violent
			_maxStayTime = 240.0f + _entourageSize * 30.0f; // 4 hours + 30 mins per entourage member
		}
		
		// Initialize elite-specific preferences
		protected void InitializeElitePreferences()
		{
			// Elite customers have specific preferences
			_elitePreferences["premium_attention"] = 0.8f + GD.Randf() * 0.2f;  // Expects premium attention
			_elitePreferences["privacy_level"] = 0.7f + GD.Randf() * 0.3f;      // Values privacy
			_elitePreferences["recognition"] = 0.6f + GD.Randf() * 0.4f;        // Likes to be recognized
			_elitePreferences["show_off"] = 0.5f + GD.Randf() * 0.5f;           // May like to show off
			_elitePreferences["networking"] = 0.7f + GD.Randf() * 0.3f;         // Values business connections
			
			// Add these preferences to the base preferences dictionary
			foreach (var pref in _elitePreferences)
			{
				_preferences[pref.Key] = pref.Value;
			}
			
			// Modify drink preferences to favor premium drinks
			_preferences["drink_whiskey"] = 0.7f + GD.Randf() * 0.3f;     // High-end whiskey
			_preferences["drink_special"] = 0.8f + GD.Randf() * 0.2f;     // Custom cocktails
			_preferences["drink_raki"] = 0.5f + GD.Randf() * 0.3f;        // May enjoy raki
			_preferences["drink_wine"] = 0.6f + GD.Randf() * 0.3f;        // Fine wines
			
			// Modify music preferences to favor elegant atmospheres
			_preferences["music_modern"] = 0.7f + GD.Randf() * 0.3f;      // Modern lounge music
			_preferences["music_fantezi"] = 0.4f + GD.Randf() * 0.4f;     // Varies by individual
			
			// Ambiance preferences
			_preferences["ambiance_luxurious"] = 0.9f + GD.Randf() * 0.1f;  // Strongly prefers luxury
			_preferences["ambiance_intimate"] = 0.7f + GD.Randf() * 0.3f;   // Values intimate settings
			_preferences["ambiance_loud"] = 0.3f - GD.Randf() * 0.2f;       // Generally dislikes loud environments
			
			// Staff preferences
			_preferences["staff_kons"] = 0.8f + GD.Randf() * 0.2f;        // Strong preference for konsomatirsler
		}
		
		// Set elite subtype and signature based on random selection
		private void SetEliteSubtype()
		{
			string[] eliteSubtypes = {
				"BusinessMogul", "PoliticalConnector", "IndustryTycoon", 
				"OldMoney", "NewMoney", "LuxuryDealer"
			};
			
			int subtypeIndex = GD.RandRange(0, eliteSubtypes.Length - 1);
			string subtype = eliteSubtypes[subtypeIndex];
			
			// Set appropriate signature based on subtype
			switch (subtype)
			{
				case "BusinessMogul":
					_signature = "Özel dikim takım elbise, parmakta yüzük, gözde keskin bakışlar";
					_businessInfluence = 0.9f;
					break;
				case "PoliticalConnector":
					_signature = "Klasik takım elbise, saat, temkinli konuşma";
					_connectionLevel = 0.9f;
					break;
				case "IndustryTycoon":
					_signature = "Modern takım, teknolojik aksesuarlar, güvenli duruş";
					_businessInfluence = 0.8f;
					_conspicuousSpending = 0.9f;
					break;
				case "OldMoney":
					_signature = "Minimal lüks, vintage aksesuarlar, gösterişsiz zenginlik";
					_statusConsciousness = 0.6f;
					_entryStyle = "Quiet";
					break;
				case "NewMoney":
					_signature = "Markalı kıyafetler, gösterişli saat, etrafa göz gezdiren bakışlar";
					_conspicuousSpending = 0.95f;
					_entryStyle = "Flash";
					break;
				case "LuxuryDealer":
					_signature = "İtalyan ayakkabılar, ince detaylı kıyafet, keskin gözler";
					_businessInfluence = 0.7f;
					_entourageSize += 1; // Extra people
					break;
			}
		}
		
		// Generate random special requests based on elite status
		private void GenerateSpecialRequests()
		{
			string[] possibleRequests = {
				"Özel içki servisi", "VIP masa", "Belirli bir kons", 
				"Özel menü", "Özel içki", "Sessiz köşe", 
				"Güvenlikli alan", "Özel müzik seçimi"
			};
			
			// Add 1-3 random special requests
			int requestCount = GD.RandRange(1, 3);
			
			for (int i = 0; i < requestCount; i++)
			{
				int index = GD.RandRange(0, possibleRequests.Length - 1);
				
				// Avoid duplicates
				if (!_specialRequests.Contains(possibleRequests[index]))
				{
					_specialRequests.Add(possibleRequests[index]);
				}
			}
		}
		
		// Generate business contacts for networking potential
		private void GenerateBusinessContacts()
		{
			string[] possibleContacts = {
				"Belediye", "Polis", "Müteahhit", "İş İnsanları", 
				"Sanatçılar", "Sporcular", "Medya", "Bankacılar"
			};
			
			// Add 2-4 random business connections
			int contactCount = GD.RandRange(2, 4);
			
			for (int i = 0; i < contactCount; i++)
			{
				int index = GD.RandRange(0, possibleContacts.Length - 1);
				string contact = possibleContacts[index];
				
				if (!_businessContacts.ContainsKey(contact))
				{
					// Connection strength (0.5-1.0)
					_businessContacts[contact] = 0.5f + GD.Randf() * 0.5f;
				}
			}
		}
		
		// Set elite appearance and animations
		private void SetEliteAppearance()
		{
			// Set appropriate appearance model based on gender
			// This would connect to the actual 3D model system
			if (_model != null)
			{
				// Apply elite appearance to model
				// (In actual implementation, this would set appropriate meshes,
				// textures, and accessories based on elite subtype)
			}
			
			// Set custom animations if available
			if (_animationPlayer != null)
			{
				// Check for elite-specific animations
				if (_animationPlayer.HasAnimation("elite_sit"))
				{
					// Override default animations with elite versions
				}
			}
		}
		
		// Override to modify behavior based on elite status
		protected override void UpdateSittingState(float delta)
		{
			base.UpdateSittingState(delta);
			
			// Elite customers expect regular attention
			if (_timeInStateBelonging > 30.0f && !_hasSpecialTable && GD.Randf() < 0.01f * _statusConsciousness)
			{
				// May become dissatisfied without VIP treatment
				AdjustSatisfaction(-0.02f, "Inadequate VIP service");
				Say("Bize özel bir masa yok mu?");
			}
			
			// May request special service occasionally
			if (_timeInStateBelonging > 60.0f && GD.Randf() < 0.005f * _attentionRequirement)
			{
				RequestSpecialService();
			}
			
			// Networking opportunities
			if (_timeInStateBelonging > 120.0f && GD.Randf() < 0.003f * _preferences["networking"])
			{
				AttemptNetworking();
			}
			
			// Higher chance of ordering premium items
			if (!_isMoving && GD.Randf() < 0.007f * _conspicuousSpending)
			{
				ChangeState(CustomerState.OrderingDrink);
			}
		}
		
		// Override to provide unique ordering behavior
		protected override void UpdateOrderingDrinkState(float delta)
		{
			// Elite customers are more impatient with service
			if (_timeInStateBelonging > 60.0f) // Only 1 minute wait tolerance (vs 2 for regular)
			{
				AdjustSatisfaction(-0.15f, "Poor service speed");
				Say("Servis ne kadar yavaş!");
				ChangeState(CustomerState.Sitting);
			}
			else
			{
				base.UpdateOrderingDrinkState(delta);
			}
		}
		
		// Override to ensure premium food selections
		protected override string GetPreferredMezeType()
		{
			// Elite customers prefer premium mezes
			string[] premiumMezes = { 
				"Karides Güveç", "Levrek Marin", "Kalamar Tava", 
				"Kuzu Tandır", "İstiridye", "Özel Kavurma", 
				"Özel Peynir Tabağı" 
			};
			
			// Sometimes select from premium options
			if (GD.Randf() < 0.7f)
			{
				int index = GD.RandRange(0, premiumMezes.Length - 1);
				return premiumMezes[index];
			}
			
			// Otherwise use base selection
			return base.GetPreferredMezeType();
		}
		
		// Override to prefer premium drinks
		protected override string GetPreferredDrinkType()
		{
			// Elite customers prefer premium drinks
			string[] premiumDrinks = { 
				"12 Yıllık Viski", "Dom Perignon", "Özel Kokteyl", 
				"Yıllık Şarap", "Premium Rakı", "Tekila" 
			};
			
			// 80% chance to order a premium drink
			if (GD.Randf() < 0.8f)
			{
				int index = GD.RandRange(0, premiumDrinks.Length - 1);
				return premiumDrinks[index];
			}
			
			// Otherwise use base selection
			return base.GetPreferredDrinkType();
		}
		
		// Special service request handling
		private void RequestSpecialService()
		{
			if (_specialRequests.Count == 0)
				return;
				
			// Select a random special request
			int index = GD.RandRange(0, _specialRequests.Count - 1);
			string request = _specialRequests[index];
			
			// Make the request
			Say($"{request} istiyorum.");
			
			// Log the request for the manager
			RequestSpecialServiceFromManager(request);
		}
		
		// Request special service from manager
		private void RequestSpecialServiceFromManager(string request)
		{
			// Contact CustomerManager to handle the special request
			if (GetTree().Root.HasNode("GameManager/CustomerManager"))
			{
				var customerManager = GetTree().Root.GetNode("GameManager/CustomerManager");
				
				if (customerManager.HasMethod("HandleSpecialRequest"))
				{
					customerManager.Call("HandleSpecialRequest", CustomerId, request, _customerType.ToString());
				}
			}
		}
		
		// Networking attempt
		private void AttemptNetworking()
		{
			// Look for notable customers to network with
			var nearbyVips = FindNearbyVips();
			
			if (nearbyVips.Count > 0)
			{
				// Choose a random VIP to network with
				int index = GD.RandRange(0, nearbyVips.Count - 1);
				var targetVip = nearbyVips[index];
				
				// Initiate networking
				Say("Merhaba, tanışabilir miyiz?");
				
				// Log the networking attempt for game events
				RecordNetworkingAttempt(targetVip);
			}
			else
			{
				// No VIPs nearby, may affect satisfaction
				AdjustSatisfaction(-0.05f, "Limited networking opportunities");
			}
		}
		
		// Find nearby VIP customers
		private List<Node3D> FindNearbyVips()
		{
			List<Node3D> nearbyVips = new List<Node3D>();
			
			// In a real implementation, this would use spatial queries or the CustomerManager
			// to find nearby VIP customers. For now, we'll use a placeholder method.
			
			// Placeholder approach - ask CustomerManager about nearby VIPs
			if (GetTree().Root.HasNode("GameManager/CustomerManager"))
			{
				var customerManager = GetTree().Root.GetNode("GameManager/CustomerManager");
				
				if (customerManager.HasMethod("GetNearbyVips"))
				{
					var result = customerManager.Call("GetNearbyVips", GlobalPosition, 5.0f); // 5m radius
					
					// Convert result to list
					if (result != null && result is Godot.Collections.Array vipArray)
					{
						foreach (var vip in vipArray)
						{
							if (vip is Node3D vipNode && vipNode != this)
							{
								nearbyVips.Add(vipNode);
							}
						}
					}
				}
			}
			
			return nearbyVips;
		}
		
		// Record networking attempt
		private void RecordNetworkingAttempt(Node3D target)
		{
			// In a real implementation, this would report to the GameManager or EventManager
			// to potentially trigger business events, reputation changes, etc.
			
			// Log for now
			GD.Print($"Elite customer {FullName} attempted to network with {target.Name}");
		}
		
		// React to insult or perceived slight
		public void ReactToInsult(string source, float severity)
		{
			_recentlyInsulted = true;
			_insultSource = source;
			_insultSeverity = severity;
			
			// Record the insult for potential story events
			_eventHistory.Add($"Insulted by {source}");
			
			// React based on severity
			if (severity > 0.7f)
			{
				// Major insult - may leave or call authorities
				AdjustSatisfaction(-0.3f, "Major insult");
				
				if (GD.Randf() < 0.6f)
				{
					Say("Bu hakaret kabul edilemez! Mekanın sahibiyle görüşeceğim!");
					PrepareToLeave();
				}
				else
				{
					Say("Bunu yanınıza bırakmayacağım!");
				}
			}
			else if (severity > 0.3f)
			{
				// Moderate insult - visible displeasure
				AdjustSatisfaction(-0.15f, "Moderate insult");
				Say("Bu davranış çok uygunsuz!");
			}
			else
			{
				// Minor slight - may just be annoyed
				AdjustSatisfaction(-0.05f, "Minor slight");
				Say("Hmmm...");
			}
		}
		
		// Handle special VIP treatment
		public void ReceiveVipTreatment(string treatmentType)
		{
			switch (treatmentType)
			{
				case "VIPTable":
					_hasSpecialTable = true;
					AdjustSatisfaction(0.1f, "VIP table");
					Say("İşte olması gereken muamele bu!");
					break;
					
				case "ComplementaryDrink":
					AdjustSatisfaction(0.08f, "Complementary drink");
					Say("Teşekkürler, çok naziksiniz.");
					break;
					
				case "SpecialAttention":
					AdjustSatisfaction(0.1f, "Special attention");
					Say("Buranın müdavimi olabilirim.");
					break;
					
				case "PremiumService":
					AdjustSatisfaction(0.15f, "Premium service");
					_generosity += 0.05f; // Increase tip likelihood
					Say("Harika bir servis!");
					break;
			}
		}
		
		// Provide business or political connection effects
		public void ProvideBusinessInfluence(string target, float amount)
		{
			// Elite customers can provide business benefits to the pavyon
			if (_businessContacts.ContainsKey(target))
			{
				// Strength based on connection level and their own influence
				float effectiveAmount = amount * _businessContacts[target] * _businessInfluence;
				
				// Report influence to ReputationManager
				if (GetTree().Root.HasNode("GameManager/ReputationManager"))
				{
					var repManager = GetTree().Root.GetNode("GameManager/ReputationManager");
					
					if (repManager.HasMethod("ApplyBusinessInfluence"))
					{
						repManager.Call("ApplyBusinessInfluence", target, effectiveAmount, FullName);
					}
				}
				
				GD.Print($"Elite customer {FullName} provided {effectiveAmount} business influence to {target}");
			}
		}
		
		// Override what happens when an elite customer gets drunk
		protected override void ProcessDrunkennessEffects(float delta)
		{
			// Elite customers try to maintain composure even when drunk
			if (_drunkennessLevel > 0.7f)
			{
				// Occasional slip in composure
				if (GD.Randf() < 0.001f * _drunkennessLevel)
				{
					SayRandomEliteDrunkPhrase();
				}
				
				// More likely to spend when very drunk
				if (GD.Randf() < 0.002f * _drunkennessLevel * _conspicuousSpending)
				{
					AttemptBigPurchase();
				}
				
				// Chance of revealing business connections
				if (GD.Randf() < 0.001f * _drunkennessLevel)
				{
					RevealBusinessSecret();
				}
			}
		}
		
		// Elite-specific drunk phrases
		private void SayRandomEliteDrunkPhrase()
		{
			string[] eliteDrunkPhrases = {
				"Bu yılki bütçemiz... *hık*... milyonlarca...",
				"Bakanlıktan o dosyayı alabilirim... *hık*",
				"Belediye başkanına bir telefon, yeter... *hık*",
				"İhaleyi... *hık*... garantiledim bile...",
				"Parayı verin... *hık*... projeyi alın...",
				"Ankara'nın yarısı benim... *hık*... elimde...",
				"O araziyi sen merak etme... *hık*... halledeceğim...",
				"Bir imza... *hık*... bir imza yeter..."
			};
			
			int index = GD.RandRange(0, eliteDrunkPhrases.Length - 1);
			Say(eliteDrunkPhrases[index], 3.0f);
		}
		
		// Attempt a flashy, big purchase when drunk
		private void AttemptBigPurchase()
		{
			// Big purchase options
			string[] bigPurchases = {
				"Bir şişe Dom Perignon!",
				"Masadaki herkese içki!",
				"En pahalı viskiden getir!",
				"Tüm masalara benden içki!"
			};
			
			int index = GD.RandRange(0, bigPurchases.Length - 1);
			Say(bigPurchases[index]);
			
			// Calculate a big purchase amount (10-30% of remaining budget)
			float purchaseAmount = _remainingBudget * (0.1f + GD.Randf() * 0.2f);
			
			// Spend the money
			if (SpendMoney(purchaseAmount, "big_purchase"))
			{
				// Successful purchase
				AdjustSatisfaction(0.1f, "Big spending");
				
				// Notify event system
				NotifyBigPurchase(purchaseAmount);
			}
		}
		
		// Notify system about big purchase
		private void NotifyBigPurchase(float amount)
		{
			// In real implementation, this would signal the event system
			// for possible reactions from other customers and staff
			
			// Log for now
			GD.Print($"Elite customer {FullName} made a big purchase of {amount}");
		}
		
		// Revealing business secrets when drunk
		private void RevealBusinessSecret()
		{
			string[] secrets = {
				"Belediyedeki o ihale... Biliyor musun...",
				"Bakanlıkta tanıdıklarım var. İstersen...",
				"O arazi meselesi... Birkaç telefon yeter...",
				"Ticaret Odasında kim kimi tanıyor, bilirim...",
				"Maltepe'de yeni yapılaşma bölgesi açılacak yakında..."
			};
			
			int index = GD.RandRange(0, secrets.Length - 1);
			Say(secrets[index], 4.0f);
			
			// This could trigger game events through the EventManager
			if (GetTree().Root.HasNode("GameManager/EventManager"))
			{
				var eventManager = GetTree().Root.GetNode("GameManager/EventManager");
				
				if (eventManager.HasMethod("TriggerEliteSecret"))
				{
					// Use a random business contact as the topic
					string topic = "General";
					if (_businessContacts.Count > 0)
					{
						List<string> contacts = new List<string>(_businessContacts.Keys);
						topic = contacts[GD.RandRange(0, contacts.Count - 1)];
					}
					
					eventManager.Call("TriggerEliteSecret", CustomerId, topic, _drunkennessLevel);
				}
			}
		}
		
		// Override payment processing to match elite behavior
		protected override void ProcessPayment()
		{
			// Higher tip percentage for elite customers
			float tipPercentage = _generosity * 0.3f; // Up to 30% tip vs 20% for regular
			tipPercentage += (_satisfaction - 0.5f) * 0.15f; // Satisfaction impact
			
			// Status consciousness affects tipping behavior
			tipPercentage += _statusConsciousness * 0.1f;
			
			// Conspicuous spending affects tip in public settings
			bool isObserved = HasNearbyObservers();
			if (isObserved)
			{
				tipPercentage += _conspicuousSpending * 0.1f;
			}
			
			float tipAmount = _totalSpent * tipPercentage;
			
			// Handle tip distribution
			if (_remainingBudget >= tipAmount)
			{
				// Prioritize assigned kons for tips
				if (_assignedKonsId != null)
				{
					GiveTip(tipAmount * 0.8f, _assignedKonsId); // 80% to primary kons
					
					// Distribute remaining 20% to other staff if they interacted
					if (_interactedStaffIds.Count > 1)
					{
						float remainingTip = tipAmount * 0.2f;
						float tipPerStaff = remainingTip / (_interactedStaffIds.Count - 1);
						
						foreach (string staffId in _interactedStaffIds)
						{
							if (staffId != _assignedKonsId)
							{
								GiveTip(tipPerStaff, staffId);
							}
						}
					}
				}
				else if (_interactedStaffIds.Count > 0)
				{
					// Distribute tips among all interacted staff
					float tipPerStaff = tipAmount / _interactedStaffIds.Count;
					
					foreach (string staffId in _interactedStaffIds)
					{
						GiveTip(tipPerStaff, staffId);
					}
				}
			}
			
			// Final impression based on experience
			LeaveBusinessCard();
			
			GD.Print($"Elite customer {FullName} paid total: {_totalSpent}, tip: {tipAmount} ({tipPercentage*100}%)");
		}
		
		// Determine if there are observers nearby
		private bool HasNearbyObservers()
		{
			// In a real implementation, this would check for nearby customers
			// For now, return a random value with 70% chance of true
			return GD.Randf() < 0.7f;
		}
		
		// Leave business card for future connections
		private void LeaveBusinessCard()
		{
			// Only leave card if somewhat satisfied
			if (_satisfaction > 0.6f)
			{
				// In real implementation, this would register with the CustomerManager
				// for potential future business connections
				
				// For now, log it
				GD.Print($"Elite customer {FullName} left a business card for future connections");
				
				// Notify reputation system
				if (GetTree().Root.HasNode("GameManager/ReputationManager"))
				{
					var repManager = GetTree().Root.GetNode("GameManager/ReputationManager");
					
					if (repManager.HasMethod("RecordEliteConnection"))
					{
						// Connection strength based on satisfaction and business influence
						float connectionStrength = _satisfaction * _businessInfluence;
						repManager.Call("RecordEliteConnection", CustomerId, FullName, connectionStrength);
					}
				}
			}
		}
		
		// Override to add elite-specific stats
		public new Dictionary<string, object> GetStats()
		{
			// Get base stats
			Dictionary<string, object> stats = base.GetStats();
			
			// Add elite-specific attributes
			stats["StatusConsciousness"] = _statusConsciousness;
			stats["BusinessInfluence"] = _businessInfluence;
			stats["HasSpecialTable"] = _hasSpecialTable;
			stats["EntourageSize"] = _entourageSize;
			stats["ConspicuousSpending"] = _conspicuousSpending;
			stats["ConnectionLevel"] = _connectionLevel;
			stats["EntryStyle"] = _entryStyle;
			stats["ServiceExpectation"] = _serviceExpectation;
			
			// Elite preferences
			Dictionary<string, float> elitePrefs = new Dictionary<string, float>();
			foreach (var pref in _elitePreferences)
			{
				elitePrefs[pref.Key] = pref.Value;
			}
			stats["ElitePreferences"] = elitePrefs;
			
			// Special requests
			stats["SpecialRequests"] = _specialRequests;
			
			// Business contacts
			Dictionary<string, float> businessContacts = new Dictionary<string, float>();
			foreach (var contact in _businessContacts)
			{
				businessContacts[contact.Key] = contact.Value;
			}
			stats["BusinessContacts"] = businessContacts;
			
			// Insult history
			stats["RecentlyInsulted"] = _recentlyInsulted;
			stats["InsultSource"] = _insultSource;
			stats["InsultSeverity"] = _insultSeverity;
			
			return stats;
		}
	}
}
