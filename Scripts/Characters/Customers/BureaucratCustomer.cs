// Scripts/Characters/Customers/BureaucratCustomer.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	public partial class BureaucratCustomer : CustomerBase
	{
		// Level of connection to government (0.0-1.0)
		private float _governmentConnection = 0.7f;
		
		// Corruption level (0.0-1.0) - willingness to engage in corrupt activities
		private float _corruptionLevel = 0.6f;
		
		// Need for discretion (0.0-1.0) - how much they value privacy
		private float _discretionNeed = 0.8f;
		
		// Status consciousness (0.0-1.0) - how much they care about appearing important
		private float _statusConsciousness = 0.75f;
		
		// Has this bureaucrat been drinking on government money?
		private bool _usingGovernmentFunds = false;
		
		// Is this bureaucrat conducting "unofficial business"?
		private bool _inUnofficialBusiness = false;
		
		// Chance to be a high-ranking bureaucrat (affects behavior and spending)
		private bool _isHighRanking = false;
		
		// Phrases specific to bureaucrat customers
		private readonly string[] _bureaucratPhrases = new string[]
		{
			"Bakanlıkta yarın önemli toplantı var.",
			"Şu dosyayı bir halledersek, sizin işinizi de çözeriz.",
			"Bizim genel müdürle konuşurum, merak etmeyin.",
			"Burada konuşulmasın, masraflar devletten.",
			"Ben de memur adamım sonuçta.",
			"Valiye selam söyleyin benden.",
			"Böyle şeyler telefonla konuşulmaz, yüz yüze görüşelim.",
			"Resmî kanallardan olmaz, özel bir çözüm bulalım.",
			"Bizim müsteşarla bir ara tanıştırayım sizi.",
			"Maliye denetiminde kontrol ettik, temiz."
		};
		
		// Overriding the _Ready method to add specific initialization
		protected override void OnReady()
		{
			base.OnReady();
			
			// Initialize bureaucrat-specific properties
			_governmentConnection = 0.5f + GD.Randf() * 0.5f; // 0.5-1.0 range
			_corruptionLevel = 0.4f + GD.Randf() * 0.6f; // 0.4-1.0 range
			_discretionNeed = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0 range
			_statusConsciousness = 0.6f + GD.Randf() * 0.4f; // 0.6-1.0 range
			
			// 40% chance of using government funds
			_usingGovernmentFunds = GD.Randf() < 0.4f;
			
			// 30% chance of being in unofficial business
			_inUnofficialBusiness = GD.Randf() < 0.3f;
			
			// 20% chance of being high-ranking
			_isHighRanking = GD.Randf() < 0.2f;
			
			// Adjust budget based on rank and corruption
			if (_isHighRanking)
			{
				_budget *= 1.5f;
				_signature = "Pahalı takım elbise, altın kol düğmeleri, diplomatik duruş";
			}
			else
			{
				_signature = "Orta sınıf takım elbise, deri ayakkabılar, resmi tavır";
			}
			
			// If using government funds, be more generous
			if (_usingGovernmentFunds)
			{
				_generosity = Mathf.Min(1.0f, _generosity + 0.2f);
			}
		}
		
		// Override InitializePreferences to set specific preferences for bureaucrat customers
		protected override void InitializePreferences()
		{
			base.InitializePreferences();
			
			// Bureaucrats prefer traditional and sophisticated atmosphere
			_preferences["ambiance_intimate"] = 0.8f + GD.Randf() * 0.2f; // 0.8-1.0
			_preferences["ambiance_luxurious"] = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0
			
			// Music preferences
			_preferences["music_taverna"] = 0.6f + GD.Randf() * 0.4f; // 0.6-1.0
			_preferences["music_arabesk"] = 0.4f + GD.Randf() * 0.4f; // 0.4-0.8
			_preferences["music_modern"] = _isHighRanking ? 0.6f : 0.3f; // Higher for high-ranking
			
			// Drink preferences - favor quality drinks
			_preferences["drink_raki"] = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0
			_preferences["drink_whiskey"] = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0
			_preferences["drink_wine"] = 0.5f + GD.Randf() * 0.3f; // 0.5-0.8
			
			// Food preferences - favor traditional and sophisticated mezes
			_preferences["meze_cold"] = 0.6f + GD.Randf() * 0.4f; // 0.6-1.0
			_preferences["meze_seafood"] = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0
			
			// Staff preferences - highly value discretion and special treatment
			_preferences["staff_kons"] = 0.8f + GD.Randf() * 0.2f; // 0.8-1.0
			_preferences["staff_waiter"] = 0.7f + GD.Randf() * 0.2f; // 0.7-0.9
		}
		
		// Override the UpdateStateMachine method to add special behavior
		protected override void UpdateStateMachine(float delta)
		{
			base.UpdateStateMachine(delta);
			
			// Looking around for potential problems
			if (_currentState == CustomerState.Sitting || 
				_currentState == CustomerState.TalkingToKons)
			{
				CheckForSuspiciousActivity(delta);
			}
			
			// Handling unofficial business
			if (_inUnofficialBusiness && _currentState == CustomerState.Sitting &&
				_timeInPavyon > 30.0f && GD.Randf() < 0.005f)
			{
				HandleUnofficialBusiness();
			}
			
			// Status consciousness behavior - complaining about service
			if (_statusConsciousness > 0.7f && 
				(_currentState == CustomerState.OrderingDrink || _currentState == CustomerState.OrderingFood) &&
				GD.Randf() < 0.03f)
			{
				// Make demanding requests based on status
				MakeDemandingRequest();
			}
		}
		
		// Method for checking suspicious activity
		private void CheckForSuspiciousActivity(float delta)
		{
			// Higher chance to look around based on discretion need
			if (GD.Randf() < _discretionNeed * 0.01f)
			{
				PlayAnimation("look_around");
				
				if (GD.Randf() < 0.2f)
				{
					// Sometimes comment on being careful
					if (_inUnofficialBusiness)
					{
						Say("Burada rahat konuşabiliriz, değil mi?", 1.5f);
					}
					else if (GD.Randf() < 0.5f)
					{
						Say("Tanıdık yüzler görmediğime sevindim.", 1.5f);
					}
				}
			}
			
			// Recognize other bureaucrats or undercover police
			if (GD.Randf() < _governmentConnection * 0.005f)
			{
				// In a real implementation, this would check nearby customers
				// For now, we'll just simulate this behavior
				if (GD.Randf() < 0.3f)
				{
					Say("Şu köşedeki adam bizim bakanlıktan değil mi?", 2.0f);
					PlayAnimation("point_discrete");
				}
			}
		}
		
		// Method for handling unofficial business
		private void HandleUnofficialBusiness()
		{
			// Choose a random unofficial business action
			float actionRoll = GD.Randf();
			
			if (actionRoll < 0.3f)
			{
				// Discreet conversation
				PlayAnimation("talk_quiet");
				Say("Bu dosyayı halledebiliriz. Detayları sonra konuşuruz.", 2.0f);
			}
			else if (actionRoll < 0.6f)
			{
				// Document exchange
				PlayAnimation("exchange_item");
				
				// This could trigger a mini-event in the actual game
				GD.Print($"Bureaucrat {FullName} attempted to exchange documents or money - potential mini-event trigger");
			}
			else
			{
				// Phone call
				PlayAnimation("phone_call");
				Say("Evet, şimdi oradayım. Hallediyorum.", 2.0f);
			}
			
			// Add event to history
			_eventHistory.Add("unofficial_business");
		}
		
		// Method for making demanding requests
		private void MakeDemandingRequest()
		{
			// Different types of demands based on status
			string[] demands = {
				"Benim masama özel ilgi gösterilsin!",
				"Rakılarınızın en iyisini istiyorum.",
				"Bizim masanın manzarası iyi değil, değiştirebilir miyiz?",
				"Garson biraz daha hızlı olabilir mi? Bekletilmeyi sevmem.",
				"Müziği biraz kısabilir misiniz? Önemli bir konuyu konuşuyoruz."
			};
			
			int index = (int)(GD.Randf() * demands.Length);
			Say(demands[index]);
			
			// Higher status consciousness means stronger reaction to service
			if (_satisfaction < 0.7f)
			{
				float dissatisfactionFactor = _statusConsciousness * 0.1f;
				AdjustSatisfaction(-dissatisfactionFactor, "Status expectations not met");
			}
		}
		
		// Override to add special behavior for paying
		protected override void ProcessPayment()
		{
			// Before payment, check if using government funds
			if (_usingGovernmentFunds)
			{
				// Try to get a receipt for "business meeting"
				Say("Makbuzu şirket adına alabilir miyim? İş toplantısıydı.");
				
				// In the actual game, this could trigger a mini-event or affect staff morals
				GD.Print($"Bureaucrat {FullName} tried to expense the night as a business meeting");
			}
			
			base.ProcessPayment();
			
			// Higher tip if trying to establish connections
			if (_corruptionLevel > 0.6f && _remainingBudget > _budget * 0.3f)
			{
				float extraTip = _totalSpent * 0.1f * _corruptionLevel;
				
				if (_remainingBudget >= extraTip)
				{
					// Give extra tip to establish connection
					GiveTip(extraTip, _assignedKonsId ?? _interactedStaffIds[0]);
					Say("Kartımı bırakıyorum. Herhangi bir durumda arayabilirsiniz.");
				}
			}
		}
		
		// Override for special behavior when ordering drinks
		public override string OrderDrink()
		{
			string drink = base.OrderDrink();
			
			// High-ranking bureaucrats may be more demanding
			if (_isHighRanking && GD.Randf() < 0.4f)
			{
				if (drink == "Rakı")
				{
					Say("Yeni rakı olsun, yıllanmış varsa daha iyi.");
				}
				else if (drink == "Viski")
				{
					Say("Single malt olsun lütfen.");
				}
			}
			
			return drink;
		}
		
		// Override for special behavior when talking to kons
		protected override void UpdateTalkingToKonsState(float delta)
		{
			// Stay longer with the kons
			if (_timeInStateBelonging > 400.0f || GD.Randf() < 0.008f) // 6.6 minutes
			{
				ChangeState(CustomerState.Sitting);
			}
			
			// Special conversation topics with kons
			if (GD.Randf() < 0.01f)
			{
				int phraseIndex = (int)(GD.Randf() * _bureaucratPhrases.Length);
				Say(_bureaucratPhrases[phraseIndex], 2.0f);
			}
			
			// Sometimes slip business cards or contacts
			if (_corruptionLevel > 0.5f && GD.Randf() < 0.01f)
			{
				PlayAnimation("give_card");
				Say("İşte kartvizitim. İhtiyacın olursa ara.");
			}
		}
	}
}
