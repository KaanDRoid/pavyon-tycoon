// Scripts/Characters/Customers/UnderCoverCustomer.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	/// <summary>
	/// UnderCoverCustomer represents a plainclothes police officer or other undercover official
	/// watching the pavyon activities. They're cautious, vigilant, and gather information.
	/// </summary>
	public partial class UnderCoverCustomer : CustomerBase
	{
		// UnderCover specific properties
		private float _suspicionLevel = 0.0f;       // How suspicious they are of illegal activities (0-1)
		private float _evidenceGathered = 0.0f;     // How much evidence they've collected (0-1)
		private float _coverStrength = 0.8f;        // How well they maintain their cover (0-1)
		private string _department = "Police";      // Which department they're from (Police, MIT, Tax Office, etc.)
		private float _bribeResistance = 0.7f;      // Resistance to bribes (0-1)
		private bool _isReporting = false;          // Whether they are currently reporting findings

		// Investigation targets and priorities
		private Dictionary<string, float> _investigationPriorities = new Dictionary<string, float>();
		
		// Cover identity specifics
		private string _coverIdentity;              // Their fake identity name/role
		private float _identityRevealChance = 0.0f; // Chance of accidentally revealing true identity
		
		// Observation timer
		private float _observationTimer = 0.0f;
		private const float OBSERVATION_INTERVAL = 30.0f; // Observe every 30 seconds

		// Report timing
		private float _timeBetweenReports = 600.0f; // 10 minutes between reports
		private float _reportTimer = 0.0f;

		// Signals
		[Signal]
		public delegate void SuspicionIncreasedEventHandler(float newLevel, string reason);
		
		[Signal]
		public delegate void EvidenceGatheredEventHandler(float amount, string source);
		
		[Signal]
		public delegate void CoverCompromisedEventHandler(float severity);

		public override void _Ready()
		{
			base._Ready();
			
			// Set specialized cover identity traits
			GenerateCoverIdentity();
			
			// Initialize investigation priorities
			InitializeInvestigationPriorities();
			
			// Set default department with chance to be from other agencies
			DetermineDepartment();
			
			// Undercover customers never drink too much - it would compromise their cover
			_drunkennessLevel = 0.0f;
			
			// Higher resistance to bribes based on department
			if (_department == "MIT" || _department == "Special Forces")
				_bribeResistance = 0.9f;

			GD.Print($"UnderCover customer {FullName} initialized. Cover: {_coverIdentity}, Department: {_department}");
		}

		public override void _Process(double delta)
		{
			base._Process(delta);
			
			// Handle specialized undercover behavior
			if (_currentState != CustomerState.Leaving && _currentState != CustomerState.SpecialEvent)
			{
				// Regularly observe surroundings
				UpdateObservation((float)delta);
				
				// Regularly send reports
				UpdateReporting((float)delta);
				
				// Check for cover compromise
				CheckCoverStatus((float)delta);
			}
		}

		// Generate a believable cover identity
		private void GenerateCoverIdentity()
		{
			string[] coverRoles = {
				"İşadamı", "Muhasebeci", "Emlakçı", "Mühendis", 
				"Tüccar", "Emekli Memur", "Esnaf", "İşletmeci",
				"Serbest Meslek", "Müteahhit", "İthalatçı", "Komisyoncu"
			};
			
			int roleIndex = (int)(GD.Randf() * coverRoles.Length);
			_coverIdentity = coverRoles[roleIndex];
			
			// Set cover strength and identity reveal chance based on experience
			// Younger agents have weaker covers
			if (Age < 35)
			{
				_coverStrength = 0.5f + GD.Randf() * 0.3f; // 0.5-0.8
				_identityRevealChance = 0.02f;
			}
			else
			{
				_coverStrength = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0
				_identityRevealChance = 0.01f;
			}
			
			// Update signature to match cover identity
			_signature = DetermineSignatureBasedOnCover();
		}

		// Determine signature appearance based on cover
		private string DetermineSignatureBasedOnCover()
		{
			switch (_coverIdentity)
			{
				case "İşadamı":
					return "Gündelik takım elbise, ucuz saat, hafif gergin duruş";
				case "Muhasebeci":
					return "Deri çanta, düzenli saç, gözlük, hesap makinesi";
				case "Emlakçı":
					return "Parlak ayakkabılar, cep telefonu, fazla parfüm";
				case "Mühendis":
					return "Sade gömlek, kot pantolon, sakin davranışlar";
				case "Tüccar":
					return "Altın yüzük, çekici kıyafet, telefon görüşmeleri";
				case "Emekli Memur":
					return "Takım içine kazak, gazete, düzenli taranmış saç";
				case "Esnaf":
					return "Gündelik kıyafet, güçlü el sıkışma, yerel ağız";
				case "İşletmeci":
					return "Spor ceket, rahat kıyafet, dikkatli bakışlar";
				default:
					return "Standart kıyafet, dikkatli gözlemci, az konuşkan";
			}
		}

		// Initialize investigation priorities
		private void InitializeInvestigationPriorities()
		{
			_investigationPriorities["illegal_floor"] = 0.8f;  // Kaçak kat
			_investigationPriorities["drug_deals"] = 0.7f;     // Uyuşturucu ticareti
			_investigationPriorities["gambling"] = 0.6f;       // Kumar
			_investigationPriorities["tax_evasion"] = 0.5f;    // Vergi kaçakçılığı
			_investigationPriorities["blackmail"] = 0.7f;      // Şantaj
			_investigationPriorities["bribery"] = 0.6f;        // Rüşvet
			_investigationPriorities["trafficking"] = 0.9f;    // İnsan kaçakçılığı
			
			// Adjust priorities based on department
			AdjustPrioritiesBasedOnDepartment();
		}

		// Determine department
		private void DetermineDepartment()
		{
			float random = GD.Randf();
			
			if (random < 0.6f)
			{
				_department = "Police";
			}
			else if (random < 0.8f)
			{
				_department = "Tax Office";
				_investigationPriorities["tax_evasion"] = 0.9f;
				_investigationPriorities["illegal_floor"] = 0.5f;
			}
			else if (random < 0.95f)
			{
				_department = "MIT"; // National Intelligence
				_bribeResistance = 0.9f;
				_coverStrength = Mathf.Min(1.0f, _coverStrength + 0.2f);
				_investigationPriorities["trafficking"] = 1.0f;
			}
			else
			{
				_department = "Special Forces";
				_bribeResistance = 0.95f;
				_coverStrength = Mathf.Min(1.0f, _coverStrength + 0.3f);
			}
		}

		// Adjust priorities based on department
		private void AdjustPrioritiesBasedOnDepartment()
		{
			switch (_department)
			{
				case "Police":
					// Standard priorities, already set
					break;
					
				case "Tax Office":
					_investigationPriorities["tax_evasion"] = 0.9f;
					_investigationPriorities["illegal_floor"] = 0.5f;
					_investigationPriorities["drug_deals"] = 0.3f;
					break;
					
				case "MIT":
					_investigationPriorities["trafficking"] = 1.0f;
					_investigationPriorities["blackmail"] = 0.9f;
					_investigationPriorities["bribery"] = 0.8f;
					break;
					
				case "Special Forces":
					_investigationPriorities["drug_deals"] = 0.9f;
					_investigationPriorities["trafficking"] = 0.9f;
					_investigationPriorities["illegal_floor"] = 0.9f;
					break;
			}
		}

		// Update observation mechanics
		private void UpdateObservation(float delta)
		{
			_observationTimer += delta;
			
			// Regular observation intervals
			if (_observationTimer >= OBSERVATION_INTERVAL)
			{
				_observationTimer = 0;
				
				// Observe surroundings and gather evidence
				ObserveSurroundings();
			}
		}

		// Update reporting mechanics
		private void UpdateReporting(float delta)
		{
			_reportTimer += delta;
			
			// Check if it's time to report findings
			if (_reportTimer >= _timeBetweenReports && _evidenceGathered > 0.3f)
			{
				_reportTimer = 0;
				
				// Roll for chance to report
				if (GD.Randf() < _evidenceGathered * 0.5f)
				{
					AttemptToReport();
				}
			}
		}

		// Check if cover might be compromised
		private void CheckCoverStatus(float delta)
		{
			// In higher risk situations, there's a chance of cover being compromised
			if (_drunkennessLevel > 0.3f) // More drunk = higher chance of slipping up
			{
				if (GD.Randf() < _identityRevealChance * (_drunkennessLevel * 2))
				{
					CoverCompromised();
				}
			}
			
			// When interacting with staff, especially Kons, there's a risk
			if (_currentState == CustomerState.TalkingToKons)
			{
				if (GD.Randf() < _identityRevealChance * 2)
				{
					CoverCompromised();
				}
			}
		}

		// Observe surroundings for suspicious activity
		private void ObserveSurroundings()
		{
			// Check for suspicious activities based on what's visible
			// In actual implementation, this would interact with the scene and other entities
			
			Dictionary<string, float> observableEvidence = GetObservableEvidence();
			
			foreach (var evidence in observableEvidence)
			{
				string activityType = evidence.Key;
				float visibilityLevel = evidence.Value;
				
				// Check if this activity matches investigation priorities
				if (_investigationPriorities.ContainsKey(activityType))
				{
					float priority = _investigationPriorities[activityType];
					float observationChance = priority * visibilityLevel;
					
					// Chance to observe based on awareness skill and priority
					if (GD.Randf() < observationChance * _awarenessSkill)
					{
						// Evidence found
						GatherEvidence(activityType, visibilityLevel * 0.2f);
					}
				}
			}
			
			// Pretend to enjoy the venue - decrease suspicion from others
			float randomEnjoymentDisplay = GD.Randf();
			if (randomEnjoymentDisplay < 0.3f)
			{
				PlayAnimation("enjoy_drink");
			}
			else if (randomEnjoymentDisplay < 0.6f)
			{
				PlayAnimation("look_around");
			}
		}

		// Get observable evidence in the environment
		private Dictionary<string, float> GetObservableEvidence()
		{
			Dictionary<string, float> visibleEvidence = new Dictionary<string, float>();
			
			// Check for illegal activities based on environment
			// This is a simplified version - in actual implementation would interact with scene
			
			// Check if illegal floor is active from BuildingManager
			if (GetTree().Root.HasNode("GameManager/BuildingManager"))
			{
				var buildingManager = GetTree().Root.GetNode("GameManager/BuildingManager");
				
				if (buildingManager.HasMethod("HasIllegalFloor") && buildingManager.HasMethod("IsIllegalFloorActive"))
				{
					bool hasIllegalFloor = (bool)buildingManager.Call("HasIllegalFloor");
					bool isIllegalFloorActive = (bool)buildingManager.Call("IsIllegalFloorActive");
					
					if (hasIllegalFloor && isIllegalFloorActive)
					{
						// Calculate visibility based on secrecy level
						float secrecyLevel = 0.5f; // Default medium secrecy
						
						if (buildingManager.HasMethod("GetIllegalFloorSecrecyLevel"))
						{
							secrecyLevel = (float)buildingManager.Call("GetIllegalFloorSecrecyLevel");
						}
						
						float visibilityLevel = 1.0f - secrecyLevel;
						visibleEvidence["illegal_floor"] = visibilityLevel;
					}
				}
			}
			
			// Check staff for suspicious behaviors
			if (GetTree().Root.HasNode("GameManager/StaffManager"))
			{
				var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
				
				// Look for illegal staff activities
				if (staffManager.HasMethod("GetStaffByType"))
				{
					var illegalStaff = staffManager.Call("GetStaffByType", "IllegalFloorStaff");
					
					if (illegalStaff != null && illegalStaff is Godot.Collections.Array array && array.Count > 0)
					{
						visibleEvidence["illegal_staff"] = 0.6f;
					}
				}
			}
			
			// Add some random observable evidence based on time spent
			if (_timeInPavyon > 60.0f && GD.Randf() < 0.3f) // After 1 hour
			{
				string[] possibleEvidence = { "drug_deals", "gambling", "bribery", "blackmail" };
				string randomEvidence = possibleEvidence[GD.RandRange(0, possibleEvidence.Length - 1)];
				
				visibleEvidence[randomEvidence] = 0.3f + GD.Randf() * 0.4f; // 0.3-0.7 visibility
			}
			
			return visibleEvidence;
		}

		// Gather evidence about suspicious activity
		private void GatherEvidence(string activityType, float amount)
		{
			// Increase evidence counter
			_evidenceGathered = Mathf.Min(1.0f, _evidenceGathered + amount);
			
			// Add to event history
			_eventHistory.Add($"Observed {activityType}");
			
			// Emit signal
			EmitSignal(SignalName.EvidenceGatheredEventHandler, amount, activityType);
			
			// Log evidence gathering
			GD.Print($"UnderCover {FullName} gathered evidence about {activityType}. Total evidence: {_evidenceGathered}");
			
			// Increase suspicion chance
			_suspicionLevel = Mathf.Min(1.0f, _suspicionLevel + (amount * 0.3f));
			
			// React subtly (animation)
			if (GD.Randf() < 0.5f)
			{
				PlayAnimation("look_concerned");
			}
		}

		// Try to report findings
		private void AttemptToReport()
		{
			// Check if in a safe location to report
			if (_currentState == CustomerState.UsingBathroom)
			{
				// Safe to report
				ReportFindings();
			}
			else if (_isMoving || _currentState == CustomerState.Leaving)
			{
				// Chance to report while moving or leaving
				if (GD.Randf() < 0.6f)
				{
					ReportFindings();
				}
			}
			else if (GD.Randf() < 0.2f) // Small chance to report in other states
			{
				ReportFindings();
			}
		}

		// Report findings to authorities
		private void ReportFindings()
		{
			_isReporting = true;
			
			// Animation - pretend to use phone or step aside
			PlayAnimation("check_phone");
			
			// Delay to simulate reporting
			Timer reportTimer = new Timer
			{
				WaitTime = 3.0f,
				OneShot = true
			};
			
			AddChild(reportTimer);
			reportTimer.Timeout += () => 
			{
				_isReporting = false;
				CompleteReport();
			};
			reportTimer.Start();
			
			GD.Print($"UnderCover {FullName} is reporting findings to {_department}");
		}

		// Complete the reporting process
		private void CompleteReport()
		{
			// Based on evidence level, potentially trigger consequences
			if (_evidenceGathered > 0.7f)
			{
				// Serious evidence could trigger a raid
				TriggerAuthoritiesResponse();
			}
			
			// Reset evidence counter partially
			_evidenceGathered *= 0.3f; // Keep 30% for continuity
			
			// Decide if it's time to leave
			if (GD.Randf() < _evidenceGathered + 0.3f)
			{
				PrepareToLeave();
			}
		}

		// Trigger authorities response based on evidence
		private void TriggerAuthoritiesResponse()
		{
			// This would interface with the game manager to trigger appropriate events
			if (GetTree().Root.HasNode("GameManager/EventManager"))
			{
				var eventManager = GetTree().Root.GetNode("GameManager/EventManager");
				
				// Determine response type based on department
				string responseType = "police_warning"; // Default
				
				if (_evidenceGathered > 0.9f)
				{
					responseType = $"{_department.ToLower()}_raid";
				}
				else if (_evidenceGathered > 0.7f)
				{
					responseType = $"{_department.ToLower()}_investigation";
				}
				
				// Trigger the event
				if (eventManager.HasMethod("TriggerEvent"))
				{
					eventManager.Call("TriggerEvent", responseType);
					GD.Print($"UnderCover {FullName} triggered {responseType} event");
				}
			}
		}

		// Cover is compromised
		private void CoverCompromised()
		{
			float severity = 0.3f + GD.Randf() * 0.4f; // 0.3-0.7 severity
			
			// Add to event history
			_eventHistory.Add("Cover compromised");
			
			// Emit signal
			EmitSignal(SignalName.CoverCompromisedEventHandler, severity);
			
			// Depending on severity, might need to leave immediately
			if (severity > 0.5f || _suspicionLevel > 0.7f)
			{
				GD.Print($"UnderCover {FullName}'s cover is seriously compromised, leaving immediately");
				PrepareToLeave();
			}
			else
			{
				// Try to recover
				GD.Print($"UnderCover {FullName}'s cover is slightly compromised, attempting to recover");
				AttemptToRecoverCover();
			}
		}

		// Try to recover compromised cover
		private void AttemptToRecoverCover()
		{
			// Play it cool
			PlayAnimation("act_casual");
			
			// Say something to deflect suspicion
			string[] deflections = {
				"Sadece işten sonra rahatlıyorum...",
				"Bu aralar çok stresli işler var...",
				"Karı kız muhabbeti işte, ne yaparsın...",
				"Ben de yeniyim buralarda...",
				"Sadece birkaç kadeh için geldim."
			};
			
			int index = (int)(GD.Randf() * deflections.Length);
			Say(deflections[index]);
			
			// Chance to order drink to blend in
			if (_drinkCount < 2) // Limit drinks to maintain cover
			{
				OrderDrink();
			}
		}

		// Override receive drink to limit consumption
		public override void ReceiveDrink(string drinkType, float quality)
		{
			// Undercover agents need to stay sharp - they pretend to drink more than they do
			
			// Lower drunkenness increase
			float actualDrunkennessIncrease = 0.05f; // Very minimal increase
			
			// Fake enjoying the drink
			PlayAnimation("drink");
			
			// Appropriate reaction for maintaining cover
			string[] reactions = {
				"Mmm, güzel...",
				"İşte bu iyi geldi.",
				"Tam da aradığım tat.",
				"Teşekkürler."
			};
			
			int index = (int)(GD.Randf() * reactions.Length);
			Say(reactions[index]);
			
			// Limited drunkenness increase
			AdjustDrunkenness(actualDrunkennessIncrease, $"Pretending to drink {drinkType}");
			
			// Normal satisfaction change
			float satisfactionChange = (quality - 0.5f) * 0.2f;
			AdjustSatisfaction(satisfactionChange, $"Drink quality: {quality}");
			
			// Pay for the drink
			float drinkPrice = CalculateDrinkPrice(drinkType);
			SpendMoney(drinkPrice, "drink");
		}

		// Override order drink to make appropriate choices
		public override string OrderDrink()
		{
			// Undercover agents often order simple drinks or ones that can be nursed
			string[] coverDrinks = { "Bira", "Viski (tek)", "Rakı (az)" };
			
			int index = (int)(GD.Randf() * coverDrinks.Length);
			string drinkChoice = coverDrinks[index];
			
			// Order drink with appropriate cover animation
			PlayAnimation("order_drink");
			Say($"Bir {drinkChoice} alabilir miyim?");
			
			// Add to order history
			if (_orderHistory.ContainsKey(drinkChoice))
				_orderHistory[drinkChoice]++;
			else
				_orderHistory[drinkChoice] = 1;
			
			_drinkCount++;
			
			return drinkChoice;
		}

		// Override talking to kons - cautious behavior
		protected override void UpdateTalkingToKonsState(float delta)
		{
			// Undercover agents are cautious with kons - they participate but gather info
			
			// Every few seconds, chance to gather intelligence
			if (GD.Randf() < 0.1f * delta)
			{
				// Try to gather info from kons conversation
				float evidenceAmount = 0.02f + GD.Randf() * 0.03f; // Small evidence increments
				GatherEvidence("staff_conversation", evidenceAmount);
			}
			
			// Limited interaction time - they don't want to get too involved
			if (_timeInStateBelonging > 180.0f || GD.Randf() < 0.02f) // 3 minutes max
			{
				ChangeState(CustomerState.Sitting);
			}
		}

		// Override prepare to leave - might need to report before leaving
		public override void PrepareToLeave()
		{
			// If there's significant evidence, try to report before leaving
			if (_evidenceGathered > 0.5f && !_isReporting)
			{
				ReportFindings();
			}
			
			// Then proceed with normal leaving process
			base.PrepareToLeave();
		}

		// Handle bribe attempts
		public bool AttemptToBribe(float amount)
		{
			// Check if bribe is accepted based on resistance and amount
			float normalizedAmount = amount / _budget; // Relative to budget
			float acceptChance = (1.0f - _bribeResistance) * normalizedAmount * 5.0f;
			
			// Cap the chance
			acceptChance = Mathf.Clamp(acceptChance, 0.0f, 0.8f);
			
			// Department specific modifiers
			if (_department == "MIT" || _department == "Special Forces")
			{
				acceptChance *= 0.5f; // Much harder to bribe
			}
			else if (_department == "Tax Office")
			{
				acceptChance *= 1.2f; // Slightly easier to bribe
			}
			
			// Roll for bribe acceptance
			bool accepted = GD.Randf() < acceptChance;
			
			if (accepted)
			{
				// Accept bribe
				_remainingBudget += amount;
				_satisfaction += 0.1f;
				
				// Record event
				_eventHistory.Add("Accepted bribe");
				
				// But still have a chance to report later (double agent)
				if (_department == "MIT" || _department == "Special Forces")
				{
					// High chance to still report
					if (GD.Randf() < 0.8f)
					{
						// Schedule delayed report
						Timer reportTimer = new Timer
						{
							WaitTime = 300.0f + GD.Randf() * 300.0f, // 5-10 minutes later
							OneShot = true
						};
						
						AddChild(reportTimer);
						reportTimer.Timeout += () => 
						{
							ReportFindings();
						};
						reportTimer.Start();
					}
				}
				
				return true;
			}
			else
			{
				// Reject bribe
				_suspicionLevel += 0.3f;
				_evidenceGathered += 0.2f;
				
				// Record event and likely report
				_eventHistory.Add("Rejected bribe");
				
				// High chance to report this
				if (GD.Randf() < 0.7f)
				{
					ReportFindings();
				}
				
				return false;
			}
		}

		// Extra awareness skill for undercover agents
		private float _awarenessSkill = 0.8f;

		// Override customer type specific initialization
		protected override void InitializeByCustomerType()
		{
			base.InitializeByCustomerType();
			
			// Specific adjustments for undercover
			_budget = Mathf.Clamp(_budget, 2000, 5000); // Government budget
			_generosity = 0.3f; // Not generous with taxpayer money
			_drunkennessLevel = 0.0f; // Start sober
			_aggressionLevel = 0.2f; // Generally calm unless provoked
			_kargaLevel = 0.0f; // Never does karga (leaves without paying)
			
			// Strong observation skills
			_awarenessSkill = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0
			
			// Stay longer to observe
			_maxStayTime = 360.0f; // 6 hours
			
			// Preferences
			_preferences["music_arabesk"] = 0.4f;
			_preferences["music_taverna"] = 0.5f;
			_preferences["music_fantezi"] = 0.3f;
			_preferences["music_oyunHavasi"] = 0.4f;
			_preferences["music_modern"] = 0.5f;
			
			// Undercover agents limit drinking
			_preferences["drink_raki"] = 0.3f;
			_preferences["drink_beer"] = 0.7f; // Prefer beer (easier to nurse)
			_preferences["drink_wine"] = 0.3f;
			_preferences["drink_whiskey"] = 0.6f;
			_preferences["drink_vodka"] = 0.2f;
			_preferences["drink_special"] = 0.2f;
			
			// Less interested in food - here to observe
			_preferences["meze_cold"] = 0.4f;
			_preferences["meze_hot"] = 0.3f;
			_preferences["meze_seafood"] = 0.2f;
			_preferences["meze_meats"] = 0.3f;
			
			// Interested in staff but cautious
			_preferences["staff_kons"] = 0.5f;
			_preferences["staff_waiter"] = 0.3f;
			_preferences["staff_musician"] = 0.3f;
			
			// Prefers positions good for observing
			_preferences["ambiance_loud"] = 0.3f; // Harder to hear
			_preferences["ambiance_intimate"] = 0.7f; // Good for observing
			_preferences["ambiance_luxurious"] = 0.5f;
			_preferences["ambiance_traditional"] = 0.5f;
		}
	}
}
