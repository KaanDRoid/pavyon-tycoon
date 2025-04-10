// Scripts/Characters/Customers/YoungCustomer.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	public partial class YoungCustomer : CustomerBase
	{
		// Social media addiction level (0.0-1.0)
		private float _socialMediaAddiction = 0.7f;
		
		// Trend following level (0.0-1.0)
		private float _trendFollowing = 0.8f;
		
		// Friend group influence level (0.0-1.0)
		private float _friendInfluence = 0.6f;
		
		// Budget consciousness level (0.0-1.0)
		private float _budgetConsciousness = 0.9f;
		
		// Is this young customer on a date?
		private bool _isOnDate = false;
		
		// Phrases specific to young customers
		private readonly string[] _youngPhrases = new string[]
		{
			"Abi bu mekanın Wi-Fi şifresi ne?",
			"Instagram'a story atmak için en iyi köşe neresi?",
			"Arkadaşlar birazdan gelecek, biraz yer ayırabilir miyiz?",
			"Buranın filtresi efsane olur ya!",
			"Spotify çalma listem tam bu ortama uyar aslında.",
			"TikTok'ta burayı görmüştüm, merak ettim.",
			"Bir kadeh daha içsem sonra Kadıköy'e geçsem...",
			"Abi fiyatlar biraz öğrenciye göre değil ya.",
			"Alalım bi' vodka kola, iki kişi içeriz.",
			"Kardeş bu masaya bir şişe soda alabilir miyiz?"
		};
		
		// Overriding the _Ready method to add specific initialization
		protected override void OnReady()
		{
			base.OnReady();
			
			// Specific initialization for young customers
			_socialMediaAddiction = 0.5f + GD.Randf() * 0.5f; // 0.5-1.0 range
			_trendFollowing = 0.6f + GD.Randf() * 0.4f; // 0.6-1.0 range
			_friendInfluence = 0.4f + GD.Randf() * 0.6f; // 0.4-1.0 range
			_budgetConsciousness = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0 range
			
			// 30% chance of being on a date
			_isOnDate = GD.Randf() < 0.3f;
			
			// Adjust stay time based on being on a date
			if (_isOnDate)
			{
				_maxStayTime *= 1.5f; // Stay longer on dates
			}
			
			// Young customers are more trend-following
			_signature = GetRandomYoungSignature();
		}
		
		// Override InitializePreferences to set specific preferences for young customers
		protected override void InitializePreferences()
		{
			base.InitializePreferences();
			
			// Young customers prefer modern music and energetic atmosphere
			_preferences["music_modern"] = 0.8f + GD.Randf() * 0.2f; // 0.8-1.0
			_preferences["music_fantezi"] = 0.7f + GD.Randf() * 0.2f; // 0.7-0.9
			_preferences["music_oyunHavasi"] = 0.5f + GD.Randf() * 0.4f; // 0.5-0.9
			
			// Less interest in traditional music
			_preferences["music_arabesk"] = 0.1f + GD.Randf() * 0.3f; // 0.1-0.4
			_preferences["music_taverna"] = 0.2f + GD.Randf() * 0.3f; // 0.2-0.5
			
			// Drink preferences
			_preferences["drink_vodka"] = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0
			_preferences["drink_beer"] = 0.6f + GD.Randf() * 0.3f; // 0.6-0.9
			_preferences["drink_special"] = 0.5f + GD.Randf() * 0.5f; // 0.5-1.0
			_preferences["drink_raki"] = 0.1f + GD.Randf() * 0.3f; // 0.1-0.4
			
			// Food preferences - prefer snacks and simple mezes
			_preferences["meze_hot"] = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0
			_preferences["meze_cold"] = 0.5f + GD.Randf() * 0.3f; // 0.5-0.8
			
			// Ambience preferences
			_preferences["ambiance_loud"] = 0.7f + GD.Randf() * 0.3f; // 0.7-1.0
			_preferences["ambiance_luxurious"] = 0.5f + GD.Randf() * 0.3f; // 0.5-0.8
			_preferences["ambiance_traditional"] = 0.1f + GD.Randf() * 0.3f; // 0.1-0.4
			
			// Staff preferences
			_preferences["staff_kons"] = _isOnDate ? 0.8f : 0.5f; // Higher if on date
		}
		
		// Override the UpdateStateMachine method to add special behavior
		protected override void UpdateStateMachine(float delta)
		{
			base.UpdateStateMachine(delta);
			
			// Check for phone usage (taking pictures, social media)
			if (_currentState == CustomerState.Sitting || 
				_currentState == CustomerState.WatchingShow)
			{
				CheckForPhoneUsage(delta);
			}
			
			// Budget checking - young customers check their wallets more often
			if ((_currentState == CustomerState.Sitting || 
				_currentState == CustomerState.OrderingDrink) && 
				_timeInPavyon > 30.0f && _remainingBudget < _budget * 0.3f)
			{
				if (GD.Randf() < _budgetConsciousness * 0.01f)
				{
					Say("Abi ben bütçeyi aşıyorum ya, biraz yavaşlayalım...");
					AdjustSatisfaction(-0.05f, "Budget concerns");
				}
			}
			
			// Friend influence - chance to order more due to peer pressure
			if (_currentState == CustomerState.Sitting && 
				_drinkCount > 0 && GD.Randf() < _friendInfluence * 0.005f)
			{
				// Order extra drinks due to friend influence
				ChangeState(CustomerState.OrderingDrink);
				Say("Hadi bir tane daha, arkadaşlar bekliyor!");
			}
		}
		
		// Special method for phone usage behavior
		private void CheckForPhoneUsage(float delta)
		{
			// Chance to use phone based on social media addiction
			if (GD.Randf() < _socialMediaAddiction * 0.01f)
			{
				// Choose a random phone-related action
				float actionRoll = GD.Randf();
				
				if (actionRoll < 0.4f)
				{
					// Taking pictures
					PlayAnimation("use_phone");
					Say("Şu an bu hikayem patlar!");
				}
				else if (actionRoll < 0.8f)
				{
					// Checking social media
					PlayAnimation("use_phone");
					// No speech, just checking phone
				}
				else
				{
					// Showing something to friends
					PlayAnimation("show_phone");
					Say("Bak şuna ya, inanılmaz!");
				}
			}
		}
		
		// Override dance behavior to make young customers more likely to dance
		protected override void UpdateDancingState(float delta)
		{
			// Young customers dance longer and with more energy
			if (_timeInStateBelonging > 180.0f || GD.Randf() < 0.01f) // 3 minutes max dance time
			{
				ChangeState(CustomerState.Sitting);
			}
			
			// 10% chance to take selfie while dancing
			if (GD.Randf() < 0.1f * _socialMediaAddiction)
			{
				PlayAnimation("dance_selfie");
				Say("Bu hikayem patlar!");
			}
		}
		
		// Get a random signature (appearance description) for young customers
		private string GetRandomYoungSignature()
		{
			string[] youngSignatures = new string[]
			{
				"Yırtık kot pantolon, vintage gömlek, saç şekillendirici",
				"Spor ayakkabı, oversize sweatshirt, piercing",
				"Retro stil, elektronik sigara, sürekli telefona bakmak",
				"Modern casual tarz, kulağında AirPods, tarz gözlükler",
				"Kot ceket, grafik t-shirt, renkli saç",
				"Trend parçalar, bileklik koleksiyonu, minimal makyaj",
				"Spor/casual karışımı, akıllı saat, sneaker tutkusu"
			};
			
			int index = (int)(GD.Randf() * youngSignatures.Length);
			return youngSignatures[index];
		}
		
		// Override to add special behavior for young customers leaving
		public override void PrepareToLeave()
		{
			base.PrepareToLeave();
			
			// 50% chance to check social media before leaving
			if (GD.Randf() < 0.5f)
			{
				Say("Son bir story atayım da gidelim.");
				PlayAnimation("use_phone");
			}
		}
		
		// Override for special drink behavior
		public override string OrderDrink()
		{
			string drink = base.OrderDrink();
			
			// Young customers may share drinks to save money
			if (_budgetConsciousness > 0.7f && GD.Randf() < 0.3f)
			{
				Say("Bunu arkadaşımla paylaşacağım.");
			}
			
			return drink;
		}
	}
}
