using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Staff
{
	
	public partial class Security : StaffBase
	{
		// Güvenlik özellikleri
		private float _intimidationSkill = 0.5f;      // Korkutma becerisi
		private float _awarenessSkill = 0.5f;         // Farkındalık becerisi
		private float _strengthValue = 0.5f;          // Güç değeri
		private float _conflictResolutionSkill = 0.5f; // Çatışma çözümü becerisi
		
		// Güvenlik istatistikleri
		private int _fightsResolved = 0;          // Çözülen kavga sayısı
		private int _customersEjected = 0;        // Çıkarılan müşteri sayısı
		private int _threatsNeutralized = 0;      // Etkisiz hale getirilen tehdit sayısı
		private int _injuriesReceived = 0;        // Alınan yaralanma sayısı
		
		// Güvenlik durumu
		private bool _isPatrolling = false;           // Devriye geziyor mu
		private List<Vector3> _patrolPoints = new List<Vector3>(); // Devriye noktaları
		private int _currentPatrolIndex = 0;          // Mevcut devriye noktası indeksi
		private bool _isHandlingConflict = false;     // Çatışma ile ilgileniyor mu
		private bool _isIncapacitated = false;        // Yaralanma nedeniyle etkisiz hale geldi mi
		private float _recoveryTime = 0.0f;           // İyileşme süresi
		
		// Kimlik kontrolü
		private HashSet<string> _blacklistedCustomers = new HashSet<string>(); // Kara listeye alınan müşteriler
		private HashSet<string> _vipCustomers = new HashSet<string>(); // VIP müşteriler
		
		// Signals
		[Signal]
		public delegate void FightResolvedEventHandler(Vector3 location, float effectivenessScore);
		
		[Signal]
		public delegate void CustomerEjectedEventHandler(string customerId, string reason);
		
		[Signal]
		public delegate void SecurityInjuredEventHandler(float injurySeverity, float recoveryTime);
		
		[Signal]
		public delegate void SuspiciousActivityDetectedEventHandler(Vector3 location, string activityType);
		
		public override void _Ready()
		{
			base._Ready();
			
			// Tipi ayarla
			Type = StaffType.Security;
			
			// Beceri değerlerini başlat (StaffBase'deki skills sözlüğünden al)
			if (_skills.ContainsKey("intimidation")) _intimidationSkill = _skills["intimidation"];
			if (_skills.ContainsKey("awareness")) _awarenessSkill = _skills["awareness"];
			if (_skills.ContainsKey("strength")) _strengthValue = _skills["strength"];
			if (_skills.ContainsKey("conflictResolution")) _conflictResolutionSkill = _skills["conflictResolution"];
			
			// Ayrıca temel değerlerden güç değerini de al
			_strengthValue = Mathf.Max(_strengthValue, Strength);
			
			// Trait'lere göre değerleri düzenle
			AdjustValuesByTraits();
			
			// Devriye noktalarını belirle
			SetupPatrolPoints();
			
			GD.Print($"Security {Name} initialized with intimidation: {_intimidationSkill}, strength: {_strengthValue}, awareness: {_awarenessSkill}");
		}
		
		// Devriye noktalarını ayarla
		private void SetupPatrolPoints()
		{
			// Devriye noktalarını sahneden bul
			var currentScene = GetTree().CurrentScene;
			
			if (currentScene != null)
			{
				if (currentScene.HasNode("SecurityPatrolPoints"))
				{
					var patrolPointsNode = currentScene.GetNode("SecurityPatrolPoints");
					
					foreach (Node child in patrolPointsNode.GetChildren())
					{
						if (child is Node3D point)
						{
							_patrolPoints.Add(point.GlobalPosition);
						}
					}
				}
			}
			
			// Eğer sahnede devriye noktası yoksa, varsayılan noktaları ekle
			if (_patrolPoints.Count == 0)
			{
				// Varsayılan devriye yolu (kare şeklinde)
				_patrolPoints.Add(new Vector3(10, 0, 10));
				_patrolPoints.Add(new Vector3(-10, 0, 10));
				_patrolPoints.Add(new Vector3(-10, 0, -10));
				_patrolPoints.Add(new Vector3(10, 0, -10));
			}
			
			GD.Print($"Security {Name} set up {_patrolPoints.Count} patrol points");
		}
		
		// Trait'lere göre değerleri düzenle
		private void AdjustValuesByTraits()
		{
			foreach (string trait in GetTraits())
			{
				switch (trait)
				{
					case "Intimidating": // Özel güvenlik trait'i
						_intimidationSkill = Mathf.Min(1.0f, _intimidationSkill + 0.2f);
						break;
					case "Professional":
						_conflictResolutionSkill = Mathf.Min(1.0f, _conflictResolutionSkill + 0.15f);
						_awarenessSkill = Mathf.Min(1.0f, _awarenessSkill + 0.1f);
						break;
					case "Experienced":
						_awarenessSkill = Mathf.Min(1.0f, _awarenessSkill + 0.15f);
						break;
					case "Aggressive": // Özel güvenlik trait'i
						_intimidationSkill = Mathf.Min(1.0f, _intimidationSkill + 0.15f);
						_conflictResolutionSkill = Mathf.Max(0.1f, _conflictResolutionSkill - 0.1f);
						break;
					case "Observant": // Özel güvenlik trait'i
						_awarenessSkill = Mathf.Min(1.0f, _awarenessSkill + 0.25f);
						break;
					case "Strong": // Özel güvenlik trait'i
						_strengthValue = Mathf.Min(1.0f, _strengthValue + 0.25f);
						break;
					case "Calm": // Özel güvenlik trait'i
						_conflictResolutionSkill = Mathf.Min(1.0f, _conflictResolutionSkill + 0.2f);
						break;
					case "Lazy":
						_awarenessSkill = Mathf.Max(0.1f, _awarenessSkill - 0.15f);
						break;
					case "Alcoholic":
						_awarenessSkill = Mathf.Max(0.1f, _awarenessSkill - 0.2f);
						_intimidationSkill = Mathf.Max(0.1f, _intimidationSkill - 0.1f);
						break;
				}
			}
		}
		
		public override void _Process(double delta)
		{
			base._Process(delta);
			
			// Eğer yaralanma nedeniyle etkisiz hale geldiyse, iyileşme süresini azalt
			if (_isIncapacitated)
			{
				_recoveryTime -= (float)delta;
				
				if (_recoveryTime <= 0)
				{
					RecoverFromInjury();
				}
				
				return;
			}
			
			// Devriye modundaysa ve bir çatışma ile ilgilenmiyorsa, devriye gezmeye devam et
			if (_isPatrolling && !_isHandlingConflict && !_isMoving)
			{
				ContinuePatrol();
			}
			
			// Çevre gözetimi - şüpheli aktiviteler için
			if (!_isHandlingConflict && GD.Randf() < _awarenessSkill * 0.01f)
			{
				CheckForSuspiciousActivity();
			}
		}
		
		// Özel beceri geliştirme - StaffBase.ImproveSkills() metodunu override eder
		public override void ImproveSkills()
		{
			base.ImproveSkills();
			
			// Güvenlik-spesifik becerileri geliştir
			float baseImprovement = 0.005f; // Günlük temel gelişim
			
			// Çatışma çözümü deneyimle gelişir
			float experienceModifier = 1.0f + (_fightsResolved * 0.01f);
			experienceModifier = Mathf.Min(experienceModifier, 1.5f); // En fazla %50 bonus
			
			// Her beceri için gelişim
			if (GD.Randf() < 0.6f) // %60 ihtimalle korkutma gelişimi
			{
				_intimidationSkill = Mathf.Min(1.0f, _intimidationSkill + baseImprovement * experienceModifier);
				if (_skills.ContainsKey("intimidation")) 
					_skills["intimidation"] = _intimidationSkill;
			}
			
			if (GD.Randf() < 0.7f) // %70 ihtimalle farkındalık gelişimi
			{
				_awarenessSkill = Mathf.Min(1.0f, _awarenessSkill + baseImprovement * experienceModifier);
				if (_skills.ContainsKey("awareness")) 
					_skills["awareness"] = _awarenessSkill;
			}
			
			if (GD.Randf() < 0.5f) // %50 ihtimalle güç gelişimi
			{
				_strengthValue = Mathf.Min(1.0f, _strengthValue + baseImprovement * experienceModifier);
				if (_skills.ContainsKey("strength")) 
					_skills["strength"] = _strengthValue;
			}
			
			if (GD.Randf() < 0.5f) // %50 ihtimalle çatışma çözümü gelişimi
			{
				_conflictResolutionSkill = Mathf.Min(1.0f, _conflictResolutionSkill + baseImprovement * experienceModifier);
				if (_skills.ContainsKey("conflictResolution")) 
					_skills["conflictResolution"] = _conflictResolutionSkill;
			}
		}
		
		// Devriye modunu başlat
		public void StartPatrol()
		{
			if (_isIncapacitated) return;
			
			_isPatrolling = true;
			_currentPatrolIndex = 0;
			
			// İlk devriye noktasına git
			if (_patrolPoints.Count > 0)
			{
				MoveTo(_patrolPoints[_currentPatrolIndex]);
				SetActivity(ActivityState.Working);
				
				GD.Print($"Security {Name} started patrol");
			}
		}
		
		// Devriye modunda bir sonraki noktaya git
		private void ContinuePatrol()
		{
			if (_patrolPoints.Count == 0) return;
			
			// Bir sonraki devriye noktasına geç
			_currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Count;
			
			// Sonraki noktaya git
			MoveTo(_patrolPoints[_currentPatrolIndex]);
			
			// Arada bir etrafı kontrol etmek için dur
			if (GD.Randf() < 0.3f)
			{
				Timer pauseTimer = new Timer
				{
					WaitTime = 3.0f,
					OneShot = true
				};
				
				AddChild(pauseTimer);
				pauseTimer.Timeout += () => 
				{
					// Hala devriyedeyse ve çatışma yoksa devam et
					if (_isPatrolling && !_isHandlingConflict && !_isMoving)
					{
						MoveTo(_patrolPoints[_currentPatrolIndex]);
					}
				};
				pauseTimer.Start();
				
				PlayAnimation("look_around");
			}
		}
		
		// Devriye modunu durdur
		public void StopPatrol()
		{
			_isPatrolling = false;
			SetActivity(ActivityState.Idle);
			
			GD.Print($"Security {Name} stopped patrol");
		}
		
		// Şüpheli aktivite kontrolü
		private void CheckForSuspiciousActivity()
		{
			// Sahne üzerinde şüpheli aktiviteler için kontrol
			// Bu fonksiyon uygulamada oyun motorunun düğüm kontrolü ve alan taraması ile gerçekleştirilir
			
			// Farkındalık değerine göre şüpheli aktivite tespit etme şansı
			float detectionChance = _awarenessSkill * 0.5f;
			
			// Rastgele kontrol - şüpheli aktivite tespit edildi
			if (GD.Randf() < detectionChance)
			{
				// Gerçek uygulamada burada yakındaki nesnelerin kontrol edilmesi gerekir
				// Şimdilik rastgele bir lokasyon ve aktivite tipi üretelim (demo amaçlı)
				Vector3 location = new Vector3(
					(GD.Randf() * 20f) - 10f, 
					0, 
					(GD.Randf() * 20f) - 10f
				);
				
				string[] activityTypes = { "Drug Deal", "Theft", "Harassment", "Vandalism", "Illegal Gambling" };
				string activityType = activityTypes[GD.RandRange(0, activityTypes.Length - 1)];
				
				// Şüpheli aktivite sinyali gönder
				EmitSignal(SignalName.SuspiciousActivityDetected, location, activityType);
				
				// Aktivite lokasyonuna git
				MoveTo(location);
				_isHandlingConflict = true;
				
				// Aktiviteyi çözme animasyonu
				PlayAnimation("confront");
				
				// Belirli bir süre sonra çatışmayı çöz
				Timer resolutionTimer = new Timer
				{
					WaitTime = 5.0f,
					OneShot = true
				};
				
				AddChild(resolutionTimer);
				resolutionTimer.Timeout += () => 
				{
					_isHandlingConflict = false;
					_threatsNeutralized++;
					
					// Deneyim kazan
					AddExperience(2);
					
					if (_isPatrolling)
					{
						ContinuePatrol();
					}
					else
					{
						SetActivity(ActivityState.Idle);
					}
				};
				resolutionTimer.Start();
				
				GD.Print($"Security {Name} detected suspicious activity: {activityType} at {location}");
			}
		}
		
		// Kavga çözme
		public void ResolveFight(Vector3 location, List<Node3D> participants)
		{
			if (_isIncapacitated) return;
			
			// Kavga lokasyonuna git
			MoveTo(location);
			_isHandlingConflict = true;
			SetActivity(ActivityState.Working);
			
			// Korkutma ve güç değerlerine dayalı etkililik hesaplama
			float intimidationFactor = _intimidationSkill * 0.5f;
			float strengthFactor = _strengthValue * 0.3f;
			float resolutionFactor = _conflictResolutionSkill * 0.2f;
			
			float effectivenessScore = intimidationFactor + strengthFactor + resolutionFactor;
			
			// Başarısızlık durumunda yaralanma riski
			float injuryRisk = 0.6f - (effectivenessScore * 0.5f);
			injuryRisk = Mathf.Max(0.05f, injuryRisk); // En az %5 risk
			
			// Yaralanma kontrolü
			if (GD.Randf() < injuryRisk)
			{
				GetInjured(0.2f + GD.Randf() * 0.4f); // %20-%60 şiddetinde yaralanma
				return;
			}
			
			// Kavga çözme animasyonu
			PlayAnimation("resolve_fight");
			
			// Belirli bir süre sonra çatışmayı çöz
			Timer resolutionTimer = new Timer
			{
				WaitTime = 10.0f,
				OneShot = true
			};
			
			AddChild(resolutionTimer);
			resolutionTimer.Timeout += () => 
			{
				_isHandlingConflict = false;
				_fightsResolved++;
				
				// Deneyim kazan
				AddExperience(3);
				
				// Kavga çözme sinyali gönder
				EmitSignal(SignalName.FightResolved, location, effectivenessScore);
				
				if (_isPatrolling)
				{
					ContinuePatrol();
				}
				else
				{
					SetActivity(ActivityState.Idle);
				}
				
				// Gerekirse kavgadaki kişileri çıkar
				foreach (var participant in participants)
				{
					if (ShouldEjectCustomer(participant))
					{
						EjectCustomer(participant, "Fighting");
					}
				}
			};
			resolutionTimer.Start();
			
			GD.Print($"Security {Name} is resolving a fight with {participants.Count} participants. Effectiveness: {effectivenessScore}");
		}
		
		// Yaralanma
		private void GetInjured(float severity)
		{
			_isIncapacitated = true;
			_isHandlingConflict = false;
			_isPatrolling = false;
			
			// Yaralanma istatistiğini güncelle
			_injuriesReceived++;
			
			// İyileşme süresi hesapla (şiddete bağlı olarak 30s-300s arası)
			_recoveryTime = 30.0f + (severity * 270.0f);
			
			// Yaralanma animasyonu
			PlayAnimation("injured");
			
			// Yaralanma sinyali gönder
			EmitSignal(SignalName.SecurityInjured, severity, _recoveryTime);
			
			GD.Print($"Security {Name} got injured! Severity: {severity}, Recovery time: {_recoveryTime} seconds");
		}
		
		// İyileşme
		private void RecoverFromInjury()
		{
			_isIncapacitated = false;
			
			// İyileşme animasyonu
			PlayAnimation("recover");
			
			// Ruh halinde düşüş
			AdjustMood(-0.2f, "Injury");
			
			// Belirli bir süre bekle sonra normal duruma dön
			Timer recoveryTimer = new Timer
			{
				WaitTime = 3.0f,
				OneShot = true
			};
			
			AddChild(recoveryTimer);
			recoveryTimer.Timeout += () => 
			{
				SetActivity(ActivityState.Idle);
				
				// Devriyeye devam et
				if (_isPatrolling)
				{
					ContinuePatrol();
				}
			};
			recoveryTimer.Start();
			
			GD.Print($"Security {Name} recovered from injury");
		}
		
		// Kimlik kontrolü
		public bool CheckID(Node3D customer)
		{
			if (customer == null) return false;
			
			// Kimlik kontrolü için farkındalık kullan
			float detectionRate = 0.7f + (_awarenessSkill * 0.3f);
			
			// Sahte kimlik durumu (gerçek uygulamada müşteri sınıfından alınır)
			bool hasFakeID = false;
			
			if (customer.GetType().GetMethod("HasFakeID") != null)
			{
				try 
				{
					hasFakeID = (bool)customer.Call("HasFakeID");
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error calling HasFakeID: {e.Message}");
				}
			}
			
			// Yaş kontrolü (gerçek uygulamada müşteri sınıfından alınır)
			int age = 25; // Varsayılan yaş
			
			if (customer.GetType().GetProperty("Age") != null)
			{
				try 
				{
					age = (int)customer.Get("Age");
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error getting Age: {e.Message}");
				}
			}
			
			// Sahte kimlik tespiti
			if (hasFakeID && GD.Randf() < detectionRate)
			{
				// Sahte kimlik yakalandı
				EjectCustomer(customer, "Fake ID");
				return false;
			}
			
			// Yaş kontrolü
			if (age < 18)
			{
				// Yaşı tutmuyor
				EjectCustomer(customer, "Underage");
				return false;
			}
			
			// Kara liste kontrolü
			if (_blacklistedCustomers.Contains(customer.Name))
			{
				// Kara listedeki müşteri
				EjectCustomer(customer, "Blacklisted");
				return false;
			}
			
			// VIP kontrolü
			if (_vipCustomers.Contains(customer.Name))
			{
				// VIP müşteri - ekstra nazik davran
				GD.Print($"Security {Name} welcomes VIP customer {customer.Name}");
				return true;
			}
			
			return true;
		}
		
		// Müşteriyi çıkarma kararı
		private bool ShouldEjectCustomer(Node3D customer)
		{
			if (customer == null) return false;
			
			// Çıkarma kriterleri (gerçek uygulamada müşteri sınıfından alınır)
			
			// Saldırganlık seviyesi
			float aggressionLevel = 0.0f;
			
			if (customer.GetType().GetProperty("AggressionLevel") != null)
			{
				try 
				{
					aggressionLevel = (float)customer.Get("AggressionLevel");
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error getting AggressionLevel: {e.Message}");
				}
			}
			
			// Sarhoşluk seviyesi
			float drunkennessLevel = 0.0f;
			
			if (customer.GetType().GetProperty("DrunkennessLevel") != null)
			{
				try 
				{
					drunkennessLevel = (float)customer.Get("DrunkennessLevel");
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error getting DrunkennessLevel: {e.Message}");
				}
			}
			
			// Şüpheli davranış
			bool isSuspicious = false;
			
			if (customer.GetType().GetProperty("IsSuspicious") != null)
			{
				try 
				{
					isSuspicious = (bool)customer.Get("IsSuspicious");
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error getting IsSuspicious: {e.Message}");
				}
			}
			
			// Çıkarma kriterleri
			return (aggressionLevel > 0.7f) || (drunkennessLevel > 0.9f) || isSuspicious;
		}
		
		// Müşteriyi çıkar
		public void EjectCustomer(Node3D customer, string reason)
		{
			if (customer == null) return;
			
			string customerId = customer.Name;
			
			// Çıkarma animasyonu
			PlayAnimation("eject_customer");
			
			// Kara listeye ekle
			_blacklistedCustomers.Add(customerId);
			
			// Çıkarma sinyali gönder
			EmitSignal(SignalName.CustomerEjected, customerId, reason);
			
			// Müşteri sınıfına çıkarma çağrısı
			if (customer.GetType().GetMethod("GetEjected") != null)
			{
				try 
				{
					customer.Call("GetEjected", reason);
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error calling GetEjected: {e.Message}");
				}
			}
			
			// İstatistiği güncelle
			_customersEjected++;
			
			GD.Print($"Security {Name} ejected customer {customerId} for reason: {reason}");
		}
		
		// Kara liste ve VIP listesini yönetme
		public void AddToBlacklist(string customerId)
		{
			_blacklistedCustomers.Add(customerId);
			_vipCustomers.Remove(customerId); // VIP'den çıkar
			
			GD.Print($"Customer {customerId} added to blacklist");
		}
		
		public void RemoveFromBlacklist(string customerId)
		{
			_blacklistedCustomers.Remove(customerId);
			
			GD.Print($"Customer {customerId} removed from blacklist");
		}
		
		public void AddToVipList(string customerId)
		{
			_vipCustomers.Add(customerId);
			_blacklistedCustomers.Remove(customerId); // Kara listeden çıkar
			
			GD.Print($"Customer {customerId} added to VIP list");
		}
		
		public void RemoveFromVipList(string customerId)
		{
			_vipCustomers.Remove(customerId);
			
			GD.Print($"Customer {customerId} removed from VIP list");
		}
		
		// Müşteri ile etkileşim - StaffBase.InteractWithCustomer() metodunu override eder
		public override void InteractWithCustomer(Node3D customer)
		{
			if (customer == null || _isIncapacitated) return;
			
			// Müşteri kontrolü
			bool allowEntry = CheckID(customer);
			
			// Enerji tüketimi
			AdjustEnergy(-0.01f, "Customer Interaction");
			
			if (!allowEntry)
			{
				GD.Print($"Security {Name} denied entry to {customer.Name}");
				return;
			}
			
			// Şüpheli müşteri kontrolü
			if (ShouldEjectCustomer(customer))
			{
				string reason = "Suspicious Behavior";
				
				// Saldırganlık ve sarhoşluk seviyelerini kontrol et
				if (customer.GetType().GetProperty("AggressionLevel") != null)
				{
					try 
					{
						float aggressionLevel = (float)customer.Get("AggressionLevel");
						if (aggressionLevel > 0.7f) reason = "Aggression";
					}
					catch (Exception e) { }
				}
				
				if (customer.GetType().GetProperty("DrunkennessLevel") != null)
				{
					try 
					{
						float drunkennessLevel = (float)customer.Get("DrunkennessLevel");
						if (drunkennessLevel > 0.9f) reason = "Extreme Drunkenness";
					}
					catch (Exception e) { }
				}
				
				EjectCustomer(customer, reason);
			}
			else
			{
				GD.Print($"Security {Name} allowed entry to {customer.Name}");
			}
		}
		
		// Özel performans davranışı - StaffBase.PerformSpecialBehavior() metodunu override eder
		public override void PerformSpecialBehavior()
		{
			// Yüksek tetikte olma durumu
			ActivateHighAlert();
		}
		
		// Yüksek tetik modu
		public void ActivateHighAlert()
		{
			// Farkındalık becerisini geçici olarak artır
			float awarenessBoost = 0.3f;
			float duration = 120.0f; // 2 dakika
			
			// Animasyon değişimi
			SetActivity(ActivityState.Special);
			
			// Farkındalık değerini geçici olarak artır
			float originalAwareness = _awarenessSkill;
			_awarenessSkill = Mathf.Min(1.0f, _awarenessSkill + awarenessBoost);
			
			// Timer ile normal duruma dönüş
			Timer timer = new Timer
			{
				WaitTime = duration,
				OneShot = true
			};
			
			AddChild(timer);
			timer.Timeout += () => 
			{
				_awarenessSkill = originalAwareness;
				SetActivity(_isPatrolling ? ActivityState.Working : ActivityState.Idle);
				GD.Print($"Security {Name}'s high alert mode ended");
			};
			timer.Start();
			
			GD.Print($"Security {Name} activated high alert mode! Awareness: {_awarenessSkill}, Duration: {duration} seconds");
		}
		
		// Özel animasyon - StaffBase.PlaySpecialAnimation() metodunu override eder
		protected override void PlaySpecialAnimation()
		{
			// Yüksek tetik modu animasyonu
			PlayAnimation("high_alert");
		}
		
		// Seviye atlama özel efektleri - StaffBase.OnLevelUp() metodunu override eder
		protected override void OnLevelUp()
		{
			base.OnLevelUp();
			
			// Korkutma becerisi gelişimi
			_intimidationSkill = Mathf.Min(1.0f, _intimidationSkill + 0.05f);
			
			// Farkındalık becerisi gelişimi
			_awarenessSkill = Mathf.Min(1.0f, _awarenessSkill + 0.04f);
			
			// Güç değeri gelişimi
			_strengthValue = Mathf.Min(1.0f, _strengthValue + 0.04f);
			
			// Çatışma çözümü becerisi gelişimi
			_conflictResolutionSkill = Mathf.Min(1.0f, _conflictResolutionSkill + 0.03f);
			
			// Beceri değerlerini skills sözlüğüne aktar
			if (_skills.ContainsKey("intimidation")) _skills["intimidation"] = _intimidationSkill;
			if (_skills.ContainsKey("awareness")) _skills["awareness"] = _awarenessSkill;
			if (_skills.ContainsKey("strength")) _skills["strength"] = _strengthValue;
			if (_skills.ContainsKey("conflictResolution")) _skills["conflictResolution"] = _conflictResolutionSkill;
			
			GD.Print($"Security {Name} leveled up: intimidation {_intimidationSkill}, awareness {_awarenessSkill}, strength {_strengthValue}");
		}
		
		// Günlük güncelleme - StaffBase.UpdateDaily() metodunu override eder
		public override void UpdateDaily()
		{
			base.UpdateDaily();
			
			// İstatistikleri sıfırla
			_fightsResolved = 0;
			_customersEjected = 0;
			_threatsNeutralized = 0;
			_injuriesReceived = 0;
			
			// Her gün için kara liste ve VIP kontrolü - sistemin bir parçası olarak
			// Bu gerçek uygulamada yalnızca belirli günlerde veya özel olaylarla güncellenir
		}
		
		// Özel risk faktörleri - StaffBase.ApplySpecialRiskFactors() metodunu override eder
		protected override void ApplySpecialRiskFactors()
		{
			base.ApplySpecialRiskFactors();
			
			// Güvenlik-spesifik risk faktörleri
			
			// Yüksek yaralanma oranı = daha fazla sadakatsizlik riski
			if (_injuriesReceived > 1)
			{
				_disloyaltyRisk += 0.05f * _injuriesReceived;
			}
		}
		
		// Özel performans faktörü hesaplama - StaffBase.CalculateSpecialPerformanceFactor() metodunu override eder
		protected override float CalculateSpecialPerformanceFactor()
		{
			// Korkutma, farkındalık ve güç faktörü
			return (_intimidationSkill * 0.4f + _awarenessSkill * 0.3f + _strengthValue * 0.3f - 0.5f) * 0.2f;
		}
		
		// Özellik değerlerini döndür
		public new Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = base.GetStats();
			
			// Güvenlik-spesifik değerleri ekle
			stats["IntimidationSkill"] = _intimidationSkill;
			stats["AwarenessSkill"] = _awarenessSkill;
			stats["StrengthValue"] = _strengthValue;
			stats["ConflictResolutionSkill"] = _conflictResolutionSkill;
			stats["FightsResolved"] = _fightsResolved;
			stats["CustomersEjected"] = _customersEjected;
			stats["ThreatsNeutralized"] = _threatsNeutralized;
			stats["InjuriesReceived"] = _injuriesReceived;
			stats["BlacklistedCustomers"] = _blacklistedCustomers.Count;
			stats["VipCustomers"] = _vipCustomers.Count;
			
			return stats;
		}
	}
}
