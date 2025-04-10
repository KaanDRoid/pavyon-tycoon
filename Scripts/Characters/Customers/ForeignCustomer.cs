// Scripts/Characters/Customers/ForeignCustomer.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	
	public partial class ForeignCustomer : CustomerBase
	{
		// Foreign customer specific properties
		private string _nationality = "Unknown";
		private float _languageBarrier = 0.5f;        // 0.0-1.0, affects communication and satisfaction
		private float _culturalFascination = 0.8f;    // 0.0-1.0, interest in local culture
		private bool _isDiscoElysiumCharacter = false;// Special character flag
		private string _discoCharacterType = "";      // Which Disco Elysium character
		
		// Cultural preferences
		private Dictionary<string, float> _culturalPreferences = new Dictionary<string, float>();
		
		// Translation phrases - things they try to say in Turkish
		private List<string> _attemptedTurkishPhrases = new List<string>();
		
		// Foreign visitor movement patterns
		private float _explorationTendency = 0.7f;    // Tendency to explore the pavyon
		private bool _hasTakenPhotos = false;         // Has taken photos of the venue
		private float _drinkingTolerance = 0.6f;      // How well they handle local drinks (0.0-1.0)
		
		// Constructor
		public ForeignCustomer() : base()
		{
			// Initialize attempted Turkish phrases
			InitializeAttemptedPhrases();
			
			// Override customer type
			_customerType = CustomerType.Foreigner;
		}

		public override void _Ready()
		{
			base._Ready();
			
			// Set specific properties for foreign customers
			SetNationality(GenerateRandomNationality());
			
			// Initialize Disco Elysium character if applicable
			float discoChance = 0.1f; // 10% chance to be a Disco Elysium character
			if (GD.Randf() < discoChance)
			{
				SetupDiscoElysiumCharacter();
			}
			
			// Update appearance based on nationality or character type
			UpdateAppearance();
		}
		
		// Initialize customer preferences based on nationality
		protected override void InitializePreferences()
		{
			// Call base implementation first
			base.InitializePreferences();
			
			// Cultural preferences - foreigners generally prefer:
			_preferences["music_traditional"] = 0.8f;     // Authentic Turkish music
			_preferences["ambiance_traditional"] = 0.8f;  // Traditional atmosphere
			_preferences["drink_raki"] = 0.7f;            // Trying rakı (local drink)
			_preferences["meze_cold"] = 0.7f;             // Traditional mezeler
			_preferences["staff_kons"] = 0.6f;            // Interest in konsomatris (unique to Turkish culture)
			
			// Cultural preferences dictionary
			_culturalPreferences["local_music"] = 0.8f;         // Interest in Turkish music
			_culturalPreferences["local_dance"] = 0.7f;         // Interest in Turkish dance
			_culturalPreferences["taking_photos"] = 0.5f;       // Desire to take photos
			_culturalPreferences["buying_souvenirs"] = 0.4f;    // Interest in souvenirs
			_culturalPreferences["conversation"] = 0.6f;        // Desire to talk to locals
		}
		
		// Setup for special Disco Elysium character traits
		private void SetupDiscoElysiumCharacter()
		{
			_isDiscoElysiumCharacter = true;
			
			// Randomly select a Disco Elysium character type
			string[] characterTypes = {
				"HarryDuBois", "KimKitsuragi", "JeanVicquemare", "JudithMinot", 
				"TitusHardie", "ClassicalCop", "SorrowfulCop", "ApocalypticCop"
			};
			
			int index = (int)(GD.Randf() * characterTypes.Length);
			_discoCharacterType = characterTypes[index];
			
			// Customize properties based on character type
			switch (_discoCharacterType)
			{
				case "HarryDuBois":
					FullName = "Harry Du Bois";
					_drunkennessLevel = 0.6f; // Starts already somewhat drunk
					_generosity = 0.7f;       // Very generous with money
					_kargaLevel = 0.4f;       // Might forget to pay
					_aggressionLevel = 0.3f;   // Can get aggressive when drunk
					_signature = "Disco detective with amnesia, chaotic behavior";
					// Special dialogue phrases
					_attemptedTurkishPhrases.Add("Ben... bir dedektifim? *hık*");
					_attemptedTurkishPhrases.Add("Kravat... nerede kravatım?");
					_attemptedTurkishPhrases.Add("Rakı? Bu... iyi bir fikir değil...");
					
					// Alter preferences
					_preferences["drink_beer"] = 0.9f;
					_preferences["drink_whiskey"] = 0.8f;
					_preferences["drink_raki"] = 0.5f; // Should probably avoid rakı
					_budget = 2500f; // Has unexpected amounts of money sometimes
					break;
					
				case "KimKitsuragi":
					FullName = "Kim Kitsuragi";
					_drunkennessLevel = 0.0f; // Rarely drinks
					_generosity = 0.4f;       // Careful with money
					_kargaLevel = 0.0f;       // Always pays
					_aggressionLevel = 0.1f;  // Very restrained
					_signature = "Methodical detective, takes notes, observant";
					// Special dialogue phrases
					_attemptedTurkishPhrases.Add("İlginç... bunu not ediyorum.");
					_attemptedTurkishPhrases.Add("Bu yer hakkında notlar almalıyım.");
					_attemptedTurkishPhrases.Add("Bu kültürel olarak... etkileyici.");
					
					// Alter preferences
					_preferences["music_arabesk"] = 0.3f; // Finds it too emotional
					_preferences["music_modern"] = 0.7f;  // More orderly
					_preferences["ambiance_intimate"] = 0.8f; // Prefers quieter spots
					_budget = 3000f; // Has a careful budget
					break;
					
				case "ClassicalCop":
					FullName = "Lt. Jean Torson";
					_generosity = 0.3f;       // Traditional, careful with money
					_kargaLevel = 0.0f;       // Law-abiding
					_aggressionLevel = 0.4f;  // Can be stern
					_signature = "By-the-book attitude, skeptical of foreign culture";
					_attemptedTurkishPhrases.Add("Kurallara uymalısınız!");
					
					// Alter preferences
					_preferences["music_modern"] = 0.8f;
					_preferences["ambiance_traditional"] = 0.4f; // Finds it odd
					_budget = 2800f;
					break;
					
				case "SorrowfulCop":
					FullName = "Trant Heidelstam";
					_generosity = 0.6f;       // Generous from melancholy
					_drunkennessLevel = 0.3f; // Drinks to cope
					_signature = "Melancholic, relates to emotional music";
					_attemptedTurkishPhrases.Add("Bu müzik... kalbime dokunuyor.");
					
					// Alter preferences
					_preferences["music_arabesk"] = 0.9f; // Loves emotional music
					_preferences["drink_raki"] = 0.8f;    // Enjoys contemplative drinking
					_budget = 3200f;
					break;
					
				case "ApocalypticCop":
					FullName = "Noid Dros";
					_aggressionLevel = 0.6f;  // Paranoid
					_signature = "Paranoid, sees conspiracies everywhere";
					_attemptedTurkishPhrases.Add("Duvarların kulakları var!");
					
					// Alter preferences
					_preferences["ambiance_intimate"] = 0.9f; // Needs privacy
					_preferences["staff_security"] = 0.3f;    // Distrusts authority
					_budget = 2000f; // Limited budget
					break;
					
				default:
					FullName = "Revachol Visitor";
					_signature = "Mysterious foreign visitor";
					break;
			}
			
			// Override age and gender based on character
			if (_discoCharacterType == "KimKitsuragi" || _discoCharacterType == "HarryDuBois")
			{
				Age = 44;
				Gender = "Male";
			}
			else if (_discoCharacterType == "JudithMinot")
			{
				Age = 35;
				Gender = "Female";
			}
			else
			{
				Age = GD.RandRange(30, 50);
				Gender = "Male"; // Most DE characters are male
			}
			
			IsVIP = true; // Disco Elysium characters are always VIP
		}
		
		// Generate a random nationality for standard foreign customers
		private string GenerateRandomNationality()
		{
			string[] nationalities = {
				"American", "British", "German", "French", "Russian", 
				"Japanese", "Chinese", "Italian", "Spanish", "Dutch",
				"Swedish", "Norwegian", "Australian", "Canadian", "Brazilian"
			};
			
			int index = (int)(GD.Randf() * nationalities.Length);
			return nationalities[index];
		}
		
		// Set nationality and adjust properties accordingly
		public void SetNationality(string nationality)
		{
			_nationality = nationality;
			
			// Adjust language barrier and cultural preferences based on nationality
			switch (_nationality)
			{
				case "American":
					_languageBarrier = 0.7f;
					_culturalFascination = 0.8f;
					_budget = Mathf.Clamp(_budget * 1.2f, 3000, 12000); // Americans tend to have more spending money
					break;
				case "British":
					_languageBarrier = 0.6f;
					_culturalFascination = 0.7f;
					_attemptedTurkishPhrases.Add("Merhaba mate! Bir bira lütfen!");
					break;
				case "German":
					_languageBarrier = 0.5f;
					_culturalFascination = 0.9f; // Germans often interested in cultural authenticity
					_drinkingTolerance = 0.8f;   // Higher alcohol tolerance
					_attemptedTurkishPhrases.Add("Bu rakı çok güzel, ja?");
					break;
				case "Russian":
					_languageBarrier = 0.7f;
					_drinkingTolerance = 0.9f;   // Highest alcohol tolerance
					_generosity = 0.7f;          // Can be generous with tips
					_attemptedTurkishPhrases.Add("Na zdorovye! Şerefe!");
					break;
				case "Japanese":
					_languageBarrier = 0.8f;     // Higher language barrier
					_culturalFascination = 0.9f; // Very interested in local culture
					_attemptedTurkishPhrases.Add("Kamera çekebilir miyim?");
					_hasTakenPhotos = true;      // Likely to take photos
					break;
				case "French":
					_languageBarrier = 0.6f;
					_culturalPreferences["local_music"] = 0.9f; // French visitors often appreciate music
					_attemptedTurkishPhrases.Add("Bu müzik... magnifique!");
					_preferences["drink_wine"] = 0.8f; // Prefers wine if available
					break;
				default:
					_languageBarrier = 0.6f;
					_culturalFascination = 0.7f;
					break;
			}
			
			// Add generic attempted Turkish phrases based on language barrier
			if (_languageBarrier > 0.6f) 
			{
				_attemptedTurkishPhrases.Add("Mer...haba? Doğru mu?");
				_attemptedTurkishPhrases.Add("Evet... hayır... uh...");
			}
			else 
			{
				_attemptedTurkishPhrases.Add("Merhaba! Nasılsınız?");
				_attemptedTurkishPhrases.Add("Teşekkür ederim!");
			}
		}
		
		// Initialize common attempted Turkish phrases
		private void InitializeAttemptedPhrases()
		{
			_attemptedTurkishPhrases = new List<string>
			{
				"Merhaba!",
				"Teşekkürler!",
				"Şerefe!",
				"Ne kadar?",
				"Çok güzel!",
				"Türkiye çok güzel.",
				"Bir daha lütfen?",
				"Anlamadım...",
				"İngilizce biliyor musunuz?",
				"Bu ne?",
				"Hesap lütfen.",
				"Nerede... tuvalet?"
			};
		}
		
		// Say something in attempted Turkish with some mistakes
		public void SayAttemptedTurkish()
		{
			// Based on language barrier, may make mistakes
			if (_attemptedTurkishPhrases.Count == 0) return;
			
			int index = (int)(GD.Randf() * _attemptedTurkishPhrases.Count);
			string phrase = _attemptedTurkishPhrases[index];
			
			// May mispronounce if language barrier is high
			if (_languageBarrier > 0.6f && GD.Randf() < 0.7f)
			{
				phrase = MispronouncePhrase(phrase);
			}
			
			Say(phrase);
		}
		
		// Add some mispronunciations to Turkish phrases
		private string MispronouncePhrase(string phrase)
		{
			// Replace some Turkish characters with incorrect ones
			phrase = phrase.Replace("ş", "s");
			phrase = phrase.Replace("ı", "i");
			phrase = phrase.Replace("ğ", "g");
			phrase = phrase.Replace("ü", "u");
			phrase = phrase.Replace("ö", "o");
			phrase = phrase.Replace("ç", "c");
			
			// Add some typical foreign accent markers
			if (GD.Randf() < 0.3f)
			{
				phrase += "?"; // Uncertain tone
			}
			
			return phrase;
		}
		
		// Update appearance based on nationality or character type
		private void UpdateAppearance()
		{
			// Override appearance based on Disco Elysium character or nationality
			if (_isDiscoElysiumCharacter)
			{
				// Set appearance based on Disco Elysium character
				switch (_discoCharacterType)
				{
					case "HarryDuBois":
						// Would set clothing, face model, etc. once implemented
						break;
					case "KimKitsuragi":
						// Would set clothing, face model, etc. once implemented
						break;
					// Add other character visuals
				}
			}
			else
			{
				// Set appearance based on nationality
				// This would call into a character customization system once implemented
			}
		}
		
		// Override durum güncelleme methods to incorporate foreign customer behaviors
		protected override void UpdateSittingState(float delta)
		{
			base.UpdateSittingState(delta);
			
			// Additional behaviors for foreign customers
			
			// Taking photos behavior
			if (!_hasTakenPhotos && GD.Randf() < 0.005f * _culturalFascination)
			{
				TakePhotos();
			}
			
			// Try to speak Turkish occasionally
			if (GD.Randf() < 0.003f)
			{
				SayAttemptedTurkish();
			}
			
			// Be more interested in observing the environment
			if (GD.Randf() < 0.007f * _explorationTendency)
			{
				ObserveEnvironment();
			}
		}
		
		// Photo taking behavior
		private void TakePhotos()
		{
			PlayAnimation("take_photo");
			Say("*Fotoğraf çekiyor*");
			_hasTakenPhotos = true;
			
			// Can annoy other customers
			AlertNearbyCustomers("photo");
			
			// Log the activity
			GD.Print($"Foreign customer {FullName} ({_nationality}) is taking photos in the pavyon");
		}
		
		// Observe environment - looking around with curiosity
		private void ObserveEnvironment()
		{
			PlayAnimation("look_around");
			
			string[] observations = {
				"Wow, interesting place...",
				"So this is a real Turkish pavyon!",
				"*looking around with curiosity*",
				"*taking mental notes*",
				"*nodding with interest*"
			};
			
			int index = (int)(GD.Randf() * observations.Length);
			Say(observations[index]);
			
			// Increase satisfaction due to cultural experience
			AdjustSatisfaction(0.02f, "Cultural fascination");
		}
		
		// Alert nearby customers about something (like photo taking)
		private void AlertNearbyCustomers(string action)
		{
			// This would interface with the CustomerManager to find nearby customers
			// and potentially trigger reactions
			
			if (GetTree().Root.HasNode("GameManager/CustomerManager"))
			{
				var customerManager = GetTree().Root.GetNode("GameManager/CustomerManager");
				
				if (customerManager.HasMethod("NotifyNearbyCustomers"))
				{
					try
					{
						customerManager.Call("NotifyNearbyCustomers", CustomerId, Position, action);
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error calling NotifyNearbyCustomers: {e.Message}");
					}
				}
			}
		}
		
		// Override drink receiving to account for different alcohol tolerance
		public override void ReceiveDrink(string drinkType, float quality)
		{
			// First call base implementation
			base.ReceiveDrink(drinkType, quality);
			
			// Then apply foreign-specific modifications
			
			// Language barrier can cause ordering mistakes
			if (_languageBarrier > 0.6f && GD.Randf() < 0.3f)
			{
				// Confusion about what was ordered
				Say("This isn't what I ordered... I think?");
				AdjustSatisfaction(-0.05f, "Order misunderstanding");
			}
			
			// Cultural experience of Turkish drinks
			if (drinkType == "Rakı" && GD.Randf() < _culturalFascination)
			{
				// Special reaction to trying rakı for the first time
				string[] rakiReactions = {
					"Whoa! This is strong!",
					"*coughs* What is in this?",
					"Oh! It turns white with water!",
					"Interesting anise flavor...",
					"So this is the famous rakı!"
				};
				
				int index = (int)(GD.Randf() * rakiReactions.Length);
				Say(rakiReactions[index]);
				
				// Adjust drunkeness based on tolerance
				float extraDrunkenness = (1.0f - _drinkingTolerance) * 0.1f;
				AdjustDrunkenness(extraDrunkenness, "Low tolerance to rakı");
				
				// But satisfaction increases from cultural experience
				AdjustSatisfaction(0.1f, "Authentic cultural experience");
			}
		}
		
		// Override meze receiving to account for different food preferences
		public override void ReceiveMeze(string mezeType, float quality)
		{
			// First call base implementation
			base.ReceiveMeze(mezeType, quality);
			
			// Then apply foreign-specific modifications
			
			// Cultural experience of Turkish meze
			if (GD.Randf() < _culturalFascination)
			{
				// Special reaction to trying meze
				string[] mezeReactions = {
					"Mmm, very different!",
					"This is delicious!",
					"What's in this again?",
					"*takes photo of the food*",
					"Is this how you eat it?"
				};
				
				int index = (int)(GD.Randf() * mezeReactions.Length);
				Say(mezeReactions[index]);
				
				// Satisfaction increases from cultural experience
				AdjustSatisfaction(0.08f, "Authentic food experience");
			}
		}
		
		// Override bahşiş verme behavior for foreigners
		protected override void GiveTip(float amount, string staffId)
		{
			float tipAmount = amount;
			
			// Adjust tip based on nationality customs
			switch (_nationality)
			{
				case "American":
					tipAmount *= 1.5f; // Americans often tip more generously
					break;
				case "British":
					tipAmount *= 1.1f; // Slightly more than base
					break;
				case "Japanese":
					tipAmount *= 0.8f; // Not as accustomed to tipping
					break;
				case "German":
					tipAmount *= 1.2f; // More generous than average
					break;
			}
			
			// Apply cultural modifier - fascination with local culture increases tipping
			tipAmount *= (1.0f + (_culturalFascination - 0.5f) * 0.4f);
			
			// If a Disco Elysium character, apply character-specific modifiers
			if (_isDiscoElysiumCharacter)
			{
				switch (_discoCharacterType)
				{
					case "HarryDuBois":
						// Harry might forget he already tipped or tip far too much
						if (GD.Randf() < 0.3f)
						{
							tipAmount *= 2.0f;
							Say("Is this how tipping works here? Take it all!");
						}
						break;
					case "KimKitsuragi":
						// Kim tips exactly the appropriate amount
						tipAmount = amount * 1.0f;
						Say("*precisely counts the correct tip amount*");
						break;
				}
			}
			
			// Now call the base implementation with adjusted amount
			base.GiveTip(tipAmount, staffId);
			
			// Log special tipping behavior
			if (tipAmount > amount * 1.3f)
			{
				GD.Print($"Foreign customer {FullName} gave an unusually large tip: {tipAmount}");
			}
		}
		
		// Override process payment for foreign customers
		protected override void ProcessPayment()
		{
			// Foreign customers might be confused by the bill
			if (_languageBarrier > 0.6f && !_isDiscoElysiumCharacter)
			{
				Say("Wait, how much is this in euros?");
				
				// Slight delay in payment due to confusion
				Timer delayTimer = new Timer
				{
					WaitTime = 2.0f,
					OneShot = true
				};
				
				AddChild(delayTimer);
				delayTimer.Timeout += () => base.ProcessPayment();
				delayTimer.Start();
			}
			else
			{
				// Normal payment processing
				base.ProcessPayment();
				
				// Cultural farewell
				if (GD.Randf() < 0.7f)
				{
					SayAttemptedTurkish(); // Try to say goodbye in Turkish
				}
			}
			
			// Record the visit as a cultural experience
			if (GetTree().Root.HasNode("GameManager/ReputationManager"))
			{
				var reputationManager = GetTree().Root.GetNode("GameManager/ReputationManager");
				
				if (reputationManager.HasMethod("RecordForeignVisitor"))
				{
					try
					{
						reputationManager.Call("RecordForeignVisitor", _nationality, _satisfaction, _totalSpent);
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error calling RecordForeignVisitor: {e.Message}");
					}
				}
			}
		}
		
		// Get nationality property
		public string GetNationality()
		{
			return _nationality;
		}
		
		// Check if this is a Disco Elysium character
		public bool IsDiscoElysiumCharacter()
		{
			return _isDiscoElysiumCharacter;
		}
		
		// Get Disco Elysium character type
		public string GetDiscoCharacterType()
		{
			return _discoCharacterType;
		}
		
		// Override GetStats to include foreign-specific stats
		public new Dictionary<string, object> GetStats()
		{
			// Get the base stats
			Dictionary<string, object> stats = base.GetStats();
			
			// Add foreign-specific stats
			stats["Nationality"] = _nationality;
			stats["LanguageBarrier"] = _languageBarrier;
			stats["CulturalFascination"] = _culturalFascination;
			stats["ExplorationTendency"] = _explorationTendency;
			stats["DrinkingTolerance"] = _drinkingTolerance;
			stats["HasTakenPhotos"] = _hasTakenPhotos;
			stats["IsDiscoElysiumCharacter"] = _isDiscoElysiumCharacter;
			
			if (_isDiscoElysiumCharacter)
			{
				stats["DiscoCharacterType"] = _discoCharacterType;
			}
			
			return stats;
		}
	}
}
