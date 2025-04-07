using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PavyonTycoon.Characters.Staff
{
	public partial class StaffManager : Node
	{
		// Mevcut tüm personel
		private List<StaffBase> _allStaff = new List<StaffBase>();
		
		// Tür bazlı personel listeleri - Enum kullanımı daha tip güvenli
		private Dictionary<StaffType, List<StaffBase>> _staffByType = new Dictionary<StaffType, List<StaffBase>>();
		
		// String anahtar olarak personel listeleri - Geriye dönük uyumluluk için
		private Dictionary<string, List<StaffBase>> _staffByTypeString = new Dictionary<string, List<StaffBase>>();
		
		// Personel yüklemesi için 3D yerleşim noktaları
		private Dictionary<StaffType, List<Node3D>> _staffPositions = new Dictionary<StaffType, List<Node3D>>();
		
		// Personel türü başına maksimum sayı
		private Dictionary<StaffType, int> _maxStaffByType = new Dictionary<StaffType, int>
		{
			{ StaffType.Konsomatris, 10 },
			{ StaffType.Waiter, 5 },
			{ StaffType.Security, 3 },
			{ StaffType.Musician, 5 },
			{ StaffType.Chef, 2 },
			{ StaffType.Cleaner, 3 },
			{ StaffType.IllegalFloorStaff, 5 }
		};
		
		// Personel türlerine göre temel maaş
		private Dictionary<StaffType, float> _baseSalaryByType = new Dictionary<StaffType, float>
		{
			{ StaffType.Konsomatris, 5000f },
			{ StaffType.Waiter, 3500f },
			{ StaffType.Security, 4500f },
			{ StaffType.Musician, 4000f },
			{ StaffType.Chef, 6000f },
			{ StaffType.Cleaner, 3000f },
			{ StaffType.IllegalFloorStaff, 7000f }
		};
		
		// Günlük maaş değerleri
		private Dictionary<StaffType, float> _dailySalaryByType = new Dictionary<StaffType, float>
		{
			{ StaffType.Konsomatris, 300f },
			{ StaffType.Waiter, 200f },
			{ StaffType.Security, 250f },
			{ StaffType.Musician, 270f },
			{ StaffType.Chef, 280f },
			{ StaffType.Cleaner, 180f },
			{ StaffType.IllegalFloorStaff, 400f }
		};
		
		// Personel sahne yolları
		private Dictionary<StaffType, string> _staffScenePaths = new Dictionary<StaffType, string>
		{
			{ StaffType.Konsomatris, "res://Scenes/Staff/Konsomatris.tscn" },
			{ StaffType.Waiter, "res://Scenes/Staff/Waiter.tscn" },
			{ StaffType.Security, "res://Scenes/Staff/Security.tscn" },
			{ StaffType.Musician, "res://Scenes/Staff/Musician.tscn" },
			{ StaffType.Chef, "res://Scenes/Staff/Chef.tscn" },
			{ StaffType.Cleaner, "res://Scenes/Staff/Cleaner.tscn" },
			{ StaffType.IllegalFloorStaff, "res://Scenes/Staff/IllegalFloorStaff.tscn" }
		};
		
		// Personel isim listesi (rastgele isim oluşturmak için)
		private List<string> _maleFirstNames = new List<string>();
		private List<string> _femaleFirstNames = new List<string>();
		private List<string> _lastNames = new List<string>();
		
		// Personel özellikleri (trait) ve maliyet çarpanları
		private Dictionary<string, float> _traitCostMultipliers = new Dictionary<string, float>
		{
			{ "Professional", 1.5f },       // Profesyonel
			{ "Experienced", 1.3f },        // Tecrübeli
			{ "Charismatic", 1.4f },        // Karizmatik
			{ "Beautiful", 1.4f },          // Güzel/Yakışıklı
			{ "FastLearner", 1.2f },        // Hızlı öğrenen
			{ "Loyal", 1.25f },             // Sadık
			{ "Talented", 1.4f },           // Yetenekli
			{ "Discreet", 1.35f },          // Ketum (özellikle illegal işler için)
			{ "Rookie", 0.8f },             // Acemi
			{ "Lazy", 0.6f },               // Tembel
			{ "Unreliable", 0.7f },         // Güvenilmez
			{ "Gossiper", 0.65f },          // Dedikoducu
			{ "Alcoholic", 0.75f }          // Alkolik
		};
		
		// Personel trait'lerine göre beceri etkileri
		private Dictionary<string, Dictionary<string, float>> _traitSkillEffects = new Dictionary<string, Dictionary<string, float>>
		{
			{ "Professional", new Dictionary<string, float> { { "performance", 0.3f }, { "loyalty", 0.2f } } },
			{ "Experienced", new Dictionary<string, float> { { "performance", 0.2f }, { "efficiency", 0.2f } } },
			{ "Charismatic", new Dictionary<string, float> { { "customerSatisfaction", 0.25f } } },
			{ "Beautiful", new Dictionary<string, float> { { "customerSatisfaction", 0.3f }, { "tips", 0.2f } } },
			{ "FastLearner", new Dictionary<string, float> { { "skillGain", 0.25f } } },
			{ "Loyal", new Dictionary<string, float> { { "loyalty", 0.4f } } },
			{ "Talented", new Dictionary<string, float> { { "performance", 0.3f } } },
			{ "Discreet", new Dictionary<string, float> { { "loyalty", 0.3f }, { "illegalRiskReduction", 0.2f } } },
			{ "Rookie", new Dictionary<string, float> { { "performance", -0.1f }, { "skillGain", 0.1f } } },
			{ "Lazy", new Dictionary<string, float> { { "efficiency", -0.2f }, { "energy", -0.15f } } },
			{ "Unreliable", new Dictionary<string, float> { { "reliability", -0.25f }, { "loyalty", -0.2f } } },
			{ "Gossiper", new Dictionary<string, float> { { "illegalRiskIncrease", 0.2f }, { "customerSatisfaction", -0.1f } } },
			{ "Alcoholic", new Dictionary<string, float> { { "reliability", -0.15f }, { "performance", -0.2f }, { "customerSatisfaction", -0.1f } } }
		};
		
		// Toplam maaş gideri
		public float TotalSalaryExpense { get; private set; } = 0f;
		
		// EconomyManager referansı
		private EconomyManager _economyManager;
		
		// Signals
		[Signal]
		public delegate void StaffHiredEventHandler(StaffBase staff);
		
		[Signal]
		public delegate void StaffFiredEventHandler(StaffBase staff, string reason);
		
		[Signal]
		public delegate void SalariesPaidEventHandler(float totalAmount);
		
		[Signal]
		public delegate void LoyaltyChangedEventHandler(StaffBase staff, float previousLoyalty, float newLoyalty);
		
		// GameManager'dan çağrılan Ready metodu
		public override void _Ready()
		{
			base._Ready();
			
			// Dictionary'leri başlat
			foreach (StaffType type in Enum.GetValues(typeof(StaffType)))
			{
				_staffByType[type] = new List<StaffBase>();
				_staffPositions[type] = new List<Node3D>();
				
				// String karşılığı da oluştur
				string typeString = type.ToString();
				if (!_staffByTypeString.ContainsKey(typeString))
				{
					_staffByTypeString[typeString] = new List<StaffBase>();
				}
			}
			
			// Özel string anahtar için ekstra personel tipleri
			if (!_staffByTypeString.ContainsKey("Kons"))
			{
				_staffByTypeString["Kons"] = _staffByTypeString["Konsomatris"];
			}
			
			if (!_staffByTypeString.ContainsKey("IllegalStaff"))
			{
				_staffByTypeString["IllegalStaff"] = _staffByTypeString["IllegalFloorStaff"];
			}
			
			// İsim listelerini yükle
			LoadNameLists();
			
			// 3D yerleşim noktalarını bul
			FindStaffPositions();
			
			// Manager referanslarını al
			if (GetTree().Root.HasNode("GameManager/EconomyManager"))
			{
				_economyManager = GetTree().Root.GetNode<EconomyManager>("GameManager/EconomyManager");
			}
			
			GD.Print("StaffManager initialized.");
		}
		
		// Günlük güncelleme
		public void UpdateDaily()
		{
			// Tüm personel için günlük güncelleme
			foreach (var staff in _allStaff)
			{
				staff.UpdateDaily();
			}
			
			// Maaş ödemesi
			PaySalaries();
			
			// Sadakatsizlik ve ihbar riski kontrolü
			CheckStaffRisks();
			
			// Yeni gün başlangıç işlemleri
			OnNewDay();
		}
		
		// Saatlik güncelleme
		public void UpdateHourly()
		{
			// Tüm personel için saatlik güncelleme
			foreach (var staff in _allStaff)
			{
				staff.UpdateHourly();
			}
		}
		
		// Gece modu güncelleme
		public void UpdateNightMode()
		{
			// Tüm personel için gece modu davranışı
			foreach (var staff in _allStaff)
			{
				if (typeof(StaffBase).GetMethod("WorkNightMode") != null)
				{
					try 
					{
						staff.Call("WorkNightMode");
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error calling WorkNightMode on {staff.Name}: {e.Message}");
					}
				}
			}
		}
		
		// Sabah modu güncelleme
		public void UpdateDayMode()
		{
			// Tüm personel için sabah modu davranışı
			foreach (var staff in _allStaff)
			{
				if (typeof(StaffBase).GetMethod("WorkDayMode") != null)
				{
					try 
					{
						staff.Call("WorkDayMode");
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error calling WorkDayMode on {staff.Name}: {e.Message}");
					}
				}
			}
		}
		
		// Yeni personel işe al (Enum kullanımı)
		public StaffBase HireStaff(StaffType type, List<string> traits = null, string fullName = "", string gender = "")
		{
			// Personel sayısını kontrol et
			if (_staffByType[type].Count >= _maxStaffByType[type])
			{
				GD.Print($"Maksimum {type} sayısına ulaşıldı!");
				return null;
			}
			
			// Rastgele isim ve cinsiyet oluştur (eğer belirtilmemişse)
			if (string.IsNullOrEmpty(fullName))
			{
				gender = string.IsNullOrEmpty(gender) ? (new Random().Next(2) == 0 ? "Male" : "Female") : gender;
				fullName = GenerateRandomName(gender);
			}
			
			// Yaş belirle (20-45 arası)
			int age = new Random().Next(20, 46);
			
			// Temel maaş
			float baseSalary = _baseSalaryByType[type];
			
			// Trait'lere göre maliyet hesaplama
			float hireCost = baseSalary;
			if (traits != null && traits.Count > 0)
			{
				foreach (string trait in traits)
				{
					if (_traitCostMultipliers.ContainsKey(trait))
					{
						hireCost *= _traitCostMultipliers[trait];
					}
				}
			}
			
			// Ekonomi kontrolü - Yeterli para var mı?
			if (_economyManager != null)
			{
				if (!_economyManager.SpendMoney(hireCost, $"Hire {type}"))
				{
					GD.Print($"Not enough money to hire {type} (Cost: {hireCost})");
					return null;
				}
			}
			
			// Personel sahnesini yükle
			StaffBase newStaff;
			
			// Sahne yolunu kontrol et
			if (ResourceLoader.Exists(_staffScenePaths[type]))
			{
				PackedScene staffScene = ResourceLoader.Load<PackedScene>(_staffScenePaths[type]);
				newStaff = staffScene.Instantiate() as StaffBase;
			}
			else
			{
				// Eğer sahne bulunamazsa, temel StaffBase oluştur ve başlat
				newStaff = new StaffBase();
				newStaff.Initialize(type);
			}
			
			if (newStaff != null)
			{
				// Personel özelliklerini ayarla
				if (typeof(StaffBase).GetProperty("FullName") != null)
				{
					newStaff.SetOrAddProperty("FullName", fullName);
				}
				newStaff.Name = fullName;
				
				if (typeof(StaffBase).GetProperty("Age") != null)
				{
					newStaff.SetOrAddProperty("Age", age);
				}
				
				if (typeof(StaffBase).GetProperty("Gender") != null)
				{
					newStaff.SetOrAddProperty("Gender", gender);
				}
				
				if (typeof(StaffBase).GetProperty("Salary") != null)
				{
					newStaff.SetOrAddProperty("Salary", baseSalary);
				}
				else
				{
					newStaff.SetSalary(baseSalary);
				}
				
				// Trait'leri atama
				if (traits != null && traits.Count > 0)
				{
					foreach (string trait in traits)
					{
						newStaff.AddTrait(trait);
						
						// Trait'e göre becerileri ayarlama
						if (_traitSkillEffects.ContainsKey(trait))
						{
							foreach (var skillEffect in _traitSkillEffects[trait])
							{
								newStaff.AdjustSkillByTrait(skillEffect.Key, skillEffect.Value);
							}
						}
					}
				}
				
				// Rastgele bir çalışma pozisyonu belirle
				if (_staffPositions[type].Count > 0)
				{
					int posIndex = new Random().Next(_staffPositions[type].Count);
					Node3D staffPosition = _staffPositions[type][posIndex];
					
					// Pozisyona yerleştir
					newStaff.GlobalPosition = staffPosition.GlobalPosition;
					newStaff.GlobalRotation = staffPosition.GlobalRotation;
				}
				
				// Oyun dünyasına ekle
				AddChild(newStaff);
				
				// Listelere ekle
				_allStaff.Add(newStaff);
				_staffByType[type].Add(newStaff);
				
				// String bazlı listeye de ekle
				string typeString = type.ToString();
				if (_staffByTypeString.ContainsKey(typeString))
				{
					_staffByTypeString[typeString].Add(newStaff);
				}
				
				// Özel alias listelerini güncelle
				if (type == StaffType.Konsomatris && _staffByTypeString.ContainsKey("Kons"))
				{
					_staffByTypeString["Kons"].Add(newStaff);
				}
				else if (type == StaffType.IllegalFloorStaff && _staffByTypeString.ContainsKey("IllegalStaff"))
				{
					_staffByTypeString["IllegalStaff"].Add(newStaff);
				}
				
				// Sinyal gönderme
				EmitSignal(SignalName.StaffHired, newStaff);
				
				GD.Print($"Yeni {type} işe alındı: {fullName}");
				return newStaff;
			}
			
			GD.Print($"{type} personel sahnesi yüklenemedi!");
			return null;
		}
		
		// Yeni personel işe al (String anahtar kullanımı - Geriye dönük uyumluluk için)
		public StaffBase HireStaff(string typeString, List<string> traits = null, string fullName = "")
		{
			// String anahtarı enum'a çevir
			StaffType? type = GetStaffTypeFromString(typeString);
			
			if (type.HasValue)
			{
				return HireStaff(type.Value, traits, fullName);
			}
			
			GD.PrintErr($"Bilinmeyen personel tipi: {typeString}");
			return null;
		}
		
		// String tanımlayıcıyı StaffType enum'a çevir
		private StaffType? GetStaffTypeFromString(string typeString)
		{
			switch (typeString)
			{
				case "Kons":
					return StaffType.Konsomatris;
				case "Security":
					return StaffType.Security;
				case "Waiter":
					return StaffType.Waiter;
				case "Musician":
					return StaffType.Musician;
				case "Chef":
					return StaffType.Chef;
				case "Cleaner":
					return StaffType.Cleaner;
				case "IllegalStaff":
					return StaffType.IllegalFloorStaff;
				default:
					// Doğrudan enum çevirisi dene
					if (Enum.TryParse<StaffType>(typeString, out StaffType result))
					{
						return result;
					}
					return null;
			}
		}
		
		// Personel işten çıkar
		public bool FireStaff(StaffBase staff, string reason = "")
		{
			if (staff == null || !_allStaff.Contains(staff)) return false;
			
			StaffType? staffType = null;
			string staffTypeString = "";
			
			// Personelin tipini bul (hem enum hem string olarak)
			foreach (var entry in _staffByType)
			{
				if (entry.Value.Contains(staff))
				{
					staffType = entry.Key;
					staffTypeString = entry.Key.ToString();
					break;
				}
			}
			
			if (!staffType.HasValue)
			{
				GD.PrintErr($"Personel tipi bulunamadı: {staff.Name}");
				return false;
			}
			
			// Personel listelerinden çıkarma
			_allStaff.Remove(staff);
			_staffByType[staffType.Value].Remove(staff);
			
			// String bazlı listelerden de çıkar
			if (_staffByTypeString.ContainsKey(staffTypeString))
			{
				_staffByTypeString[staffTypeString].Remove(staff);
			}
			
			// Özel alias listelerini güncelle
			if (staffType == StaffType.Konsomatris && _staffByTypeString.ContainsKey("Kons"))
			{
				_staffByTypeString["Kons"].Remove(staff);
			}
			else if (staffType == StaffType.IllegalFloorStaff && _staffByTypeString.ContainsKey("IllegalStaff"))
			{
				_staffByTypeString["IllegalStaff"].Remove(staff);
			}
			
			// Tazminat ödeme (opsiyonel)
			float severancePayment = _dailySalaryByType[staffType.Value] * 5; // 5 günlük maaş tazminat
			if (_economyManager != null)
			{
				_economyManager.SpendMoney(severancePayment, "Staff Severance Payment");
			}
			
			// Sinyal gönderme
			EmitSignal(SignalName.StaffFired, staff, reason);
			
			// İşten çıkarma metodunu çağır
			if (staff.GetType().GetMethod("Fire") != null)
			{
				staff.Call("Fire");
			}
			else
			{
				// Node ağacından çıkarma
				RemoveChild(staff);
				staff.QueueFree();
			}
			
			GD.Print($"Fired {staffTypeString}: {staff.Name} for reason: {reason}. Paid {severancePayment} severance.");
			return true;
		}
		
		// Maaş ödemeleri
		public void PaySalaries()
		{
			TotalSalaryExpense = 0f;
			List<StaffBase> staffToFire = new List<StaffBase>();
			
			// Tüm aktif personelin maaşlarını hesapla
			foreach (var staff in _allStaff)
			{
				bool isActive = true;
				
				// IsActive property'yi kontrol et
				if (typeof(StaffBase).GetProperty("IsActive") != null)
				{
					isActive = (bool)staff.GetPropertyValue("IsActive");
				}
				
				if (isActive)
				{
					float salary = CalculateStaffSalary(staff);
					TotalSalaryExpense += salary;
				}
			}
			
			// Ekonomi üzerinden ödeme yap
			bool paymentSuccess = false;
			if (_economyManager != null)
			{
				paymentSuccess = _economyManager.SpendMoney(TotalSalaryExpense, "Personel Maaşları");
				GD.Print($"Toplam {TotalSalaryExpense} maaş ödemesi {(paymentSuccess ? "yapıldı" : "başarısız oldu")}.");
			}
			
			if (paymentSuccess)
			{
				// Maaş ödemesi başarılı - personel bağlılığını artır
				foreach (var staff in _allStaff)
				{
					bool isActive = true;
					if (typeof(StaffBase).GetProperty("IsActive") != null)
					{
						isActive = (bool)staff.GetPropertyValue("IsActive");
					}
					
					if (isActive)
					{
						staff.AdjustLoyalty(0.05f, "Regular Salary Payment");
					}
				}
				
				EmitSignal(SignalName.SalariesPaid, TotalSalaryExpense);
			}
			else
			{
				// Maaş ödemesi başarısız - personel bağlılığını azalt
				HandleFailedSalaryPayment();
			}
			
			// İstifa eden personeli işten çıkar
			foreach (var staff in staffToFire)
			{
				FireStaff(staff, "Resigned due to missed payments");
			}
		}
		
		// Personel maaşını hesaplama (beceri seviyesi, çalışma süresi, vb. faktörlere göre)
		private float CalculateStaffSalary(StaffBase staff)
		{
			StaffType? staffType = GetStaffTypeFromStaff(staff);
			if (!staffType.HasValue) return 0f;
			
			float baseSalary = _dailySalaryByType[staffType.Value];
			
			// Beceri seviyesine göre artış
			float skillModifier = 1.0f + (staff.GetSkillLevel() * 0.1f); // Her beceri seviyesi %10 artış
			
			// Çalışma süresine göre artış (her ay için %2)
			float experienceModifier = 1.0f + (staff.GetWorkDuration() * 0.02f);
			
			// Seviyeye göre artış
			float levelModifier = 1.0f;
			int level = 1;
			
			// Level property'yi kontrol et
			if (typeof(StaffBase).GetProperty("Level") != null)
			{
				level = (int)staff.GetPropertyValue("Level");
				levelModifier = 1.0f + ((level - 1) * 0.05f); // Her seviye %5 artış
			}
			
			// Trait'lere göre maaş değişikliği
			float traitModifier = 1.0f;
			foreach (string trait in staff.GetTraits())
			{
				// Örnek: bazı trait'ler maaşı etkiler
				if (trait == "Professional") traitModifier += 0.15f;
				else if (trait == "Experienced") traitModifier += 0.1f;
				else if (trait == "Talented") traitModifier += 0.1f;
				else if (trait == "Rookie") traitModifier -= 0.1f;
			}
			
			// Toplam maaş hesabı
			return baseSalary * skillModifier * experienceModifier * levelModifier * traitModifier;
		}
		
		// Maaş ödenemediğinde bağlılık düşüşü
		private void HandleFailedSalaryPayment()
		{
			foreach (var staff in _allStaff.ToList()) // ToList() ile listeyi klonlayalım ki döngü içinde değişiklik yapabilelim
			{
				// Maaş ödenmediğinde büyük bağlılık düşüşü
				staff.AdjustLoyalty(-0.3f, "Missed Salary Payment");
				
				// Düşük bağlılık sonucu istifa etme olasılığı
				if (staff.Loyalty < 0.2f && GD.Randf() < 0.7f) // %70 ihtimalle istifa
				{
					FireStaff(staff, "Resigned due to missed payments");
				}
				// Düşük bağlılık sonucu sadakatsiz davranışlar (örn. ihbar etme)
				else if (staff.Loyalty < 0.4f && GD.Randf() < 0.3f) // %30 ihtimalle problem
				{
					// İhbar etme, sabotaj veya diğer sadakatsiz davranışlar
					HandleDisloyal(staff);
				}
			}
		}
		
		// Bahşişleri personele dağıtma
		public void DistributeTipsToStaff(float totalTips)
		{
			if (totalTips <= 0 || _allStaff.Count == 0) return;
			
			// Bahşişin dağıtımında önem sırası (her personel tipi için ağırlık)
			Dictionary<StaffType, float> tipWeights = new Dictionary<StaffType, float>
			{
				{ StaffType.Konsomatris, 0.4f },  // Konslar toplam bahşişin %40'ını alır
				{ StaffType.Waiter, 0.3f },       // Garsonlar %30'unu
				{ StaffType.Musician, 0.15f },    // Müzisyenler %15'ini
				{ StaffType.Chef, 0.1f },         // Aşçılar %10'unu
				{ StaffType.Security, 0.03f },     // Güvenlik %3'ünü
				{ StaffType.Cleaner, 0.02f }      // Temizlikçiler %2'sini
			};
			
			// Her personel tipi için bahşiş havuzu oluşturma
			Dictionary<StaffType, float> tipPools = new Dictionary<StaffType, float>();
			
			foreach (var entry in tipWeights)
			{
				StaffType staffType = entry.Key;
				float weight = entry.Value;
				float poolAmount = totalTips * weight;
				
				// Personel tipi mevcut değilse havuzu diğerlerine dağıt
				if (_staffByType[staffType].Count == 0)
				{
					continue;
				}
				
				tipPools[staffType] = poolAmount;
			}
			
			// Her personel için bahşiş dağıtımı
			foreach (var entry in tipPools)
			{
				StaffType staffType = entry.Key;
				float poolAmount = entry.Value;
				
				List<StaffBase> staffOfType = _staffByType[staffType];
				int staffCount = staffOfType.Count;
				
				if (staffCount == 0) continue;
				
				// Her personele beceri seviyesine göre bahşiş dağıtımı
				float totalSkill = staffOfType.Sum(s => s.GetSkillLevel());
				if (totalSkill <= 0) totalSkill = staffCount; // Eğer toplam beceri yoksa eşit dağıt
				
				foreach (var staff in staffOfType)
				{
					float skillRatio = totalSkill > 0 ? staff.GetSkillLevel() / totalSkill : 1.0f / staffCount;
					float tipAmount = poolAmount * skillRatio;
					
					// Bahşiş alımı bağlılığı hafif artırır
					staff.AdjustLoyalty(0.01f, "Received Tips");
					
					// Bahşiş bilgisini personele bildir
					staff.ReceiveTips(tipAmount);
					
					GD.Print($"{staff.GetStaffTypeString()} {staff.Name} received {tipAmount} in tips.");
				}
			}
		}
		
		// Personel risklerini kontrol et
		private void CheckStaffRisks()
		{
			Random rnd = new Random();
			
			foreach (var staff in _allStaff.ToList()) // ToList() ile listeyi klonlayalım ki döngü içinde değişiklik yapabilelim
			{
				float disloyaltyRisk = 0f;
				float reportRisk = 0f;
				
				// DisloyaltyRisk ve ReportRisk property'leri kontrol edilir
				if (typeof(StaffBase).GetProperty("DisloyaltyRisk") != null)
				{
					disloyaltyRisk = (float)staff.GetPropertyValue("DisloyaltyRisk");
				}
				else
				{
					// Alternatif hesaplama: sadakatsizlik riskini bağlılık ve ruh haline göre hesapla
					disloyaltyRisk = Mathf.Max(0f, (0.5f - staff.Loyalty) * 2f);
				}
				
				if (typeof(StaffBase).GetProperty("ReportRisk") != null)
				{
					reportRisk = (float)staff.GetPropertyValue("ReportRisk");
				}
				else
				{
					// Alternatif hesaplama: ihbar riskini sadakatsizlik riskinin bir oranı olarak hesapla
					reportRisk = disloyaltyRisk * 0.2f;
				}
				
				// Sadakatsizlik riski - iş bırakma
				if (disloyaltyRisk > 0.5f && rnd.NextDouble() < (disloyaltyRisk - 0.5f))
				{
					GD.Print($"{staff.Name} istifa etti! Sadakatsizlik: {disloyaltyRisk}");
					FireStaff(staff, "Resignation due to disloyalty");
					continue;
				}
				
				// İhbar riski - polis baskını riski
				if (reportRisk > 0.7f && rnd.NextDouble() < (reportRisk - 0.7f))
				{
					// EventManager üzerinden polis baskını olayı tetikle
					GD.Print($"{staff.Name} polise ihbarda bulundu! İhbar riski: {reportRisk}");
					
					// Şimdilik sadece personeli işten çıkaralım
					FireStaff(staff, "Reported to police");
					
					// GameManager üzerinden polis baskını olayını tetikle
					TriggerPoliceRaid(staff);
				}
			}
		}
		
		// Polis baskını olayını tetikle
		private void TriggerPoliceRaid(StaffBase staff)
		{
			// GameManager.ReputationManager üzerinden polis baskını olayını tetikle
			if (GetTree().Root.HasNode("GameManager/ReputationManager"))
			{
				var reputationManager = GetTree().Root.GetNode("GameManager/ReputationManager");
				reputationManager.Call("TriggerPoliceRaid", "Staff Betrayal");
			}
			else
			{
				GD.Print("Police raid triggered by staff betrayal");
			}
		}
		
		// Sadakatsiz personel davranışı
		private void HandleDisloyal(StaffBase staff)
		{
			float disloyaltyChance = GD.Randf();
			
			if (disloyaltyChance < 0.1f) // %10 ihtimalle ihbar
			{
				// ReputationManager ve BuildingManager varsa illegal kat kontrolü yap
				bool hasIllegalFloor = CheckIllegalFloor();
				
				if (hasIllegalFloor)
				{
					TriggerPoliceRaid(staff);
					GD.Print($"{staff.GetStaffTypeString()} {staff.Name} betrayed and reported illegal activities!");
				}
			}
			else if (disloyaltyChance < 0.3f) // %20 ihtimalle dedikodu
			{
				// Müşterilere dedikodu yapma, işletmenin itibarını zedeleme
				AdjustReputation(-10, "Staff Gossip");
				GD.Print($"{staff.GetStaffTypeString()} {staff.Name} is spreading rumors and gossip.");
			}
			else if (disloyaltyChance < 0.5f) // %20 ihtimalle hırsızlık
			{
				// Kasadan para çalma
				float stolenAmount = GD.Randf() * 500.0f + 100.0f; // 100-600 arası
				if (_economyManager != null)
				{
					_economyManager.SpendMoney(stolenAmount, "Theft");
				}
				GD.Print($"{staff.GetStaffTypeString()} {staff.Name} stole {stolenAmount} from the register!");
			}
			else // %50 ihtimalle kötü performans
			{
				// Sadece kötü performans gösterme
				GD.Print($"{staff.GetStaffTypeString()} {staff.Name} is showing poor performance due to dissatisfaction.");
			}
		}
		
		// İllegal kat kontrolü
		private bool CheckIllegalFloor()
		{
			if (GetTree().Root.HasNode("GameManager/BuildingManager"))
			{
				var buildingManager = GetTree().Root.GetNode("GameManager/BuildingManager");
				
				if (buildingManager.HasMethod("HasIllegalFloor") && buildingManager.HasMethod("IsIllegalFloorActive"))
				{
					bool hasIllegalFloor = (bool)buildingManager.Call("HasIllegalFloor");
					bool isIllegalFloorActive = (bool)buildingManager.Call("IsIllegalFloorActive");
					
					return hasIllegalFloor && isIllegalFloorActive;
				}
			}
			
			return false;
		}
		
		// İtibar değişimi
		private void AdjustReputation(int amount, string reason)
		{
			if (GetTree().Root.HasNode("GameManager/ReputationManager"))
			{
				var reputationManager = GetTree().Root.GetNode("GameManager/ReputationManager");
				
				if (reputationManager.HasMethod("AdjustReputation"))
				{
					reputationManager.Call("AdjustReputation", amount, reason);
				}
			}
		}
		
		// Tüm personele zam yap
		public void GiveRaiseToAll(float percentageIncrease)
		{
			foreach (var staff in _allStaff)
			{
				if (staff.GetType().GetMethod("GiveRaise") != null)
				{
					staff.Call("GiveRaise", percentageIncrease);
				}
				else
				{
					float currentSalary = staff.Salary;
					float newSalary = currentSalary * (1f + (percentageIncrease / 100f));
					staff.SetSalary(newSalary);
				}
			}
			
			GD.Print($"Tüm personele %{percentageIncrease} zam yapıldı!");
		}
		
		// Belirli bir tür personele zam yap (Enum kullanımı)
		public void GiveRaiseByType(StaffType type, float percentageIncrease)
		{
			foreach (var staff in _staffByType[type])
			{
				if (staff.GetType().GetMethod("GiveRaise") != null)
				{
					staff.Call("GiveRaise", percentageIncrease);
				}
				else
				{
					float currentSalary = staff.Salary;
					float newSalary = currentSalary * (1f + (percentageIncrease / 100f));
					staff.SetSalary(newSalary);
				}
			}
			
			GD.Print($"Tüm {type} personeline %{percentageIncrease} zam yapıldı!");
		}
		
		// Belirli bir tür personele zam yap (String kullanımı)
		public void GiveRaiseByType(string typeString, float percentageIncrease)
		{
			StaffType? type = GetStaffTypeFromString(typeString);
			
			if (type.HasValue)
			{
				GiveRaiseByType(type.Value, percentageIncrease);
			}
			else if (_staffByTypeString.ContainsKey(typeString))
			{
				foreach (var staff in _staffByTypeString[typeString])
				{
					if (staff.GetType().GetMethod("GiveRaise") != null)
					{
						staff.Call("GiveRaise", percentageIncrease);
					}
					else
					{
						float currentSalary = staff.Salary;
						float newSalary = currentSalary * (1f + (percentageIncrease / 100f));
						staff.SetSalary(newSalary);
					}
				}
				
				GD.Print($"Tüm {typeString} personeline %{percentageIncrease} zam yapıldı!");
			}
			else
			{
				GD.PrintErr($"Bilinmeyen personel tipi: {typeString}");
			}
		}
		
		// Yeni gün başlangıcında çağrılır
		public void OnNewDay()
		{
			// Tüm personelin günlük güncellenmesi - maaş ödemesi zaten UpdateDaily içinde yapılıyor
			foreach (var staff in _allStaff)
			{
				// Personelin çalışma süresini güncelle
				staff.IncrementWorkDuration();
				
				// Personelin dinlenme ve enerji seviyelerini sıfırla
				staff.ResetEnergyForNewDay();
				
				// Personel becerilerinin gelişmesi
				staff.ImproveSkills();
				
				// Personel bağlılığının doğal değişimi
				UpdateStaffLoyaltyDaily(staff);
			}
			
			// Yeni personel adayları oluşturma (işe alım havuzu)
			GenerateStaffCandidates();
		}
		
		// Günlük bağlılık güncellemesi
		private void UpdateStaffLoyaltyDaily(StaffBase staff)
		{
			// Temel bağlılık değişimi (her gün çok az düşer, iyi koşullarla artar)
			float baseChange = -0.01f; // Her gün %1 düşüş
			
			// Maaş memnuniyeti
			float salary = CalculateStaffSalary(staff);
			float expectedSalary = staff.Salary * 0.05f; // Günlük beklenti, aylık maaşın %5'i
			
			if (salary >= expectedSalary * 1.2f) // Beklentinin %20 üstünde
				baseChange += 0.03f;
			else if (salary >= expectedSalary) // Beklenti karşılanmış
				baseChange += 0.01f;
			else if (salary < expectedSalary * 0.8f) // Beklentinin %20 altında
				baseChange -= 0.03f;
			
			// Çalışma koşulları (mekandaki ekipman ve mobilyaya bağlı)
			float workingConditionModifier = GetWorkingConditionModifier(staff);
			baseChange += workingConditionModifier * 0.02f; // Her seviye %2 etki
			
			// Müşteri memnuniyeti (konslar ve garsonlar için önemli)
			if (staff.GetStaffTypeString() == "Konsomatris" || staff.GetStaffTypeString() == "Waiter")
			{
				float customerSatisfaction = GetAverageCustomerSatisfaction();
				baseChange += (customerSatisfaction - 0.5f) * 0.02f; // Ortalama memnuniyete göre +/- %2 etki
			}
			
			// Personel trait'leri bağlılığı etkiler
			foreach (string trait in staff.GetTraits())
			{
				if (trait == "Loyal") baseChange += 0.02f;
				else if (trait == "Unreliable") baseChange -= 0.02f;
			}
			
			// Bağlılık değişimini uygula
			staff.AdjustLoyalty(baseChange, "Daily Update");
		}
		
		// Çalışma koşulları modifikatörü
		private float GetWorkingConditionModifier(StaffBase staff)
		{
			if (GetTree().Root.HasNode("GameManager/BuildingManager"))
			{
				var buildingManager = GetTree().Root.GetNode("GameManager/BuildingManager");
				
				if (buildingManager.HasMethod("GetWorkingConditionModifier"))
				{
					return (float)buildingManager.Call("GetWorkingConditionModifier", staff.GetStaffTypeString());
				}
			}
			
			return 0f; // Varsayılan değer
		}
		
		// Ortalama müşteri memnuniyeti
		private float GetAverageCustomerSatisfaction()
		{
			if (GetTree().Root.HasNode("GameManager/CustomerManager"))
			{
				var customerManager = GetTree().Root.GetNode("GameManager/CustomerManager");
				
				if (customerManager.HasMethod("GetAverageCustomerSatisfaction"))
				{
					return (float)customerManager.Call("GetAverageCustomerSatisfaction");
				}
			}
			
			return 0.5f; // Varsayılan değer
		}
		
		// Yeni personel adayları oluşturma
		private void GenerateStaffCandidates()
		{
			// Her personel tipi için rastgele adaylar oluştur
			// Bu adaylar işe alım menüsünde gösterilecek
			// Burada sadece konsept olarak belirtildi, gerçek uygulama UI ile entegre edilmelidir
		}
		
		// Rastgele isim oluştur
		private string GenerateRandomName(string gender)
		{
			Random rnd = new Random();
			string firstName;
			
			if (gender == "Male")
				firstName = _maleFirstNames[rnd.Next(_maleFirstNames.Count)];
			else
				firstName = _femaleFirstNames[rnd.Next(_femaleFirstNames.Count)];
				
			string lastName = _lastNames[rnd.Next(_lastNames.Count)];
			
			return $"{firstName} {lastName}";
		}
		
		// İsim listelerini yükle
		private void LoadNameLists()
		{
			// Örnek isim listeleri (gerçek oyunda dosyadan yüklenebilir)
			
			// Erkek isimleri
			_maleFirstNames = new List<string>
			{
				"Ali", "Mehmet", "Ahmet", "Mustafa", "İbrahim", 
				"Hasan", "Hüseyin", "Murat", "Emre", "Kemal",
				"Oğuz", "Serkan", "Burak", "Selim", "Kaan"
			};
			
			// Kadın isimleri
			_femaleFirstNames = new List<string>
			{
				"Ayşe", "Fatma", "Zeynep", "Merve", "Ebru",
				"Esra", "Derya", "Serap", "Melis", "Buse",
				"Selin", "Emine", "Gül", "Sevgi", "Nur"
			};
			
			// Soyadları
			_lastNames = new List<string>
			{
				"Yılmaz", "Kaya", "Demir", "Çelik", "Şahin",
				"Yıldız", "Yıldırım", "Öztürk", "Aydın", "Özdemir",
				"Arslan", "Doğan", "Kılıç", "Aslan", "Çetin"
			};
		}
		
		// Personel yerleşim noktalarını bul
		private void FindStaffPositions()
		{
			// Oyun dünyasında StaffPosition* node'larını bul
			var currentScene = GetTree().CurrentScene;
			
			if (currentScene != null)
			{
				// Her personel türü için pozisyon node'larını bul
				foreach (StaffType type in Enum.GetValues(typeof(StaffType)))
				{
					string posGroupName = $"StaffPositions{type}";
					
					// Bu pozisyon grubu node'u mevcut ise
					if (currentScene.HasNode(posGroupName))
					{
						Node posGroup = currentScene.GetNode(posGroupName);
						
						// Grup içindeki tüm Node3D'leri bul
						foreach (Node child in posGroup.GetChildren())
						{
							if (child is Node3D pos)
							{
								_staffPositions[type].Add(pos);
							}
						}
					}
				}
			}
		}
		
		// Personel tipini bul
		private StaffType? GetStaffTypeFromStaff(StaffBase staff)
		{
			// Önce Type property'ye bak
			if (typeof(StaffBase).GetProperty("Type") != null)
			{
				return (StaffType)staff.GetPropertyValue("Type");
			}
			
			// Sonra StaffType string'ini çevir
			string typeString = staff.GetStaffTypeString();
			return GetStaffTypeFromString(typeString);
		}
		
		// Tür bazlı personel sayısını al
		public int GetStaffCountByType(StaffType type)
		{
			return _staffByType[type].Count;
		}
		
		// Tür bazlı personel sayısını al (String kullanımı)
		public int GetStaffCountByType(string typeString)
		{
			StaffType? type = GetStaffTypeFromString(typeString);
			
			if (type.HasValue)
			{
				return _staffByType[type.Value].Count;
			}
			else if (_staffByTypeString.ContainsKey(typeString))
			{
				return _staffByTypeString[typeString].Count;
			}
			
			return 0;
		}
		
		// Toplam personel sayısını al
		public int GetTotalStaffCount()
		{
			return _allStaff.Count;
		}
		
		// ID ile personel bul
		public StaffBase GetStaffById(string id)
		{
			// StaffID property varsa kullan
			if (typeof(StaffBase).GetProperty("StaffID") != null)
			{
				return _allStaff.FirstOrDefault(s => (string)s.GetPropertyValue("StaffID") == id);
			}
			
			// Yoksa Name kullan
			return _allStaff.FirstOrDefault(s => s.Name == id);
		}
		
		// Tür bazlı personel listesini al (Enum kullanımı)
		public List<StaffBase> GetStaffByType(StaffType type)
		{
			return new List<StaffBase>(_staffByType[type]);
		}
		
		// Tür bazlı personel listesini al (String kullanımı)
		public List<StaffBase> GetStaffByType(string typeString)
		{
			StaffType? type = GetStaffTypeFromString(typeString);
			
			if (type.HasValue)
			{
				return new List<StaffBase>(_staffByType[type.Value]);
			}
			else if (_staffByTypeString.ContainsKey(typeString))
			{
				return new List<StaffBase>(_staffByTypeString[typeString]);
			}
			
			return new List<StaffBase>();
		}
		
		// Tüm personel listesini al
		public List<StaffBase> GetAllStaff()
		{
			return new List<StaffBase>(_allStaff);
		}
		
		// Personel kapasitesini artırma (genişleme için)
		public void IncreaseStaffCapacity(StaffType type, int amount)
		{
			if (_maxStaffByType.ContainsKey(type))
			{
				_maxStaffByType[type] += amount;
				GD.Print($"Increased {type} capacity by {amount}. New capacity: {_maxStaffByType[type]}");
			}
		}
		
		// Personel kapasitesini artırma (String kullanımı)
		public void IncreaseStaffCapacity(string typeString, int amount)
		{
			StaffType? type = GetStaffTypeFromString(typeString);
			
			if (type.HasValue)
			{
				IncreaseStaffCapacity(type.Value, amount);
			}
			else
			{
				GD.PrintErr($"Bilinmeyen personel tipi: {typeString}");
			}
		}
		
		// Personel alım maliyetini öğrenme
		public float GetHireCost(StaffType type, List<string> traits = null)
		{
			float cost = _baseSalaryByType[type];
			
			// Trait'lere göre maliyet hesaplama
			if (traits != null && traits.Count > 0)
			{
				foreach (string trait in traits)
				{
					if (_traitCostMultipliers.ContainsKey(trait))
					{
						cost *= _traitCostMultipliers[trait];
					}
				}
			}
			
			return cost;
		}
		
		// Personel alım maliyetini öğrenme (String kullanımı)
		public float GetHireCost(string typeString, List<string> traits = null)
		{
			StaffType? type = GetStaffTypeFromString(typeString);
			
			if (type.HasValue)
			{
				return GetHireCost(type.Value, traits);
			}
			
			return 0f;
		}
		
		// Oyun kaydı için personel verilerini serialize et
		public Dictionary<string, object> SerializeData()
		{
			Dictionary<string, object> data = new Dictionary<string, object>();
			List<Dictionary<string, object>> staffDataList = new List<Dictionary<string, object>>();
			
			// Tüm personelin verilerini topla
			foreach (var staff in _allStaff)
			{
				// Temel bilgiler
				Dictionary<string, object> staffData = new Dictionary<string, object>();
				
				// StaffID property varsa kullan
				if (typeof(StaffBase).GetProperty("StaffID") != null)
				{
					staffData["id"] = staff.GetPropertyValue("StaffID");
				}
				else
				{
					staffData["id"] = staff.Name;
				}
				
				staffData["name"] = staff.Name;
				
				// Staff tipini belirle
				StaffType? staffType = GetStaffTypeFromStaff(staff);
				if (staffType.HasValue)
				{
					staffData["type"] = (int)staffType.Value;
				}
				else
				{
					staffData["typeString"] = staff.GetStaffTypeString();
				}
				
				// Temel özellikler
				if (typeof(StaffBase).GetProperty("Age") != null)
				{
					staffData["age"] = staff.GetPropertyValue("Age");
				}
				
				if (typeof(StaffBase).GetProperty("Gender") != null)
				{
					staffData["gender"] = staff.GetPropertyValue("Gender");
				}
				
				staffData["salary"] = staff.Salary;
				
				if (typeof(StaffBase).GetProperty("TipPercentage") != null)
				{
					staffData["tipPercentage"] = staff.GetPropertyValue("TipPercentage");
				}
				
				if (typeof(StaffBase).GetProperty("IsActive") != null)
				{
					staffData["isActive"] = staff.GetPropertyValue("IsActive");
				}
				
				// Performans ve durum özellikleri
				staffData["loyalty"] = staff.Loyalty;
				staffData["energy"] = staff.Energy;
				staffData["mood"] = staff.Mood;
				
				if (typeof(StaffBase).GetProperty("Satisfaction") != null)
				{
					staffData["satisfaction"] = staff.GetPropertyValue("Satisfaction");
				}
				
				// Beceriler ve yetenekler
				if (typeof(StaffBase).GetProperty("Charisma") != null)
				{
					staffData["charisma"] = staff.GetPropertyValue("Charisma");
				}
				
				if (typeof(StaffBase).GetProperty("Diligence") != null)
				{
					staffData["diligence"] = staff.GetPropertyValue("Diligence");
				}
				
				if (typeof(StaffBase).GetProperty("Intelligence") != null)
				{
					staffData["intelligence"] = staff.GetPropertyValue("Intelligence");
				}
				
				if (typeof(StaffBase).GetProperty("Strength") != null)
				{
					staffData["strength"] = staff.GetPropertyValue("Strength");
				}
				
				// Tecrübe ve seviye
				if (typeof(StaffBase).GetProperty("Level") != null)
				{
					staffData["level"] = staff.GetPropertyValue("Level");
				}
				
				if (typeof(StaffBase).GetProperty("ExperiencePoints") != null)
				{
					staffData["experience"] = staff.GetPropertyValue("ExperiencePoints");
				}
				
				// İşe alım tarihi
				if (typeof(StaffBase).GetProperty("HireDate") != null)
				{
					DateTime hireDate = (DateTime)staff.GetPropertyValue("HireDate");
					staffData["hireDate"] = hireDate.Ticks;
				}
				
				// Traits
				staffData["traits"] = staff.GetTraits();
				
				// Ek veriler
				staffData["workDuration"] = staff.GetWorkDuration();
				
				staffDataList.Add(staffData);
			}
			
			data["staff"] = staffDataList;
			data["totalSalary"] = TotalSalaryExpense;
			
			return data;
		}
		
		// Oyun kaydından personel verilerini deserialize et
		public void DeserializeData(Dictionary<string, object> data)
		{
			if (data.ContainsKey("staff") && data["staff"] is List<Dictionary<string, object>> staffDataList)
			{
				// Önce tüm mevcut personeli temizle
				foreach (var staff in _allStaff.ToList())
				{
					FireStaff(staff);
				}
				
				// Personel verilerini yükle
				foreach (var staffData in staffDataList)
				{
					StaffType? type = null;
					string typeString = "";
					
					// Personel tipini belirle
					if (staffData.ContainsKey("type"))
					{
						type = (StaffType)(int)staffData["type"];
						typeString = type.ToString();
					}
					else if (staffData.ContainsKey("typeString"))
					{
						typeString = (string)staffData["typeString"];
						type = GetStaffTypeFromString(typeString);
					}
					
					if (!type.HasValue)
					{
						GD.PrintErr($"Invalid staff type: {typeString}");
						continue;
					}
					
					string fullName = (string)staffData["name"];
					string gender = staffData.ContainsKey("gender") ? (string)staffData["gender"] : "Male";
					
					// Traits yükle
					List<string> traits = null;
					if (staffData.ContainsKey("traits") && staffData["traits"] is List<string>)
					{
						traits = (List<string>)staffData["traits"];
					}
					
					// Yeni personel oluştur
					StaffBase newStaff = HireStaff(type.Value, traits, fullName, gender);
					
					if (newStaff != null)
					{
						// Diğer özellikleri ayarla
						if (staffData.ContainsKey("age") && typeof(StaffBase).GetProperty("Age") != null)
						{
							newStaff.SetPropertyValue("Age", (int)staffData["age"]);
						}
						
						if (staffData.ContainsKey("salary"))
						{
							newStaff.SetSalary((float)staffData["salary"]);
						}
						
						if (staffData.ContainsKey("tipPercentage") && typeof(StaffBase).GetProperty("TipPercentage") != null)
						{
							newStaff.SetPropertyValue("TipPercentage", (float)staffData["tipPercentage"]);
						}
						
						if (staffData.ContainsKey("isActive") && typeof(StaffBase).GetProperty("IsActive") != null)
						{
							newStaff.SetPropertyValue("IsActive", (bool)staffData["isActive"]);
						}
						
						// Performans ve durum özellikleri
						if (staffData.ContainsKey("loyalty"))
						{
							float previousLoyalty = newStaff.Loyalty;
							float newLoyalty = (float)staffData["loyalty"];
							newStaff.AdjustLoyalty(newLoyalty - previousLoyalty, "Load Game");
						}
						
						if (staffData.ContainsKey("energy"))
						{
							float previousEnergy = newStaff.Energy;
							float newEnergy = (float)staffData["energy"];
							newStaff.AdjustEnergy(newEnergy - previousEnergy, "Load Game");
						}
						
						if (staffData.ContainsKey("mood"))
						{
							float previousMood = newStaff.Mood;
							float newMood = (float)staffData["mood"];
							newStaff.AdjustMood(newMood - previousMood, "Load Game");
						}
						
						if (staffData.ContainsKey("satisfaction") && typeof(StaffBase).GetMethod("AdjustSatisfaction") != null)
						{
							newStaff.Call("AdjustSatisfaction", (float)staffData["satisfaction"] - 0.6f, "Load Game");
						}
						
						// Temel yetenekler
						if (staffData.ContainsKey("charisma") && typeof(StaffBase).GetProperty("Charisma") != null)
						{
							newStaff.SetPropertyValue("Charisma", (float)staffData["charisma"]);
						}
						
						if (staffData.ContainsKey("diligence") && typeof(StaffBase).GetProperty("Diligence") != null)
						{
							newStaff.SetPropertyValue("Diligence", (float)staffData["diligence"]);
						}
						
						if (staffData.ContainsKey("intelligence") && typeof(StaffBase).GetProperty("Intelligence") != null)
						{
							newStaff.SetPropertyValue("Intelligence", (float)staffData["intelligence"]);
						}
						
						if (staffData.ContainsKey("strength") && typeof(StaffBase).GetProperty("Strength") != null)
						{
							newStaff.SetPropertyValue("Strength", (float)staffData["strength"]);
						}
						
						// Tecrübe ve seviye
						if (staffData.ContainsKey("level") && typeof(StaffBase).GetProperty("Level") != null)
						{
							newStaff.SetPropertyValue("Level", (int)staffData["level"]);
						}
						
						if (staffData.ContainsKey("experience") && typeof(StaffBase).GetProperty("ExperiencePoints") != null)
						{
							newStaff.SetPropertyValue("ExperiencePoints", (int)staffData["experience"]);
						}
						
						// İşe alım tarihi
						if (staffData.ContainsKey("hireDate") && typeof(StaffBase).GetProperty("HireDate") != null)
						{
							DateTime hireDate = new DateTime((long)staffData["hireDate"]);
							newStaff.SetPropertyValue("HireDate", hireDate);
						}
						
						// Çalışma süresi
						if (staffData.ContainsKey("workDuration"))
						{
							int workDuration = (int)staffData["workDuration"];
							for (int i = 0; i < workDuration; i++)
							{
								newStaff.IncrementWorkDuration();
							}
						}
					}
				}
			}
			
			if (data.ContainsKey("totalSalary"))
			{
				TotalSalaryExpense = (float)data["totalSalary"];
			}
		}
	}
	
	// StaffBase sınıfı için uzantı metodları
	public static class StaffBaseExtensions
	{
		// Property değerini okuma
		public static object GetPropertyValue(this StaffBase staff, string propertyName)
		{
			var property = staff.GetType().GetProperty(propertyName);
			if (property != null)
			{
				return property.GetValue(staff);
			}
			return null;
		}
		
		// Property değerini ayarlama
		public static void SetPropertyValue(this StaffBase staff, string propertyName, object value)
		{
			var property = staff.GetType().GetProperty(propertyName);
			if (property != null && property.CanWrite)
			{
				property.SetValue(staff, value);
			}
		}
		
		// Property ayarlama veya dinamik özellik ekleme
		public static void SetOrAddProperty(this StaffBase staff, string propertyName, object value)
		{
			var property = staff.GetType().GetProperty(propertyName);
			if (property != null && property.CanWrite)
			{
				property.SetValue(staff, value);
			}
			else
			{
				// Dinamik özellik ekle
				staff.Set(propertyName, value);
			}
		}
		
		// StaffType string temsilini al
		public static string GetStaffTypeString(this StaffBase staff)
		{
			// Type property'sini kontrol et
			var typeProperty = staff.GetType().GetProperty("Type");
			if (typeProperty != null)
			{
				return typeProperty.GetValue(staff).ToString();
			}
			
			// StaffType property'sini kontrol et
			var staffTypeProperty = staff.GetType().GetProperty("StaffType");
			if (staffTypeProperty != null)
			{
				return (string)staffTypeProperty.GetValue(staff);
			}
			
			// Varsayılan olarak obje adının ilk kısmını al
			string name = staff.GetType().Name;
			if (name == "StaffBase")
			{
				return "Unknown";
			}
			return name;
		}
	}
}
