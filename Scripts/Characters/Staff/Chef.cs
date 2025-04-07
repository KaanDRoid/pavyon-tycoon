using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Staff
{
	
	public partial class Chef : StaffBase
	{
		// Aşçı özellikleri
		private float _cookingSkill = 0.5f;        // Pişirme becerisi
		private float _presentationSkill = 0.5f;   // Sunum becerisi
		private float _innovationSkill = 0.5f;     // Yenilikçilik becerisi
		private float _consistencySkill = 0.5f;    // Tutarlılık becerisi
		private float _speedSkill = 0.5f;          // Hız becerisi
		
		// Türk pavyon mezeleri / yemekleri
		public enum MezeType
		{
			Peynir,         // Peynir tabağı
			Kavun,          // Kavun
			Balik,          // Balık tabağı (Hamsi, çupra vb.)
			Kofte,          // Köfte
			Ciger,          // Ciğer
			Cacik,          // Cacık
			Patlican,       // Patlıcan salatası
			Humus,          // Humus
			Lahmacun,       // Lahmacun
			AciliEzme,      // Acılı ezme
			Patates,        // Kızartma patates
			Borek,          // Çeşitli börekler
			Dolma,          // Dolma
			Pilav,          // Pilav
			Cilingir,       // Çilingir sofrası (karışık meze)
			
			// Yeni Meze ve Atıştırmalıklar
			KlasikMezeTabagi, // Klasik Meze Tabağı (Humus, haydari, ezme, acılı ezme, patlıcan salatası)
			SigaraBoregi,    // Peynirli sigara böreği
			
			// Ana Yemekler ve Izgaralar
			AdanaKebap,     // Adana kebap
			SisKebap,       // Şiş kebap
			TavukIzgara,    // Tavuk ızgara
			Durum,          // Dürüm çeşitleri (etli, tavuklu)
			
			// Çorbalar
			MercimekCorbasi, // Mercimek çorbası
			EzogelinCorbasi, // Ezogelin çorbası
			
			// Tatlılar
			Baklava,        // Baklava
			Kunefe,         // Künefe
			Sutlac,         // Sütlaç
			
			// İçecekler
			TurkKahvesi,    // Türk kahvesi
			FiltreKahve,    // Filtre kahve
			BitkiCayi,      // Bitki çayları
			
			// Özel Menü
			OzelMenuYemegi  // Özel gün ve sezonluk menüler için
		}
		
		// Meze uzmanlık seviyeleri (0-1)
		private Dictionary<MezeType, float> _mezeSkillLevels = new Dictionary<MezeType, float>();
		
		// Mutfak aktivitesi
		private bool _isCooking = false;          // Şu anda yemek pişiriyor mu
		private bool _isPreparingMeze = false;    // Şu anda meze hazırlıyor mu
		private Queue<MezeOrder> _pendingOrders = new Queue<MezeOrder>(); // Bekleyen siparişler
		private int _maxSimultaneousOrders = 3;   // Aynı anda hazırlayabildiği sipariş sayısı
		private int _currentSimultaneousOrders = 0; // Şu anda hazırlanan sipariş sayısı
		
		// Yemek pişirme/meze hazırlama süreleri (saniye)
		private Dictionary<MezeType, float> _preparationTimes = new Dictionary<MezeType, float>();
		
		// İstatistikler
		private int _mezesServed = 0;             // Servis edilen meze sayısı
		private int _ordersRejected = 0;          // Reddedilen sipariş sayısı
		private int _customersComplained = 0;     // Şikayet eden müşteri sayısı
		private int _specialMezesCreated = 0;     // Oluşturulan özel meze sayısı
		
		// Lezzet ayarları
		private float _spiceLevel = 0.5f;         // Baharat seviyesi (0-1)
		private float _oilLevel = 0.5f;           // Yağ seviyesi (0-1)
		private float _sweetness = 0.3f;          // Tatlılık seviyesi (0-1)
		private float _portionSize = 0.5f;        // Porsiyon büyüklüğü (0-1)
		
		// Müşteri memnuniyet faktörleri
		private float _qualityEffect = 0.0f;      // Yemek kalitesi etkisi
		private float _fullnessEffect = 0.0f;     // Tokluk etkisi
		private float _uniquenessEffect = 0.0f;   // Benzersizlik etkisi
		
		// Yeni eklenen - müşteri memnuniyet kategorileri
		private float _fastServiceEffect = 0.0f;   // Hızlı servis etkisi
		private float _comfortFoodEffect = 0.0f;   // Konforlu yemek etkisi
		private float _premiumItemEffect = 0.0f;   // Premium yemek etkisi
		
		// Signals
		[Signal]
		public delegate void OrderCompletedEventHandler(string mezeType, float quality);
		
		[Signal]
		public delegate void OrderRejectedEventHandler(string mezeType, string reason);
		
		[Signal]
		public delegate void SpecialMezeCreatedEventHandler(string mezeName, float impressionScore);
		
		[Signal]
		public delegate void CustomerComplaintEventHandler(string mezeType, string complaint);
		
		[Signal]
		public delegate void SpecialEventPreparedEventHandler(string eventName, float bonusEffect);
		
		public override void _Ready()
		{
			base._Ready();
			
			// Tipi ayarla
			Type = StaffType.Chef;
			
			// Beceri değerlerini başlat (StaffBase'deki skills sözlüğünden al)
			if (_skills.ContainsKey("cooking")) _cookingSkill = _skills["cooking"];
			if (_skills.ContainsKey("presentation")) _presentationSkill = _skills["presentation"];
			if (_skills.ContainsKey("innovation")) _innovationSkill = _skills["innovation"];
			if (_skills.ContainsKey("consistency")) _consistencySkill = _skills["consistency"];
			if (_skills.ContainsKey("speed")) _speedSkill = _skills["speed"];
			
			// Meze beceri seviyelerini başlat
			InitializeMezeSkills();
			
			// Hazırlama sürelerini başlat
			InitializePreparationTimes();
			
			// Trait'lere göre değerleri düzenle
			AdjustValuesByTraits();
			
			GD.Print($"Chef {Name} initialized with cooking skill: {_cookingSkill}, presentation: {_presentationSkill}");
		}
		
		public override void _Process(double delta)
		{
			base._Process(delta);
			
			// Bekleyen siparişleri işle
			ProcessPendingOrders();
		}
		
		// Meze beceri seviyelerini başlat
		private void InitializeMezeSkills()
		{
			// Tüm mezeler için başlangıç beceri seviyeleri
			foreach (MezeType type in Enum.GetValues(typeof(MezeType)))
			{
				// Rastgele başlangıç seviyesi (0.2-0.6 arası)
				_mezeSkillLevels[type] = 0.2f + GD.Randf() * 0.4f;
			}
			
			// Bazı rastgele uzmanlık alanları seç (1-3 meze)
			int specialtyCount = GD.RandRange(1, 3);
			for (int i = 0; i < specialtyCount; i++)
			{
				Array mezeTypes = Enum.GetValues(typeof(MezeType));
				MezeType specialtyType = (MezeType)mezeTypes.GetValue(GD.RandRange(0, mezeTypes.Length - 1));
				
				// Uzmanlık seviyesini artır (0.6-0.9 arası)
				_mezeSkillLevels[specialtyType] = 0.6f + GD.Randf() * 0.3f;
			}
		}
		
		// Hazırlama sürelerini başlat
		private void InitializePreparationTimes()
		{
			// Her meze için hazırlama süresi (saniye)
			_preparationTimes[MezeType.Peynir] = 60;       // Peynir tabağı - 1 dk
			_preparationTimes[MezeType.Kavun] = 30;        // Kavun - 30 sn
			_preparationTimes[MezeType.Balik] = 300;       // Balık tabağı - 5 dk
			_preparationTimes[MezeType.Kofte] = 240;       // Köfte - 4 dk
			_preparationTimes[MezeType.Ciger] = 180;       // Ciğer - 3 dk
			_preparationTimes[MezeType.Cacik] = 120;       // Cacık - 2 dk
			_preparationTimes[MezeType.Patlican] = 150;    // Patlıcan salatası - 2.5 dk
			_preparationTimes[MezeType.Humus] = 120;       // Humus - 2 dk
			_preparationTimes[MezeType.Lahmacun] = 240;    // Lahmacun - 4 dk
			_preparationTimes[MezeType.AciliEzme] = 120;   // Acılı ezme - 2 dk
			_preparationTimes[MezeType.Patates] = 180;     // Kızartma patates - 3 dk
			_preparationTimes[MezeType.Borek] = 300;       // Börek - 5 dk
			_preparationTimes[MezeType.Dolma] = 360;       // Dolma - 6 dk
			_preparationTimes[MezeType.Pilav] = 240;       // Pilav - 4 dk
			_preparationTimes[MezeType.Cilingir] = 600;    // Çilingir sofrası - 10 dk
			
			// Yeni eklenen yemekler için hazırlama süreleri
			_preparationTimes[MezeType.KlasikMezeTabagi] = 360;  // Klasik Meze Tabağı - 6 dk
			_preparationTimes[MezeType.SigaraBoregi] = 180;      // Sigara Böreği - 3 dk
			_preparationTimes[MezeType.AdanaKebap] = 420;        // Adana Kebap - 7 dk
			_preparationTimes[MezeType.SisKebap] = 420;          // Şiş Kebap - 7 dk
			_preparationTimes[MezeType.TavukIzgara] = 360;       // Tavuk Izgara - 6 dk
			_preparationTimes[MezeType.Durum] = 240;             // Dürüm - 4 dk
			_preparationTimes[MezeType.MercimekCorbasi] = 180;   // Mercimek Çorbası - 3 dk
			_preparationTimes[MezeType.EzogelinCorbasi] = 210;   // Ezogelin Çorbası - 3.5 dk
			_preparationTimes[MezeType.Baklava] = 60;            // Baklava - 1 dk (hazır varsayılır)
			_preparationTimes[MezeType.Kunefe] = 300;            // Künefe - 5 dk
			_preparationTimes[MezeType.Sutlac] = 90;             // Sütlaç - 1.5 dk (hazır varsayılır)
			_preparationTimes[MezeType.TurkKahvesi] = 120;       // Türk Kahvesi - 2 dk
			_preparationTimes[MezeType.FiltreKahve] = 180;       // Filtre Kahve - 3 dk
			_preparationTimes[MezeType.BitkiCayi] = 120;         // Bitki Çayı - 2 dk
			_preparationTimes[MezeType.OzelMenuYemegi] = 480;    // Özel Menü - 8 dk
		}
		
		// Trait'lere göre değerleri düzenle
		private void AdjustValuesByTraits()
		{
			foreach (string trait in GetTraits())
			{
				switch (trait)
				{
					case "Professional":
						_cookingSkill = Mathf.Min(1.0f, _cookingSkill + 0.2f);
						_consistencySkill = Mathf.Min(1.0f, _consistencySkill + 0.15f);
						break;
					case "Experienced":
						_cookingSkill = Mathf.Min(1.0f, _cookingSkill + 0.15f);
						_speedSkill = Mathf.Min(1.0f, _speedSkill + 0.1f);
						break;
					case "Creative": // Özel aşçı trait'i
						_innovationSkill = Mathf.Min(1.0f, _innovationSkill + 0.25f);
						_presentationSkill = Mathf.Min(1.0f, _presentationSkill + 0.1f);
						break;
					case "FastWorker": // Özel aşçı trait'i
						_speedSkill = Mathf.Min(1.0f, _speedSkill + 0.25f);
						_maxSimultaneousOrders = 4; // Daha fazla sipariş hazırlayabilir
						break;
					case "Perfectionist": // Özel aşçı trait'i
						_presentationSkill = Mathf.Min(1.0f, _presentationSkill + 0.2f);
						_consistencySkill = Mathf.Min(1.0f, _consistencySkill + 0.2f);
						_speedSkill = Mathf.Max(0.1f, _speedSkill - 0.1f); // Daha yavaş çalışır
						break;
					case "MezeUstasi": // Özel aşçı trait'i
						// Tüm meze becerilerini artır
						foreach (MezeType type in Enum.GetValues(typeof(MezeType)))
						{
							_mezeSkillLevels[type] = Mathf.Min(1.0f, _mezeSkillLevels[type] + 0.15f);
						}
						break;
					case "BahcivanAhmet": // Özel aşçı trait'i - bir çeşit meze uzmanı
						_mezeSkillLevels[MezeType.Patlican] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Patlican] + 0.3f);
						_mezeSkillLevels[MezeType.Dolma] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Dolma] + 0.3f);
						_mezeSkillLevels[MezeType.AciliEzme] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.AciliEzme] + 0.3f);
						break;
					case "KasapSelim": // Özel aşçı trait'i - et uzmanı
						_mezeSkillLevels[MezeType.Kofte] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Kofte] + 0.3f);
						_mezeSkillLevels[MezeType.Ciger] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Ciger] + 0.3f);
						_mezeSkillLevels[MezeType.Balik] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Balik] + 0.2f);
						// Yeni eklenen et ürünleri
						_mezeSkillLevels[MezeType.AdanaKebap] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.AdanaKebap] + 0.3f);
						_mezeSkillLevels[MezeType.SisKebap] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.SisKebap] + 0.3f);
						_mezeSkillLevels[MezeType.TavukIzgara] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.TavukIzgara] + 0.2f);
						break;
					case "Lazy":
						_speedSkill = Mathf.Max(0.1f, _speedSkill - 0.15f);
						_maxSimultaneousOrders = 2; // Daha az sipariş hazırlayabilir
						break;
					case "Alcoholic":
						_consistencySkill = Mathf.Max(0.1f, _consistencySkill - 0.2f);
						break;
					// Yeni trait'ler ekleyebiliriz
					case "TatlıUstası": // Tatlı uzmanı
						_mezeSkillLevels[MezeType.Baklava] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Baklava] + 0.3f);
						_mezeSkillLevels[MezeType.Kunefe] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Kunefe] + 0.3f);
						_mezeSkillLevels[MezeType.Sutlac] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Sutlac] + 0.3f);
						break;
					case "ÇorbaUstası": // Çorba uzmanı
						_mezeSkillLevels[MezeType.MercimekCorbasi] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.MercimekCorbasi] + 0.3f);
						_mezeSkillLevels[MezeType.EzogelinCorbasi] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.EzogelinCorbasi] + 0.3f);
						break;
				}
			}
		}
		
		// Özel beceri geliştirme - StaffBase.ImproveSkills() metodunu override eder
		public override void ImproveSkills()
		{
			base.ImproveSkills();
			
			// Aşçı-spesifik becerileri geliştir
			float baseImprovement = 0.005f; // Günlük temel gelişim
			
			// Hazırlanan yemek sayısı gelişim hızını etkiler
			float mezeModifier = 1.0f + (Mathf.Min(_mezesServed, 20) * 0.01f);
			
			// Her beceri için rastgele gelişim
			if (GD.Randf() < 0.7f) // %70 ihtimalle pişirme gelişimi
			{
				_cookingSkill = Mathf.Min(1.0f, _cookingSkill + baseImprovement * mezeModifier);
				if (_skills.ContainsKey("cooking")) 
					_skills["cooking"] = _cookingSkill;
			}
			
			if (GD.Randf() < 0.6f) // %60 ihtimalle sunum gelişimi
			{
				_presentationSkill = Mathf.Min(1.0f, _presentationSkill + baseImprovement * mezeModifier);
				if (_skills.ContainsKey("presentation")) 
					_skills["presentation"] = _presentationSkill;
			}
			
			if (GD.Randf() < 0.5f) // %50 ihtimalle yenilikçilik gelişimi
			{
				_innovationSkill = Mathf.Min(1.0f, _innovationSkill + baseImprovement * mezeModifier);
				if (_skills.ContainsKey("innovation")) 
					_skills["innovation"] = _innovationSkill;
			}
			
			if (GD.Randf() < 0.6f) // %60 ihtimalle tutarlılık gelişimi
			{
				_consistencySkill = Mathf.Min(1.0f, _consistencySkill + baseImprovement * mezeModifier);
				if (_skills.ContainsKey("consistency")) 
					_skills["consistency"] = _consistencySkill;
			}
			
			if (GD.Randf() < 0.5f) // %50 ihtimalle hız gelişimi
			{
				_speedSkill = Mathf.Min(1.0f, _speedSkill + baseImprovement * mezeModifier);
				if (_skills.ContainsKey("speed")) 
					_skills["speed"] = _speedSkill;
			}
			
			// En çok hazırlanan meze türlerini geliştir
			if (_pendingOrders.Count > 0 && GD.Randf() < 0.8f)
			{
				MezeType mostPreparedType = _pendingOrders.Peek().Type;
				_mezeSkillLevels[mostPreparedType] = Mathf.Min(1.0f, _mezeSkillLevels[mostPreparedType] + baseImprovement * 1.5f);
				
				GD.Print($"Chef {Name} improved skill in {mostPreparedType} meze to {_mezeSkillLevels[mostPreparedType]}");
			}
		}
		
		// Sipariş sınıfı
		private class MezeOrder
		{
			public MezeType Type { get; set; }
			public string CustomerId { get; set; }
			public float TimeRemaining { get; set; }
			public float TotalTime { get; set; }
			public bool IsHighPriority { get; set; }
			
			public MezeOrder(MezeType type, string customerId, float preparationTime, bool isHighPriority = false)
			{
				Type = type;
				CustomerId = customerId;
				TimeRemaining = preparationTime;
				TotalTime = preparationTime;
				IsHighPriority = isHighPriority;
			}
		}
		
		// Sipariş al
		public bool TakeOrder(MezeType mezeType, string customerId, bool isHighPriority = false)
		{
			// Kapasite kontrolü
			if (_currentSimultaneousOrders >= _maxSimultaneousOrders && !isHighPriority)
			{
				// Kapasite dolu ve yüksek öncelikli değilse reddet
				EmitSignal(SignalName.OrderRejected, mezeType.ToString(), "Capacity Full");
				_ordersRejected++;
				
				GD.Print($"Chef {Name} rejected order for {mezeType} - capacity full");
				return false;
			}
			
			// Hazırlama süresi
			float preparationTime = _preparationTimes[mezeType];
			
			// Beceri seviyesine göre hazırlama süresini ayarla
			float skillModifier = _mezeSkillLevels[mezeType];
			float speedModifier = _speedSkill;
			
			// Nihai hazırlama süresi
			float finalPreparationTime = preparationTime * (1.0f - ((skillModifier + speedModifier) / 4.0f));
			
			// Minimum süre
			finalPreparationTime = Mathf.Max(finalPreparationTime, preparationTime * 0.5f);
			
			// Siparişi kuyruğa ekle
			MezeOrder order = new MezeOrder(mezeType, customerId, finalPreparationTime, isHighPriority);
			
			// Yüksek öncelikli siparişler kuyruğun başına
			if (isHighPriority)
			{
				List<MezeOrder> tempList = new List<MezeOrder>();
				
				// Mevcut yüksek öncelikli siparişleri koru
				while (_pendingOrders.Count > 0 && _pendingOrders.Peek().IsHighPriority)
				{
					tempList.Add(_pendingOrders.Dequeue());
				}
				
				// Yeni siparişi ekle
				tempList.Add(order);
				
				// Kalan siparişleri ekle
				while (_pendingOrders.Count > 0)
				{
					tempList.Add(_pendingOrders.Dequeue());
				}
				
				// Kuyruğu yeniden oluştur
				foreach (var item in tempList)
				{
					_pendingOrders.Enqueue(item);
				}
			}
			else
			{
				_pendingOrders.Enqueue(order);
			}
			
			// Eş zamanlı sipariş sayısını artır
			_currentSimultaneousOrders++;
			
			// Çalışma modunu ayarla
			if (!_isCooking && !_isPreparingMeze)
			{
				if (IsMezeCookable(mezeType))
				{
					_isCooking = true;
					SetActivity(ActivityState.Working);
					PlayAnimation("cooking");
				}
				else
				{
					_isPreparingMeze = true;
					SetActivity(ActivityState.Working);
					PlayAnimation("preparing_meze");
				}
			}
			
			GD.Print($"Chef {Name} took order for {mezeType} from {customerId}. ETA: {finalPreparationTime} seconds");
			return true;
		}
		
		// Sipariş al (string meze adı ile)
		public bool TakeOrder(string mezeTypeName, string customerId, bool isHighPriority = false)
		{
			if (Enum.TryParse<MezeType>(mezeTypeName, out MezeType mezeType))
			{
				return TakeOrder(mezeType, customerId, isHighPriority);
			}
			
			GD.Print($"Invalid meze type: {mezeTypeName}");
			return false;
		}
		
		// Bir meze türünün pişirilmesi gerekip gerekmediği
		private bool IsMezeCookable(MezeType type)
		{
			switch (type)
			{
				case MezeType.Balik:
				case MezeType.Kofte:
				case MezeType.Ciger:
				case MezeType.Lahmacun:
				case MezeType.Patates:
				case MezeType.Borek:
				case MezeType.Pilav:
				// Yeni pişirilmesi gereken yemek türleri
				case MezeType.SigaraBoregi:
				case MezeType.AdanaKebap:
				case MezeType.SisKebap:
				case MezeType.TavukIzgara:
				case MezeType.Durum:
				case MezeType.MercimekCorbasi:
				case MezeType.EzogelinCorbasi:
				case MezeType.Kunefe:  // Künefe pişirme gerektirir
					return true;
				default:
					return false;
			}
		}
		
		// Bekleyen siparişleri işle
		private void ProcessPendingOrders()
		{
			if (_pendingOrders.Count == 0)
			{
				// Hiç sipariş yoksa çalışma modunu sıfırla
				if (_isCooking || _isPreparingMeze)
				{
					_isCooking = false;
					_isPreparingMeze = false;
					SetActivity(ActivityState.Idle);
				}
				
				return;
			}
			
			// İlk siparişi al
			MezeOrder currentOrder = _pendingOrders.Peek();
			
			// Zamanı güncelle
			currentOrder.TimeRemaining -= GetProcessingSpeed() * (float)GetProcessDeltaTime();
			
			// Sipariş tamamlandı mı kontrol et
			if (currentOrder.TimeRemaining <= 0)
			{
				// Siparişi tamamla
				CompleteOrder(_pendingOrders.Dequeue());
			}
		}
		
		// İşlem hızını hesapla
		private float GetProcessingSpeed()
		{
			// Temel hız
			float baseSpeed = 1.0f;
			
			// Enerji faktörü
			float energyFactor = Energy;
			
			// Ruh hali faktörü
			float moodFactor = Mood;
			
			// Hız becerisi faktörü
			float speedFactor = _speedSkill;
			
			// Eş zamanlı sipariş faktörü (ne kadar çok sipariş, o kadar yavaş)
			float ordersFactor = 1.0f - ((_currentSimultaneousOrders - 1) * 0.1f);
			ordersFactor = Mathf.Max(ordersFactor, 0.7f); // En fazla %30 yavaşlatma
			
			// Nihai hız
			return baseSpeed * energyFactor * moodFactor * speedFactor * ordersFactor;
		}
		
		// Siparişi tamamla
		private void CompleteOrder(MezeOrder order)
		{
			// Eş zamanlı sipariş sayısını azalt
			_currentSimultaneousOrders--;
			
			// Meze kalitesini hesapla
			float quality = CalculateMezeQuality(order.Type);
			
			// Meze tamamlandı sinyali gönder
			EmitSignal(SignalName.OrderCompleted, order.Type.ToString(), quality);
			
			// İstatistik güncelle
			_mezesServed++;
			
			// Müşteriye servis et (varsa)
			if (GetTree().Root.HasNode("GameManager/CustomerManager"))
			{
				var customerManager = GetTree().Root.GetNode("GameManager/CustomerManager");
				
				if (customerManager.HasMethod("ServeMezeToCustomer"))
				{
					customerManager.Call("ServeMezeToCustomer", order.CustomerId, order.Type.ToString(), quality);
					GD.Print($"Chef {Name} served {order.Type} to customer {order.CustomerId} with quality: {quality}");
				}
			}
			
			// Deneyim kazan
			AddExperience(1);
			
			// Meze türü becerisini geliştir
			ImproveMezeSkill(order.Type);
			
			// Çalışma modunu güncelle
			UpdateWorkingMode();
		}
		
		// Meze kalitesi hesapla
		private float CalculateMezeQuality(MezeType type)
		{
			// Temel beceri faktörleri
			float cookingFactor = _cookingSkill * 0.3f;
			float presentationFactor = _presentationSkill * 0.2f;
			float consistencyFactor = _consistencySkill * 0.2f;
			
			// Meze spesifik beceri faktörü
			float mezeSkillFactor = _mezeSkillLevels[type] * 0.3f;
			
			// Enerji ve ruh hali faktörü
			float conditionFactor = (Energy * 0.5f + Mood * 0.5f) * 0.2f;
			
			// Lezzet ayarlarının etkisi (meze tipine göre optimal değerler farklı olabilir)
			float tasteFactor = CalculateTasteFactor(type);
			
			// Nihai kalite (0-1 arası)
			float finalQuality = cookingFactor + presentationFactor + consistencyFactor + mezeSkillFactor + conditionFactor + tasteFactor;
			
			// Normalize et
			return Mathf.Clamp(finalQuality, 0.1f, 1.0f);
		}
		
		// Lezzet faktörü hesapla
		private float CalculateTasteFactor(MezeType type)
		{
			// Her meze türü için optimal lezzet ayarları
			float optimalSpice = 0.5f;
			float optimalOil = 0.5f;
			float optimalSweetness = 0.3f;
			float optimalPortion = 0.5f;
			
			// Meze türüne göre optimal değerleri ayarla
			switch (type)
			{
				case MezeType.AciliEzme:
					optimalSpice = 0.8f; // Acılı ezme daha baharatlı olmalı
					optimalOil = 0.6f;
					optimalSweetness = 0.1f;
					break;
				case MezeType.Balik:
					optimalSpice = 0.3f;
					optimalOil = 0.4f;
					optimalSweetness = 0.2f;
					break;
				case MezeType.Kofte:
					optimalSpice = 0.6f;
					optimalOil = 0.5f;
					optimalSweetness = 0.2f;
					break;
				case MezeType.Patates:
					optimalSpice = 0.4f;
					optimalOil = 0.7f; // Kızartma patates daha yağlı olmalı
					optimalSweetness = 0.1f;
					break;
				case MezeType.Peynir:
					optimalSpice = 0.2f;
					optimalOil = 0.3f;
					optimalSweetness = 0.2f;
					break;
				
				// Yeni eklenen yemek türleri için optimal değerler
				case MezeType.KlasikMezeTabagi:
					optimalSpice = 0.6f;
					optimalOil = 0.5f;
					optimalSweetness = 0.2f;
					optimalPortion = 0.6f; // Biraz daha büyük porsiyon
					break;
				case MezeType.SigaraBoregi:
					optimalSpice = 0.3f;
					optimalOil = 0.7f; // Kızartma olduğu için yağlı
					optimalSweetness = 0.1f;
					break;
				case MezeType.AdanaKebap:
					optimalSpice = 0.8f; // Baharatlı
					optimalOil = 0.6f; 
					optimalSweetness = 0.1f;
					optimalPortion = 0.7f; // Ana yemek olduğu için büyük porsiyon
					break;
				case MezeType.SisKebap:
					optimalSpice = 0.6f;
					optimalOil = 0.5f;
					optimalSweetness = 0.2f;
					optimalPortion = 0.7f; // Ana yemek olduğu için büyük porsiyon
					break;
				case MezeType.TavukIzgara:
					optimalSpice = 0.5f;
					optimalOil = 0.4f;
					optimalSweetness = 0.2f;
					optimalPortion = 0.7f; // Ana yemek olduğu için büyük porsiyon
					break;
				case MezeType.Durum:
					optimalSpice = 0.6f;
					optimalOil = 0.5f;
					optimalSweetness = 0.2f;
					optimalPortion = 0.6f;
					break;
				case MezeType.MercimekCorbasi:
					optimalSpice = 0.4f;
					optimalOil = 0.3f;
					optimalSweetness = 0.2f;
					optimalPortion = 0.5f;
					break;
				case MezeType.EzogelinCorbasi:
					optimalSpice = 0.5f;
					optimalOil = 0.3f;
					optimalSweetness = 0.1f;
					optimalPortion = 0.5f;
					break;
				case MezeType.Baklava:
					optimalSpice = 0.1f;
					optimalOil = 0.4f;
					optimalSweetness = 0.9f; // Çok tatlı
					optimalPortion = 0.4f; // Küçük porsiyon
					break;
				case MezeType.Kunefe:
					optimalSpice = 0.1f;
					optimalOil = 0.5f;
					optimalSweetness = 0.8f; // Tatlı
					optimalPortion = 0.5f;
					break;
				case MezeType.Sutlac:
					optimalSpice = 0.1f;
					optimalOil = 0.2f;
					optimalSweetness = 0.7f; // Tatlı
					optimalPortion = 0.5f;
					break;
				case MezeType.TurkKahvesi:
					optimalSpice = 0.3f;
					optimalOil = 0.1f;
					optimalSweetness = 0.4f; // Kişiye göre değişebilir
					optimalPortion = 0.3f; // Küçük fincan
					break;
				case MezeType.FiltreKahve:
					optimalSpice = 0.2f;
					optimalOil = 0.1f;
					optimalSweetness = 0.3f;
					optimalPortion = 0.4f;
					break;
				case MezeType.BitkiCayi:
					optimalSpice = 0.3f;
					optimalOil = 0.1f;
					optimalSweetness = 0.2f;
					optimalPortion = 0.4f;
					break;
				case MezeType.OzelMenuYemegi:
					optimalSpice = 0.7f; // Özel menü daha lezzetli olmalı
					optimalOil = 0.6f;
					optimalSweetness = 0.4f;
					optimalPortion = 0.6f;
					break;
			}
			
			// Mevcut ayarların optimal değerlere uzaklığı
			float spiceDifference = 1.0f - Mathf.Abs(_spiceLevel - optimalSpice);
			float oilDifference = 1.0f - Mathf.Abs(_oilLevel - optimalOil);
			float sweetnessDifference = 1.0f - Mathf.Abs(_sweetness - optimalSweetness);
			float portionDifference = 1.0f - Mathf.Abs(_portionSize - optimalPortion);
			
			// Lezzet faktörü (0-0.2 arası)
			return (spiceDifference + oilDifference + sweetnessDifference + portionDifference) / 20.0f;
		}
		
		// Meze becerilerini geliştir
		private void ImproveMezeSkill(MezeType type)
		{
			// Meze türü tecrübesi arttıkça beceri gelişir
			float improvementAmount = 0.005f;
			
			_mezeSkillLevels[type] = Mathf.Min(1.0f, _mezeSkillLevels[type] + improvementAmount);
		}
		
		// Çalışma modunu güncelle
		private void UpdateWorkingMode()
		{
			if (_pendingOrders.Count > 0)
			{
				// Hala işlenecek sipariş var
				MezeOrder nextOrder = _pendingOrders.Peek();
				
				if (IsMezeCookable(nextOrder.Type))
				{
					_isCooking = true;
					_isPreparingMeze = false;
					PlayAnimation("cooking");
				}
				else
				{
					_isCooking = false;
					_isPreparingMeze = true;
					PlayAnimation("preparing_meze");
				}
			}
			else
			{
				// Tüm siparişler tamamlandı
				_isCooking = false;
				_isPreparingMeze = false;
				SetActivity(ActivityState.Idle);
			}
		}
		
		// Lezzet ayarlarını değiştir
		public void AdjustTasteSettings(float spiceLevel, float oilLevel, float sweetness, float portionSize)
		{
			_spiceLevel = Mathf.Clamp(spiceLevel, 0.0f, 1.0f);
			_oilLevel = Mathf.Clamp(oilLevel, 0.0f, 1.0f);
			_sweetness = Mathf.Clamp(sweetness, 0.0f, 1.0f);
			_portionSize = Mathf.Clamp(portionSize, 0.0f, 1.0f);
			
			GD.Print($"Chef {Name} adjusted taste settings: spice {_spiceLevel}, oil {_oilLevel}, sweetness {_sweetness}, portion {_portionSize}");
		}
		
		// Özel meze oluştur
		public bool CreateSpecialMeze(string mezeName, List<MezeType> ingredients)
		{
			if (ingredients == null || ingredients.Count < 2)
			{
				GD.Print("Need at least 2 ingredients to create a special meze");
				return false;
			}
			
			// Yenilikçilik becerisi etkisi
			float innovationFactor = _innovationSkill;
			
			// İçerik bilgisi etkisi
			float ingredientKnowledgeFactor = 0.0f;
			foreach (var ingredient in ingredients)
			{
				ingredientKnowledgeFactor += _mezeSkillLevels[ingredient];
			}
			ingredientKnowledgeFactor /= ingredients.Count;
			
			// Benzersizlik puanı
			float uniquenessScore = 0.4f + (innovationFactor * 0.3f) + (ingredientKnowledgeFactor * 0.3f);
			
			// İzlenim puanı
			float impressionScore = Mathf.Clamp(uniquenessScore + GD.Randf() * 0.2f, 0.1f, 1.0f);
			
			// Özel meze istatistiğini artır
			_specialMezesCreated++;
			
			// Deneyim kazan
			AddExperience(3);
			
			// Özel meze oluşturma sinyali gönder
			EmitSignal(SignalName.SpecialMezeCreated, mezeName, impressionScore);
			
			GD.Print($"Chef {Name} created special meze '{mezeName}' with impression score: {impressionScore}");
			
			// Müşteri memnuniyet faktörlerini güncelle
			_uniquenessEffect = Mathf.Min(0.2f, _uniquenessEffect + 0.05f);
			
			return true;
		}
		
		// Müşteri şikayeti işleme
		public void HandleComplaint(string customerId, MezeType mezeType, string complaintReason)
		{
			// Şikayet istatistiğini artır
			_customersComplained++;
			
			// Şikayet sinyali gönder
			EmitSignal(SignalName.CustomerComplaintEventHandler, mezeType.ToString(), complaintReason);
			
			// Ruh halini etkile
			AdjustMood(-0.05f, "Customer Complaint");
			
			// Bu meze türünde daha dikkatli olma - bir sonraki sefere daha iyi hazırla
			float currentSkill = _mezeSkillLevels[mezeType];
			if (currentSkill < 0.8f) // Zaten usta değilse
			{
				_mezeSkillLevels[mezeType] = Mathf.Min(1.0f, currentSkill + 0.02f);
				GD.Print($"Chef {Name} is more careful about {mezeType} after complaint. New skill: {_mezeSkillLevels[mezeType]}");
			}
		}
		
		// Müşteri ile etkileşim - StaffBase.InteractWithCustomer() metodunu override eder
		public override void InteractWithCustomer(Node3D customer)
		{
			if (customer == null) return;
			
			string customerId = customer.Name;
			
			// Müşteriden sipariş kontrolü
			bool hasOrder = false;
			string mezeTypeRequest = "";
			bool isHighPriority = false;
			
			// Sipariş kontrolü
			if (customer.GetType().GetMethod("HasMezeOrder") != null)
			{
				try 
				{
					hasOrder = (bool)customer.Call("HasMezeOrder");
					if (hasOrder && customer.GetType().GetMethod("GetMezeOrderType") != null)
					{
						mezeTypeRequest = (string)customer.Call("GetMezeOrderType");
					}
					
					// VIP müşteriler yüksek öncelikli
					if (customer.GetType().GetProperty("IsVIP") != null)
					{
						isHighPriority = (bool)customer.Get("IsVIP");
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error checking meze order: {e.Message}");
				}
			}
			
			// Sipariş işleme
			if (hasOrder && !string.IsNullOrEmpty(mezeTypeRequest))
			{
				if (Enum.TryParse<MezeType>(mezeTypeRequest, out MezeType mezeType))
				{
					bool orderAccepted = TakeOrder(mezeType, customerId, isHighPriority);
					
					if (!orderAccepted)
					{
						// Siparişi reddet - müşteriye bildir
						if (customer.GetType().GetMethod("MezeOrderRejected") != null)
						{
							try 
							{
								customer.Call("MezeOrderRejected", "Chef is busy");
							}
							catch (Exception e)
							{
								GD.PrintErr($"Error notifying order rejection: {e.Message}");
							}
						}
					}
				}
				else
				{
					GD.Print($"Invalid meze type requested: {mezeTypeRequest}");
				}
			}
			else
			{
				// Sipariş yok, genel durumu bildir
				int pendingOrderCount = _pendingOrders.Count;
				
				// Müşteriye bilgi ver
				if (customer.GetType().GetMethod("ReceiveChefStatus") != null)
				{
					try 
					{
						customer.Call("ReceiveChefStatus", pendingOrderCount, _currentSimultaneousOrders, _maxSimultaneousOrders);
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error sharing chef status: {e.Message}");
					}
				}
			}
			
			// Enerji tüketimi
			AdjustEnergy(-0.01f, "Customer Interaction");
		}
		
		// Özel performans davranışı - StaffBase.PerformSpecialBehavior() metodunu override eder
		public override void PerformSpecialBehavior()
		{
			// Özel gösterişli meze hazırlama - kalite ve etki bonusu
			PerformShowCooking();
		}
		
		// Gösterişli pişirme
		private void PerformShowCooking()
		{
			// Gösterişli pişirme modu
			SetActivity(ActivityState.Special);
			
			// Gösterişli pişirme animasyonu
			PlayAnimation("show_cooking");
			
			// Süreyi belirle
			float duration = 60.0f; // 1 dakika
			
			// Geçici kalite bonusu
			float originalPresentation = _presentationSkill;
			_presentationSkill = Mathf.Min(1.0f, _presentationSkill + 0.2f);
			
			// Geçici müşteri memnuniyet faktörleri bonusu
			_qualityEffect += 0.1f;
			
			// Timer ile normal moda dönüş
			Timer timer = new Timer
			{
				WaitTime = duration,
				OneShot = true
			};
			
			AddChild(timer);
			timer.Timeout += () => 
			{
				_presentationSkill = originalPresentation;
				_qualityEffect -= 0.1f;
				
				if (_isCooking)
				{
					SetActivity(ActivityState.Working);
					PlayAnimation("cooking");
				}
				else if (_isPreparingMeze)
				{
					SetActivity(ActivityState.Working);
					PlayAnimation("preparing_meze");
				}
				else
				{
					SetActivity(ActivityState.Idle);
				}
				
				GD.Print($"Chef {Name}'s show cooking ended");
			};
			timer.Start();
			
			// Deneyim kazanma
			AddExperience(2);
			
			GD.Print($"Chef {Name} started show cooking! All mezes will have better presentation for {duration} seconds.");
		}
		
		// Özel animasyon - StaffBase.PlaySpecialAnimation() metodunu override eder
		protected override void PlaySpecialAnimation()
		{
			PlayAnimation("show_cooking");
		}
		
		// Seviye atlama özel efektleri - StaffBase.OnLevelUp() metodunu override eder
		protected override void OnLevelUp()
		{
			base.OnLevelUp();
			
			// Pişirme becerisi gelişimi
			_cookingSkill = Mathf.Min(1.0f, _cookingSkill + 0.05f);
			
			// Sunum becerisi gelişimi
			_presentationSkill = Mathf.Min(1.0f, _presentationSkill + 0.04f);
			
			// Yenilikçilik becerisi gelişimi
			_innovationSkill = Mathf.Min(1.0f, _innovationSkill + 0.03f);
			
			// Tutarlılık becerisi gelişimi
			_consistencySkill = Mathf.Min(1.0f, _consistencySkill + 0.04f);
			
			// Hız becerisi gelişimi
			_speedSkill = Mathf.Min(1.0f, _speedSkill + 0.03f);
			
			// Her 2 seviyede bir eşzamanlı sipariş kapasitesi artışı
			if (Level % 2 == 0 && _maxSimultaneousOrders < 5)
			{
				_maxSimultaneousOrders++;
			}
			
			// Beceri değerlerini skills sözlüğüne aktar
			if (_skills.ContainsKey("cooking")) _skills["cooking"] = _cookingSkill;
			if (_skills.ContainsKey("presentation")) _skills["presentation"] = _presentationSkill;
			if (_skills.ContainsKey("innovation")) _skills["innovation"] = _innovationSkill;
			if (_skills.ContainsKey("consistency")) _skills["consistency"] = _consistencySkill;
			if (_skills.ContainsKey("speed")) _skills["speed"] = _speedSkill;
			
			GD.Print($"Chef {Name} leveled up: cooking {_cookingSkill}, presentation {_presentationSkill}, speed {_speedSkill}, max orders {_maxSimultaneousOrders}");
		}
		
		// Günlük güncelleme - StaffBase.UpdateDaily() metodunu override eder
		public override void UpdateDaily()
		{
			base.UpdateDaily();
			
			// İstatistikleri sıfırla
			_mezesServed = 0;
			_ordersRejected = 0;
			_customersComplained = 0;
			_specialMezesCreated = 0;
			
			// Müşteri memnuniyet faktörlerini sıfırla
			_qualityEffect = 0.0f;
			_fullnessEffect = 0.0f;
			_uniquenessEffect = 0.0f;
			_fastServiceEffect = 0.0f;
			_comfortFoodEffect = 0.0f;
			_premiumItemEffect = 0.0f;
		}
		
		// Özel risk faktörleri - StaffBase.ApplySpecialRiskFactors() metodunu override eder
		protected override void ApplySpecialRiskFactors()
		{
			base.ApplySpecialRiskFactors();
			
			// Aşçı-spesifik risk faktörleri
			
			// Yüksek şikayet sayısı = daha fazla sadakatsizlik riski
			if (_customersComplained > 3)
			{
				_disloyaltyRisk += 0.05f;
			}
		}
		
		// Özel performans faktörü hesaplama - StaffBase.CalculateSpecialPerformanceFactor() metodunu override eder
		protected override float CalculateSpecialPerformanceFactor()
		{
			// Pişirme, sunum ve tutarlılık faktörü
			return (_cookingSkill * 0.4f + _presentationSkill * 0.3f + _consistencySkill * 0.3f - 0.5f) * 0.2f;
		}
		
		// Tüm siparişleri iptal et
		public void CancelAllOrders()
		{
			// Tüm bekleyen siparişleri temizle
			_pendingOrders.Clear();
			_currentSimultaneousOrders = 0;
			
			// Çalışma modunu sıfırla
			_isCooking = false;
			_isPreparingMeze = false;
			SetActivity(ActivityState.Idle);
			
			GD.Print($"Chef {Name} canceled all pending orders");
		}
		
		// Müşteri memnuniyeti hesapla
		public float CalculateCustomerSatisfaction(MezeType type, float quality)
		{
			// Temel memnuniyet kaliteye bağlı
			float satisfaction = quality * 0.6f;
			
			// Yemek türüne göre özel modifikasyonlar
			switch(type)
			{
				// Hızlı servis edilebilen atıştırmalıklar
				case MezeType.SigaraBoregi:
				case MezeType.Patates:
				case MezeType.Lahmacun:
				case MezeType.Durum:
					satisfaction += 0.1f + _fastServiceEffect; // Hızlı servis bonusu
					break;
				
				// Premium yemekler - yüksek memnuniyet
				case MezeType.AdanaKebap:
				case MezeType.SisKebap:
				case MezeType.TavukIzgara:
					satisfaction += 0.2f + _premiumItemEffect; // Premium yemek bonusu
					break;
				
				// Konfor yemekleri - belirli koşullarda bonus
				case MezeType.MercimekCorbasi:
				case MezeType.EzogelinCorbasi:
					satisfaction += 0.15f + _comfortFoodEffect;
					break;
				
				// Tatlılar ve içecekler - öğün sonu memnuniyeti
				case MezeType.Baklava:
				case MezeType.Kunefe:
				case MezeType.Sutlac:
				case MezeType.TurkKahvesi:
				case MezeType.FiltreKahve:
				case MezeType.BitkiCayi:
					satisfaction += 0.15f; // Öğün sonu bonusu
					break;
				
				// Özel öğeler - en yüksek memnuniyet
				case MezeType.OzelMenuYemegi:
				case MezeType.Cilingir:
				case MezeType.KlasikMezeTabagi:
					satisfaction += 0.25f + _uniquenessEffect; // Özel öğe bonusu
					break;
			}
			
			// Normalize et ve döndür
			return Mathf.Clamp(satisfaction, 0.0f, 1.0f);
		}
		
		// Özel gün/sezon etkinlikleri için hazırlık
		public void PrepareForSpecialEvent(string eventName)
		{
			// Geçici beceri artışı
			float boost = 0.1f;
			
			GD.Print($"Chef {Name} is preparing for special event: {eventName}");
			
			// Etkinlik türüne göre becerileri artır
			switch (eventName.ToLower())
			{
				case "new year":
				case "yılbaşı":
					// Yılbaşı özel - yenilikçilik ve sunum becerilerini artır
					_innovationSkill = Mathf.Min(1.0f, _innovationSkill + boost);
					_presentationSkill = Mathf.Min(1.0f, _presentationSkill + boost);
					// Özel menü becerisi geçici artış
					_mezeSkillLevels[MezeType.OzelMenuYemegi] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.OzelMenuYemegi] + 0.2f);
					break;
					
				case "birthday":
				case "doğum günü":
					// Doğum günü özel - tatlı becerilerini artır
					_mezeSkillLevels[MezeType.Baklava] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Baklava] + boost);
					_mezeSkillLevels[MezeType.Kunefe] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Kunefe] + boost);
					_mezeSkillLevels[MezeType.Sutlac] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.Sutlac] + boost);
					break;
					
				case "themed night":
				case "temalı gece":
					// Temalı geceler - yenilikçilik ve özel menü becerilerini artır
					_innovationSkill = Mathf.Min(1.0f, _innovationSkill + boost);
					_mezeSkillLevels[MezeType.OzelMenuYemegi] = Mathf.Min(1.0f, _mezeSkillLevels[MezeType.OzelMenuYemegi] + 0.15f);
					break;
					
				default:
					// Genel özel etkinlik artışı
					_cookingSkill = Mathf.Min(1.0f, _cookingSkill + boost * 0.5f);
					_presentationSkill = Mathf.Min(1.0f, _presentationSkill + boost * 0.5f);
					break;
			}
			
			// Etkinlik için özel menü oluştur
			List<MezeType> ingredients = new List<MezeType>();
			ingredients.Add(MezeType.OzelMenuYemegi);
			
			// 1-2 rastgele içerik ekle
			Array mezeTypes = Enum.GetValues(typeof(MezeType));
			for (int i = 0; i < GD.RandRange(1, 2); i++)
			{
				MezeType randomType = (MezeType)mezeTypes.GetValue(GD.RandRange(0, mezeTypes.Length - 1));
				if (randomType != MezeType.OzelMenuYemegi && !ingredients.Contains(randomType))
				{
					ingredients.Add(randomType);
				}
			}
			
			// Özel yemeği oluştur
			CreateSpecialMeze($"{eventName} Special", ingredients);
			
			// Etkinlik hazırlık sinyali gönder
			float bonusEffect = boost + (_innovationSkill * 0.1f);
			EmitSignal(SignalName.SpecialEventPreparedEventHandler, eventName, bonusEffect);
		}
		
		// Özellik değerlerini döndür
		public new Dictionary<string, object> GetStats()
		{
			Dictionary<string, object> stats = base.GetStats();
			
			// Aşçı-spesifik değerleri ekle
			stats["CookingSkill"] = _cookingSkill;
			stats["PresentationSkill"] = _presentationSkill;
			stats["InnovationSkill"] = _innovationSkill;
			stats["ConsistencySkill"] = _consistencySkill;
			stats["SpeedSkill"] = _speedSkill;
			stats["MezesServed"] = _mezesServed;
			stats["OrdersRejected"] = _ordersRejected;
			stats["CustomersComplained"] = _customersComplained;
			stats["SpecialMezesCreated"] = _specialMezesCreated;
			stats["MaxSimultaneousOrders"] = _maxSimultaneousOrders;
			stats["CurrentOrders"] = _currentSimultaneousOrders;
			stats["PendingOrdersCount"] = _pendingOrders.Count;
			
			// Lezzet ayarları
			stats["SpiceLevel"] = _spiceLevel;
			stats["OilLevel"] = _oilLevel;
			stats["Sweetness"] = _sweetness;
			stats["PortionSize"] = _portionSize;
			
			// Meze beceri seviyeleri
			Dictionary<string, float> mezeSkills = new Dictionary<string, float>();
			foreach (var item in _mezeSkillLevels)
			{
				mezeSkills[item.Key.ToString()] = item.Value;
			}
			stats["MezeSkills"] = mezeSkills;
			
			return stats;
		}
	}
}
