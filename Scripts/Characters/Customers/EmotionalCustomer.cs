// Scripts/Characters/Customers/EmotionalCustomer.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	public partial class EmotionalCustomer : SpecializedCustomerBase
	{
		// Emotional customer-specific traits
		private float _emotionalVolatility = 0.6f;     // How quickly emotions change (0-1)
		private float _melodramaChance = 0.3f;         // Chance to cause dramatic scenes (0-1)
		private float _attachmentLevel = 0.0f;         // Attachment to staff (0-1)
		private string _favoriteSongType = "Arabesk";  // Favorite music type
		private string _attachedStaffId = null;        // Staff they've become attached to
		
		// Emotional states
		private enum EmotionalState
		{
			Melancholic,
			Nostalgic,
			Euphoric,
			Desperate,
			Reflective
		}
		
		private EmotionalState _currentEmotionalState = EmotionalState.Melancholic;
		private float _emotionalStateTimer = 0.0f;
		private float _emotionalStateDuration = 180.0f; // 3 minutes per emotional state
		
		// List of melodramatic quotes in Turkish
		private string[] _melodramaticQuotes = new string[]
		{
			"Aşk bu dünyada en çaresiz olduğum şey...",
			"Benim kaderim böyle, çeken bilir...",
			"Her rakı kadehinde kendi hikayemi görüyorum...",
			"Sevdiğimi kaybettim, ne önemi var artık hiçbir şeyin?",
			"Ankara geceleri benim kalbim gibi soğuk ve kasvetli...",
			"Bir kadeh daha ver garson, acımı unutayım...",
			"Hayat bana neden bu kadar acımasız?",
			"Sensiz nefes almak bile zor geliyor...",
			"Gönlüm bir harabeye döndü, virane...",
			"Şerefine dostum, sen de anlarsın beni...",
			"Hayatım bir Müslüm Gürses şarkısı oldu..."
		};
		
		// Initialize
		public override void _Ready()
		{
			base._Ready();
			
			// Set customer type
			SetCustomerType(CustomerType.Emotional);
			
			// Set unique emotional traits
			_emotionalVolatility = 0.4f + GD.Randf() * 0.4f;  // 0.4-0.8 range
			_melodramaChance = 0.2f + GD.Randf() * 0.3f;      // 0.2-0.5 range
			
			// Set initial emotional state
			RandomizeEmotionalState();
		}

		protected override void InitializeTypePreferences()
		{
			// Strong preference for Arabesk music
			_preferences["music_arabesk"] = 0.9f;
			_preferences["music_taverna"] = 0.6f;
			_preferences["music_fantezi"] = 0.4f;
			
			// Drink preferences
			_preferences["drink_raki"] = 0.8f;       // Strongly prefers rakı
			_preferences["drink_beer"] = 0.3f;
			_preferences["drink_wine"] = 0.5f;
			
			// Prefers intimate settings
			_preferences["ambiance_intimate"] = 0.8f;
			_preferences["ambiance_loud"] = 0.2f;
			
			// Likely to form attachment to staff
			_preferences["staff_kons"] = 0.7f;
			_preferences["staff_musician"] = 0.6f;
		}

		public override void _Process(double delta)
		{
			base._Process(delta);
			
			// Update emotional state timer
			_emotionalStateTimer += (float)delta;
			
			// Change emotional state when timer expires
			if (_emotionalStateTimer >= _emotionalStateDuration)
			{
				RandomizeEmotionalState();
				_emotionalStateTimer = 0.0f;
			}
			
			// Process current emotional state
			ProcessEmotionalState((float)delta);
		}

		protected override void UpdateSittingState(float delta)
		{
			base.UpdateSittingState(delta);
			
			// Chance to say melodramatic quotes while sitting
			if (GD.Randf() < _melodramaChance * delta * 0.1f)
			{
				SayRandomMelodramaticQuote();
			}
			
			// Chance to become more attached to attending staff
			if (_assignedKonsId != null && _attachedStaffId == null && GD.Randf() < 0.05f * delta)
			{
				_attachedStaffId = _assignedKonsId;
				_attachmentLevel = 0.2f;
				GD.Print($"Emotional customer {FullName} is becoming attached to staff {_assignedKonsId}");
			}
			
			// If attached to staff, attachment grows over time
			if (_attachedStaffId != null)
			{
				_attachmentLevel = Mathf.Min(1.0f, _attachmentLevel + 0.01f * delta);
				
				// Very attached customers may cause problems
				if (_attachmentLevel > 0.8f && GD.Randf() < 0.01f * delta)
				{
					TriggerAttachmentEvent();
				}
			}
			
			// Music affects emotional customers much more
			CheckMusicEffect();
		}
		
		// Implement the specialized behavior for emotional customers
		protected override void PerformSpecialBehavior(float delta)
		{
			// Special behavior: Emotional outburst
			float outburstChance = _emotionalVolatility * (1.0f - _satisfaction);
			
			if (GD.Randf() < outburstChance)
			{
				// Determine type of outburst based on current emotional state
				string outburstType = "tears"; // Default
				
				switch (_currentEmotionalState)
				{
					case EmotionalState.Melancholic:
						outburstType = "tears";
						break;
					case EmotionalState.Nostalgic:
						outburstType = "story";
						break;
					case EmotionalState.Euphoric:
						outburstType = "toast";
						break;
					case EmotionalState.Desperate:
						outburstType = "breakdown";
						break;
					case EmotionalState.Reflective:
						outburstType = "philosophy";
						break;
				}
				
				PerformEmotionalOutburst(outburstType);
			}
		}
		
		// Handle different types of emotional outbursts
		private void PerformEmotionalOutburst(string outburstType)
		{
			// Set animation and perform action based on outburst type
			switch (outburstType)
			{
				case "tears":
					PlayAnimation("crying");
					Say("*Gözyaşlarına boğulur*");
					InfluenceNearbyCustomers("mood", 3.0f);
					
					// Staff interaction needed
					RequestStaffAssistance("emotional_support");
					break;
					
				case "story":
					PlayAnimation("talk_emotional");
					Say("Eskiden Ankara'da geceler böyle değildi...");
					
					// Tell a nostalgic story about old Ankara
					TryTriggerEvent("nostalgic_story");
					break;
					
				case "toast":
					PlayAnimation("drink_emotional");
					Say("Haydi içelim! Hayat kısa, dertler uzun!");
					
					// Encourage others to drink
					InfluenceNearbyCustomers("spending", 4.0f);
					break;
					
				case "breakdown":
					PlayAnimation("breakdown");
					Say("Artık dayanamıyorum! Kimse beni anlamıyor!");
					
					// Might require security intervention
					if (_drunkennessLevel > 0.7f)
					{
						RequestSecurityIntervention("emotional_breakdown");
					}
					break;
					
				case "philosophy":
					PlayAnimation("contemplative");
					Say("Hayat bir Ankara gecesi gibi... Soğuk ama aynı zamanda sıcak...");
					
					// Deep conversation
					TryTriggerEvent("philosophical_moment");
					break;
			}
			
			// Adjust customer's own satisfaction
			if (_drunkennessLevel > 0.6f)
			{
				// When drunk, outbursts actually improve satisfaction
				AdjustSatisfaction(0.05f, "Emotional release");
			}
			else
			{
				// When sober, they might feel embarrassed after
				AdjustSatisfaction(-0.05f, "Emotional embarrassment");
			}
			
			GD.Print($"Emotional customer {FullName} had a {outburstType} outburst");
		}
		
		// Process the current emotional state
		private void ProcessEmotionalState(float delta)
		{
			// Each emotional state has different effects on customer behavior
			switch (_currentEmotionalState)
			{
				case EmotionalState.Melancholic:
					// Slower drinking, less spending
					if (_drinkCount > 0 && GD.Randf() < 0.05f * delta)
					{
						// Stare at drink
						PlayAnimation("contemplate_drink");
					}
					break;
					
				case EmotionalState.Nostalgic:
					// More likely to talk about the past
					if (GD.Randf() < 0.1f * delta)
					{
						// Tell nostalgic story
						SayRandomNostalgicQuote();
					}
					break;
					
				case EmotionalState.Euphoric:
					// Faster drinking, more spending
					// More likely to try dancing
					if (_currentState == CustomerState.Sitting && GD.Randf() < 0.15f * delta)
					{
						ChangeState(CustomerState.Dancing);
					}
					break;
					
				case EmotionalState.Desperate:
					// Much more drinking
					if (_currentState == CustomerState.Sitting && GD.Randf() < 0.2f * delta)
					{
						ChangeState(CustomerState.OrderingDrink);
					}
					break;
					
				case EmotionalState.Reflective:
					// More likely to watch performances quietly
					if (IsShowActive() && GD.Randf() < 0.3f * delta)
					{
						ChangeState(CustomerState.WatchingShow);
					}
					break;
			}
		}
		
		// Randomize the emotional state
		private void RandomizeEmotionalState()
		{
			Array states = Enum.GetValues(typeof(EmotionalState));
			_currentEmotionalState = (EmotionalState)states.GetValue(GD.RandRange(0, states.Length - 1));
			
			// Generate a random duration for this state (2-5 minutes)
			_emotionalStateDuration = 120.0f + GD.Randf() * 180.0f;
			
			// Extra randomness based on drunkenness - more drunk means more volatile emotions
			if (_drunkennessLevel > 0.5f)
			{
				_emotionalStateDuration *= (1.0f - (_drunkennessLevel - 0.5f));
			}
			
			GD.Print($"Emotional customer {FullName} changed to {_currentEmotionalState} state for {_emotionalStateDuration/60} minutes");
			
			// React to the new emotional state
			ReactToEmotionalStateChange();
		}
		
		// React when emotional state changes
		private void ReactToEmotionalStateChange()
		{
			switch (_currentEmotionalState)
			{
				case EmotionalState.Melancholic:
					PlayAnimation("become_sad");
					Say("*İç çeker*");
					break;
					
				case EmotionalState.Nostalgic:
					PlayAnimation("reminisce");
					Say("Ah, eski günler...");
					break;
					
				case EmotionalState.Euphoric:
					PlayAnimation("become_happy");
					Say("Hayat güzel be!");
					break;
					
				case EmotionalState.Desperate:
					PlayAnimation("desperate");
					Say("Bir kadeh daha ver!");
					break;
					
				case EmotionalState.Reflective:
					PlayAnimation("thoughtful");
					Say("*Derin düşüncelere dalar*");
					break;
			}
		}
		
		// Say a random melodramatic quote
		private void SayRandomMelodramaticQuote()
		{
			int index = (int)(GD.Randf() * _melodramaticQuotes.Length);
			Say(_melodramaticQuotes[index]);
		}
		
		// Check if current music affects this customer
		private void CheckMusicEffect()
		{
			// Simulation of music effect - would interface with MusicManager
			string currentMusic = GetCurrentMusicType();
			
			if (currentMusic == _favoriteSongType)
			{
				// Favorite music has strong effect on emotional customers
				AdjustSatisfaction(0.05f, "Favorite music");
				
				// Chance for special reaction
				if (GD.Randf() < 0.2f)
				{
					PlayAnimation("emotional_music_reaction");
					Say("Bu şarkı beni derinden etkiliyor...");
				}
				
				// Arabesk might make them more melancholic
				if (_currentEmotionalState != EmotionalState.Melancholic && GD.Randf() < 0.3f)
				{
					_currentEmotionalState = EmotionalState.Melancholic;
					ReactToEmotionalStateChange();
				}
			}
		}
		
		// Get current music from music manager
		private string GetCurrentMusicType()
		{
			// In real implementation, this would get current music from MusicManager
			// For now return a placeholder
			if (GetTree().Root.HasNode("GameManager/MusicManager"))
			{
				var musicManager = GetTree().Root.GetNode("GameManager/MusicManager");
				
				if (musicManager.HasMethod("GetCurrentMusicType"))
				{
					return (string)musicManager.Call("GetCurrentMusicType");
				}
			}
			
			// Default music type
			return "Arabesk";
		}
		
		// Request staff assistance for emotional moment
		private void RequestStaffAssistance(string assistanceType)
		{
			if (GetTree().Root.HasNode("GameManager/StaffManager"))
			{
				var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
				
				if (staffManager.HasMethod("RequestStaffAssistance"))
				{
					staffManager.Call("RequestStaffAssistance", 
						CustomerId, 
						Position, 
						assistanceType,
						_attachedStaffId
					);
				}
			}
		}
		
		// Trigger an event related to attachment
		private void TriggerAttachmentEvent()
		{
			// Different attachment events based on customer state
			string eventType = "regular_attachment";
			
			if (_drunkennessLevel > 0.7f)
			{
				eventType = "drunk_attachment";
			}
			
			if (_currentEmotionalState == EmotionalState.Desperate)
			{
				eventType = "desperate_attachment";
			}
			
			// Try to trigger the event
			bool eventTriggered = TryTriggerEvent(eventType);
			
			if (eventTriggered)
			{
				GD.Print($"Emotional customer {FullName} triggered {eventType} event with staff {_attachedStaffId}");
			}
		}
		
		// Say a random nostalgic quote about old Ankara
		private void SayRandomNostalgicQuote()
		{
			string[] nostalgicQuotes = new string[]
			{
				"Eskiden bu Maltepe'nin pavyonları bambaşkaydı...",
				"Bir zamanlar Ankara geceleri daha saygındı...",
				"Gençliğimde bu sokaklarda başka türlü eğlenirdik...",
				"O eski Angaralı abiler nerede şimdi?",
				"Ulus'un eski halini bir bilseydin...",
				"Kızılay'ın eski günleri böyle değildi...",
				"Eskiden bu sokaklar daha bir başkaydı..."
			};
			
			int index = (int)(GD.Randf() * nostalgicQuotes.Length);
			Say(nostalgicQuotes[index]);
		}
		
		// Override bahşiş verme - emotional customers can give extreme tips
		protected new void GiveTip(float amount, string staffId)
		{
			// Check if this is the attached staff
			if (staffId == _attachedStaffId)
			{
				// Attached staff gets much higher tips
				amount *= (1.0f + _attachmentLevel);
			}
			
			// Emotional state affects tip amount
			switch (_currentEmotionalState)
			{
				case EmotionalState.Euphoric:
					amount *= 1.5f;  // Much higher tip when euphoric
					break;
				case EmotionalState.Desperate:
					amount *= 1.3f;  // Higher tip when desperate
					break;
				case EmotionalState.Melancholic:
					amount *= 0.8f;  // Lower tip when sad
					break;
			}
			
			// Use base method with modified amount
			base.GiveTip(amount, staffId);
		}
	}
}
