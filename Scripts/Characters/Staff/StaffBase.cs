using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Staff
{
	/// <summary>
	/// Personel türleri enumerasyonu
	/// </summary>
	public enum StaffType
	{
		Konsomatris,
		Waiter,
		Security,
		Musician,
		Chef,
		Cleaner,
		IllegalFloorStaff // Kaçak kat personeli
	}
	
	public partial class StaffBase : Node3D
	{
		// Temel personel kimlik bilgileri
		public string StaffID { get; protected set; }
		public string FullName { get; set; }
		public StaffType Type { get; protected set; }
		public string StaffTypeString => Type.ToString();
		public int Age { get; set; }
		public string Gender { get; set; }
		
		// İşle ilgili temel bilgiler
		protected float _salary = 0.0f;   // Güncel maaş
		protected float _tipPool = 0.0f;  // Biriken bahşişler
		public float TipPercentage { get; set; } = 0.2f;   // Bahşiş yüzdesi (varsayılan %20)
		public bool IsActive { get; set; } = true; // Aktif olarak çalışıyor mu
		protected int _workDuration = 0;  // Çalışma süresi (gün)
		public DateTime HireDate { get; protected set; }
		
		// Performans ve durum özellikleri (0.0-1.0 arası)
		protected float _loyalty = 0.7f;      // Bağlılık (yüksek değer = daha sadık)
		protected float _energy = 1.0f;       // Enerji seviyesi (düşer, dinlenme ile yükselir)
		protected float _mood = 0.7f;         // Ruh hali (etkileşimlere göre değişir)
		protected float _satisfaction = 0.6f;  // İş memnuniyeti
		
		// İhbar ve sadakatsizlik riski (0.0-1.0 arası)
		protected float _disloyaltyRisk = 0.0f;  // Sadakatsizlik riski
		protected float _reportRisk = 0.0f;      // Polise ihbar etme riski
		
		// Çalışan temel yetenekleri (0.0-1.0 arası değerler)
		protected float _charisma;       // Karizma
		protected float _diligence;      // Çalışkanlık
		protected float _intelligence;   // Zeka
		protected float _strength;       // Fiziksel güç
		
		// Tecrübe ve gelişim
		protected int _experiencePoints = 0;
		protected int _level = 1;
		
		// Beceri değerleri (0.0-1.0 arası)
		protected Dictionary<string, float> _skills = new Dictionary<string, float>();
		
		// Kişisel özellikler
		protected List<string> _traits = new List<string>();  // Karakter özellikleri
		protected Dictionary<string, float> _traitValues = new Dictionary<string, float>(); // Trait değerleri
		
		// Personel aktivite durumu
		public enum ActivityState
		{
			Idle,       // Boşta
			Working,    // Çalışıyor
			Resting,    // Dinleniyor
			Talking,    // Konuşuyor
			Special,    // Özel aktivite (dans, performans vb.)
			Unavailable // Kullanılamaz (hasta, izinli vb.)
		}
		
		protected ActivityState _currentActivity = ActivityState.Idle;
		
		// 3D Model ve animasyon özellikleri
		protected Node3D _model;
		protected AnimationPlayer _animationPlayer;
		
		// Yürüme yolu ve hedefler
		protected Vector3 _targetPosition;
		protected float _moveSpeed = 2.0f;    // Yürüme hızı
		protected bool _isMoving = false;
		
		// Signals
		[Signal]
		public delegate void LoyaltyChangedEventHandler(float previousLoyalty, float newLoyalty);
		
		[Signal]
		public delegate void EnergyChangedEventHandler(float previousEnergy, float newEnergy);
		
		[Signal]
		public delegate void MoodChangedEventHandler(float previousMood, float newMood);
		
		[Signal]
		public delegate void ActivityChangedEventHandler(ActivityState previousActivity, ActivityState newActivity);
		
		[Signal]
		public delegate void LevelUpEventHandler(int previousLevel, int newLevel);
		
		// Public özellikler
		public float Loyalty => _loyalty;
		public float Energy => _energy;
		public float Mood => _mood;
		public float Satisfaction => _satisfaction;
		public ActivityState CurrentActivity => _currentActivity;
		public float DisloyaltyRisk => _disloyaltyRisk;
		public float ReportRisk => _reportRisk;
		public int Level => _level;
		public int ExperiencePoints => _experiencePoints;
		public float Salary => _salary;
		public float TipPool => _tipPool;
		public float Charisma => _charisma;
		public float Diligence => _diligence;
		public float Intelligence => _intelligence;
		public float Strength => _strength;
		
		// Boş yapıcı
		public StaffBase() : base() 
		{
			StaffID = Guid.NewGuid().ToString();
			HireDate = DateTime.Now;
		}
		
		// Parametreli yapıcı
		public StaffBase(string fullName, int age, string gender, float salary, StaffType type) : base()
		{
			StaffID = Guid.NewGuid().ToString();
			FullName = fullName;
			Age = age;
			Gender = gender;
			_salary = salary;
			Type = type;
			TipPercentage = 0.2f; // Varsayılan bahşiş payı %20
			HireDate = DateTime.Now;
			
			// Rastgele temel yetenek değerleri ata (0.3-0.7 arası)
			Random rnd = new Random();
			_charisma = (float)rnd.NextDouble() * 0.4f + 0.3f;
			_diligence = (float)rnd.NextDouble() * 0.4f + 0.3f;
			_intelligence = (float)rnd.NextDouble() * 0.4f + 0.3f;
			_strength = (float)rnd.NextDouble() * 0.4f + 0.3f;
			
			// Rastgele ruh hali ve enerji
			_mood = (float)rnd.NextDouble() * 0.3f + 0.5f;  // 0.5-0.8 arası
			_energy = (float)rnd.NextDouble() * 0.2f + 0.8f; // 0.8-1.0 arası
		}
		
		// Temel başlatma
		public virtual void Initialize(StaffType staffType)
		{
			Type = staffType;
			
			// Personel tipine göre temel becerileri tanımla
			InitializeSkills();
			
			// 3D modeli ve animasyonları yükle
			LoadModelAndAnimations();
			
			GD.Print($"{StaffTypeString} {Name} initialized.");
		}
		
		public override void _Ready()
		{
			base._Ready();
			
			// Animasyon player ve model referanslarını al
			if (HasNode("AnimationPlayer"))
				_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
			
			if (_model == null && HasNode("StaffModel"))
				_model = GetNode<Node3D>("StaffModel");
			
			// İlk beceri ve özellikleri rastgele oluştur
			if (_skills.Count == 0)
			{
				GenerateRandomAttributes();
			}
			
			// Alt sınıflar için hazırlık metodu
			OnReady();
		}
		
		// Alt sınıflar için hazırlık metodu 
		protected virtual void OnReady() { }
		
		public override void _Process(double delta)
		{
			// Hareket ve animasyon güncelleme
			if (_isMoving)
			{
				ProcessMovement((float)delta);
			}
			
			// Enerji tüketimi (çalışırken)
			if (_currentActivity == ActivityState.Working)
			{
				ConsumeEnergy((float)delta);
			}
			// Enerji yenilenmesi (dinlenirken)
			else if (_currentActivity == ActivityState.Resting)
			{
				RegainEnergy((float)delta);
			}
		}
		
		// Tip özelliklerine göre becerileri başlat
		protected virtual void InitializeSkills()
		{
			// Temel beceriler (tüm personel tipleri için)
			_skills["efficiency"] = 0.5f;      // Verimlilik
			_skills["customerSatisfaction"] = 0.5f;  // Müşteri memnuniyeti
			_skills["performance"] = 0.5f;     // Performans
			_skills["reliability"] = 0.5f;     // Güvenilirlik
			
			// Personel tipine göre özel beceriler
			switch (Type)
			{
				case StaffType.Konsomatris:
					_skills["charm"] = 0.5f;           // Cazibe
					_skills["conversation"] = 0.5f;    // Sohbet yeteneği
					_skills["dancing"] = 0.5f;         // Dans yeteneği
					_skills["persuasion"] = 0.5f;      // İkna yeteneği
					break;
				case StaffType.Security:
					_skills["intimidation"] = 0.5f;    // Korkutuculuk
					_skills["awareness"] = 0.5f;       // Farkındalık
					_skills["strength"] = 0.5f;        // Güç
					_skills["conflictResolution"] = 0.5f; // Çatışma çözümü
					break;
				case StaffType.Waiter:
					_skills["speed"] = 0.5f;           // Hız
					_skills["memory"] = 0.5f;          // Hafıza
					_skills["balance"] = 0.5f;         // Denge
					_skills["upselling"] = 0.5f;       // Satış artırma
					break;
				case StaffType.Musician:
					_skills["technique"] = 0.5f;       // Teknik
					_skills["creativity"] = 0.5f;      // Yaratıcılık
					_skills["repertoire"] = 0.5f;      // Repertuar
					_skills["stagePrescence"] = 0.5f;  // Sahne hakimiyeti
					break;
				case StaffType.Chef:
					_skills["cooking"] = 0.5f;         // Yemek pişirme
					_skills["presentation"] = 0.5f;    // Sunum
					_skills["innovation"] = 0.5f;      // Yenilikçilik
					_skills["consistency"] = 0.5f;     // Tutarlılık
					break;
				case StaffType.Cleaner:
					_skills["thoroughness"] = 0.5f;    // Titizlik
					_skills["speed"] = 0.5f;           // Hız
					_skills["discretion"] = 0.5f;      // Gizlilik
					_skills["maintenance"] = 0.5f;     // Bakım
					break;
				case StaffType.IllegalFloorStaff:
					_skills["discretion"] = 0.5f;      // Gizlilik
					_skills["streetSmart"] = 0.5f;     // Sokak zekası
					_skills["composure"] = 0.5f;       // Soğukkanlılık
					_skills["negotiation"] = 0.5f;     // Pazarlık
					break;
			}
		}
		
		// Rastgele özellikler oluştur
		protected virtual void GenerateRandomAttributes()
		{
			// Rastgele beceri başlangıç değerleri
			RandomAdjustSkills(0.1f);
			
			// Rastgele ruh hali ve enerji
			_mood = GD.Randf() * 0.3f + 0.5f;  // 0.5-0.8 arası
			_energy = GD.Randf() * 0.2f + 0.8f; // 0.8-1.0 arası
		}
		
		// 3D model ve animasyonları yükle
		protected virtual void LoadModelAndAnimations()
		{
			// Not: Bu metod gerçek uygulamada model ve animasyonları yükleyecek
			// Şu an için sadece konsept olarak var
			
			// Örnek model yükleme (gerçek uygulamada değişecek)
			_model = new Node3D();
			AddChild(_model);
			
			// Animasyon oynatıcı
			if (_animationPlayer == null)
			{
				_animationPlayer = new AnimationPlayer();
				AddChild(_animationPlayer);
			}
			
			// Sahte model (test için)
			MeshInstance3D placeholder = new MeshInstance3D();
			placeholder.Mesh = new CapsuleMesh(); // Basit bir placeholder mesh
			_model.AddChild(placeholder);
		}
		
		// Beceriyi trait'e göre ayarla
		public void AdjustSkillByTrait(string skillName, float amount)
		{
			if (_skills.ContainsKey(skillName))
			{
				_skills[skillName] = Mathf.Clamp(_skills[skillName] + amount, 0.0f, 1.0f);
				GD.Print($"{Name}'s {skillName} adjusted by {amount} due to trait. New value: {_skills[skillName]}");
			}
		}
		
		// Tüm becerileri rastgele ayarla
		protected void RandomAdjustSkills(float maxVariation)
		{
			foreach (var skill in _skills.Keys.ToArray())
			{
				float adjustment = (GD.Randf() * 2.0f - 1.0f) * maxVariation; // -maxVariation to +maxVariation
				_skills[skill] = Mathf.Clamp(_skills[skill] + adjustment, 0.1f, 0.9f);
			}
		}
		
		// Beceri seviyesini artır
		public void ImproveSkill(string skillName, float amount)
		{
			if (_skills.ContainsKey(skillName))
			{
				float previousValue = _skills[skillName];
				_skills[skillName] = Mathf.Clamp(_skills[skillName] + amount, 0.0f, 1.0f);
				
				if (_skills[skillName] > previousValue)
				{
					GD.Print($"{Name}'s {skillName} improved from {previousValue} to {_skills[skillName]}");
					
					// Beceri gelişimi memnuniyeti artırır
					AdjustSatisfaction(amount * 0.5f, "Skill Improvement");
				}
			}
		}
		
		// Beceri geliştirme (düzenli çalışma ile)
		public virtual void ImproveSkills()
		{
			// Her personel tipine göre beceri gelişimini simüle et
			float baseImprovement = 0.005f; // Günlük temel gelişim
			
			// Trait etkileri
			float improvementMultiplier = 1.0f;
			foreach (string trait in _traits)
			{
				if (trait == "FastLearner") improvementMultiplier *= 1.5f;
				else if (trait == "Rookie") improvementMultiplier *= 1.2f;
				else if (trait == "Lazy") improvementMultiplier *= 0.7f;
			}
			
			// Çalışma süresi etkisi (tecrübeyle gelişim yavaşlar)
			float experienceModifier = 1.0f / (1.0f + (_workDuration * 0.01f));
			
			// Son gelişim miktarı
			float finalImprovement = baseImprovement * improvementMultiplier * experienceModifier;
			
			// Beceri tiplerine göre rastgele gelişim
			List<string> skillKeys = new List<string>(_skills.Keys);
			
			// 1-3 beceriyi rastgele geliştir
			int skillsToImprove = GD.RandRange(1, 3);
			for (int i = 0; i < skillsToImprove; i++)
			{
				if (skillKeys.Count > 0)
				{
					int index = GD.RandRange(0, skillKeys.Count - 1);
					string skillToImprove = skillKeys[index];
					skillKeys.RemoveAt(index);
					
					ImproveSkill(skillToImprove, finalImprovement);
				}
			}
		}
		
		// Bağlılık değişimi
		public void AdjustLoyalty(float amount, string reason = "")
		{
			float previousLoyalty = _loyalty;
			_loyalty = Mathf.Clamp(_loyalty + amount, 0.0f, 1.0f);
			
			if (previousLoyalty != _loyalty)
			{
				EmitSignal(SignalName.LoyaltyChanged, previousLoyalty, _loyalty);
				
				string change = amount > 0 ? "increased" : "decreased";
				GD.Print($"{Name}'s loyalty {change} by {Mathf.Abs(amount)} due to {reason}. New loyalty: {_loyalty}");
				
				// Kritik bağlılık seviyesi (0.3 altı tehlikeli)
				if (_loyalty < 0.3f && previousLoyalty >= 0.3f)
				{
					GD.Print($"WARNING: {Name}'s loyalty has fallen to a dangerous level!");
				}
			}
		}
		
		// Enerji değişimi
		public void AdjustEnergy(float amount, string reason = "")
		{
			float previousEnergy = _energy;
			_energy = Mathf.Clamp(_energy + amount, 0.0f, 1.0f);
			
			if (previousEnergy != _energy)
			{
				EmitSignal(SignalName.EnergyChanged, previousEnergy, _energy);
				
				// Kritik enerji seviyesi (0.2 altı yorgun)
				if (_energy < 0.2f && previousEnergy >= 0.2f)
				{
					GD.Print($"WARNING: {Name} is getting exhausted!");
					
					// Çok yorgunluk ruh halini etkiler
					AdjustMood(-0.1f, "Exhaustion");
				}
			}
		}
		
		// Ruh hali değişimi
		public void AdjustMood(float amount, string reason = "")
		{
			float previousMood = _mood;
			_mood = Mathf.Clamp(_mood + amount, 0.0f, 1.0f);
			
			if (previousMood != _mood)
			{
				EmitSignal(SignalName.MoodChanged, previousMood, _mood);
				
				string change = amount > 0 ? "improved" : "worsened";
				GD.Print($"{Name}'s mood {change} by {Mathf.Abs(amount)} due to {reason}. New mood: {_mood}");
				
				// Ruh hali performansı etkiler
				if (_mood < 0.3f)
				{
					// Kötü ruh hali bağlılığı yavaşça düşürür
					AdjustLoyalty(-0.01f, "Bad Mood");
				}
			}
		}
		
		// Memnuniyet değişimi
		public void AdjustSatisfaction(float amount, string reason = "")
		{
			float previousSatisfaction = _satisfaction;
			_satisfaction = Mathf.Clamp(_satisfaction + amount, 0.0f, 1.0f);
			
			if (previousSatisfaction != _satisfaction)
			{
				string change = amount > 0 ? "increased" : "decreased";
				GD.Print($"{Name}'s job satisfaction {change} by {Mathf.Abs(amount)} due to {reason}. New satisfaction: {_satisfaction}");
				
				// Memnuniyet zamanla bağlılığı etkiler
				if (_satisfaction < 0.3f)
				{
					AdjustLoyalty(-0.02f, "Low Job Satisfaction");
				}
				else if (_satisfaction > 0.7f)
				{
					AdjustLoyalty(0.01f, "High Job Satisfaction");
				}
			}
		}
		
		// Aktivite değişimi
		public void SetActivity(ActivityState newActivity)
		{
			ActivityState previousActivity = _currentActivity;
			_currentActivity = newActivity;
			
			if (previousActivity != _currentActivity)
			{
				EmitSignal(SignalName.ActivityChanged, (int)previousActivity, (int)_currentActivity);
				
				// Aktiviteye uygun animasyon oynat
				PlayActivityAnimation(newActivity);
				
				GD.Print($"{Name}'s activity changed from {previousActivity} to {_currentActivity}");
			}
		}
		
		// Aktiviteye uygun animasyon
		protected virtual void PlayActivityAnimation(ActivityState activity)
		{
			if (_animationPlayer == null) return;
			
			switch (activity)
			{
				case ActivityState.Idle:
					PlayAnimation("idle");
					break;
				case ActivityState.Working:
					PlayAnimation("work");
					break;
				case ActivityState.Resting:
					PlayAnimation("rest");
					break;
				case ActivityState.Talking:
					PlayAnimation("talk");
					break;
				case ActivityState.Special:
					PlaySpecialAnimation();
					break;
				case ActivityState.Unavailable:
					PlayAnimation("unavailable");
					break;
			}
		}
		
		// Animasyon oynat
		protected void PlayAnimation(string animName)
		{
			if (_animationPlayer != null && _animationPlayer.HasAnimation(animName))
			{
				_animationPlayer.Play(animName);
			}
		}
		
		// Özel animasyon (her personel tipi kendi özel animasyonunu tanımlar)
		protected virtual void PlaySpecialAnimation()
		{
			// Alt sınıflar override edecek
		}
		
		// Hareket etme
		public void MoveTo(Vector3 position)
		{
			_targetPosition = position;
			_isMoving = true;
			
			// Hareket animasyonu başlat
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
				PlayAnimation("idle");
				
				return;
			}
			
			// Hareket etme yönünü normalize et
			direction = direction.Normalized();
			
			// Pozisyonu güncelle
			Position += direction * _moveSpeed * delta;
		}
		
		// Enerji tüketme
		protected void ConsumeEnergy(float delta)
		{
			// Çalışma zorluğuna ve personel tipine göre enerji tüketimi
			float consumptionRate = 0.02f; // Saatte %2 azalma
			
			// Trait'lere göre tüketim
			foreach (string trait in _traits)
			{
				if (trait == "Lazy") consumptionRate *= 1.5f;
				else if (trait == "Alcoholic") consumptionRate *= 1.3f;
			}
			
			// Saatler boyunca çalışmak enerjiyi azaltır
			AdjustEnergy(-consumptionRate * delta, "Working");
		}
		
		// Enerji yenileme
		protected void RegainEnergy(float delta)
		{
			// Dinlenme verimliliğine göre enerji kazanımı
			float regenerationRate = 0.05f; // Saatte %5 artma
			
			// Trait'lere göre yenilenme
			foreach (string trait in _traits)
			{
				if (trait == "Alcoholic") regenerationRate *= 0.8f;
			}
			
			// Dinlenme enerjisini yeniler
			AdjustEnergy(regenerationRate * delta, "Resting");
		}
		
		// Bahşiş alma
		public void ReceiveTips(float amount)
		{
			if (amount <= 0) return;
			
			_tipPool += amount;
			
			// Bahşiş alma memnuniyeti ve ruh halini artırır
			AdjustMood(amount * 0.001f, "Tips");
			AdjustSatisfaction(amount * 0.0005f, "Tips");
			
			GD.Print($"{Name} received {amount} in tips. Total tips: {_tipPool}");
		}
		
		// Çalışma süresini artır
		public void IncrementWorkDuration()
		{
			_workDuration++;
		}
		
		// Günlük güncelleme - her oyun günü sonunda çağrılır
		public virtual void UpdateDaily()
		{
			// Enerjiyi yenile
			ResetEnergyForNewDay();
			
			// Bağlılık hesaplaması
			CalculateLoyalty();
			
			// Performans hesaplaması
			CalculatePerformance();
			
			// İhbar riski hesaplaması
			CalculateRisks();
			
			// Tecrübe puanı ekle
			AddExperience(1);
			
			// Çalışma süresini artır
			IncrementWorkDuration();
			
			// Beceri geliştirme
			ImproveSkills();
		}
		
		// Saatlik güncelleme - oyun saati başına çağrılır
		public virtual void UpdateHourly()
		{
			// Enerji düşüşü - saatte %5 azalır
			AdjustEnergy(-0.05f, "Hourly Work");
			
			// Düşük enerji performansı ve ruh halini etkiler
			if (_energy < 0.3f)
			{
				AdjustMood(-0.02f, "Low Energy");
			}
		}
		
		// Yeni gün için enerjiyi sıfırla
		public void ResetEnergyForNewDay()
		{
			float previousEnergy = _energy;
			_energy = 1.0f;
			EmitSignal(SignalName.EnergyChanged, previousEnergy, _energy);
		}
		
		// Trait ekleme
		public void AddTrait(string trait)
		{
			if (!_traits.Contains(trait))
			{
				_traits.Add(trait);
				GD.Print($"{Name} gained trait: {trait}");
			}
		}
		
		// Trait çıkarma
		public void RemoveTrait(string trait)
		{
			if (_traits.Contains(trait))
			{
				_traits.Remove(trait);
				GD.Print($"{Name} lost trait: {trait}");
			}
		}
		
		// Trait'leri al
		public List<string> GetTraits()
		{
			return _traits;
		}
		
		// Trait değerini ayarla
		public void SetTrait(string traitName, float value)
		{
			if (_traitValues.ContainsKey(traitName))
				_traitValues[traitName] = value;
			else
				_traitValues.Add(traitName, value);
		}
		
		// Trait değerini al
		public float GetTraitValue(string traitName)
		{
			if (_traitValues.ContainsKey(traitName))
				return _traitValues[traitName];
			
			return 0f; // Özellik yoksa 0 döndür
		}
		
		// Beceri seviyesini al
		public float GetSkillLevel(string skillName = "")
		{
			// Belirli bir beceri isteniyorsa
			if (!string.IsNullOrEmpty(skillName) && _skills.ContainsKey(skillName))
			{
				return _skills[skillName];
			}
			
			// Tüm becerilerin ortalaması
			float totalSkill = 0.0f;
			foreach (var skill in _skills.Values)
			{
				totalSkill += skill;
			}
			
			return _skills.Count > 0 ? totalSkill / _skills.Count : 0.0f;
		}
		
		// Çalışma süresini al
		public int GetWorkDuration()
		{
			return _workDuration;
		}
		
		// Bağlılık hesaplama
		protected virtual void CalculateLoyalty()
		{
			// Maaş memnuniyeti (düşük maaş = düşük bağlılık)
			float salaryFactor = CalculateSalaryFactor();
			
			// Bahşiş politikası etkisi
			float tipFactor = TipPercentage >= 0.5f ? 0.1f : TipPercentage >= 0.3f ? 0.05f : 0f;
			
			// Ruh hali etkisi
			float moodFactor = (_mood - 0.5f) / 10f;
			
			// Günlük değişim
			float dailyChange = salaryFactor + tipFactor + moodFactor;
			
			// Bağlılık değerini güncelle
			AdjustLoyalty(dailyChange, "Daily Loyalty Calculation");
		}
		
		// Maaş faktörü hesaplama - personel türüne göre farklı olabilir
		protected virtual float CalculateSalaryFactor()
		{
			float expectedSalary = 5000f + (_level * 1000f); // Basit bir beklenti formülü
			
			if (_salary >= expectedSalary * 1.5f)
				return 0.02f;  // Çok memnun
			else if (_salary >= expectedSalary)
				return 0.01f;  // Memnun
			else if (_salary >= expectedSalary * 0.8f)
				return -0.01f; // Biraz memnuniyetsiz
			else
				return -0.02f; // Çok memnuniyetsiz
		}
		
		// Performans hesaplama
		protected virtual void CalculatePerformance()
		{
			// Beceri performansı - ortama beceri seviyesi
			float skillPerformance = GetSkillLevel();
			
			// Enerji seviyesi etkisi
			float energyFactor = _energy;
			
			// Ruh hali etkisi
			float moodFactor = _mood;
			
			// Bağlılık etkisi
			float loyaltyFactor = _loyalty;
			
			// Temel performans faktörü (0-1 arası)
			float performance = (skillPerformance + energyFactor + moodFactor + loyaltyFactor) / 4f;
			
			// Alt sınıflar için özel faktörler
			float specialFactor = CalculateSpecialPerformanceFactor();
			
			// Skills'de performance değerini güncelle
			_skills["performance"] = Mathf.Clamp(performance + specialFactor, 0.0f, 1.0f);
		}
		
		// Özel performans faktörü - alt sınıflarda uygulanır
		protected virtual float CalculateSpecialPerformanceFactor()
		{
			return 0f; // Varsayılan olarak etki yok
		}
		
		// Risk hesaplama (ihbar etme, kaçıp gitme riski)
		protected virtual void CalculateRisks()
		{
			// Bağlılık düşükse risk artar
			float loyaltyRiskFactor = Mathf.Max(0f, (0.5f - _loyalty) / 10f);
			
			// Maaş faktörü - düşük maaş riski artırır
			float salaryRiskFactor = CalculateSalaryFactor() < 0f ? Mathf.Abs(CalculateSalaryFactor()) * 2f : 0f;
			
			// Ruh hali faktörü - kötü ruh hali riski artırır
			float moodRiskFactor = Mathf.Max(0f, (0.5f - _mood) / 10f);
			
			// Sadakatsizlik riski - kaçma ve casusluk
			_disloyaltyRisk = Mathf.Clamp(loyaltyRiskFactor + salaryRiskFactor + moodRiskFactor, 0.0f, 1.0f);
			
			// İhbar riski - temel olarak çok daha düşük
			// İllegal kat personeli değilse ve şahit olduğu yasadışı olaylar varsa artar
			_reportRisk = _disloyaltyRisk * 0.2f; // Temel ihbar riski sadakatsizliğin %20'si
			
			// Alt sınıflar için özel risk faktörleri eklenecek
			ApplySpecialRiskFactors();
		}
		
		// Özel risk faktörleri - alt sınıflarda uygulanır
		protected virtual void ApplySpecialRiskFactors() { }
		
		// Tecrübe ekleme ve seviye atlama
		public void AddExperience(int points)
		{
			_experiencePoints += points;
			
			// Seviye atlama kontrolü - basit bir formül
			int requiredXP = _level * 100;
			
			if (_experiencePoints >= requiredXP)
			{
				LevelUp();
			}
		}
		
		// Seviye atlama
		protected virtual void LevelUp()
		{
			int previousLevel = _level;
			_level++;
			
			// Temel yeteneklerde gelişim
			_charisma = Mathf.Min(1.0f, _charisma + 0.02f);
			_diligence = Mathf.Min(1.0f, _diligence + 0.02f);
			_intelligence = Mathf.Min(1.0f, _intelligence + 0.02f);
			_strength = Mathf.Min(1.0f, _strength + 0.02f);
			
			// Ruh halinde iyileşme
			AdjustMood(0.2f, "Level Up");
			
			// Alt sınıflar için özel gelişimler
			OnLevelUp();
			
			EmitSignal(SignalName.LevelUp, previousLevel, _level);
			
			GD.Print($"{Name} seviye atladı! Yeni seviye: {_level}");
		}
		
		// Seviye atlama özel efektleri - alt sınıflarda uygulanır
		protected virtual void OnLevelUp() { }
		
		// Maaş zammı
		public void GiveRaise(float percentageIncrease)
		{
			float oldSalary = _salary;
			_salary *= (1f + (percentageIncrease / 100f));
			
			// Zam ruh halini iyileştirir
			AdjustMood(percentageIncrease / 100f, "Salary Increase");
			
			// Zam bağlılığı artırır
			AdjustLoyalty(percentageIncrease / 200f, "Salary Increase");
			
			GD.Print($"{Name} maaş zammı aldı! {oldSalary} -> {_salary}");
		}
		
		// Yeni maaş ayarla
		public void SetSalary(float newSalary)
		{
			float oldSalary = _salary;
			_salary = newSalary;
			
			// Maaş artışı bağlılığı artırır
			if (newSalary > oldSalary)
			{
				float increase = (newSalary - oldSalary) / oldSalary;
				AdjustLoyalty(increase * 0.1f, "Salary Increase");
				AdjustSatisfaction(increase * 0.2f, "Salary Increase");
				
				GD.Print($"{Name}'s salary increased from {oldSalary} to {newSalary}");
			}
			// Maaş düşüşü bağlılığı azaltır
			else if (newSalary < oldSalary)
			{
				float decrease = (oldSalary - newSalary) / oldSalary;
				AdjustLoyalty(-decrease * 0.2f, "Salary Decrease");
				AdjustSatisfaction(-decrease * 0.3f, "Salary Decrease");
				
				GD.Print($"{Name}'s salary decreased from {oldSalary} to {newSalary}");
			}
		}
		
		// Ceza verme
		public void Penalize(string reason, float moodPenalty = 0.1f, float loyaltyPenalty = 0.05f)
		{
			AdjustMood(-moodPenalty, $"Penalty: {reason}");
			AdjustLoyalty(-loyaltyPenalty, $"Penalty: {reason}");
			
			GD.Print($"{Name} ceza aldı! Sebep: {reason}");
		}
		
		// Ödüllendirme
		public void Reward(string reason, float moodBonus = 0.1f, float loyaltyBonus = 0.05f)
		{
			AdjustMood(moodBonus, $"Reward: {reason}");
			AdjustLoyalty(loyaltyBonus, $"Reward: {reason}");
			
			GD.Print($"{Name} ödül aldı! Sebep: {reason}");
		}
		
		// Müşteri ile etkileşim (farklı personel tipleri farklı şekilde davranır)
		public virtual void InteractWithCustomer(Node3D customer)
		{
			// Temel etkileşim - Alt sınıflar override edecek
			GD.Print($"{Name} is interacting with a customer.");
			
			// Enerji harcama
			AdjustEnergy(-0.01f, "Customer Interaction");
		}
		
		// Özelleştirilmiş davranış (her personel tipi kendi özel davranışını uygular)
		public virtual void PerformSpecialBehavior()
		{
			// Alt sınıflar override edecek
			GD.Print($"{Name} is performing a special behavior.");
			
			// Aktiviteyi özel olarak işaretle
			SetActivity(ActivityState.Special);
		}
		
		// Dinlenme alanına git
		public void GoToRestArea()
		{
			// Dinlenme alanının konumunu al
			Vector3 restAreaPosition = GetRestAreaPosition();
			
			// Dinlenme alanına git
			MoveTo(restAreaPosition);
			
			// Dinlenme aktivitesine geç (hareket tamamlandığında)
			SetActivity(ActivityState.Resting);
		}
		
		// Dinlenme alanı pozisyonu alma
		protected virtual Vector3 GetRestAreaPosition()
		{
			// Bu metod gerçek uygulamada GameManager'dan veya benzeri bir sınıftan alınacak
			// Şimdilik sadece varsayılan bir konum döndürelim
			return new Vector3(0, 0, 0);
		}
		
		// İşten çıkar
		public virtual void Fire()
		{
			IsActive = false;
			GD.Print($"{Name} işten çıkarıldı!");
			
			// İşten çıkarma animasyonu
			PlayAnimation("fired");
			
			// 3D sahnesinden kaldırma
			QueueFree();
		}
		
		// İstatistikleri döndür
		public Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = new Dictionary<string, object>
			{
				{ "Name", Name },
				{ "StaffID", StaffID },
				{ "StaffType", StaffTypeString },
				{ "Age", Age },
				{ "Gender", Gender },
				{ "Loyalty", _loyalty },
				{ "Energy", _energy },
				{ "Mood", _mood },
				{ "Satisfaction", _satisfaction },
				{ "Performance", _skills.ContainsKey("performance") ? _skills["performance"] : 0.0f },
				{ "Skills", _skills },
				{ "Traits", _traits },
				{ "WorkDuration", _workDuration },
				{ "Salary", _salary },
				{ "TipPool", _tipPool },
				{ "Level", _level },
				{ "ExperiencePoints", _experiencePoints },
				{ "DisloyaltyRisk", _disloyaltyRisk },
				{ "ReportRisk", _reportRisk },
				{ "Charisma", _charisma },
				{ "Diligence", _diligence },
				{ "Intelligence", _intelligence },
				{ "Strength", _strength }
			};
			
			return stats;
		}
	}
}
