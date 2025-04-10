// Scripts/Characters/Customers/NostalgicCustomer.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Characters.Customers
{
	public partial class NostalgicCustomer : CustomerBase
	{
		// Nostaljik müşteriye özgü özellikler
		private float _tavernaLoveFactor = 0.9f;       // Taverna müziğini sevme oranı
		private float _rakiPreference = 0.9f;          // Rakı tercih oranı
		private float _traditionalFoodPreference = 0.8f; // Geleneksel meze tercihi
		private float _oldAnkaraStoriesCount = 0;      // Anlattığı eski Ankara hikayeleri sayısı
		private List<string> _nostalgicPhrases;        // Nostaljik sözler listesi
		private bool _hasSharedStories = false;        // Hikaye anlatma durumu
		
		// En sevdiği eski şarkılar
		private List<string> _favoriteSongs;
		
		// Bahsettiği eski Ankara mekanları
		private List<string> _oldAnkaraPlaces;
		
		// Eski Ankara'yı özleme seviyesi (0-1 arası)
		private float _nostalgiaMeter = 0.7f;
		
		// Geç kalma takıntısı - eskiden terbiyesizlik sayılırdı
		private bool _isPunctual = true;
		
		// Yeni jenerasyona eleştiri modunda mı?
		private bool _isCriticalOfYouth = false;
		
		public override void _Ready()
		{
			base._Ready();
			
			// Nostaljik sözleri başlat
			InitializeNostalgicPhrases();
			
			// Favori şarkıları başlat
			InitializeFavoriteSongs();
			
			// Eski Ankara mekanlarını başlat
			InitializeOldAnkaraPlaces();
		}
		
		// Nostaljik müşteriye özel override metotlar
		protected override void OnReady()
		{
			base.OnReady();
			
			// Nostaljik müşteriye özel görünüm ayarlamaları
			if (_model != null)
			{
				// Burada sahnedeki modelin görünüşü ayarlanabilir
				// Örneğin: Eski tarz kıyafet, bıyık, vs.
			}
		}
		
		// Nostaljik sözleri başlat
		private void InitializeNostalgicPhrases()
		{
			_nostalgicPhrases = new List<string>
			{
				"Bir zamanlar Maltepe'de bir pavyon vardı, adı Gönül'dü...",
				"Eskiden buralarda Zeki Müren'i dinlerdik, ah o günler...",
				"Şimdiki gençler bilmez, Ulus ne geceler görmüştür...",
				"O zamanlar Kızılay'da üç tane pavyon vardı, hepsinde de kalite...",
				"Ankara'nın o eski karı, o eski soğuğu, o eski insanları nerede?",
				"Yetmişlerde buralarda rakı kadehinden büyük dert yoktu...",
				"O zamanlar bira 5 liraydı, şimdi...",
				"Eskiden bu rakıların tadı başkaydı yeğenim, eskiden her şey başkaydı...",
				"Gazi Paşa zamanında Ankara'yı görmeliydin...",
				"Angaralıyık ulen, Angaralı! Eskinin Angarası gibisi yoktu...",
				"Bak şu Seymen havasına, artık böyle oynayan kalmadı...",
				"Oğlum yeşil ördek gibi daldım göllere, eskiden bu şarkıyla coşardı buralar...",
				"Rahmetli Müzeyyen Senar sahneye çıktığında herkes ayakta alkışlardı..."
			};
		}
		
		// Favori şarkıları başlat
		private void InitializeFavoriteSongs()
		{
			_favoriteSongs = new List<string>
			{
				"Ankara'nın Bağları",
				"Sen Nerdesin Ben Nerdeyim",
				"Sev Kardeşim",
				"İki Keklik",
				"Vurgun",
				"Seni Andım Bu Gece",
				"Neredesin Sen",
				"Sessiz Gemi",
				"Kadifeden Kesesi",
				"Havada Bulut Yok",
				"Bir Demet Yasemen",
				"Hüzün"
			};
		}
		
		// Eski Ankara mekanlarını başlat
		private void InitializeOldAnkaraPlaces()
		{
			_oldAnkaraPlaces = new List<string>
			{
				"Piknik Pavyon",
				"Süreyya Gazinosu",
				"Çankaya Gazinosu",
				"Kulis",
				"Meram Bağları",
				"Gar Gazinosu",
				"Bomonti Bahçesi",
				"Gölbaşı Gazinosu",
				"Kavaklidere Meyhanesi",
				"Yeni Melek Pavyon",
				"Hamamönü Sokakları",
				"Eski Ulus Meydanı",
				"Karpiç Lokantası"
			};
		}
		
		// Durum güncellemelerinde özelleştirilmiş davranışlar
		protected override void UpdateSittingState(float delta)
		{
			base.UpdateSittingState(delta);
			
			// Nostaljik müşteri daha sık şikayetlenir ve eski günlerden bahseder
			if (!_hasSharedStories && _timeInPavyon > 15.0f && GD.Randf() < 0.01f)
			{
				ShareNostalgicStory();
				_hasSharedStories = true;
			}
			
			// Taverna müziği çalıyorsa daha mutlu olur
			if (IsPlayingTavernaMusic() && GD.Randf() < 0.02f)
			{
				AdjustSatisfaction(0.05f, "Taverna müziği çalıyor");
				SayRandomNostalgicPhrase();
			}
			
			// Müzikte bir favori şarkı çalıyorsa coşar
			if (IsPlayingFavoriteSong() && GD.Randf() < 0.03f)
			{
				AdjustSatisfaction(0.1f, "Favori şarkı çalıyor");
				SayFavoriteSongReaction();
			}
			
			// Modern, gürültülü müzikten rahatsız olur
			if (IsPlayingModernMusic() && GD.Randf() < 0.02f)
			{
				AdjustSatisfaction(-0.05f, "Modern müzik rahatsız ediyor");
				CriticizeModernMusic();
			}
			
			// Genç müşteriler ortama hakimse rahatsız olur
			if (AreYoungCustomersDominant() && GD.Randf() < 0.015f)
			{
				_isCriticalOfYouth = true;
				AdjustSatisfaction(-0.03f, "Gençlerin tavırları");
				CriticizeYouth();
			}
		}
		
		// Taverna müziği çalıyor mu kontrolü
		private bool IsPlayingTavernaMusic()
		{
			// Gerçek uygulamada MusicManager'dan alınacak
			if (GetTree().Root.HasNode("GameManager/MusicManager"))
			{
				var musicManager = GetTree().Root.GetNode("GameManager/MusicManager");
				
				if (musicManager.HasMethod("GetCurrentMusicStyle"))
				{
					string currentStyle = (string)musicManager.Call("GetCurrentMusicStyle");
					return currentStyle == "Taverna";
				}
			}
			
			// Test için rastgele bir değer döndür
			return GD.Randf() < 0.3f; // %30 ihtimalle taverna müziği çalıyor
		}
		
		// Favori şarkı çalıyor mu kontrolü
		private bool IsPlayingFavoriteSong()
		{
			// Gerçek uygulamada MusicManager'dan alınacak
			if (GetTree().Root.HasNode("GameManager/MusicManager"))
			{
				var musicManager = GetTree().Root.GetNode("GameManager/MusicManager");
				
				if (musicManager.HasMethod("GetCurrentSong"))
				{
					string currentSong = (string)musicManager.Call("GetCurrentSong");
					return _favoriteSongs.Contains(currentSong);
				}
			}
			
			// Test için rastgele bir değer döndür
			return GD.Randf() < 0.1f; // %10 ihtimalle favori şarkı çalıyor
		}
		
		// Modern müzik çalıyor mu kontrolü
		private bool IsPlayingModernMusic()
		{
			// Gerçek uygulamada MusicManager'dan alınacak
			if (GetTree().Root.HasNode("GameManager/MusicManager"))
			{
				var musicManager = GetTree().Root.GetNode("GameManager/MusicManager");
				
				if (musicManager.HasMethod("GetCurrentMusicStyle"))
				{
					string currentStyle = (string)musicManager.Call("GetCurrentMusicStyle");
					return currentStyle == "ModernLounge" || currentStyle == "FanteziPop";
				}
			}
			
			// Test için rastgele bir değer döndür
			return GD.Randf() < 0.2f; // %20 ihtimalle modern müzik çalıyor
		}
		
		// Genç müşteriler mekanı domine ediyor mu kontrolü
		private bool AreYoungCustomersDominant()
		{
			// Gerçek uygulamada CustomerManager'dan alınacak
			if (GetTree().Root.HasNode("GameManager/CustomerManager"))
			{
				var customerManager = GetTree().Root.GetNode<CustomerManager>("GameManager/CustomerManager");
				
				if (customerManager.HasMethod("IsCustomerTypeDominant"))
				{
					return (bool)customerManager.Call("IsCustomerTypeDominant", "Young");
				}
			}
			
			// Test için rastgele bir değer döndür
			return GD.Randf() < 0.15f; // %15 ihtimalle genç müşteriler dominant
		}
		
		// Nostaljik bir hikaye anlat
		private void ShareNostalgicStory()
		{
			int index = (int)(GD.Randf() * _nostalgicPhrases.Count);
			Say(_nostalgicPhrases[index], 5.0f);
			_oldAnkaraStoriesCount++;
			
			// Hikaye anlatınca rakı içme ihtiyacı artar
			if (GD.Randf() < 0.7f)
			{
				ChangeState(CustomerState.OrderingDrink);
			}
		}
		
		// Rastgele nostaljik bir söz söyle
		private void SayRandomNostalgicPhrase()
		{
			int index = (int)(GD.Randf() * _nostalgicPhrases.Count);
			Say(_nostalgicPhrases[index], 3.0f);
		}
		
		// Favori şarkı çalınca tepki ver
		private void SayFavoriteSongReaction()
		{
			string[] reactions = new string[]
			{
				"İşte bu! Bu şarkı zamanında çok dinlenirdi!",
				"Ah, sonunda adam gibi bir şarkı! Gel rakıyı tazele!",
				"Bu şarkıyı rahmetli babamla dinlerdik...",
				"Bu şarkı benim gençliğimin en güzel hatırası!",
				"Bu şarkı çalınca hep o eski Ankara geliyor aklıma...",
				"İşte buyur, bu şarkı gerçek müzik!"
			};
			
			int index = (int)(GD.Randf() * reactions.Length);
			Say(reactions[index], 4.0f);
			
			// Mutluluğu artır
			AdjustMood(0.1f);
			
			// Coşarsa alkış yapabilir
			if (GD.Randf() < 0.5f)
			{
				PlayAnimation("clap");
			}
		}
		
		// Modern müzik çalınca eleştir
		private void CriticizeModernMusic()
		{
			string[] criticisms = new string[]
			{
				"Bu da müzik mi şimdi? Bizim zamanımızda müzik dinlerdin...",
				"Kapatın şu gürültüyü, kafa şişti!",
				"Buna müzik diyorlar... Nerede o eski besteler...",
				"Şu sese bak, kulaklarım ağrıdı. Eskiden ahenkli olurdu müzik...",
				"Bizim zamanımızda sanatçı sanatçıydı, değerini bilirdik..."
			};
			
			int index = (int)(GD.Randf() * criticisms.Length);
			Say(criticisms[index], 3.0f);
			
			// Memnuniyeti düşür
			AdjustMood(-0.05f);
		}
		
		// Gençleri eleştir
		private void CriticizeYouth()
		{
			string[] youthCriticisms = new string[]
			{
				"Şimdiki gençlere bak... Saygı kalmamış, adap kalmamış...",
				"Bizim zamanımızda büyüklerimize böyle davranmazdık!",
				"Hepsi telefona gömülmüş... Muhabbet, sohbet nerede?",
				"Bunlar rakı içmeyi de bilmez, muhabbeti de...",
				"Bu gençlik nereye gidiyor böyle? Hiç mi büyük görmediler?"
			};
			
			int index = (int)(GD.Randf() * youthCriticisms.Length);
			Say(youthCriticisms[index], 3.0f);
		}
		
		// İçki tercihi override
		protected override string GetPreferredDrinkType()
		{
			// Nostaljik müşteriler genelde rakı tercih eder
			if (GD.Randf() < _rakiPreference)
				return "Rakı";
				
			// Çok nadiren diğer içecekleri dener
			return base.GetPreferredDrinkType();
		}
		
		// Meze tercihi override
		protected override string GetPreferredMezeType()
		{
			// Geleneksel meze tercihi
			if (GD.Randf() < _traditionalFoodPreference)
			{
				string[] traditionalMezes = { "Patlıcan Salatası", "Cacık", "Humus", "Haydari", "Çiğ Köfte", "Pastırma" };
				int index = (int)(GD.Randf() * traditionalMezes.Length);
				return traditionalMezes[index];
			}
			
			return base.GetPreferredMezeType();
		}
		
		// İçki teslim alma override
		public override void ReceiveDrink(string drinkType, float quality)
		{
			// Eğer rakı geldiyse ve kalitesi iyiyse ekstra memnuniyet
			if (drinkType == "Rakı" && quality > 0.7f)
			{
				AdjustSatisfaction(0.1f, "Kaliteli rakı");
				Say("İşte budur! Gerçek rakı böyle olur!");
			}
			// Eğer rakı geldiyse ama kalitesi kötüyse ekstra şikayet
			else if (drinkType == "Rakı" && quality < 0.4f)
			{
				AdjustSatisfaction(-0.15f, "Kötü rakı");
				Say("Bu da ne böyle? Eskiden rakı dediğin böyle mi olurdu?");
			}
			
			base.ReceiveDrink(drinkType, quality);
			
			// İçtikçe nostalji artar
			if (drinkType == "Rakı")
			{
				_nostalgiaMeter = Mathf.Min(1.0f, _nostalgiaMeter + 0.05f);
				
				// Nostalji yüksekse ve yeni hikaye anlatmamışsa
				if (_nostalgiaMeter > 0.8f && GD.Randf() < 0.3f && !_hasSharedStories)
				{
					ShareNostalgicStory();
					_hasSharedStories = true;
				}
			}
		}
		
		// Durum değişikliğinde özel metodlar
		protected override void OnStateChanged(CustomerState previousState, CustomerState newState)
		{
			base.OnStateChanged(previousState, newState);
			
			// Nostaljik müşterilere özgü durum değişikliği davranışları
			if (newState == CustomerState.OrderingDrink)
			{
				// Rakı siparişi verilirken özel replikler
				if (GetPreferredDrinkType() == "Rakı")
				{
					string[] rakiOrders = new string[]
					{
						"Garson! Bir rakı, ama buzlu olmasın!",
						"Bir rakı getir evladım, yanına da biraz su...",
						"Rakı var mı oğlum? Tekirdağ olursa daha iyi...",
						"Bana bir rakı getir delikanlı, buz az olsun!",
						"Şöyle bir rakı açalım bakalım..."
					};
					
					int index = (int)(GD.Randf() * rakiOrders.Length);
					Say(rakiOrders[index]);
				}
			}
			else if (newState == CustomerState.TalkingToKons)
			{
				// Konslarla konuşurken genelde eski hikayeleri anlatır
				ShareNostalgicStory();
			}
			else if (newState == CustomerState.Leaving)
			{
				// Ayrılırken söylenebilecek nostaljik sözler
				string[] leavingPhrases = new string[]
				{
					"Güzel bir akşamdı, eski günlere dönmüş gibi oldum...",
					"Eskisi kadar değil ama idare eder... Hesabı alalım.",
					"Yine gelirim evladım, iyi iş çıkarıyorsunuz...",
					"Maltepe'nin eski günleri gibiydi, teşekkürler...",
					"Eski günler aklıma geldi, iyi oldu... Hesap lütfen."
				};
				
				int index = (int)(GD.Randf() * leavingPhrases.Length);
				Say(leavingPhrases[index]);
			}
		}
		
		// Bahşiş verme override - nostaljik müşteri genelde cömert
		protected override void GiveTip(float amount, string staffId)
		{
			// Nostaljik müşteri, kalıplaşmış yöntemlere sadık kaldığı için
			// ve "eskiden herkes cömert olurdu" mantığıyla daha fazla bahşiş verir
			float bonusAmount = amount * 0.2f; // %20 daha fazla bahşiş
			
			// Memnuniyet yüksekse ekstra artır
			if (_satisfaction > 0.8f)
				bonusAmount = amount * 0.3f; // %30 daha fazla bahşiş
			
			base.GiveTip(amount + bonusAmount, staffId);
			
			// Bahşiş verirken söylenecek sözler
			string[] tipPhrases = new string[]
			{
				"Al evladım, eskiden böyle olurdu işler...",
				"Hizmetin güzeldi, bu da benden...",
				"Gençliğimde bizler de pavyonda çalışırdık, al bakalım...",
				"Emeğinin karşılığı, helal et...",
				"Eskiden müşteri demek bereket demekti, al bakalım..."
			};
			
			int index = (int)(GD.Randf() * tipPhrases.Length);
			Say(tipPhrases[index]);
		}
		
		// Mekan kalitesi değerlendirmesi
		protected float EvaluateVenueQuality()
		{
			float score = 0.0f;
			
			// Dekorasyon değerlendirmesi - eski tarz dekorasyon tercih eder
			float decorScore = GetDecorationScore();
			score += decorScore * 0.3f;
			
			// Müzik değerlendirmesi - taverna müziği tercih eder
			float musicScore = IsPlayingTavernaMusic() ? 0.9f : (IsPlayingModernMusic() ? 0.2f : 0.5f);
			score += musicScore * 0.3f;
			
			// Servis kalitesi değerlendirmesi
			float serviceScore = GetServiceScore();
			score += serviceScore * 0.2f;
			
			// Rakı kalitesi değerlendirmesi
			float rakiScore = GetRakiQualityScore();
			score += rakiScore * 0.2f;
			
			return score;
		}
		
		// Dekorasyon skorunu hesapla
		private float GetDecorationScore()
		{
			// Gerçek uygulamada BuildingManager'dan alınacak
			if (GetTree().Root.HasNode("GameManager/BuildingManager"))
			{
				var buildingManager = GetTree().Root.GetNode("GameManager/BuildingManager");
				
				if (buildingManager.HasMethod("GetDecorationStyleScore"))
				{
					return (float)buildingManager.Call("GetDecorationStyleScore", "Traditional");
				}
			}
			
			// Test için rastgele bir değer döndür
			return 0.5f + GD.Randf() * 0.5f;
		}
		
		// Servis skorunu hesapla
		private float GetServiceScore()
		{
			// Gerçek uygulamada StaffManager'dan alınacak
			if (GetTree().Root.HasNode("GameManager/StaffManager"))
			{
				var staffManager = GetTree().Root.GetNode("GameManager/StaffManager");
				
				if (staffManager.HasMethod("GetAverageServiceScore"))
				{
					return (float)staffManager.Call("GetAverageServiceScore");
				}
			}
			
			// Test için rastgele bir değer döndür
			return 0.6f + GD.Randf() * 0.4f;
		}
		
		// Rakı kalitesi skorunu hesapla
		private float GetRakiQualityScore()
		{
			// Gerçek uygulamada InventoryManager'dan alınacak
			if (GetTree().Root.HasNode("GameManager/InventoryManager"))
			{
				var inventoryManager = GetTree().Root.GetNode("GameManager/InventoryManager");
				
				if (inventoryManager.HasMethod("GetDrinkQuality"))
				{
					return (float)inventoryManager.Call("GetDrinkQuality", "Rakı");
				}
			}
			
			// Test için rastgele bir değer döndür
			return 0.6f + GD.Randf() * 0.4f;
		}
		
		// Hesap ödeme override - rakı için ek yorum
		protected override void ProcessPayment()
		{
			base.ProcessPayment();
			
			// Rakı içtiyse hesap sırasında özel yorum
			if (_orderHistory.ContainsKey("Rakı") && _orderHistory["Rakı"] > 0)
			{
				string[] rakiComments = new string[]
				{
					"Rakı dediğin böyle olmalı işte, teşekkürler...",
					"Rakı güzeldi, eski günlerdeki gibi keyif aldık...",
					"Rakınız iyiymiş, tekrar geleceğim...",
					"Eskisi kadar değil ama rakınız fena değil...",
					"Rakıyı özlemişim, iyi geldi..."
				};
				
				int index = (int)(GD.Randf() * rakiComments.Length);
				Say(rakiComments[index]);
			}
		}
		
		// Ruh hali ayarlama - özelleştirilmiş
		public void AdjustMood(float amount)
		{
			// Nostaljik müşterilerin ruh halleri daha dalgalıdır
			float amplifiedAmount = amount;
			
			// Taverna müziği çalıyorsa olumlu etkiler güçlenir
			if (amount > 0 && IsPlayingTavernaMusic())
			{
				amplifiedAmount *= 1.5f;
			}
			
			// Modern müzik çalıyorsa olumsuz etkiler güçlenir
			if (amount < 0 && IsPlayingModernMusic())
			{
				amplifiedAmount *= 1.5f;
			}
			
			// Temel sınıftaki metodu çağır
			AdjustSatisfaction(amplifiedAmount, "Mood change");
		}
		
		// Sadakat değerlendirmesi
		public float EvaluateLoyalty()
		{
			// Nostaljik müşteriler, eski günleri andıran mekanlara daha sadıktırlar
			float venueQuality = EvaluateVenueQuality();
			float baseLoyalty = _loyaltyLevel;
			
			return Mathf.Clamp(baseLoyalty + (venueQuality - 0.5f) * 0.3f, 0.0f, 1.0f);
		}
		
		// Ayrılma sebeplerini değerlendir
		protected override void HandleOutOfMoney()
		{
			// Nostaljik müşteriler genelde daha saygılı ve kural takıntılı olur
			// Bu yüzden 'karga' yapma olasılıkları düşüktür
			if (GD.Randf() < _kargaLevel * 0.5f) // Normal karga seviyesinin yarısı
			{
				AttemptKarga();
			}
			else
			{
				PrepareToLeave();
				Say("Evladım, hesabı alalım. Para sıkıntısı var, kusura bakma...");
			}
		}
	}
}
