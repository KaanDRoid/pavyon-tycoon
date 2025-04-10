// Scripts/Characters/Customers/WorkerCustomer.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	/// <summary>
	/// WorkerCustomer represents the working-class customers who visit the pavyon.
	/// These customers typically have limited budgets but are loyal regulars
	/// who seek escape from their daily work lives.
	/// </summary>
	public partial class WorkerCustomer : CustomerBase
	{
		// Worker-specific properties
		private bool _isRegular = false;          // Regular patron status
		private int _visitCount = 0;              // Number of times visited the pavyon
		private string _occupation = "";          // Job title/occupation
		private float _stressLevel = 0.8f;        // Work stress level (0-1)
		private float _payDay = false;            // If today is payday (more budget)
		private string _neighborhood = "";        // Which neighborhood they're from
		private float _alcoholTolerance = 0.6f;   // How well they handle alcohol (0-1)
		
		// Consumption patterns
		private int _maxDrinksPerVisit = 4;       // Maximum number of drinks they'll order
		private float _danceChance = 0.3f;        // Chance to dance when music plays
		
		// Worker-specific animations and dialogue content
		private string[] _workComplaints;         // Complaints about work
		private string[] _payDayPhrases;          // Phrases used when it's payday
		
		// Chance and timing for behaviors
		private float _checkWatchTimer = 0.0f;    // Timer for "checking watch" animation
		private float _complainAboutWorkTimer = 0.0f;   // Timer for work complaints
		
		public WorkerCustomer() : base()
		{
			// Initialize arrays
			InitializeDialoguePhrases();
		}
		
		// Constructor with parameters
		public WorkerCustomer(string fullName, int age, string gender, float budget) 
			: base(fullName, age, gender, CustomerType.Worker, budget)
		{
			InitializeWorkerProperties();
			InitializeDialoguePhrases();
		}
		
		public override void _Ready()
		{
			base._Ready();
			
			// If properties weren't set in constructor, initialize them now
			if (string.IsNullOrEmpty(_occupation))
			{
				InitializeWorkerProperties();
			}
		}
		
		// Initialize worker-specific properties with appropriate values
		protected void InitializeWorkerProperties()
		{
			// Generate occupation from common blue-collar jobs in Ankara
			string[] occupations = {
				"İnşaat İşçisi", "Fabrika İşçisi", "Şoför", "Tesisatçı", "Elektrikçi", 
				"Güvenlik", "Belediye İşçisi", "Mobilyacı", "Boyacı", "Makinist",
				"Bakkal", "Garson", "Aşçı Yardımcısı", "Tamirci", "Tezgahtar"
			};
			_occupation = occupations[GD.RandRange(0, occupations.Length - 1)];
			
			// Generate neighborhood (working-class areas in Ankara)
			string[] neighborhoods = {
				"Mamak", "Keçiören", "Sincan", "Altındağ", "Etimesgut", 
				"Batıkent", "Demetevler", "Siteler", "Eryaman", "Etlik",
				"Yenimahalle", "Şentepe", "Akdere", "Tuzluçayır", "Natoyolu"
			};
			_neighborhood = neighborhoods[GD.RandRange(0, neighborhoods.Length - 1)];
			
			// Determine if it's payday (payday is usually 15th or last day of month)
			_payDay = IsPayday();
			
			// Adjust budget if it's payday
			if (_payDay)
			{
				_budget *= 1.5f;
				_remainingBudget = _budget;
				_maxDrinksPerVisit += 2; // Can afford more drinks on payday
			}
			
			// Set stress level based on occupation
			_stressLevel = 0.7f + (GD.Randf() * 0.3f); // Between 0.7-1.0 for workers
			
			// Determine alcohol tolerance (higher for regular drinkers)
			_alcoholTolerance = 0.5f + (GD.Randf() * 0.4f); // Between 0.5-0.9 for workers
			
			// Set worker-specific traits
			_isRegular = GD.Randf() < 0.7f; // 70% chance of being a regular
			_visitCount = _isRegular ? GD.RandRange(5, 30) : GD.RandRange(0, 4);
			
			// Initialize timers for behaviors
			_checkWatchTimer = GD.RandRange(60, 180); // Check watch every 1-3 minutes
			_complainAboutWorkTimer = GD.RandRange(120, 300); // Complain every 2-5 minutes
			
			// Adjust signature based on worker traits
			SetWorkerSignature();
		}
		
		// Initialize dialogue phrases specific to workers
		protected void InitializeDialoguePhrases()
		{
			// Work complaints
			_workComplaints = new string[] {
				"Patron yine maaşları geciktiriyor ya!",
				"Bu hafta 60 saat çalıştım, bıktım artık!",
				"Şantiyeyi bi görsen, her yer toz duman...",
				"Ustabaşı yine boş boş emir verdi bütün gün.",
				"Vardiyalı çalışmaktan düzenim kalmadı.",
				"Ev kira, çocuk okul... Nasıl yetiştireceğiz bilmiyorum.",
				"Bir de şefimiz var, sürekli tepemizde bağırıyor!",
				"Sabah 7'den akşam 7'ye kadar ayaktayım, kemiklerim ağrıyor.",
				"Sigorta bile yapmıyorlar bize ya!",
				"Bizim iş yerinde sendika falan hikaye, konuşsan kapıyı gösterirler.",
				"Elim nasır bağladı valla, bak şuna!",
				"Bugün makine bozuldu, tüm gün uğraştık.",
				"Bir mola vermek için bile izin almak lazım."
			};
			
			// Payday phrases
			_payDayPhrases = new string[] {
				"Bugün maaş günü, biraz rahatladık çok şükür!",
				"Maaşı aldık, bu gece benden bir tur daha!",
				"Emekçinin bayramı bugün, maaş geldi cebe!",
				"Ay başı geldi sonunda, boş cüzdan doldu biraz.",
				"Bugün cepte para var, bir rahat nefes alalım bari.",
				"Maaş yattı, kredi kartı borcunu kapattık, kalan da buraya!",
				"Sonunda maaş! Ev sahibine, markete, bakkala, hepsine borç ödeyeceğiz ama bi gece de kendimize ayıralım.",
				"Maaşı alır almaz koştum buraya, biraz kafayı dağıtalım!"
			};
		}
		
		// Set the worker's signature appearance details
		private void SetWorkerSignature()
		{
			string[] workerSignatures = {
				"Yıpranmış kot pantolon, eskimiş gömlek, yorgun gözler",
				"Solmuş iş pantolonu, kalın kemer, nasırlı eller",
				"İşten yeni çıkmış haliyle, lekeli polo yaka tişört",
				"Sürekli omzunu ovalayan, halsiz tavırlar, kırışık gömlek",
				"Bıyıklı, hafif sakallı, işçi yüzükleri, şişmiş bilekler",
				"Başına taktığı kasketle, gri gömlek, biraz kir izleri",
				"Kolları kıvrılmış uzun kollu gömlek, kösele ayakkabılar",
				"Sürekli belini tutan, kaslarını geren, hareketleri yavaş",
				"İşçi botlu, kalın tabanlı, kırışık yüz hatları",
				"Terli saçlar, yanık ten, yorgun bir gülümseme"
			};
			
			int index = GD.RandRange(0, workerSignatures.Length - 1);
			_signature = workerSignatures[index];
		}
		
		// Check if today is payday (15th or end of month)
		private bool IsPayday()
		{
			// Access TimeManager to get current date
			var timeManager = GetTree().Root.GetNode<TimeManager>("/root/TimeManager");
			if (timeManager != null)
			{
				int currentDay = timeManager.CurrentDay;
				int daysInMonth = 30; // Simplified
				
				// Payday is typically 15th or last day of month in Turkey
				return (currentDay == 15 || currentDay == daysInMonth);
			}
			
			// Random chance as fallback
			return GD.Randf() < 0.1f; // 10% chance of being payday
		}
		
		public override void _Process(double delta)
		{
			base._Process(delta);
			
			// Worker-specific behaviors
			if (_currentState == CustomerState.Sitting)
			{
				// Check watch periodically (workers often worry about time)
				_checkWatchTimer -= (float)delta;
				if (_checkWatchTimer <= 0)
				{
					CheckWatch();
					_checkWatchTimer = GD.RandRange(60, 180); // Reset timer
				}
				
				// Complain about work periodically
				_complainAboutWorkTimer -= (float)delta;
				if (_complainAboutWorkTimer <= 0 && _drinkCount > 0)
				{
					ComplainAboutWork();
					_complainAboutWorkTimer = GD.RandRange(120, 300); // Reset timer
				}
				
				// Workers have earlier leaving tendency - check the time more strictly
				if (_timeInPavyon > _maxStayTime * 0.8f && GD.Randf() < 0.01f)
				{
					Say("Yarın erken kalkacağım, fazla kalamam.");
					
					// 70% chance to leave early if it's not payday
					if (!_payDay && GD.Randf() < 0.7f)
					{
						PrepareToLeave();
					}
				}
			}
		}
		
		// Worker-specific animations and dialogues
		
		// Check watch animation and dialogue
		private void CheckWatch()
		{
			PlayAnimation("check_watch");
			
			// If it's getting late, show concern about time
			if (_timeInPavyon > 120.0f) // After 2 hours
			{
				string[] timeWorryPhrases = {
					"Saat kaç oldu ya, erken kalkmam lazım yarın...",
					"Vay be, zaman nasıl geçiyor!",
					"Biraz daha takılıp gitsem mi acaba?",
					"Vardiyaya yetişmem lazım yarın sabah."
				};
				
				int index = GD.RandRange(0, timeWorryPhrases.Length - 1);
				Say(timeWorryPhrases[index], 2.0f);
			}
		}
		
		// Complain about work
		private void ComplainAboutWork()
		{
			// Get a random work complaint
			int index = GD.RandRange(0, _workComplaints.Length - 1);
			string complaint = _workComplaints[index];
			
			// Only complain if a bit drunk
			if (_drunkennessLevel > 0.3f)
			{
				PlayAnimation("talk_frustrated");
				Say(complaint, 3.0f);
				
				// Complaining slightly reduces stress
				_stressLevel = Mathf.Max(0.3f, _stressLevel - 0.05f);
				
				// Small chance to gain satisfaction from venting
				if (GD.Randf() < 0.3f)
				{
					AdjustSatisfaction(0.05f, "Venting about work");
				}
			}
		}
		
		// When worker receives a drink
		public override void ReceiveDrink(string drinkType, float quality)
		{
			base.ReceiveDrink(drinkType, quality);
			
			// If it's payday, special reaction
			if (_payDay && _drinkCount == 1) // First drink
			{
				int index = GD.RandRange(0, _payDayPhrases.Length - 1);
				Say(_payDayPhrases[index], 3.0f);
			}
			
			// Workers are more resistant to alcohol (they drink often)
			// Reduce the drunkenness increase by alcohol tolerance
			AdjustDrunkenness(-0.02f * _alcoholTolerance, "Alcohol tolerance");
			
			// Stress reduction from drinking
			_stressLevel = Mathf.Max(0.2f, _stressLevel - 0.1f);
			
			// After multiple drinks, might express financial concerns
			if (_drinkCount >= 3 && !_payDay && GD.Randf() < 0.4f)
			{
				string[] moneyWorryPhrases = {
					"Bu ay bütçe biraz sıkı ya, dikkatli harcamalıyım...",
					"Hesap biraz kabarıyor, yavaşlamam lazım.",
					"Ayın sonunu getirebilecek miyiz bakalım?",
					"Keşke her gün maaş günü olsa!"
				};
				
				int index = GD.RandRange(0, moneyWorryPhrases.Length - 1);
				Say(moneyWorryPhrases[index], 2.5f);
			}
			
			// Check if reached drink limit
			if (_drinkCount >= _maxDrinksPerVisit && GD.Randf() < 0.5f)
			{
				string[] limitReachedPhrases = {
					"Bu benim son kadehim artık...",
					"Yeter bu kadar, cüzdan boşalıyor!",
					"Ay sonu geldi, hesabı kapatalım artık.",
					"Sabah erken kalkacağım, fazla içmeyeyim."
				};
				
				int index = GD.RandRange(0, limitReachedPhrases.Length - 1);
				Say(limitReachedPhrases[index], 3.0f);
				
				// 50% chance to start leaving after max drinks
				if (GD.Randf() < 0.5f)
				{
					PrepareToLeave();
				}
			}
		}
		
		// Override bahşiş (tip) calculations for workers
		protected override void ProcessPayment()
		{
			// Workers give less tip on normal days
			float tipPercentage = _payDay ? _generosity * 0.15f : _generosity * 0.1f;
			
			// Satisfaction affects tip (but workers tip less in general)
			tipPercentage += (_satisfaction - 0.5f) * 0.05f;
			
			// Drunk workers might tip more or less unpredictably
			if (_drunkennessLevel > 0.7f)
			{
				tipPercentage += (GD.Randf() * 0.1f) - 0.05f; // -5% to +5% random adjustment
			}
			
			float tipAmount = _totalSpent * tipPercentage;
			
			// Pay the tip
			if (_remainingBudget >= tipAmount)
			{
				if (_assignedKonsId != null)
				{
					GiveTip(tipAmount, _assignedKonsId);
					
					// Workers might say something about tipping
					if (_payDay && GD.Randf() < 0.7f)
					{
						string[] tipPhrases = {
							"Buyur güzelim, bugün maaş günü!",
							"Emekçiden emekçiye, buyur!",
							"Az ama öz olsun, haftaya yine geliyoruz!"
						};
						
						int index = GD.RandRange(0, tipPhrases.Length - 1);
						Say(tipPhrases[index], 2.0f);
					}
				}
				else if (_interactedStaffIds.Count > 0)
				{
					int randomIndex = (int)(GD.Randf() * _interactedStaffIds.Count);
					GiveTip(tipAmount, _interactedStaffIds[randomIndex]);
				}
			}
			
			// Increase visit count
			_visitCount++;
			
			// Workers who visit regularly become more loyal
			if (_visitCount > 5)
			{
				_loyaltyLevel = Mathf.Min(1.0f, _loyaltyLevel + 0.05f);
			}
			
			GD.Print($"Worker customer {FullName} paid total: {_totalSpent}, tip: {tipAmount}, " +
					 $"payday: {_payDay}, visit count: {_visitCount}");
		}
		
		// Workers react differently to music and performances
		protected override void UpdateWatchingShowState(float delta)
		{
			base.UpdateWatchingShowState(delta);
			
			// Workers enjoy folk music and arabesk more
			if (IsArabeskPlaying() && GD.Randf() < 0.2f)
			{
				// Extra satisfaction from worker-preferred music
				AdjustSatisfaction(0.03f, "Preferred music playing");
				
				// Might express appreciation for music
				if (GD.Randf() < 0.3f)
				{
					string[] musicAppreciationPhrases = {
						"İşte bu ya! Tam benim tarzım!",
						"Ah be usta, yürekten söylüyor!",
						"Bu şarkı benim hayatım resmen...",
						"Şu sözlere bak, her bir kelimesi yürek yakıyor!"
					};
					
					int index = GD.RandRange(0, musicAppreciationPhrases.Length - 1);
					Say(musicAppreciationPhrases[index], 3.0f);
				}
			}
		}
		
		// Check if arabesk music is playing
		private bool IsArabeskPlaying()
		{
			// In a full implementation, query the AudioManager or similar
			// For now, return a random chance
			return GD.Randf() < 0.3f;
		}
		
		// Workers have specific dance moves
		protected override void UpdateDancingState(float delta)
		{
			base.UpdateDancingState(delta);
			
			// Workers dance differently based on drunkenness
			if (_isMoving == false && _currentState == CustomerState.Dancing)
			{
				if (_drunkennessLevel > 0.7f)
				{
					PlayAnimation("dance_wild"); // Energetic, uninhibited dancing when drunk
					
					if (GD.Randf() < 0.05f)
					{
						string[] drunkDancePhrases = {
							"Hadi coştur beni maestro!",
							"Şöyle bir kıralım be!",
							"Çal muço çaaaaal!",
							"Bir haftanın stresi çıkıyor valla!"
						};
						
						int index = GD.RandRange(0, drunkDancePhrases.Length - 1);
						Say(drunkDancePhrases[index], 2.0f);
					}
				}
				else
				{
					PlayAnimation("dance_shy"); // More reserved, self-conscious dancing when sober
				}
				
				// Dancing reduces stress significantly
				_stressLevel = Mathf.Max(0.1f, _stressLevel - 0.2f);
				
				// Satisfaction boost from dancing (stress relief)
				if (GD.Randf() < 0.1f)
				{
					AdjustSatisfaction(0.05f, "Dancing stress relief");
				}
			}
		}
		
		// When state changes
		protected override void OnStateChanged(CustomerState previousState, CustomerState newState)
		{
			base.OnStateChanged(previousState, newState);
			
			// Worker-specific state change behavior
			if (newState == CustomerState.Entering)
			{
				// Workers often look tired when entering
				PlayAnimation("walk_tired");
				
				// First-time phrases
				if (_visitCount == 0)
				{
					string[] firstVisitPhrases = {
						"İş arkadaşları burası iyi demişti, bir bakalım...",
						"Yorucu bir günün ardından biraz rahatlayalım bakalım."
					};
					
					int index = GD.RandRange(0, firstVisitPhrases.Length - 1);
					Say(firstVisitPhrases[index], 3.0f);
				}
				// Regular customer phrases
				else if (_isRegular)
				{
					string[] regularPhrases = {
						"Selam ahbap, yine geldik işte!",
						"Vay ustam, nasılsın bakalım?",
						"Benim her zamanki masayı hazırla abi!"
					};
					
					int index = GD.RandRange(0, regularPhrases.Length - 1);
					Say(regularPhrases[index], 3.0f);
				}
			}
			
			if (newState == CustomerState.Leaving && previousState != CustomerState.Leaving)
			{
				// Workers often mention going back to work tomorrow
				if (_timeInPavyon > 180 || _drinkCount > 3)
				{
					string[] leavingPhrases = {
						"Yarın yine erken kalkış var, ben kaçayım artık...",
						"İyi eğlendik ama iş güç bekliyor yarın.",
						"Hesabı alalım, mesai saati yaklaşıyor yine!"
					};
					
					int index = GD.RandRange(0, leavingPhrases.Length - 1);
					Say(leavingPhrases[index], 3.0f);
				}
			}
		}
		
		// Workers are more price-sensitive
		protected override float CalculateDrinkPrice(string drinkType)
		{
			float basePrice = base.CalculateDrinkPrice(drinkType);
			
			// Notice price increases more acutely
			float priceMultiplier = GetPriceMultiplier("drink");
			
			if (priceMultiplier > 1.2f && GD.Randf() < 0.4f)
			{
				// Complain about high prices
				string[] highPricePhrases = {
					"Fiyatlar uçmuş yine ya!",
					"Eskiden bu kadar pahalı değildi burada...",
					"Bir maaş veriyoruz neredeyse iki kadehe!",
					"Bizim maaşla zor yetişiyoruz bu fiyatlara!"
				};
				
				int index = GD.RandRange(0, highPricePhrases.Length - 1);
				Say(highPricePhrases[index], 3.0f);
				
				// Higher prices affect satisfaction more for workers
				AdjustSatisfaction(-0.05f, "High drink prices");
			}
			
			return basePrice;
		}
		
		// Workers enjoy talking to kons but have limited budgets
		protected override void UpdateTalkingToKonsState(float delta)
		{
			base.UpdateTalkingToKonsState(delta);
			
			// Workers get stressed about spending too much on kons
			if (_timeInStateBelonging > 180.0f && _remainingBudget < _budget * 0.3f)
			{
				if (GD.Randf() < 0.3f && !_payDay)
				{
					string[] moneyWorryPhrases = {
						"Bütçeye dikkat etmem lazım, bugün bu kadar...",
						"Ay sonu yaklaşıyor, biraz frene basalım.",
						"Canım, bugünlük bu kadar eğlenebildik."
					};
					
					int index = GD.RandRange(0, moneyWorryPhrases.Length - 1);
					Say(moneyWorryPhrases[index], 3.0f);
					
					// End conversation with kons
					ChangeState(CustomerState.Sitting);
				}
			}
		}
		
		// Workers have unique preferences by occupation
		protected override void InitializePreferences()
		{
			base.InitializePreferences();
			
			// Override with worker-specific preferences
			_preferences["music_arabesk"] = 0.8f;
			_preferences["music_taverna"] = 0.7f;
			_preferences["music_fantezi"] = 0.6f;
			_preferences["music_oyunHavasi"] = 0.7f;
			_preferences["music_modern"] = 0.4f;
			
			// Workers typically prefer raki and beer
			_preferences["drink_raki"] = 0.7f;
			_preferences["drink_beer"] = 0.8f;
			_preferences["drink_wine"] = 0.4f;
			_preferences["drink_whiskey"] = 0.3f;
			_preferences["drink_vodka"] = 0.5f;
			_preferences["drink_special"] = 0.3f;
			
			// Meze preferences
			_preferences["meze_cold"] = 0.6f;
			_preferences["meze_hot"] = 0.7f;
			_preferences["meze_seafood"] = 0.4f;
			_preferences["meze_meats"] = 0.8f;
			
			// Adjust based on specific occupation
			AdjustPreferencesByOccupation();
		}
		
		// Adjust preferences based on worker's specific occupation
		private void AdjustPreferencesByOccupation()
		{
			// Construction workers
			if (_occupation.Contains("İnşaat") || _occupation.Contains("Boyacı"))
			{
				_preferences["drink_raki"] += 0.1f;
				_preferences["meze_meats"] += 0.1f;
				_alcoholTolerance += 0.1f;
				_stressLevel += 0.1f;
			}
			// Drivers
			else if (_occupation.Contains("Şoför") || _occupation.Contains("Taksi"))
			{
				_preferences["drink_beer"] += 0.1f;
				_preferences["music_arabesk"] += 0.2f;
				_checkWatchTimer *= 0.7f; // Check watch more often
			}
			// Factory workers
			else if (_occupation.Contains("Fabrika") || _occupation.Contains("Makinist"))
			{
				_preferences["drink_vodka"] += 0.1f;
				_preferences["music_fantezi"] += 0.1f;
				_stressLevel += 0.15f;
			}
			// Service industry
			else if (_occupation.Contains("Garson") || _occupation.Contains("Aşçı"))
			{
				_preferences["drink_beer"] += 0.15f;
				_generosity += 0.1f; // Service workers tend to tip better
				_alcoholTolerance += 0.15f;
			}
		}
		
		// Return worker-specific stats
		public new Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = base.GetStats();
			
			// Add worker-specific stats
			stats["IsRegular"] = _isRegular;
			stats["VisitCount"] = _visitCount;
			stats["Occupation"] = _occupation;
			stats["StressLevel"] = _stressLevel;
			stats["PayDay"] = _payDay;
			stats["Neighborhood"] = _neighborhood;
			stats["AlcoholTolerance"] = _alcoholTolerance;
			
			return stats;
		}
	}
}
