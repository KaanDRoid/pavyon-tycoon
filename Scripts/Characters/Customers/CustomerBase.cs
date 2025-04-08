// Scripts/Characters/Customers/CustomerBase.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	public partial class CustomerBase : Node3D
	{
		// Temel müşteri kimlik bilgileri
		public string CustomerId { get; protected set; }
		public string FullName { get; set; }
		public int Age { get; set; }
		public string Gender { get; set; }
		
		// Müşteri tipi (Worker, Elite, Nostalgic vb)
		public enum CustomerType
		{
			Regular,     // Sıradan müşteri
			Worker,      // İşçi 
			Elite,       // Zengin/Elit müşteri
			Nostalgic,   // Nostaljik müşteri (Eski Ankara'yı özleyen)
			Emotional,   // Duygusal müşteri (Kederli, dertli)
			Young,       // Genç müşteri (20-30 yaş arası)
			Bureaucrat,  // Bürokrat (Devlet memuru)
			UnderCover,  // Sivil polis
			Sapkali,     // Şapkalı (tarlasını satıp pavyonda harcayan çiftçi)
			Gangster,    // Kabadayı/Mafya tipli
			Foreigner    // Yabancı müşteri
		}
		
		// Müşteri davranış durumları
		public enum CustomerState
		{
			Entering,       // Pavyona giriş yapıyor
			WaitingToSit,   // Oturacak yer arıyor
			Sitting,        // Oturuyor
			OrderingDrink,  // İçki sipariş ediyor
			OrderingFood,   // Yemek/meze sipariş ediyor
			WatchingShow,   // Sahne gösterisini izliyor
			TalkingToKons,  // Konsla sohbet ediyor
			Dancing,        // Dans ediyor
			UsingBathroom,  // Tuvaleti kullanıyor
			Leaving,        // Ayrılıyor
			SpecialEvent    // Özel durum/olay (kavga, vb.)
		}
		
		// Müşteri özellikleri ve durumu (0.0-1.0 arası)
		protected float _budget;             // Harcayabileceği toplam para
		protected float _remainingBudget;    // Kalan para
		protected float _generosity;         // Cömertlik (bahşiş verme eğilimi)
		protected float _drunkennessLevel;   // Sarhoşluk seviyesi
		protected float _satisfaction;       // Memnuniyet
		protected float _aggressionLevel;    // Agresiflik seviyesi
		protected float _loyaltyLevel;       // Sadakat (düzenli gelme eğilimi)
		protected float _kargaLevel;         // Karga durumu (hesap ödememek için kaçma eğilimi)
		
		// Özel tercihler (0.0-1.0 arası)
		protected Dictionary<string, float> _preferences = new Dictionary<string, float>();
		
		// Pavyon içindeki hedef ve yollar
		protected Vector3 _targetPosition;
		protected Vector3 _tablePosition;    // Oturduğu masa
		protected Vector3 _bathroomPosition; // Tuvalet konumu
		protected Vector3 _exitPosition;     // Çıkış konumu
		protected float _moveSpeed = 2.0f;   // Yürüme hızı
		protected bool _isMoving = false;
		
		// Sipariş ve harcama takibi
		protected int _drinkCount = 0;           // İçilen içki sayısı
		protected int _mezeCount = 0;            // Yenilen meze sayısı
		protected float _totalSpent = 0.0f;      // Toplam harcama
		protected float _tipAmount = 0.0f;       // Verilen bahşiş
		protected Dictionary<string, int> _orderHistory = new Dictionary<string, int>();
		
		// Etkileşim verileri
		protected string _assignedKonsId;    // Atanan kons ID'si
		protected List<string> _interactedStaffIds = new List<string>(); // Etkileşimde bulunulan personel
		protected float _timeInPavyon = 0.0f; // Pavyonda geçirilen süre (dakika)
		protected float _maxStayTime = 180.0f; // Maksimum kalma süresi (dakika)
		
		// Etkileşim ve olay geçmişi
		protected List<string> _eventHistory = new List<string>();
		
		// VIP özelliği
		public bool IsVIP { get; set; } = false;
		
		// Mevcut durum
		protected CustomerState _currentState = CustomerState.Entering;
		protected CustomerType _customerType = CustomerType.Regular;
		
		// Visuals and animations
		protected AnimationPlayer _animationPlayer;
		protected Node3D _model;
		
		// Konuşma balonları ve UI
		protected Control _speechBubble;
		protected Label _speechLabel;
		
		// İmza olarak kullanılacak şapka, takım elbise vb.
		protected string _signature;
		
		// Signals
		[Signal]
		public delegate void StateChangedEventHandler(int previousState, int newState);
		
		[Signal]
		public delegate void SatisfactionChangedEventHandler(float previousValue, float newValue);
		
		[Signal]
		public delegate void DrunkennessChangedEventHandler(float previousValue, float newValue);
		
		[Signal]
		public delegate void CustomerSpentMoneyEventHandler(float amount, string category);
		
		[Signal]
		public delegate void CustomerLeftEventHandler(string customerId, float totalSpent, float satisfaction);
		
		public CustomerBase()
		{
			CustomerId = Guid.NewGuid().ToString();
		}
		
		// Parametreli constructor
		public CustomerBase(string fullName, int age, string gender, CustomerType type, float budget)
		{
			CustomerId = Guid.NewGuid().ToString();
			FullName = fullName;
			Age = age;
			Gender = gender;
			_customerType = type;
			_budget = budget;
			_remainingBudget = budget;
			
			// Tercihleri başlat
			InitializePreferences();
			
			// Müşteri tipine göre başlangıç değerlerini ayarla
			InitializeByCustomerType();
		}
		
		public override void _Ready()
		{
			// Animation player ve model
			if (HasNode("AnimationPlayer"))
				_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
			
			if (HasNode("CustomerModel"))
				_model = GetNode<Node3D>("CustomerModel");
			
			// Konuşma balonu
			if (HasNode("SpeechBubble"))
			{
				_speechBubble = GetNode<Control>("SpeechBubble");
				if (_speechBubble.HasNode("SpeechLabel"))
					_speechLabel = _speechBubble.GetNode<Label>("SpeechLabel");
				
				_speechBubble.Visible = false;
			}
			
			// Alt sınıflar için hazırlık metodu
			OnReady();
		}
		
		// Alt sınıflar için hazırlık metodu
		protected virtual void OnReady() { }
		
		public override void _Process(double delta)
		{
			// Hareket güncelleme
			if (_isMoving)
			{
				ProcessMovement((float)delta);
			}
			
			// Durum makinesini güncelle
			UpdateStateMachine((float)delta);
			
			// Sarhoşluk etkisi (yürüyüş vb.)
			if (_drunkennessLevel > 0.5f)
			{
				ProcessDrunkennessEffects((float)delta);
			}
			
			// Pavyonda geçirilen süreyi güncelle
			_timeInPavyon += (float)delta * 60.0f;  // Saniyeyi dakikaya çevir
			
			// Kalma süresi kontrolü
			if (_timeInPavyon >= _maxStayTime && _currentState != CustomerState.Leaving)
			{
				PrepareToLeave();
			}
		}
		
		// Tercihleri başlat
		protected virtual void InitializePreferences()
		{
			// Müzik tercihleri
			_preferences["music_arabesk"] = 0.5f;
			_preferences["music_taverna"] = 0.5f;
			_preferences["music_fantezi"] = 0.5f;
			_preferences["music_oyunHavasi"] = 0.5f;
			_preferences["music_modern"] = 0.5f;
			
			// İçki tercihleri
			_preferences["drink_raki"] = 0.5f;
			_preferences["drink_beer"] = 0.5f;
			_preferences["drink_wine"] = 0.5f;
			_preferences["drink_whiskey"] = 0.5f;
			_preferences["drink_vodka"] = 0.5f;
			_preferences["drink_special"] = 0.5f;
			
			// Meze tercihleri
			_preferences["meze_cold"] = 0.5f;    // Soğuk mezeler
			_preferences["meze_hot"] = 0.5f;     // Sıcak mezeler
			_preferences["meze_seafood"] = 0.5f; // Deniz ürünleri
			_preferences["meze_meats"] = 0.5f;   // Et ürünleri
			
			// Personel tercihleri
			_preferences["staff_kons"] = 0.5f;
			_preferences["staff_waiter"] = 0.5f;
			_preferences["staff_musician"] = 0.5f;
			
			// Ambiyans tercihleri
			_preferences["ambiance_loud"] = 0.5f;
			_preferences["ambiance_intimate"] = 0.5f;
			_preferences["ambiance_luxurious"] = 0.5f;
			_preferences["ambiance_traditional"] = 0.5f;
		}
		
		// Müşteri tipine göre değerleri ayarla
		protected virtual void InitializeByCustomerType()
		{
			// Başlangıç değerleri
			_generosity = 0.5f;
			_drunkennessLevel = 0.0f;
			_satisfaction = 0.7f;  // Başlangıçta biraz memnun
			_aggressionLevel = 0.1f;
			_loyaltyLevel = 0.5f;
			_kargaLevel = 0.1f;
			
			// Müşteri tipine göre ayarlamalar
			switch (_customerType)
			{
				case CustomerType.Worker:
					_budget = Mathf.Clamp(_budget, 500, 2000);
					_generosity = 0.4f;
					_preferences["music_arabesk"] = 0.8f;
					_preferences["music_taverna"] = 0.7f;
					_preferences["drink_raki"] = 0.6f;
					_preferences["drink_beer"] = 0.8f;
					_maxStayTime = 120.0f;  // 2 saat
					_signature = "Eskimiş takım elbise, yorgun gözler";
					break;
					
				case CustomerType.Elite:
					_budget = Mathf.Clamp(_budget, 5000, 20000);
					_generosity = 0.7f;
					_preferences["music_modern"] = 0.8f;
					_preferences["drink_whiskey"] = 0.8f;
					_preferences["drink_special"] = 0.9f;
					_preferences["ambiance_luxurious"] = 0.9f;
					_maxStayTime = 240.0f;  // 4 saat
					_signature = "Pahalı takım elbise, rolex saat";
					IsVIP = true;
					break;
					
				case CustomerType.Nostalgic:
					_budget = Mathf.Clamp(_budget, 2000, 5000);
					_generosity = 0.6f;
					_preferences["music_taverna"] = 0.9f;
					_preferences["drink_raki"] = 0.9f;
					_preferences["meze_cold"] = 0.8f;
					_preferences["ambiance_traditional"] = 0.9f;
					_maxStayTime = 300.0f;  // 5 saat
					_signature = "Bıyık, vintage gömlek, rakıya düşkün";
					break;
					
				case CustomerType.Emotional:
					_budget = Mathf.Clamp(_budget, 1000, 4000);
					_generosity = 0.5f;
					_preferences["music_arabesk"] = 0.9f;
					_preferences["drink_raki"] = 0.8f;
					_maxStayTime = 360.0f;  // 6 saat (uzun süre kalır)
					_signature = "Dağınık saçlar, kırmızı gözler, melankolik tavır";
					break;
					
				case CustomerType.Young:
					_budget = Mathf.Clamp(_budget, 500, 3000);
					_generosity = 0.4f;
					_preferences["music_fantezi"] = 0.8f;
					_preferences["music_modern"] = 0.7f;
					_preferences["drink_vodka"] = 0.7f;
					_preferences["drink_beer"] = 0.7f;
					_maxStayTime = 180.0f;  // 3 saat
					_signature = "Tarz kıyafetler, telefonla sürekli ilgilenmek";
					break;
					
				case CustomerType.Bureaucrat:
					_budget = Mathf.Clamp(_budget, 3000, 8000);
					_generosity = 0.6f;
					_preferences["ambiance_intimate"] = 0.8f;
					_preferences["drink_whiskey"] = 0.7f;
					_preferences["drink_raki"] = 0.6f;
					_maxStayTime = 180.0f;  // 3 saat
					_signature = "Resmi giyim, dikkatli bakışlar, sürekli etrafı süzmek";
					break;
					
				case CustomerType.UnderCover:
					_budget = Mathf.Clamp(_budget, 1000, 3000);
					_generosity = 0.3f;
					_preferences["ambiance_intimate"] = 0.7f;
					_drunkennessLevel = 0.0f; // Az içki içer
					_maxStayTime = 240.0f;  // 4 saat (gözlem yapmak için)
					_signature = "Deri ceket, disiplinli duruş, az içki tüketimi";
					break;
					
				case CustomerType.Sapkali:
					_budget = Mathf.Clamp(_budget, 10000, 50000); // Tarlayı satmış!
					_generosity = 0.9f; // Çok cömert
					_preferences["music_oyunHavasi"] = 0.9f;
					_preferences["music_fantezi"] = 0.8f;
					_preferences["staff_kons"] = 0.9f;
					_maxStayTime = 480.0f;  // 8 saat (tüm parayı bitirene kadar)
					_signature = "Şapka, kırsal giyim tarzı, bol para harcama";
					break;
					
				case CustomerType.Gangster:
					_budget = Mathf.Clamp(_budget, 5000, 15000);
					_generosity = 0.5f;
					_aggressionLevel = 0.6f; // Daha agresif
					_preferences["music_arabesk"] = 0.7f;
					_preferences["drink_raki"] = 0.7f;
					_maxStayTime = 240.0f;  // 4 saat
					_signature = "Altın zincir, kabadayı duruşu, silah çıkarma tehdidi";
					break;
					
				case CustomerType.Foreigner:
					_budget = Mathf.Clamp(_budget, 3000, 10000);
					_generosity = 0.8f;
					_preferences["ambiance_traditional"] = 0.8f; // Yerel deneyim için
					_maxStayTime = 180.0f;  // 3 saat
					_signature = "Turist kıyafetleri, her şeyi merakla izlemek";
					break;
					
				default: // Regular
					_budget = Mathf.Clamp(_budget, 1000, 3000);
					_generosity = 0.5f;
					_maxStayTime = 180.0f;  // 3 saat
					_signature = "Standart kıyafet";
					break;
			}
			
			_remainingBudget = _budget;
		}
		
		// Durum makinesini güncelle
		protected virtual void UpdateStateMachine(float delta)
		{
			switch (_currentState)
			{
				case CustomerState.Entering:
					UpdateEnteringState(delta);
					break;
					
				case CustomerState.WaitingToSit:
					UpdateWaitingToSitState(delta);
					break;
					
				case CustomerState.Sitting:
					UpdateSittingState(delta);
					break;
					
				case CustomerState.OrderingDrink:
					UpdateOrderingDrinkState(delta);
					break;
					
				case CustomerState.OrderingFood:
					UpdateOrderingFoodState(delta);
					break;
					
				case CustomerState.WatchingShow:
					UpdateWatchingShowState(delta);
					break;
					
				case CustomerState.TalkingToKons:
					UpdateTalkingToKonsState(delta);
					break;
					
				case CustomerState.Dancing:
					UpdateDancingState(delta);
					break;
					
				case CustomerState.UsingBathroom:
					UpdateUsingBathroomState(delta);
					break;
					
				case CustomerState.Leaving:
					UpdateLeavingState(delta);
					break;
					
				case CustomerState.SpecialEvent:
					UpdateSpecialEventState(delta);
					break;
			}
		}
		
		// Durum güncelleme metotları
		protected virtual void UpdateEnteringState(float delta)
		{
			// Eğer hareket etmiyorsa, bir masaya oturmak için bekle
			if (!_isMoving)
			{
				ChangeState(CustomerState.WaitingToSit);
			}
		}
		
		protected virtual void UpdateWaitingToSitState(float delta)
		{
			// Bu durumda CustomerManager'dan masa tahsisi bekleniyor
			// Atama dışardan yapılacak, bekle
		}
		
		protected virtual void UpdateSittingState(float delta)
		{
			// Oturma durumundayken rastgele içki veya yemek siparişi verme kararı
			if (!_isMoving && _timeInPavyon > 5.0f) // Geldikten 5 dakika sonra
			{
				float orderChance = 0.005f; // Her frame'de %0.5 sipariş verme şansı
				
				// Sarhoşluk arttıkça sipariş verme şansı artar
				orderChance += _drunkennessLevel * 0.005f;
				
				if (GD.Randf() < orderChance)
				{
					// İçki mi yemek mi sipariş edilecek?
					if (GD.Randf() < 0.7f || _mezeCount == 0) // İlk başta içki sipariş etme eğilimi
					{
						ChangeState(CustomerState.OrderingDrink);
					}
					else
					{
						ChangeState(CustomerState.OrderingFood);
					}
				}
				
				// Kons ile etkileşim kararı
				if (_assignedKonsId != null && GD.Randf() < 0.003f)
				{
					ChangeState(CustomerState.TalkingToKons);
				}
				
				// Sahne gösterisi izleme kararı
				if (IsShowActive() && GD.Randf() < 0.01f)
				{
					ChangeState(CustomerState.WatchingShow);
				}
				
				// Dans etme kararı (özellikle oyun havası çalıyorsa)
				if (IsDanceMusicPlaying() && GD.Randf() < 0.005f)
				{
					ChangeState(CustomerState.Dancing);
				}
				
				// Tuvalet ihtiyacı (içki içtikçe artar)
				if (_drinkCount > 0 && GD.Randf() < 0.002f * _drinkCount)
				{
					ChangeState(CustomerState.UsingBathroom);
				}
			}
		}
		
		protected virtual void UpdateOrderingDrinkState(float delta)
		{
			// Garson arama/çağırma ve sipariş verme işlemleri
			// Not: Garson atama ve etkileşim CustomerManager ve StaffManager tarafından yönetilecek
			
			// Burada zaman aşımı kontrolü yapılabilir
			if (_timeInStateBelonging > 120.0f) // 2 dakika içinde sipariş verilemezse
			{
				// Memnuniyeti azalt
				AdjustSatisfaction(-0.1f, "Slow service");
				
				// Oturma durumuna geri dön
				ChangeState(CustomerState.Sitting);
			}
		}
		
		protected virtual void UpdateOrderingFoodState(float delta)
		{
			// Garson arama/çağırma ve yemek siparişi verme işlemleri
			
			// Zaman aşımı kontrolü
			if (_timeInStateBelonging > 120.0f)
			{
				AdjustSatisfaction(-0.1f, "Slow food service");
				ChangeState(CustomerState.Sitting);
			}
		}
		
		protected virtual void UpdateWatchingShowState(float delta)
		{
			// Gösterinin kalitesine göre memnuniyeti güncelle
			// Gösterilen performansın kalitesi CustomerManager tarafından sağlanmalı
			
			// Gösteriyi belirli bir süre izledikten sonra oturmaya dön
			if (_timeInStateBelonging > 180.0f) // 3 dakika izleme
			{
				ChangeState(CustomerState.Sitting);
			}
		}
		
		protected virtual void UpdateTalkingToKonsState(float delta)
		{
			// Kons ile etkileşim - bahşiş verme, içki ısmarlama kararları
			
			// Etkileşim süresi ve memnuniyet durumuna göre
			if (_timeInStateBelonging > 300.0f || GD.Randf() < 0.01f) // 5 dakika konuşma veya rastgele sonlandırma
			{
				ChangeState(CustomerState.Sitting);
			}
		}
		
		protected virtual void UpdateDancingState(float delta)
		{
			// Dans etme - enerji ve sarhoşluk seviyesine göre dans süresi değişebilir
			
			// Dans süresi kontrolü
			if (_timeInStateBelonging > 120.0f || GD.Randf() < 0.02f) // 2 dakika dans veya şans
			{
				ChangeState(CustomerState.Sitting);
			}
		}
		
		protected virtual void UpdateUsingBathroomState(float delta)
		{
			// Tuvalete gitme ve dönme
			
			// Tuvalet kullanım süresi
			if (_timeInStateBelonging > 60.0f) // 1 dakika
			{
				// Masaya geri dön
				MoveTo(_tablePosition);
				
				// Oturma durumuna geç (hareket tamamlanınca)
				if (!_isMoving)
				{
					ChangeState(CustomerState.Sitting);
				}
			}
		}
		
		protected virtual void UpdateLeavingState(float delta)
		{
			// Çıkışa doğru hareket et
			
			// Çıkışa vardığında
			if (!_isMoving && Vector3.Distance(Position, _exitPosition) < 1.0f)
			{
				// Hesap ödeme ve çıkış işlemleri
				ProcessPayment();
				
				// Müşteri ayrıldı sinyali gönder
				EmitSignal(SignalName.CustomerLeft, CustomerId, _totalSpent, _satisfaction);
				
				// Sahne ağacından kaldır
				QueueFree();
			}
		}
		
		protected virtual void UpdateSpecialEventState(float delta)
		{
			// Özel olaylar (kavga, şikayet vb.)
			// Bu olaylar genellikle dışarıdan tetiklenir
		}
		
		// Pavyonda gösterinin aktif olup olmadığını kontrol et
		protected bool IsShowActive()
		{
			// Gerçek uygulamada StageManager'dan alınacak
			// Şimdilik her zaman false
			return false;
		}
		
		// Dans müziği çalıp çalmadığını kontrol et
		protected bool IsDanceMusicPlaying()
		{
			// Gerçek uygulamada MusicManager'dan alınacak
			// Şimdilik her zaman false
			return false;
		}
		
		// Durum değiştirme
		public void ChangeState(CustomerState newState)
		{
			// Önceki durum ve durumda geçirilen süre
			CustomerState previousState = _currentState;
			_currentState = newState;
			
			// Durumda geçirilen süreyi sıfırla
			_timeInStateBelonging = 0.0f;
			
			// Durum değişikliğine özel davranışlar
			OnStateChanged(previousState, newState);
			
			// Durum değişikliği sinyali gönder
			EmitSignal(SignalName.StateChanged, (int)previousState, (int)newState);
			
			GD.Print($"Customer {FullName} changed state from {previousState} to {newState}");
		}
		
		// Durum değişikliğinde çağrılır
		protected virtual void OnStateChanged(CustomerState previousState, CustomerState newState)
		{
			// Duruma özel animasyonlar ve davranışlar
			switch (newState)
			{
				case CustomerState.Entering:
					PlayAnimation("walk");
					break;
				
				case CustomerState.WaitingToSit:
					PlayAnimation("idle_stand");
					break;
				
				case CustomerState.Sitting:
					PlayAnimation("sit");
					break;
				
				case CustomerState.OrderingDrink:
					PlayAnimation("call_waiter");
					Say("Garson!");
					break;
				
				case CustomerState.OrderingFood:
					PlayAnimation("call_waiter");
					Say("Meze var mı?");
					break;
				
				case CustomerState.WatchingShow:
					PlayAnimation("watch_show");
					break;
				
				case CustomerState.TalkingToKons:
					PlayAnimation("talk");
					break;
				
				case CustomerState.Dancing:
					PlayAnimation("dance");
					break;
				
				case CustomerState.UsingBathroom:
					PlayAnimation("walk");
					Say("Lavabo nerede?");
					break;
				
				case CustomerState.Leaving:
					PlayAnimation("walk");
					if (_drunkennessLevel > 0.7f)
						Say("Hesabı alabilir miyim? *Hık*");
					else
						Say("Hesabı alabilir miyim?");
					break;
				
				case CustomerState.SpecialEvent:
					// Özel duruma göre değişebilir
					break;
			}
		}
		
		// Animasyon oynatma
		protected void PlayAnimation(string animName)
		{
			if (_animationPlayer != null && _animationPlayer.HasAnimation(animName))
			{
				_animationPlayer.Play(animName);
			}
			else
			{
				GD.Print($"Animation not found: {animName}");
			}
		}
		
		// Konuşma balonu gösterme
		public void Say(string text, float duration = 3.0f)
		{
			if (_speechBubble != null && _speechLabel != null)
			{
				_speechLabel.Text = text;
				_speechBubble.Visible = true;
				
				// Timer ile süre sonunda balonu gizle
				Timer timer = new Timer
				{
					WaitTime = duration,
					OneShot = true
				};
				
				AddChild(timer);
				timer.Timeout += () => _speechBubble.Visible = false;
				timer.Start();
			}
		}
		
		// Hareket etme
		public void MoveTo(Vector3 position)
		{
			_targetPosition = position;
			_isMoving = true;
			
			// Hareket animasyonu başlat
			if (_drunkennessLevel > 0.7f)
				PlayAnimation("walk_drunk");
			else
				PlayAnimation("walk");
			
			// Karakteri yönlendir
			LookAt(new Vector3(position.X, Position.Y, position.Z), Vector3.Up);
		}
		
		// Hareketi işle
		protected void ProcessMovement(float delta)
		{
			Vector3 direction = _targetPosition - Position;
			
			// Hedef noktaya ulaşılıp ulaşılmadığını kontrol et
			if (direction.Length() < 0.1f)
			{
				_isMoving = false;
				
				// Duruş animasyonuna geri dön
				if (_currentState == CustomerState.Sitting)
					PlayAnimation("sit");
				else
					PlayAnimation("idle");
				
				return;
			}
			
			// Hareket etme yönünü normalize et
			direction = direction.Normalized();
			
			// Sarhoşluk etkisini ekle
			if (_drunkennessLevel > 0.5f)
			{
				// Sarhoşluk arttıkça daha fazla rastgele sapma ekle
				float drunkFactor = _drunkennessLevel - 0.5f;
				direction.X += (GD.Randf() * 2.0f - 1.0f) * drunkFactor * 0.2f;
				direction.Z += (GD.Randf() * 2.0f - 1.0f) * drunkFactor * 0.2f;
				direction = direction.Normalized();
			}
			
			// Pozisyonu güncelle
			float speed = _moveSpeed;
			if (_drunkennessLevel > 0.7f)
				speed *= 0.7f; // Sarhoşlar daha yavaş hareket eder
				
			Position += direction * speed * delta;
		}
		
		// Sarhoşluk efektlerini işle
		protected void ProcessDrunkennessEffects(float delta)
		{
			// Sarhoşluk arttıkça daha sık rastgele konuşma ve hareket
			if (GD.Randf() < 0.002f * _drunkennessLevel)
			{
				SayRandomDrunkPhrase();
			}
			
			// Çok sarhoş olunca yere düşme riski
			if (_drunkennessLevel > 0.8f && GD.Randf() < 0.001f)
			{
				PlayAnimation("fall_drunk");
				Say("Ooops.. *Hık*");
			}
			
			// Sarhoşluk memnuniyeti etkiler
			if (_drunkennessLevel > 0.9f)
			{
				// Çok sarhoş olunca memnuniyet düşer
				AdjustSatisfaction(-0.001f * delta, "Too drunk");
			}
		}
		
		// Rastgele sarhoş lafları
		protected void SayRandomDrunkPhrase()
		{
			string[] drunkPhrases = new string[] {
				"Angaralıyık ulaağn! *Hık*",
				"Bize her yer Ankara! *Hık*",
				"Bir kadeh daha, Şerefee! *Hık*",
				"Ahh beee, hayat... *Hık*",
				"Muhabbet koyulaştı! *Hık*",
				"Sen benim kimm olduğumu biliyo musun? *Hık*",
				"Pavyonu satın alıcam yarın! *Hık*",
				"Nerelere geldik, nelerden geçtik... *Hık*",
				"Baaaak, dinle beni şimdi! *Hık*"
			};
			
			int index = (int)(GD.Randf() * drunkPhrases.Length);
			Say(drunkPhrases[index], 2.0f);
		}
		
		// Memnuniyet seviyesini ayarla
		public void AdjustSatisfaction(float amount, string reason = "")
		{
			float previousValue = _satisfaction;
			_satisfaction = Mathf.Clamp(_satisfaction + amount, 0.0f, 1.0f);
			
			// Değişiklik sinyali gönder
			if (previousValue != _satisfaction)
			{
				EmitSignal(SignalName.SatisfactionChanged, previousValue, _satisfaction);
				
				// Kritik değişimler için log
				if (Mathf.Abs(amount) > 0.05f)
				{
					string direction = amount > 0 ? "increased" : "decreased";
					GD.Print($"Customer {FullName} satisfaction {direction} by {Mathf.Abs(amount)} due to {reason}. New satisfaction: {_satisfaction}");
				}
				
				// Memnuniyet çok düşükse ayrılma kontrolü
				if (_satisfaction < 0.2f && previousValue >= 0.2f)
				{
					// %70 ihtimalle ayrılma kararı
					if (GD.Randf() < 0.7f)
					{
						PrepareToLeave();
					}
				}
			}
		}
		
		// Sarhoşluk seviyesini ayarla
		public void AdjustDrunkenness(float amount, string reason = "")
		{
			float previousValue = _drunkennessLevel;
			_drunkennessLevel = Mathf.Clamp(_drunkennessLevel + amount, 0.0f, 1.0f);
			
			// Değişiklik sinyali gönder
			if (previousValue != _drunkennessLevel)
			{
				EmitSignal(SignalName.DrunkennessChanged, previousValue, _drunkennessLevel);
				
				// Kritik değişimler için log
				if (Mathf.Abs(amount) > 0.05f)
				{
					string direction = amount > 0 ? "increased" : "decreased";
					GD.Print($"Customer {FullName} drunkenness {direction} by {Mathf.Abs(amount)} due to {reason}. New drunkenness: {_drunkennessLevel}");
				}
				
				// Sarhoşluk değişimi animasyon değişimini tetikleyebilir
				UpdateDrunkennessAppearance();
			}
		}
		
		// Sarhoşluk seviyesine göre görünümü güncelle
		protected void UpdateDrunkennessAppearance()
		{
			if (_drunkennessLevel > 0.7f)
			{
				// Çok sarhoş - sallanma, kızarmış yüz
				if (_model != null)
				{
					// Model görünümünü güncelle
				}
			}
			else if (_drunkennessLevel > 0.4f)
			{
				// Orta derece sarhoş
			}
			else
			{
				// Ayık
			}
		}
		
		// İçki sipariş et
		public string OrderDrink()
		{
			// Tercih edilen içki tipini belirle
			string drinkType = GetPreferredDrinkType();
			
			// Sipariş geçmişine ekle
			if (_orderHistory.ContainsKey(drinkType))
				_orderHistory[drinkType]++;
			else
				_orderHistory[drinkType] = 1;
			
			_drinkCount++;
			
			// Sipariş animasyonu
			PlayAnimation("order_drink");
			Say($"Bir {drinkType} lütfen!");
			
			return drinkType;
		}
		
		// Meze sipariş et
		public string OrderMeze()
		{
			// Tercih edilen meze tipini belirle
			string mezeType = GetPreferredMezeType();
			
			// Sipariş geçmişine ekle
			if (_orderHistory.ContainsKey(mezeType))
				_orderHistory[mezeType]++;
			else
				_orderHistory[mezeType] = 1;
			
			_mezeCount++;
			
			// Sipariş animasyonu
			PlayAnimation("order_food");
			Say($"Bir porsiyon {mezeType} alabilir miyim?");
			
			return mezeType;
		}
		
		// Tercih edilen içki tipini belirle
		protected string GetPreferredDrinkType()
		{
			// Tercih ve sarhoşluk seviyesine göre içki seçimi
			
			// Müşteri tipine özgü içki tercihleri
			string[] popularDrinks = { "Rakı", "Bira", "Viski", "Votka", "Şarap", "Özel Kokteyl" };
			
			// En yüksek tercih değerini bul
			string preferredDrink = popularDrinks[0];
			float highestPref = _preferences["drink_raki"];
			
			if (_preferences["drink_beer"] > highestPref)
			{
				highestPref = _preferences["drink_beer"];
				preferredDrink = popularDrinks[1];
			}
			
			if (_preferences["drink_whiskey"] > highestPref)
			{
				highestPref = _preferences["drink_whiskey"];
				preferredDrink = popularDrinks[2];
			}
			
			if (_preferences["drink_vodka"] > highestPref)
			{
				highestPref = _preferences["drink_vodka"];
				preferredDrink = popularDrinks[3];
			}
			
			if (_preferences["drink_wine"] > highestPref)
			{
				highestPref = _preferences["drink_wine"];
				preferredDrink = popularDrinks[4];
			}
			
			if (_preferences["drink_special"] > highestPref)
			{
				highestPref = _preferences["drink_special"];
				preferredDrink = popularDrinks[5];
			}
			
			// Sarhoşluk arttıkça rastgele seçim şansı artar
			if (_drunkennessLevel > 0.6f && GD.Randf() < _drunkennessLevel * 0.5f)
			{
				int randomIndex = (int)(GD.Randf() * popularDrinks.Length);
				preferredDrink = popularDrinks[randomIndex];
			}
			
			return preferredDrink;
		}
		
		// Tercih edilen meze tipini belirle
		protected string GetPreferredMezeType()
		{
			// Popüler mezeler
			string[] coldMezes = { "Haydari", "Patlıcan Salatası", "Cacık", "Humus" };
			string[] hotMezes = { "Kaşarlı Mantar", "Sigara Böreği", "Sucuk", "Köfte" };
			string[] seafood = { "Kalamar", "Karides Güveç", "Balık Köftesi" };
			string[] meats = { "Çiğ Köfte", "Pastırma", "Kavurma" };
			
			// En yüksek tercih kategorisini bul
			string category = "cold";
			float highestPref = _preferences["meze_cold"];
			
			if (_preferences["meze_hot"] > highestPref)
			{
				highestPref = _preferences["meze_hot"];
				category = "hot";
			}
			
			if (_preferences["meze_seafood"] > highestPref)
			{
				highestPref = _preferences["meze_seafood"];
				category = "seafood";
			}
			
			if (_preferences["meze_meats"] > highestPref)
			{
				highestPref = _preferences["meze_meats"];
				category = "meats";
			}
			
			// Kategoriye göre rastgele bir meze seç
			int index = 0;
			string[] selectedCategory;
			
			switch (category)
			{
				case "hot":
					selectedCategory = hotMezes;
					break;
				case "seafood":
					selectedCategory = seafood;
					break;
				case "meats":
					selectedCategory = meats;
					break;
				default:
					selectedCategory = coldMezes;
					break;
			}
			
			index = (int)(GD.Randf() * selectedCategory.Length);
			return selectedCategory[index];
		}
		
		// İçki teslim alma
		public void ReceiveDrink(string drinkType, float quality)
		{
			// İçkiyi al, sarhoşluk arttır
			float drunkennessIncrease = 0.1f; // Temel sarhoşluk artışı
			
			// İçki tipine göre sarhoşluk artışını ayarla
			switch (drinkType)
			{
				case "Rakı":
					drunkennessIncrease = 0.15f;
					break;
				case "Bira":
					drunkennessIncrease = 0.08f;
					break;
				case "Viski":
					drunkennessIncrease = 0.12f;
					break;
				case "Votka":
					drunkennessIncrease = 0.12f;
					break;
				case "Şarap":
					drunkennessIncrease = 0.1f;
					break;
				case "Özel Kokteyl":
					drunkennessIncrease = 0.14f;
					break;
			}
			
			// İçki kalitesine göre memnuniyet
			float satisfactionChange = (quality - 0.5f) * 0.2f; // Kalite 0.5'in üstündeyse pozitif, altındaysa negatif
			
			// Tercih edilen içki tipine göre ek memnuniyet
			string myPreferredDrink = GetPreferredDrinkType();
			if (drinkType == myPreferredDrink)
				satisfactionChange += 0.05f; // Tercih edilen içkiyse bonus memnuniyet
			
			// Sarhoşluk ve memnuniyet güncelleme
			AdjustDrunkenness(drunkennessIncrease, $"Drinking {drinkType}");
			AdjustSatisfaction(satisfactionChange, $"Drink quality: {quality}");
			
			// İçki alma animasyonu
			PlayAnimation("drink");
			
			// Ankara pavyon kültürüne özgü replikler
			string[] drinkPhrases = new string[]
			{
				"Şerefe!",
				"Olmaz olmazımızsın!",
				"İçelim güzel abim/ablacım!",
				"Şu kadehin hatırına...",
				"Ah, vur dibine!"
			};
			
			int index = (int)(GD.Randf() * drinkPhrases.Length);
			Say(drinkPhrases[index]);
			
			// Ödeme işlemi
			float drinkPrice = CalculateDrinkPrice(drinkType);
			SpendMoney(drinkPrice, "drink");
		}
		
		// Meze teslim alma
		public void ReceiveMeze(string mezeType, float quality)
		{
			// Mezeyi al
			
			// Meze kalitesine göre memnuniyet
			float satisfactionChange = (quality - 0.5f) * 0.2f; // Kalite 0.5'in üstündeyse pozitif, altındaysa negatif
			
			// Tercih edilen meze kategorisine göre ek memnuniyet
			// (detaylı tercih kontrolü gerekirse genişletilebilir)
			
			// Sarhoşluğu hafif azalt (yemek yeme etkisi)
			AdjustDrunkenness(-0.03f, $"Eating {mezeType}");
			AdjustSatisfaction(satisfactionChange, $"Meze quality: {quality}");
			
			// Meze alma animasyonu
			PlayAnimation("eat");
			
			// Meze tepkileri
			string[] mezePhrases = new string[]
			{
				"Mmm, çok güzel olmuş.",
				"Bu meze bir harika!",
				"Eline sağlık aşçı başı!",
				"Buna bayılıyorum.",
				"İşte bu tadı seviyorum."
			};
			
			int index = (int)(GD.Randf() * mezePhrases.Length);
			Say(mezePhrases[index]);
			
			// Ödeme işlemi
			float mezePrice = CalculateMezePrice(mezeType);
			SpendMoney(mezePrice, "food");
		}
		
		// Para harcama
		protected bool SpendMoney(float amount, string category)
		{
			if (_remainingBudget >= amount)
			{
				_remainingBudget -= amount;
				_totalSpent += amount;
				
				// Para harcama sinyali gönder
				EmitSignal(SignalName.CustomerSpentMoney, amount, category);
				
				// İstatistik güncelle
				GD.Print($"Customer {FullName} spent {amount} on {category}. Remaining budget: {_remainingBudget}");
				
				return true;
			}
			else
			{
				GD.Print($"Customer {FullName} cannot afford {amount} for {category}. Remaining budget: {_remainingBudget}");
				
				// Parasız kalma durumu
				HandleOutOfMoney();
				
				return false;
			}
		}
		
		// Parasız kalma durumunu işle
		protected virtual void HandleOutOfMoney()
		{
			// Karga durumuna göre kaçma veya ayrılma kararı
			if (GD.Randf() < _kargaLevel)
			{
				// Karga yapmaya çalış (hesap ödemeden kaç)
				AttemptKarga();
			}
			else
			{
				// Normal ayrılma - hesabı öde ve çık
				PrepareToLeave();
				Say("Hesabı alabilir miyim? Bütçem bu kadar yetti...");
			}
		}
		
		// Karga yapma girişimi (hesap ödemeden kaçma)
		protected void AttemptKarga()
		{
			// Karga yapmanın başarı şansı
			float kargaSuccessChance = _kargaLevel;
			
			// Sarhoşluk arttıkça başarı şansı düşer
			kargaSuccessChance *= (1.0f - _drunkennessLevel * 0.5f);
			
			// Güvenlik personeli varlığı başarı şansını düşürür
			float securityPresence = GetSecurityPresence();
			kargaSuccessChance *= (1.0f - securityPresence);
			
			// Karga başarı şansı
			if (GD.Randf() < kargaSuccessChance)
			{
				// Başarılı karga - hesap ödemeden kaybol
				GD.Print($"Customer {FullName} successfully did 'karga' (left without paying)!");
				
				// Ayrıldı sinyali gönder (ödemeden kaçtı)
				EmitSignal(SignalName.CustomerLeft, CustomerId, 0.0f, _satisfaction);
				
				// Şüpheli davranışı kaydet - polis vb. için
				RecordSuspiciousBehavior("karga");
				
				// Sahne ağacından kaldır
				QueueFree();
			}
			else
			{
				// Başarısız karga girişimi - yakalandı!
				GD.Print($"Customer {FullName} failed to do 'karga' and got caught!");
				
				// Güvenlik müdahalesi talep et
				RequestSecurityIntervention("attempted_karga");
				
				// Ödeme işlemi zorla gerçekleşir
				ProcessPayment();
				
				// Müşteri ayrılır
				EmitSignal(SignalName.CustomerLeft, CustomerId, _totalSpent, _satisfaction);
				
				// Sahne ağacından kaldır
				QueueFree();
			}
		}
		
		// Güvenlik varlığını kontrol et
		protected float GetSecurityPresence()
		{
			// Gerçek uygulamada SecurityManager'dan alınacak
			// Şimdilik varsayılan değer
			return 0.5f;
		}
		
		// Güvenlik müdahalesi iste
		protected void RequestSecurityIntervention(string reason)
		{
			if (GetTree().Root.HasNode("GameManager/SecurityManager"))
			{
				var securityManager = GetTree().Root.GetNode("GameManager/SecurityManager");
				
				if (securityManager.HasMethod("InterventionRequest"))
				{
					securityManager.Call("InterventionRequest", CustomerId, Position, reason);
				}
			}
		}
		
		// Şüpheli davranışı kaydet
		protected void RecordSuspiciousBehavior(string behavior)
		{
			if (GetTree().Root.HasNode("GameManager/ReputationManager"))
			{
				var reputationManager = GetTree().Root.GetNode("GameManager/ReputationManager");
				
				if (reputationManager.HasMethod("RecordSuspiciousBehavior"))
				{
					reputationManager.Call("RecordSuspiciousBehavior", CustomerId, behavior, _customerType.ToString());
				}
			}
		}
		
		// İçki fiyatını hesapla
		protected float CalculateDrinkPrice(string drinkType)
		{
			// Temel fiyatlar
			float basePrice = 0.0f;
			
			switch (drinkType)
			{
				case "Rakı":
					basePrice = 300.0f;
					break;
				case "Bira":
					basePrice = 120.0f;
					break;
				case "Viski":
					basePrice = 350.0f;
					break;
				case "Votka":
					basePrice = 280.0f;
					break;
				case "Şarap":
					basePrice = 250.0f;
					break;
				case "Özel Kokteyl":
					basePrice = 400.0f;
					break;
				default:
					basePrice = 200.0f;
					break;
			}
			
			// Ekonomi yöneticisinden fiyat çarpanını al
			float priceMultiplier = GetPriceMultiplier("drink");
			
			return basePrice * priceMultiplier;
		}
		
		// Meze fiyatını hesapla
		protected float CalculateMezePrice(string mezeType)
		{
			// Temel fiyatlar - meze kategorisine göre
			float basePrice = 0.0f;
			
			// Soğuk mezeler
			if (mezeType.Contains("Haydari") || mezeType.Contains("Salata") || 
				mezeType.Contains("Cacık") || mezeType.Contains("Humus"))
			{
				basePrice = 150.0f;
			}
			// Sıcak mezeler
			else if (mezeType.Contains("Mantar") || mezeType.Contains("Böreği") || 
					 mezeType.Contains("Sucuk") || mezeType.Contains("Köfte"))
			{
				basePrice = 220.0f;
			}
			// Deniz ürünleri
			else if (mezeType.Contains("Kalamar") || mezeType.Contains("Karides") || 
					 mezeType.Contains("Balık"))
			{
				basePrice = 350.0f;
			}
			// Et ürünleri
			else if (mezeType.Contains("Çiğ Köfte") || mezeType.Contains("Pastırma") || 
					 mezeType.Contains("Kavurma"))
			{
				basePrice = 280.0f;
			}
			else
			{
				basePrice = 200.0f; // Varsayılan
			}
			
			// Ekonomi yöneticisinden fiyat çarpanını al
			float priceMultiplier = GetPriceMultiplier("food");
			
			return basePrice * priceMultiplier;
		}
		
		// Ekonomi yöneticisinden fiyat çarpanı
		protected float GetPriceMultiplier(string category)
		{
			if (GetTree().Root.HasNode("GameManager/EconomyManager"))
			{
				var economyManager = GetTree().Root.GetNode("GameManager/EconomyManager");
				
				if (economyManager.HasProperty($"{category}PriceMultiplier"))
				{
					return (float)economyManager.Get($"{category}PriceMultiplier");
				}
			}
			
			return 1.0f; // Varsayılan çarpan
		}
		
		// Bahşiş verme
		protected void GiveTip(float amount, string staffId)
		{
			if (_remainingBudget >= amount)
			{
				_remainingBudget -= amount;
				_tipAmount += amount;
				
				// Bahşiş verme işlemi
				if (GetTree().Root.HasNode("GameManager/StaffManager"))
				{
					var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
					
					if (staffManager.HasMethod("GiveTipToStaff"))
					{
						staffManager.Call("GiveTipToStaff", staffId, amount, CustomerId);
					}
				}
				
				GD.Print($"Customer {FullName} gave {amount} tip to staff {staffId}");
			}
		}
		
		// Ayrılmaya hazırlan
		public void PrepareToLeave()
		{
			if (_currentState == CustomerState.Leaving)
				return;
				
			ChangeState(CustomerState.Leaving);
			
			// Çıkış noktasına doğru hareket et
			MoveTo(_exitPosition);
		}
		
		// Hesabı öde ve ayrıl
		protected void ProcessPayment()
		{
			// Bahşiş miktarı - cömertlik ve memnuniyet faktörüne bağlı
			float tipPercentage = _generosity * 0.2f; // %0-20 arası bahşiş
			tipPercentage += (_satisfaction - 0.5f) * 0.1f; // Memnuniyete göre +/- %10
			
			// Sarhoşluk bahşiş yüzdesini etkiler
			if (_drunkennessLevel > 0.7f)
				tipPercentage += 0.1f; // Çok sarhoş müşteriler daha cömert olabilir
			
			float tipAmount = _totalSpent * tipPercentage;
			
			// Bahşiş ödeme
			if (_remainingBudget >= tipAmount)
			{
				// Atanan konsa bahşiş ver
				if (_assignedKonsId != null)
				{
					GiveTip(tipAmount, _assignedKonsId);
				}
				else if (_interactedStaffIds.Count > 0)
				{
					// Rastgele bir personele bahşiş ver
					int randomIndex = (int)(GD.Randf() * _interactedStaffIds.Count);
					GiveTip(tipAmount, _interactedStaffIds[randomIndex]);
				}
			}
			
			GD.Print($"Customer {FullName} paid total: {_totalSpent}, tip: {tipAmount}");
		}
		
		// Müşteri tipi adını al
		public string GetCustomerTypeName()
		{
			return _customerType.ToString();
		}
		
		// Müşteri tipini ayarla
		public void SetCustomerType(CustomerType type)
		{
			_customerType = type;
			InitializeByCustomerType();
		}
		
		// Müşteri tipini ayarla (string kullanarak)
		public void SetCustomerType(string typeName)
		{
			if (Enum.TryParse<CustomerType>(typeName, out CustomerType parsedType))
			{
				SetCustomerType(parsedType);
			}
			else
			{
				GD.Print($"Invalid customer type: {typeName}");
			}
		}
		
		// Oturacak masa atama
		public void AssignTable(Vector3 tablePosition)
		{
			_tablePosition = tablePosition;
			
			// Masaya git
			MoveTo(_tablePosition);
			
			// Durum değişikliği - masa atanınca oturmak için hareket eder
			if (_currentState == CustomerState.WaitingToSit)
			{
				ChangeState(CustomerState.Sitting);
			}
		}
		
		// Kons atama
		public void AssignKons(string konsId)
		{
			_assignedKonsId = konsId;
			
			if (!_interactedStaffIds.Contains(konsId))
				_interactedStaffIds.Add(konsId);
			
			GD.Print($"Customer {FullName} assigned to kons with ID: {konsId}");
		}
		
		// Etkileşim staff listesine personel ekle
		public void AddInteractedStaff(string staffId)
		{
			if (!_interactedStaffIds.Contains(staffId))
				_interactedStaffIds.Add(staffId);
		}
		
		// Pavyon konumlarını ayarla
		public void SetPavyonLocations(Vector3 bathroom, Vector3 exit)
		{
			_bathroomPosition = bathroom;
			_exitPosition = exit;
		}
		
		// Zorunlu ayrılma (güvenlik tarafından atılma)
		public void ForceEject(string reason)
		{
			GD.Print($"Customer {FullName} is being forcefully ejected. Reason: {reason}");
			
			// Eğer müşteri hesap ödeyebilecek durumdaysa öder
			if (_remainingBudget >= _totalSpent * 0.5f)
			{
				ProcessPayment();
			}
			else
			{
				// Ödenemeyecek durumdaysa, kalan parayla ödenir
				SpendMoney(_remainingBudget, "forced_payment");
			}
			
			// Ayrıldı sinyali gönder
			EmitSignal(SignalName.CustomerLeft, CustomerId, _totalSpent, 0.0f); // Memnuniyet 0 - zorla çıkarıldı
			
			// Sahne ağacından kaldır
			QueueFree();
		}
		
		// Özellik değerlerini döndür
		public Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = new Dictionary<string, object>
			{
				{ "CustomerId", CustomerId },
				{ "FullName", FullName },
				{ "Age", Age },
				{ "Gender", Gender },
				{ "CustomerType", _customerType.ToString() },
				{ "CurrentState", _currentState.ToString() },
				{ "Budget", _budget },
				{ "RemainingBudget", _remainingBudget },
				{ "Generosity", _generosity },
				{ "DrunkennessLevel", _drunkennessLevel },
				{ "Satisfaction", _satisfaction },
				{ "AggressionLevel", _aggressionLevel },
				{ "LoyaltyLevel", _loyaltyLevel },
				{ "KargaLevel", _kargaLevel },
				{ "TotalSpent", _totalSpent },
				{ "TipAmount", _tipAmount },
				{ "DrinkCount", _drinkCount },
				{ "MezeCount", _mezeCount },
				{ "TimeInPavyon", _timeInPavyon },
				{ "IsVIP", IsVIP },
				{ "Signature", _signature }
			};
			
			return stats;
		}
	}
}
