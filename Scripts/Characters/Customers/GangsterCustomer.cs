using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	public partial class GangsterCustomer : CustomerBase
	{
		// Gangster-specific properties
		private float _respectLevel = 0.5f;        // How much respect they feel they're getting (0-1)
		private float _territorialFactor = 0.7f;   // How territorial they are about "their" table/spot
		private float _intimidationLevel = 0.6f;   // How intimidating they appear to others
		private bool _hasCrew = false;             // Whether they came with a crew
		private int _crewSize = 0;                 // Size of their crew (0 if came alone)
		private float _violenceProbability = 0.3f; // Baseline probability of violent behavior
		
		// Criminal activity tracking
		private bool _involvedInIllegalFloor = false; // Whether they participate in illegal floor activities
		private float _illegalInterest = 0.0f;        // Interest in illegal activities (0-1)
		private bool _hasWeapon = false;              // Whether they're carrying a weapon
		
		// Social dynamics
		private List<string> _rivalGangIds = new List<string>();    // IDs of rival gangs/gangsters
		private Dictionary<string, float> _staffRespect = new Dictionary<string, float>(); // Respect level for each staff member
		
		// Ankara-specific gangster traits
		private bool _isLocalCabadayi = false;     // Whether they're a local "kabadayı" (neighborhood tough guy)
		private string _neighborhood = "";         // Their neighborhood in Ankara
		private bool _hasRapSheet = false;         // Whether they have a police record
		
		// Specific gangster preferences
		private string _preferredSeatingArea = "corner"; // Where they prefer to sit (corner, near stage, etc.)
		private string _gangsterSignature = "";          // Their signature move or phrase
		
		// Timers for threat assessment
		private float _threatAssessmentTimer = 0.0f;
		private float _threatAssessmentInterval = 60.0f; // Assess threats every minute
		
		// Override constructor
		public GangsterCustomer() : base()
		{
			// Set gangster-specific initialization
			InitializeGangsterTraits();
		}
		
		// Custom constructor
		public GangsterCustomer(string fullName, int age, string gender, float budget) 
			: base(fullName, age, gender, CustomerType.Gangster, budget)
		{
			InitializeGangsterTraits();
		}
		
		// Initialize gangster-specific traits
		private void InitializeGangsterTraits()
		{
			// Determine if this gangster has a crew
			_hasCrew = GD.Randf() < 0.4f; // 40% chance to have a crew
			
			if (_hasCrew)
			{
				_crewSize = GD.RandRange(1, 3); // 1-3 crew members
				_intimidationLevel += 0.2f * _crewSize; // More crew = more intimidating
				_budget *= (1.0f + (_crewSize * 0.3f)); // More crew = more collective budget
			}
			
			// Determine if they're carrying a weapon
			_hasWeapon = GD.Randf() < 0.3f; // 30% chance
			if (_hasWeapon)
			{
				_intimidationLevel += 0.15f;
				_violenceProbability += 0.1f;
			}
			
			// Set neighborhood loyalty (Ankara districts with rough reputations)
			string[] ankaraDistricts = {
				"Çinçin", "Altındağ", "Hüseyingazi", "Keçiören", "Demetevler", 
				"Şentepe", "Mamak", "Maltepe", "Ulus", "Bentderesi"
			};
			
			int districtIdx = GD.RandRange(0, ankaraDistricts.Length - 1);
			_neighborhood = ankaraDistricts[districtIdx];
			
			// Determine if they're a local "kabadayı"
			_isLocalCabadayi = GD.Randf() < 0.25f; // 25% chance
			if (_isLocalCabadayi)
			{
				_respectLevel = 0.7f; // Expects more respect
				_territorialFactor = 0.8f;
				_gangsterSignature = "Mahalle Kabadayısı"; 
			}
			
			// Has a police record?
			_hasRapSheet = GD.Randf() < 0.6f; // 60% chance
			
			// Interest in illegal activities depends on various factors
			_illegalInterest = 0.4f + (GD.Randf() * 0.4f); // Base 0.4-0.8
			if (_hasRapSheet)
				_illegalInterest += 0.1f;
				
			if (_isLocalCabadayi)
				_illegalInterest += 0.1f;
				
			_illegalInterest = Mathf.Clamp(_illegalInterest, 0.0f, 1.0f);
			
			// Generate signature line/move typical for Ankara gangsters
			string[] signatures = {
				"Angaralıyık ulan!",
				"Çakallar korkak gezer bizim mahallede...",
				"Bize her yer Angara!",
				"Babalar buraya, çocuklar kenara...",
				"Maltepe'de beni tanımayan yoktur...",
				"Bak kardeşim, ben herkesle iyi geçinirim ama...",
				"Saygıyı da severim, saygısızlığı da bilirim...",
				"Bu alemde ya adam gibi yaşarsın, ya da..."
			};
			
			if (string.IsNullOrEmpty(_gangsterSignature))
			{
				int sigIdx = GD.RandRange(0, signatures.Length - 1);
				_gangsterSignature = signatures[sigIdx];
			}
			
			// Override appearance signature
			_signature = "Altın zincir, kabadayı duruşu, deri ceket";
		}
		
		public override void _Ready()
		{
			base._Ready();
			
			// Add additional setup if needed
		}
		
		protected override void OnReady()
		{
			base.OnReady();
			
			// Override preference values for gangsters
			_preferences["music_arabesk"] = 0.8f;      // Love arabesk
			_preferences["music_taverna"] = 0.7f;      // Enjoy taverna
			_preferences["drink_raki"] = 0.9f;         // Prefer rakı strongly
			_preferences["drink_whiskey"] = 0.6f;      // Like whiskey too
			_preferences["staff_kons"] = 0.8f;         // Enjoy attention from kons
			_preferences["ambiance_intimate"] = 0.7f;  // Prefer intimate settings
			
			// Lower patience for bad service
			_preferences["wait_tolerance"] = 0.3f;
			
			// Adjust base traits from parent class
			_kargaLevel = 0.05f;      // Less likely to run without paying - "honor"
			_aggressionLevel = 0.6f;  // More aggressive baseline
			_loyaltyLevel = 0.7f;     // More loyal to "their" places
			_generosity = 0.6f;       // Can be generous with money when respected
		}
		
		public override void _Process(double delta)
		{
			base._Process(delta);
			
			// Update threat assessment timer
			_threatAssessmentTimer += (float)delta;
			if (_threatAssessmentTimer >= _threatAssessmentInterval)
			{
				_threatAssessmentTimer = 0;
				AssessThreats();
			}
			
			// Check for territorial behavior
			if (_currentState == CustomerState.Sitting && GD.Randf() < 0.005f * _territorialFactor)
			{
				DefendTerritory();
			}
		}
		
		// Override state update functions for gangster-specific behavior
		protected override void UpdateSittingState(float delta)
		{
			base.UpdateSittingState(delta);
			
			// Gangsters check for respect periodically
			if (GD.Randf() < 0.007f)
			{
				AssessRespect();
			}
			
			// Check for illegal floor interest
			if (!_involvedInIllegalFloor && GD.Randf() < 0.002f * _illegalInterest)
			{
				InquireAboutIllegalActivities();
			}
		}
		
		// Handle threats in environment
		private void AssessThreats()
		{
			// Look for rival gangsters
			var customerManager = GetNode<Node>("/root/GameManager/CustomerManager");
			if (customerManager != null && customerManager.HasMethod("GetNearbyCustomers"))
			{
				try
				{
					var nearbyCustomers = (Godot.Collections.Array)customerManager.Call("GetNearbyCustomers", this.GlobalPosition, 5.0f);
					
					foreach (var customer in nearbyCustomers)
					{
						// Check if it's another gangster and not from crew
						if (customer is CustomerBase cust && 
							cust.GetCustomerTypeName() == "Gangster" && 
							cust != this)
						{
							if (!IsFromCrew(cust) && IsRival(cust))
							{
								HandleRivalPresence(cust);
							}
						}
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error assessing threats: {e.Message}");
				}
			}
		}
		
		// Determine if a customer is from this gangster's crew
		private bool IsFromCrew(CustomerBase customer)
		{
			// In a real implementation, crew members would be linked
			return false;
		}
		
		// Determine if a customer is a rival
		private bool IsRival(CustomerBase customer)
		{
			// Check if this is a known rival
			if (_rivalGangIds.Contains(customer.CustomerId))
				return true;
				
			// Small random chance to consider someone a rival
			if (GD.Randf() < 0.2f)
			{
				_rivalGangIds.Add(customer.CustomerId);
				return true;
			}
			
			return false;
		}
		
		// Handle rival gangster presence
		private void HandleRivalPresence(CustomerBase rival)
		{
			float confrontationChance = _aggressionLevel * 0.3f;
			
			// Adjust based on drunkenness
			confrontationChance += _drunkennessLevel * 0.2f;
			
			// Check if we're going to start trouble
			if (GD.Randf() < confrontationChance)
			{
				if (GD.Randf() < _violenceProbability)
				{
					// Start a fight
					StartFight(rival);
				}
				else
				{
					// Just verbal confrontation
					VerbalConfrontation(rival);
				}
			}
			else
			{
				// Tense but peaceful coexistence
				// Just show some animation of tension
				PlayAnimation("tense_look");
				Say("...", 1.0f);
			}
		}
		
		// Start a fight with another customer
		private void StartFight(CustomerBase target)
		{
			// Change state to special event (fighting)
			ChangeState(CustomerState.SpecialEvent);
			
			// Trigger fight event in the customer manager
			var customerManager = GetNode<Node>("/root/GameManager/CustomerManager");
			if (customerManager != null && customerManager.HasMethod("TriggerFightEvent"))
			{
				customerManager.Call("TriggerFightEvent", this.CustomerId, target.CustomerId, GlobalPosition);
			}
			
			// Direct animation and dialogue
			PlayAnimation("aggressive");
			Say(_gangsterSignature, 3.0f);
			
			// Notify security
			var securityManager = GetNode<Node>("/root/GameManager/SecurityManager");
			if (securityManager != null && securityManager.HasMethod("ReportFight"))
			{
				securityManager.Call("ReportFight", GlobalPosition, "GangsterFight");
			}
		}
		
		// Verbal confrontation with rival
		private void VerbalConfrontation(CustomerBase target)
		{
			PlayAnimation("threatening");
			
			string[] threats = {
				$"Buranın ağası benim {_neighborhood}'da beni bilmeyen yok!",
				"Bana bak yeğenim, uslu uslu otur şurada, dalga geçme benimle...",
				"Sen kimsin la? Bu mahallede beni tanımayan yok!",
				"Bir hareketini göreyim, alayınızı paketlerim!",
				"Şu lafını duymamış gibi yapıyorum, bir daha olmasın..."
			};
			
			int threatIdx = GD.RandRange(0, threats.Length - 1);
			Say(threats[threatIdx], 3.0f);
			
			// Trigger a mild disturbance
			var eventManager = GetNode<Node>("/root/GameManager/EventManager");
			if (eventManager != null && eventManager.HasMethod("TriggerMinorDisturbance"))
			{
				eventManager.Call("TriggerMinorDisturbance", GlobalPosition, "GangsterThreat");
			}
		}
		
		// Defend "their" territory/table
		private void DefendTerritory()
		{
			var customerManager = GetNode<Node>("/root/GameManager/CustomerManager");
			if (customerManager != null && customerManager.HasMethod("GetCustomersNearTable"))
			{
				try
				{
					var nearTable = (Godot.Collections.Array)customerManager.Call("GetCustomersNearTable", _tablePosition, 3.0f);
					
					foreach (var customer in nearTable)
					{
						if (customer is CustomerBase cust && cust != this && !IsFromCrew(cust))
						{
							// Someone's approaching "their" table
							PlayAnimation("territorial");
							
							string[] territorialPhrases = {
								"Burası benim yerim, başka yere...",
								"Uzaklaş masadan yeğenim, bu bizim köşemiz...",
								"Bu masa dolu kardeşim, görüyorsun...",
								"Biraz uzak dur masadan aslanım..."
							};
							
							int phraseIdx = GD.RandRange(0, territorialPhrases.Length - 1);
							Say(territorialPhrases[phraseIdx], 3.0f);
							break;
						}
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error in territorial behavior: {e.Message}");
				}
			}
		}
		
		// Assess whether they're getting enough respect
		private void AssessRespect()
		{
			float respectThreshold = 0.4f + (_respectLevel * 0.3f);
			
			// If they feel disrespected
			if (_satisfaction < respectThreshold)
			{
				// Decrease tip probability
				_generosity -= 0.1f;
				_generosity = Mathf.Max(_generosity, 0.1f);
				
				// Increase aggression
				_aggressionLevel += 0.1f;
				_aggressionLevel = Mathf.Min(_aggressionLevel, 1.0f);
				
				// Express dissatisfaction
				PlayAnimation("annoyed");
				
				string[] complaints = {
					"Bu ne rezalet ya! Saygı kalmamış...",
					"Bu pavyonun eski kalitesi nerede?",
					"Adam yerine koymuyorlar artık...",
					"Bir daha mı geleceğiz sanıyorlar?"
				};
				
				int complaintIdx = GD.RandRange(0, complaints.Length - 1);
				Say(complaints[complaintIdx], 3.0f);
				
				// If very disrespected, consider leaving
				if (_satisfaction < 0.2f)
				{
					// 50% chance to leave when very disrespected
					if (GD.Randf() < 0.5f)
					{
						// Slam some money on the table (pay but leave angry)
						PlayAnimation("angry_pay");
						Say("Al paranı! Bir daha gelmem buraya...", 3.0f);
						PrepareToLeave();
					}
				}
			}
			else
			{
				// If they feel respected, increase generosity
				_generosity += 0.05f;
				_generosity = Mathf.Min(_generosity, 0.9f);
				
				// And decrease aggression
				_aggressionLevel -= 0.05f;
				_aggressionLevel = Mathf.Max(_aggressionLevel, 0.4f); // Never below baseline
			}
		}
		
		// Check for illegal activities
		private void InquireAboutIllegalActivities()
		{
			// Try to find a staff member to ask about illegal floor
			var staffManager = GetNode<Node>("/root/GameManager/StaffManager");
			if (staffManager != null)
			{
				// Find closest security or manager
				Node3D nearestStaff = null;
				
				if (staffManager.HasMethod("GetNearestStaffOfType"))
				{
					try
					{
						nearestStaff = (Node3D)staffManager.Call("GetNearestStaffOfType", GlobalPosition, "Security");
						
						if (nearestStaff == null)
						{
							// Try with manager if no security
							nearestStaff = (Node3D)staffManager.Call("GetNearestStaffOfType", GlobalPosition, "Manager");
						}
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error finding staff for illegal inquiry: {e.Message}");
					}
				}
				
				if (nearestStaff != null)
				{
					// Approach the staff and ask about illegal activities
					MoveTo(nearestStaff.GlobalPosition);
					
					// When movement completes
					Timer timer = new Timer();
					timer.WaitTime = 3.0f;
					timer.OneShot = true;
					AddChild(timer);
					
					timer.Timeout += () => {
						if (Vector3.Distance(GlobalPosition, nearestStaff.GlobalPosition) < 2.0f)
						{
							// Whisper about illegal floor
							PlayAnimation("whisper");
							Say("Arkadaki özel oda açık mı bu akşam?", 2.0f);
							
							// The staff's response would be handled elsewhere
							// For now, just mark that the inquiry was made
							_involvedInIllegalFloor = true;
							
							// Return to table after asking
							MoveTo(_tablePosition);
						}
					};
					
					timer.Start();
				}
			}
		}
		
		// Override drink choice to reflect gangster preferences
		protected override string GetPreferredDrinkType()
		{
			// Gangsters strongly prefer rakı, especially when showing off
			if (_hasCrew || GD.Randf() < 0.7f)
			{
				return "Rakı";
			}
			
			// Otherwise use parent method for variety
			return base.GetPreferredDrinkType();
		}
		
		// Override response to receiving a drink
		public override void ReceiveDrink(string drinkType, float quality)
		{
			base.ReceiveDrink(drinkType, quality);
			
			// Extra gangster-specific reactions
			if (drinkType == "Rakı")
			{
				string[] rakiPhrases = {
					"Rakı adamın kanında olmalı!",
					"Şerefe aslanım!",
					"Bu masada rakı bitmez...",
					"Hadi hep birlikte, şerefinize!",
					"Rakı sofrasına ne yakışır? Yiğit adam yakışır!"
				};
				
				int phraseIdx = GD.RandRange(0, rakiPhrases.Length - 1);
				Say(rakiPhrases[phraseIdx], 3.0f);
				
				// Show off with the crew if present
				if (_hasCrew)
				{
					PlayAnimation("toast_crew");
				}
			}
			else if (drinkType == "Viski" || drinkType == "Whiskey")
			{
				// Gangsters showing off with whiskey
				string[] whiskyPhrases = {
					"Yabancı içkiler bizim mahalleye de geldi artık...",
					"Jack Daniel's getir, az koy!",
					"İçki dediğin böyle olur...",
					"Rakı güzel de, viski başka be..."
				};
				
				int phraseIdx = GD.RandRange(0, whiskyPhrases.Length - 1);
				Say(whiskyPhrases[phraseIdx], 3.0f);
			}
		}
		
		// Override behavior when talking to a kons
		protected override void UpdateTalkingToKonsState(float delta)
		{
			// First run the base implementation
			base.UpdateTalkingToKonsState(delta);
			
			// Gangster-specific behavior with kons
			if (GD.Randf() < 0.01f) // 1% chance per frame when in this state
			{
				PlayAnimation("show_off");
				
				string[] konsLines = {
					"Güzelim, bizim mahallede herkes beni tanır...",
					"Sana istediğin her şeyi alabilirim...",
					"Sen böyle güzel kızlarla çalışmak için fazla iyisin...",
					"Bir ara benim mekana da gel...",
					"Kalbim sadece sana çalışıyor bu gece..."
				};
				
				int lineIdx = GD.RandRange(0, konsLines.Length - 1);
				Say(konsLines[lineIdx], 3.0f);
				
				// If in a good mood and well-treated, might give extra tip
				if (_satisfaction > 0.7f && GD.Randf() < _generosity * 0.5f)
				{
					GiveTip(_budget * 0.05f, _assignedKonsId); // Give 5% of budget as extra tip
					
					// Make it rain gesture
					PlayAnimation("throw_money");
					Say("Al koçum, iyi çalış!", 2.0f);
				}
			}
		}
		
		// Override payment behavior
		protected override void ProcessPayment()
		{
			// If satisfied, can be very generous
			if (_satisfaction > 0.8f && _respectLevel > 0.7f)
			{
				// Big tipper
				float tipPercentage = _generosity * 0.3f; // Up to 30% tip
				float tipAmount = _totalSpent * tipPercentage;
				
				PlayAnimation("big_tipper");
				Say("Hesabı alayım... Para önemli değil, yeter ki saygı olsun!", 3.0f);
				
				// Process the standard tip logic with our enhanced amount
				if (_remainingBudget >= tipAmount)
				{
					if (_assignedKonsId != null)
					{
						GiveTip(tipAmount, _assignedKonsId);
					}
					else if (_interactedStaffIds.Count > 0)
					{
						int randomIndex = (int)(GD.Randf() * _interactedStaffIds.Count);
						GiveTip(tipAmount, _interactedStaffIds[randomIndex]);
					}
				}
			}
			else if (_satisfaction < 0.3f)
			{
				// Angry payment
				PlayAnimation("angry_pay");
				Say("Al şunu! Saygı kalmamış...", 3.0f);
				
				// No tip when angry
			}
			else
			{
				// Standard payment processing
				base.ProcessPayment();
			}
		}
		
		// Respond to being disrespected by staff
		public void RespondToDisrespect(string staffId, float severity)
		{
			// Track staff respect
			if (!_staffRespect.ContainsKey(staffId))
				_staffRespect[staffId] = 0.5f; // Start neutral
				
			_staffRespect[staffId] -= severity;
			
			// Overall respect and satisfaction impact
			_respectLevel -= severity * 0.5f;
			_respectLevel = Mathf.Max(_respectLevel, 0.0f);
			
			AdjustSatisfaction(-severity, "Disrespect");
			
			// Reaction depends on severity
			if (severity > 0.5f) // Major disrespect
			{
				PlayAnimation("very_angry");
				
				string[] majorResponses = {
					"Sen benim kim olduğumu biliyor musun?!",
					"Bu son uyarım, sonra fena olur!",
					"Babalarınızı ararsınız şimdi...",
					"Konuşmasını bilmiyorsan, dinlemesini öğretiriz!",
					$"Beni {_neighborhood}'da bi sor bakalım, nasıl saygısızlık yaparsın?"
				};
				
				int responseIdx = GD.RandRange(0, majorResponses.Length - 1);
				Say(majorResponses[responseIdx], 4.0f);
				
				// Major disrespect could trigger confrontation
				if (GD.Randf() < _aggressionLevel)
				{
					// Start confrontation with staff
					ChangeState(CustomerState.SpecialEvent);
					
					// Trigger confrontation event
					var eventManager = GetNode<Node>("/root/GameManager/EventManager");
					if (eventManager != null && eventManager.HasMethod("TriggerStaffConfrontation"))
					{
						eventManager.Call("TriggerStaffConfrontation", CustomerId, staffId, "Disrespect");
					}
				}
			}
			else // Minor disrespect
			{
				PlayAnimation("annoyed");
				
				string[] minorResponses = {
					"Bak kardeşim, adam ol...",
					"Şimdi gülüp geçiyorum ama...",
					"Saygıda kusur etme bence...",
					"Sen galiba beni tanımıyorsun..."
				};
				
				int responseIdx = GD.RandRange(0, minorResponses.Length - 1);
				Say(minorResponses[responseIdx], 3.0f);
			}
		}
		
		// React when security tries to eject
		public override void ForceEject(string reason)
		{
			// Resistance chance based on aggressiveness and drunkenness
			float resistChance = _aggressionLevel * 0.6f + _drunkennessLevel * 0.3f;
			
			if (GD.Randf() < resistChance)
			{
				// Resist ejection
				PlayAnimation("resist_security");
				
				string[] resistPhrases = {
					"Elini sürme ulan! Ben buradan çıkmıyorum!",
					"Sen kim oluyorsun da beni çıkarıyorsun?",
					"Bir adım daha atarsan görürsün!",
					"Bak başını belaya sokma, ben gitmiyorum!",
					$"{_neighborhood} çocukları böyle muamele görmez!"
				};
				
				int phraseIdx = GD.RandRange(0, resistPhrases.Length - 1);
				Say(resistPhrases[phraseIdx], 3.0f);
				
				// Trigger security incident
				var securityManager = GetNode<Node>("/root/GameManager/SecurityManager");
				if (securityManager != null && securityManager.HasMethod("HandleEjectionResistance"))
				{
					securityManager.Call("HandleEjectionResistance", CustomerId, GlobalPosition, _hasWeapon);
				}
			}
			else
			{
				// Comply, but with an attitude
				PlayAnimation("leave_angry");
				
				string[] leavingPhrases = {
					"Tamam gidiyorum, ama bu işin peşini bırakmam!",
					"Sen daha beni göreceksin...",
					"Bu muameleyi unutmayacağım...",
					"Bir daha kapından geçersem namerdim!",
					"Son gülen iyi güler, bunu unutma..."
				};
				
				int phraseIdx = GD.RandRange(0, leavingPhrases.Length - 1);
				Say(leavingPhrases[phraseIdx], 3.0f);
				
				// Call the base method to actually leave
				base.ForceEject(reason);
			}
		}
		
		// Get additional statistics for this customer type
		public new Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = base.GetStats();
			
			// Add gangster-specific stats
			stats["RespectLevel"] = _respectLevel;
			stats["TerritorialFactor"] = _territorialFactor;
			stats["IntimidationLevel"] = _intimidationLevel;
			stats["HasCrew"] = _hasCrew;
			stats["CrewSize"] = _crewSize;
			stats["ViolenceProbability"] = _violenceProbability;
			stats["HasWeapon"] = _hasWeapon;
			stats["IsLocalCabadayi"] = _isLocalCabadayi;
			stats["Neighborhood"] = _neighborhood;
			stats["GangsterSignature"] = _gangsterSignature;
			
			return stats;
		}
	}
}
