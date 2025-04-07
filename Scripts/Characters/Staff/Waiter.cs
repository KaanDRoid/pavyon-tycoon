using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Staff
{
	
	public partial class Waiter : StaffBase
	{
		// Garson özellikleri
		private float _speedSkill = 0.5f;          // Hız becerisi
		private float _balanceSkill = 0.5f;        // Denge becerisi (tepsi taşıma)
		private float _memorySkill = 0.5f;         // Hafıza becerisi (sipariş hatırlama)
		private float _upsellingSkill = 0.5f;      // Ek satış becerisi
		
		// Servis takibi
		private int _ordersServed = 0;             // Servis edilen sipariş sayısı
		private int _ordersMissed = 0;             // Kaçırılan sipariş sayısı
		private int _upsellSuccess = 0;            // Başarılı ek satış sayısı
		private float _spillProbability = 0.1f;    // Dökme olasılığı
		private float _orderEfficiency = 0.0f;     // Sipariş verimliliği (zaman tasarrufu)
		
		// Meşguliyet durumu
		private bool _isCarryingOrder = false;     // Sipariş taşıyor mu
		private Node3D _currentOrderTarget = null; // Mevcut sipariş hedefi
		private float _carryingCapacity = 3.0f;    // Taşıyabildiği maksimum sipariş sayısı
		private float _currentCarryingLoad = 0.0f; // Şu an taşıdığı sipariş miktarı
		
		// Sipariş takibi
		private Queue<Vector3> _pendingOrderLocations = new Queue<Vector3>(); // Bekleyen siparişlerin konumları
		private Dictionary<string, DateTime> _pendingOrderTimes = new Dictionary<string, DateTime>(); // Sipariş zamanları
		
		// Rotalar
		private Vector3 _barPosition = Vector3.Zero;     // Bar lokasyonu
		private Vector3 _kitchenPosition = Vector3.Zero; // Mutfak lokasyonu
		
		// Signals
		[Signal]
		public delegate void OrderServedEventHandler(string customerId, float timeTaken);
		
		[Signal]
		public delegate void OrderSpilledEventHandler(float damageAmount);
		
		[Signal]
		public delegate void UpsellSuccessEventHandler(string customerId, float extraAmount);
		
		public override void _Ready()
		{
			base._Ready();
			
			// Tipi ayarla
			Type = StaffType.Waiter;
			
			// Beceri değerlerini başlat (StaffBase'deki skills sözlüğünden al)
			if (_skills.ContainsKey("speed")) _speedSkill = _skills["speed"];
			if (_skills.ContainsKey("balance")) _balanceSkill = _skills["balance"];
			if (_skills.ContainsKey("memory")) _memorySkill = _skills["memory"];
			if (_skills.ContainsKey("upselling")) _upsellingSkill = _skills["upselling"];
			
			// Dökülme olasılığını hesapla (beceri arttıkça azalır)
			_spillProbability = Mathf.Max(0.01f, 0.2f - (_balanceSkill * 0.15f));
			
			// Trait'lere göre değerleri düzenle
			AdjustValuesByTraits();
			
			// İşletme lokasyonlarını bul
			FindBusinessLocations();
			
			GD.Print($"Waiter {Name} initialized with speed: {_speedSkill}, balance: {_balanceSkill}, memory: {_memorySkill}");
		}
		
		// İşletme lokasyonlarını bul
		private void FindBusinessLocations()
		{
			// Bar ve mutfak lokasyonlarını bul
			var currentScene = GetTree().CurrentScene;
			
			if (currentScene != null)
			{
				// Bar lokasyonu
				if (currentScene.HasNode("BarLocation"))
				{
					var barLocation = currentScene.GetNode<Node3D>("BarLocation");
					_barPosition = barLocation.GlobalPosition;
				}
				
				// Mutfak lokasyonu
				if (currentScene.HasNode("KitchenLocation"))
				{
					var kitchenLocation = currentScene.GetNode<Node3D>("KitchenLocation");
					_kitchenPosition = kitchenLocation.GlobalPosition;
				}
			}
		}
		
		// Trait'lere göre değerleri düzenle
		private void AdjustValuesByTraits()
		{
			foreach (string trait in GetTraits())
			{
				switch (trait)
				{
					case "FastLearner":
						_memorySkill = Mathf.Min(1.0f, _memorySkill + 0.15f);
						break;
					case "Professional":
						_speedSkill = Mathf.Min(1.0f, _speedSkill + 0.15f);
						_balanceSkill = Mathf.Min(1.0f, _balanceSkill + 0.15f);
						break;
					case "Experienced":
						_upsellingSkill = Mathf.Min(1.0f, _upsellingSkill + 0.15f);
						_spillProbability = Mathf.Max(0.01f, _spillProbability - 0.05f);
						break;
					case "Charismatic":
						_upsellingSkill = Mathf.Min(1.0f, _upsellingSkill + 0.20f);
						break;
					case "Clumsy": // Özel garson trait'i
						_balanceSkill = Mathf.Max(0.1f, _balanceSkill - 0.15f);
						_spillProbability = Mathf.Min(0.3f, _spillProbability + 0.1f);
						break;
					case "Attentive": // Özel garson trait'i
						_memorySkill = Mathf.Min(1.0f, _memorySkill + 0.20f);
						break;
					case "Lazy":
						_speedSkill = Mathf.Max(0.1f, _speedSkill - 0.15f);
						break;
					case "Alcoholic":
						_balanceSkill = Mathf.Max(0.1f, _balanceSkill - 0.15f);
						_spillProbability = Mathf.Min(0.3f, _spillProbability + 0.05f);
						break;
				}
			}
		}
		
		// Özel beceri geliştirme - StaffBase.ImproveSkills() metodunu override eder
		public override void ImproveSkills()
		{
			base.ImproveSkills();
			
			// Garson-spesifik becerileri geliştir
			float baseImprovement = 0.005f; // Günlük temel gelişim
			
			// Çalışkanlık gelişim hızını etkiler
			float improvementMultiplier = 1.0f + (Diligence - 0.5f);
			
			// Her beceri için rastgele gelişim
			if (GD.Randf() < 0.7f) // %70 ihtimalle hız gelişimi
			{
				_speedSkill = Mathf.Min(1.0f, _speedSkill + baseImprovement * improvementMultiplier);
				if (_skills.ContainsKey("speed")) 
					_skills["speed"] = _speedSkill;
			}
			
			if (GD.Randf() < 0.6f) // %60 ihtimalle denge gelişimi
			{
				_balanceSkill = Mathf.Min(1.0f, _balanceSkill + baseImprovement * improvementMultiplier);
				if (_skills.ContainsKey("balance")) 
					_skills["balance"] = _balanceSkill;
				
				// Dökülme olasılığını güncelle
				_spillProbability = Mathf.Max(0.01f, 0.2f - (_balanceSkill * 0.15f));
			}
			
			if (GD.Randf() < 0.5f) // %50 ihtimalle hafıza gelişimi
			{
				_memorySkill = Mathf.Min(1.0f, _memorySkill + baseImprovement * improvementMultiplier);
				if (_skills.ContainsKey("memory")) 
					_skills["memory"] = _memorySkill;
			}
			
			if (GD.Randf() < 0.4f) // %40 ihtimalle ek satış gelişimi
			{
				_upsellingSkill = Mathf.Min(1.0f, _upsellingSkill + baseImprovement * improvementMultiplier);
				if (_skills.ContainsKey("upselling")) 
					_skills["upselling"] = _upsellingSkill;
			}
		}
		
		// Sipariş al
		public void TakeOrder(Node3D customer, bool isDrink)
		{
			if (customer == null) return;
			
			// Müşteri ID'si
			string customerId = customer.Name;
			
			// Sipariş zamanını kaydet
			_pendingOrderTimes[customerId] = DateTime.Now;
			
			// Sipariş konumunu kaydet
			_pendingOrderLocations.Enqueue(customer.GlobalPosition);
			
			// Sipariş için bar veya mutfağa git
			MoveTo(isDrink ? _barPosition : _kitchenPosition);
			
			// Aktiviteyi güncelle
			SetActivity(ActivityState.Working);
			
			GD.Print($"Waiter {Name} took an order from customer {customerId} for {(isDrink ? "drink" : "food")}");
		}
		
		// Sipariş hazırla
		public void PrepareOrder()
		{
			if (_pendingOrderLocations.Count == 0) return;
			
			// Sipariş hazırlama süresi (hız becerisine bağlı)
			float preparationTime = 3.0f - (_speedSkill * 1.5f); // 1.5 - 3 saniye arası
			
			// Hazırlık animasyonu
			PlayAnimation("prepare_order");
			
			// Timer kullanarak gecikme oluştur
			Timer timer = new Timer
			{
				WaitTime = preparationTime,
				OneShot = true
			};
			
			AddChild(timer);
			timer.Timeout += OnOrderPrepared;
			timer.Start();
			
			GD.Print($"Waiter {Name} is preparing an order. ETA: {preparationTime} seconds");
		}
		
		// Sipariş hazırlandı
		private void OnOrderPrepared()
		{
			if (_pendingOrderLocations.Count == 0) return;
			
			// Siparişi taşıma durumuna geç
			_isCarryingOrder = true;
			_currentCarryingLoad = 1.0f;
			
			// Sipariş konumuna git
			Vector3 orderLocation = _pendingOrderLocations.Dequeue();
			MoveTo(orderLocation);
			
			GD.Print($"Waiter {Name} prepared the order and is now delivering it");
		}
		
		// Siparişi servis et
		public void ServeOrder(Node3D customer)
		{
			if (!_isCarryingOrder || customer == null) return;
			
			// Müşteri ID'si
			string customerId = customer.Name;
			
			// Zaman hesaplama
			float timeTaken = 0;
			if (_pendingOrderTimes.ContainsKey(customerId))
			{
				TimeSpan span = DateTime.Now - _pendingOrderTimes[customerId];
				timeTaken = (float)span.TotalSeconds;
				_pendingOrderTimes.Remove(customerId);
			}
			
			// Dökme ihtimali kontrolü
			if (GD.Randf() < _spillProbability)
			{
				SpillOrder();
				return;
			}
			
			// Müşteriye servis
			if (customer.GetType().GetMethod("ReceiveOrder") != null)
			{
				try 
				{
					customer.Call("ReceiveOrder", timeTaken);
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error calling ReceiveOrder: {e.Message}");
				}
			}
			
			// Servis sayacını artır
			_ordersServed++;
			
			// Taşıma durumunu sıfırla
			_isCarryingOrder = false;
			_currentCarryingLoad = 0.0f;
			
			// Ek satış denemesi
			TryUpsell(customer);
			
			// Aktiviteyi güncelle
			SetActivity(ActivityState.Idle);
			
			// Servis tamamlandı sinyali gönder
			EmitSignal(SignalName.OrderServed, customerId, timeTaken);
			
			GD.Print($"Waiter {Name} served the order to customer {customerId} in {timeTaken} seconds");
		}
		
		// Siparişi dök
		private void SpillOrder()
		{
			// Dökülme animasyonu
			PlayAnimation("spill");
			
			// Siparişi sıfırla
			_isCarryingOrder = false;
			_currentCarryingLoad = 0.0f;
			
			// Hasar miktarı hesapla (denge becerisine göre azalır)
			float damageAmount = 100.0f * (1.0f - _balanceSkill * 0.5f);
			
			// Ruh hali düşüşü
			AdjustMood(-0.1f, "Spilled Order");
			
			// Dökülme sinyali gönder
			EmitSignal(SignalName.OrderSpilled, damageAmount);
			
			GD.Print($"Waiter {Name} spilled the order! Damage: {damageAmount}");
		}
		
		// Ek satış dene
		private void TryUpsell(Node3D customer)
		{
			// Ek satış başarı şansı
			float successChance = _upsellingSkill * 0.7f; // Maksimum %70 şans
			
			if (GD.Randf() < successChance)
			{
				// Ek satış miktarı
				float extraAmount = 100.0f + (200.0f * _upsellingSkill);
				
				// Müşteriye ek satış uygula
				if (customer.GetType().GetMethod("ApplyUpsell") != null)
				{
					try 
					{
						bool accepted = (bool)customer.Call("ApplyUpsell", extraAmount);
						
						if (accepted)
						{
							// Ek satış başarılı
							_upsellSuccess++;
							
							// Ek satış başarı sinyali gönder
							EmitSignal(SignalName.UpsellSuccess, customer.Name, extraAmount);
							
							GD.Print($"Waiter {Name} successfully upsold customer {customer.Name} for {extraAmount}");
						}
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error calling ApplyUpsell: {e.Message}");
					}
				}
			}
		}
		
		// Çoklu sipariş al
		public void TakeMultipleOrders(List<Node3D> customers, bool isDrink)
		{
			// Kapasiteyi aşıyorsa reddedilebilir
			if (customers.Count > _carryingCapacity)
			{
				GD.Print($"Waiter {Name} cannot take {customers.Count} orders at once. Max capacity: {_carryingCapacity}");
				
				// Kapasiteye göre kısıtla
				customers = customers.GetRange(0, Mathf.Min(customers.Count, (int)_carryingCapacity));
			}
			
			// Her müşteri için sipariş kaydı
			foreach (var customer in customers)
			{
				string customerId = customer.Name;
				_pendingOrderTimes[customerId] = DateTime.Now;
				_pendingOrderLocations.Enqueue(customer.GlobalPosition);
			}
			
			// Sipariş için bar veya mutfağa git
			MoveTo(isDrink ? _barPosition : _kitchenPosition);
			
			// Aktiviteyi güncelle
			SetActivity(ActivityState.Working);
			
			// Yük miktarını güncelle
			_currentCarryingLoad = customers.Count;
			
			GD.Print($"Waiter {Name} took multiple orders ({customers.Count}) for {(isDrink ? "drinks" : "food")}");
		}
		
		// Müşteri ile etkileşim - StaffBase.InteractWithCustomer() metodunu override eder
		public override void InteractWithCustomer(Node3D customer)
		{
			if (customer == null) return;
			
			string customerId = customer.Name;
			
			// Servis ya da sipariş alımı için etkileşim
			if (_isCarryingOrder)
			{
				// Eğer sipariş bu müşteri içinse, servis et
				ServeOrder(customer);
			}
			else
			{
				// Yeni sipariş al
				// Yemek mi içecek mi kontrolü - bu gerçek uygulamada müşteri sınıfından gelebilir
				bool isDrink = true; // Varsayılan olarak içecek siparişi
				
				if (customer.GetType().GetMethod("WantsDrink") != null)
				{
					try 
					{
						isDrink = (bool)customer.Call("WantsDrink");
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error calling WantsDrink: {e.Message}");
					}
				}
				
				TakeOrder(customer, isDrink);
			}
			
			// Enerji tüketimi
			AdjustEnergy(-0.01f, "Customer Interaction");
		}
		
		// Özel performans davranışı - StaffBase.PerformSpecialBehavior() metodunu override eder
		public override void PerformSpecialBehavior()
		{
			// Hızlı servis modu
			ActivateSpeedMode();
		}
		
		// Hızlı servis modu
		public void ActivateSpeedMode()
		{
			// Kısa süreli hız artışı
			float speedBoost = 0.3f;
			float duration = 60.0f; // 1 dakika
			
			// Animasyon değişimi
			SetActivity(ActivityState.Special);
			
			// Hız değerlerini geçici olarak artır
			float originalSpeed = _speedSkill;
			_speedSkill = Mathf.Min(1.0f, _speedSkill + speedBoost);
			
			// Daha hızlı hareket
			_moveSpeed *= 1.5f;
			
			// Timer ile normal hıza dönüş
			Timer timer = new Timer
			{
				WaitTime = duration,
				OneShot = true
			};
			
			AddChild(timer);
			timer.Timeout += () => 
			{
				_speedSkill = originalSpeed;
				_moveSpeed /= 1.5f;
				SetActivity(ActivityState.Idle);
				GD.Print($"Waiter {Name}'s speed mode ended");
			};
			timer.Start();
			
			GD.Print($"Waiter {Name} activated speed mode! Speed: {_speedSkill}, Duration: {duration} seconds");
		}
		
		// Özel animasyon - StaffBase.PlaySpecialAnimation() metodunu override eder
		protected override void PlaySpecialAnimation()
		{
			// Hızlı servis animasyonu
			PlayAnimation("speed_service");
		}
		
		// Seviye atlama özel efektleri - StaffBase.OnLevelUp() metodunu override eder
		protected override void OnLevelUp()
		{
			base.OnLevelUp();
			
			// Hız becerisi gelişimi
			_speedSkill = Mathf.Min(1.0f, _speedSkill + 0.05f);
			
			// Denge becerisi gelişimi
			_balanceSkill = Mathf.Min(1.0f, _balanceSkill + 0.04f);
			
			// Hafıza becerisi gelişimi
			_memorySkill = Mathf.Min(1.0f, _memorySkill + 0.03f);
			
			// Ek satış becerisi gelişimi
			_upsellingSkill = Mathf.Min(1.0f, _upsellingSkill + 0.03f);
			
			// Taşıma kapasitesi artışı (her 2 seviyede bir)
			if (Level % 2 == 0)
			{
				_carryingCapacity = Mathf.Min(8.0f, _carryingCapacity + 1.0f);
			}
			
			// Dökülme olasılığını güncelle
			_spillProbability = Mathf.Max(0.01f, _spillProbability - 0.01f);
			
			// Beceri değerlerini skills sözlüğüne aktar
			if (_skills.ContainsKey("speed")) _skills["speed"] = _speedSkill;
			if (_skills.ContainsKey("balance")) _skills["balance"] = _balanceSkill;
			if (_skills.ContainsKey("memory")) _skills["memory"] = _memorySkill;
			if (_skills.ContainsKey("upselling")) _skills["upselling"] = _upsellingSkill;
			
			GD.Print($"Waiter {Name} leveled up: speed {_speedSkill}, balance {_balanceSkill}, carry capacity {_carryingCapacity}");
		}
		
		// Günlük güncelleme - StaffBase.UpdateDaily() metodunu override eder
		public override void UpdateDaily()
		{
			base.UpdateDaily();
			
			// İstatistikleri sıfırla
			_ordersServed = 0;
			_ordersMissed = 0;
			_upsellSuccess = 0;
			
			// Bekleyen siparişleri temizle
			_pendingOrderLocations.Clear();
			_pendingOrderTimes.Clear();
			_isCarryingOrder = false;
			_currentCarryingLoad = 0.0f;
		}
		
		// Özel risk faktörleri - StaffBase.ApplySpecialRiskFactors() metodunu override eder
		protected override void ApplySpecialRiskFactors()
		{
			base.ApplySpecialRiskFactors();
			
			// Garson-spesifik risk faktörleri
			
			// Yüksek hata oranı = daha fazla sorun çıkarma riski
			if (_spillProbability > 0.2f)
			{
				_disloyaltyRisk += 0.03f;
			}
		}
		
		// Özel performans faktörü hesaplama - StaffBase.CalculateSpecialPerformanceFactor() metodunu override eder
		protected override float CalculateSpecialPerformanceFactor()
		{
			// Hız ve denge faktörü
			return (_speedSkill * 0.4f + _balanceSkill * 0.3f + _memorySkill * 0.3f - 0.5f) * 0.2f;
		}
		
		// Özellik değerlerini döndür
		public new Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = base.GetStats();
			
			// Garson-spesifik değerleri ekle
			stats["SpeedSkill"] = _speedSkill;
			stats["BalanceSkill"] = _balanceSkill;
			stats["MemorySkill"] = _memorySkill;
			stats["UpsellingSkill"] = _upsellingSkill;
			stats["OrdersServed"] = _ordersServed;
			stats["OrdersMissed"] = _ordersMissed;
			stats["UpsellSuccess"] = _upsellSuccess;
			stats["SpillProbability"] = _spillProbability;
			stats["CarryingCapacity"] = _carryingCapacity;
			
			return stats;
		}
	}
}
