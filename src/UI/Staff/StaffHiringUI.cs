// src/UI/Staff/StaffHiringUI.cs
using Godot;
using System;
using System.Collections.Generic;
using PavyonTycoon.Staff;
using PavyonTycoon.Core;

namespace PavyonTycoon.UI.Staff
{
	public partial class StaffHiringUI : Control
	{
		// UI elemanları
		private ItemList candidateList;
		private Panel candidateDetailPanel;
		private VBoxContainer candidateInfoContainer;
		private Button hireButton;
		private Button refreshButton;
		private Button closeButton;
		private OptionButton jobTypeFilter;
		private Label hiringCostLabel;
		private Label availableFundsLabel;
		
		// Filtreleme için
		private Dictionary<string, Button> jobFilterButtons = new Dictionary<string, Button>();
		private string currentJobFilter = "Tümü";
		
		// Seçili aday
		private StaffMember selectedCandidate;
		
		// Referanslar
		private StaffManager staffManager;
		private EconomyManager economyManager;
		
		public override void _Ready()
		{
			// UI elemanlarını al
			candidateList = GetNode<ItemList>("VBoxContainer/HSplitContainer/CandidateList");
			candidateDetailPanel = GetNode<Panel>("VBoxContainer/HSplitContainer/DetailPanel");
			candidateInfoContainer = GetNode<VBoxContainer>("VBoxContainer/HSplitContainer/DetailPanel/ScrollContainer/InfoContainer");
			hireButton = GetNode<Button>("VBoxContainer/ButtonContainer/HireButton");
			refreshButton = GetNode<Button>("VBoxContainer/ButtonContainer/RefreshButton");
			closeButton = GetNode<Button>("VBoxContainer/ButtonContainer/CloseButton");
			jobTypeFilter = GetNode<OptionButton>("VBoxContainer/FilterContainer/JobTypeFilter");
			hiringCostLabel = GetNode<Label>("VBoxContainer/CostContainer/HiringCostLabel");
			availableFundsLabel = GetNode<Label>("VBoxContainer/CostContainer/AvailableFundsLabel");
			
			// Buton sinyallerini bağla
			hireButton.Pressed += OnHireButtonPressed;
			refreshButton.Pressed += OnRefreshButtonPressed;
			closeButton.Pressed += OnCloseButtonPressed;
			
			// Liste sinyallerini bağla
			candidateList.ItemSelected += OnCandidateSelected;
			
			// Filtre sinyallerini bağla
			jobTypeFilter.ItemSelected += OnFilterChanged;
			
			// Yöneticilere referans al
			var gameManager = GetNode<GameManager>("/root/Main/GameManager");
			if (gameManager != null)
			{
				staffManager = gameManager.GetNode<StaffManager>("StaffManager");
				economyManager = gameManager.GetNode<EconomyManager>("EconomyManager");
			}
			
			if (staffManager == null)
			{
				GD.PrintErr("StaffHiringUI: StaffManager bulunamadı!");
			}
			
			// İş pozisyonlarını filtre menüsüne ekle
			InitializeJobTypeFilter();
			
			// Maliyet bilgilerini göster
			UpdateCostInfo();
			
			// Adayları listele
			RefreshCandidateList();
			
			// Detay panelini başlangıçta gizle
			candidateDetailPanel.Visible = false;
			
			// İşe alma butonunu devre dışı bırak
			hireButton.Disabled = true;
			
			GD.Print("Personel işe alma arayüzü başlatıldı");
		}
		
		private void InitializeJobTypeFilter()
		{
			// Filtre seçeneklerini temizle
			jobTypeFilter.Clear();
			
			// "Tümü" seçeneği
			jobTypeFilter.AddItem("Tüm Pozisyonlar");
			
			// İş pozisyonlarını ekle
			if (staffManager != null)
			{
				var jobPositions = staffManager.GetJobPositions();
				
				foreach (var job in jobPositions.Values)
				{
					jobTypeFilter.AddItem(job.Title);
				}
			}
			
			// Varsayılan olarak "Tümü" seçili
			jobTypeFilter.Selected = 0;
		}
		
		private void UpdateCostInfo()
		{
			if (staffManager == null || economyManager == null) return;
			
			// İşe alma maliyetini göster
			hiringCostLabel.Text = $"İşe Alma Maliyeti: {staffManager.HiringCost:F0}₺";
			
			// Mevcut para durumunu göster
			availableFundsLabel.Text = $"Mevcut Para: {economyManager.Money:F0}₺";
			
			// Para yetmiyorsa renklendir
			if (economyManager.Money < staffManager.HiringCost)
			{
				availableFundsLabel.AddThemeColorOverride("font_color", Colors.Red);
			}
			else
			{
				availableFundsLabel.AddThemeColorOverride("font_color", Colors.Green);
			}
		}
		
		private void RefreshCandidateList()
		{
			if (staffManager == null) return;
			
			// Listeyi temizle
			candidateList.Clear();
			
			// Adayları al
			var availableCandidates = staffManager.GetAvailableStaffForHire();
			
			// Filtrele
			List<StaffMember> filteredCandidates = FilterCandidates(availableCandidates);
			
			// Listeye ekle
			foreach (var candidate in filteredCandidates)
			{
				// Temel bilgiler
				string displayText = $"{candidate.FullName} ({candidate.JobTitle})";
				
				// İkonu belirle
				Texture2D icon = GetStaffTypeIcon(candidate);
				
				// Listeye ekle
				int index = candidateList.AddItem(displayText, icon);
				
				// Metadata olarak personeli sakla
				candidateList.SetItemMetadata(index, candidate);
				
				// Ek bilgiler ve biçimlendirme
				candidateList.SetItemTooltip(index, $"Maaş: {candidate.Salary:F0}₺");
				
				// Kalite durumuna göre renklendirme
				float qualityScore = CalculateCandidateQuality(candidate);
				if (qualityScore > 8f)
				{
					candidateList.SetItemCustomFgColor(index, Colors.LightGreen);
				}
				else if (qualityScore > 6f)
				{
					candidateList.SetItemCustomFgColor(index, Colors.Green);
				}
			}
		}
		
		// Adayları filtrele
		private List<StaffMember> FilterCandidates(List<StaffMember> candidates)
		{
			List<StaffMember> filtered = new List<StaffMember>();
			
			// İş pozisyonu filtresi
			string selectedJobType = jobTypeFilter.GetItemText(jobTypeFilter.Selected);
			bool filterByJobType = selectedJobType != "Tüm Pozisyonlar";
			
			foreach (var candidate in candidates)
			{
				// İş pozisyonu filtresi
				if (filterByJobType && candidate.JobTitle != selectedJobType)
					continue;
				
				// Diğer filtreler buraya eklenebilir
				
				// Filtreleri geçen adayı listeye ekle
				filtered.Add(candidate);
			}
			
			return filtered;
		}
		
		// Aday kalitesini hesapla (0-10 arası)
		private float CalculateCandidateQuality(StaffMember candidate)
		{
			if (candidate == null) return 0f;
			
			float totalScore = 0f;
			int attributeCount = 0;
			
			// Tüm özelliklerin ortalamasını al
			var attributes = candidate.GetAllAttributes();
			foreach (var attr in attributes)
			{
				totalScore += attr.Value;
				attributeCount++;
			}
			
			// Sadakat de bir faktör
			totalScore += candidate.Loyalty / 10f;
			attributeCount++;
			
			// Seviye baz alınarak bonus
			totalScore += candidate.Level * 0.5f;
			
			// Ortalama hesapla
			return attributeCount > 0 ? totalScore / (attributeCount + 0.5f) : 0f;
		}
		
		// Personel türüne göre ikon 
		private Texture2D GetStaffTypeIcon(StaffMember staff)
		{
			string iconPath = "res://assets/icons/staff/";
			
			if (staff is Kons)
				iconPath += "kons_icon.png";
			else if (staff is SecurityStaff)
				iconPath += "security_icon.png";
			else if (staff is Waiter)
				iconPath += "waiter_icon.png";
			else if (staff is Musician)
				iconPath += "musician_icon.png";
			else if (staff is Cook)
				iconPath += "cook_icon.png";
			else if (staff is IllegalFloorStaff)
				iconPath += "illegal_staff_icon.png";
			else
				iconPath += "generic_staff_icon.png";
			
			// İkon dosyası varsa yükle, yoksa null döndür
			if (ResourceLoader.Exists(iconPath))
				return ResourceLoader.Load<Texture2D>(iconPath);
			else
				return null;
		}
		
		// Aday detaylarını göster
		private void ShowCandidateDetails(StaffMember candidate)
		{
			if (candidate == null)
			{
				candidateDetailPanel.Visible = false;
				hireButton.Disabled = true;
				return;
			}
			
			// Referansı sakla
			selectedCandidate = candidate;
			
			// Bilgileri temizle
			foreach (Node child in candidateInfoContainer.GetChildren())
			{
				child.QueueFree();
			}
			
			// Temel bilgileri ekle
			AddCandidateHeader(candidate);
			AddCandidateJobInfo(candidate);
			AddCandidateAttributes(candidate);
			AddCandidateSpecialInfo(candidate);
			
			// İşe alma butonunu etkinleştir
			bool canAfford = economyManager != null && economyManager.Money >= staffManager.HiringCost;
			hireButton.Disabled = !canAfford;
			
			if (!canAfford)
			{
				hireButton.TooltipText = "Yeterli paranız yok!";
			}
			else
			{
				hireButton.TooltipText = "";
			}
			
			// Paneli göster
			candidateDetailPanel.Visible = true;
		}
		
		// Aday bilgi başlığı ekle
		private void AddCandidateHeader(StaffMember candidate)
		{
			var nameLabel = new Label();
			nameLabel.Text = candidate.FullName;
			nameLabel.AddThemeColorOverride("font_color", Colors.White);
			nameLabel.AddThemeConstantOverride("font_size", 18);
			nameLabel.AddThemeFontSizeOverride("font_size", 18);
			candidateInfoContainer.AddChild(nameLabel);
			
			var titleLabel = new Label();
			titleLabel.Text = candidate.JobTitle;
			titleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 1.0f));
			candidateInfoContainer.AddChild(titleLabel);
			
			// Ayırıcı
			var separator = new HSeparator();
			candidateInfoContainer.AddChild(separator);
		}
		
		// İş bilgilerini ekle
		private void AddCandidateJobInfo(StaffMember candidate)
		{
			// Ana bilgi grid'i
			var infoGrid = new GridContainer();
			infoGrid.Columns = 2;
			candidateInfoContainer.AddChild(infoGrid);
			
			// Maaş beklentisi
			float monthlySalary = candidate.Salary * 30; // Günlük x 30
			Color salaryColor = GetSalaryColor(candidate.Salary);
			AddInfoRow(infoGrid, "Günlük Maaş:", $"{candidate.Salary:F0}₺", salaryColor);
			AddInfoRow(infoGrid, "Aylık Maaş:", $"{monthlySalary:F0}₺", salaryColor);
			
			// Personel seviyesi
			AddInfoRow(infoGrid, "Seviye:", $"{candidate.Level}");
			
			// Kalite puanı
			float quality = CalculateCandidateQuality(candidate);
			Color qualityColor = GetQualityColor(quality);
			AddInfoRow(infoGrid, "Kalite Puanı:", $"{quality:F1}/10", qualityColor);
			
			// Boşluk
			candidateInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		// Özellikleri ekle
		private void AddCandidateAttributes(StaffMember candidate)
		{
			// Başlık
			var attrTitle = new Label();
			attrTitle.Text = "Özellikler";
			attrTitle.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			candidateInfoContainer.AddChild(attrTitle);
			
			// Özellik grid'i
			var attrGrid = new GridContainer();
			attrGrid.Columns = 2;
			candidateInfoContainer.AddChild(attrGrid);
			
			// Tüm özellikleri ekle
			var attributes = candidate.GetAllAttributes();
			foreach (var attr in attributes)
			{
				// Özellik rengi (değere göre)
				Color attrColor = GetAttributeColor(attr.Value);
				
				AddInfoRow(attrGrid, $"{attr.Key}:", $"{attr.Value:F1}/10", attrColor);
			}
			
			// Boşluk
			candidateInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		// Personel türüne göre özel bilgiler ekle
		private void AddCandidateSpecialInfo(StaffMember candidate)
		{
			// Her personel türü için özel bilgiler
			if (candidate is Kons kons)
			{
				AddKonsSpecialInfo(kons);
			}
			else if (candidate is SecurityStaff security)
			{
				AddSecuritySpecialInfo(security);
			}
			else if (candidate is Waiter waiter)
			{
				AddWaiterSpecialInfo(waiter);
			}
			else if (candidate is Musician musician)
			{
				AddMusicianSpecialInfo(musician);
			}
			else if (candidate is Cook cook)
			{
				AddCookSpecialInfo(cook);
			}
			else if (candidate is IllegalFloorStaff illegalStaff)
			{
				AddIllegalStaffSpecialInfo(illegalStaff);
			}
			
			// Tavsiye/Uyarı
			AddHiringRecommendation(candidate);
		}
		
		// Konslar için özel bilgiler
		private void AddKonsSpecialInfo(Kons kons)
		{
			// Kons başlığı
			var title = new Label();
			title.Text = "Kons Özellikleri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			candidateInfoContainer.AddChild(title);
			
			// Özel bilgi grid'i
			var specialGrid = new GridContainer();
			specialGrid.Columns = 2;
			candidateInfoContainer.AddChild(specialGrid);
			
			// Kons değerleri
			AddInfoRow(specialGrid, "İçki Satış Çarpanı:", $"+{(kons.DrinkSalesMultiplier - 1f) * 100:F0}%");
			AddInfoRow(specialGrid, "Bahşiş Payı:", $"{(1f - kons.TipPercentage) * 100:F0}%");
			AddInfoRow(specialGrid, "Müdavim Kapasitesi:", $"{kons.MaxRegularCustomers}");
			
			// Boşluk
			candidateInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		// Güvenlik personeli için özel bilgiler
		private void AddSecuritySpecialInfo(SecurityStaff security)
		{
			// Güvenlik başlığı
			var title = new Label();
			title.Text = "Güvenlik Özellikleri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			candidateInfoContainer.AddChild(title);
			
			// Özel bilgi grid'i
			var specialGrid = new GridContainer();
			specialGrid.Columns = 2;
			candidateInfoContainer.AddChild(specialGrid);
			
			// Güvenlik değerleri
			AddInfoRow(specialGrid, "Tehdit Seviyesi:", $"{security.ThreatLevel:F1}/5");
			AddInfoRow(specialGrid, "Tespit Şansı:", $"%{security.DetectionChance * 100:F0}");
			AddInfoRow(specialGrid, "Dövüş Yeteneği:", $"{security.FightingAbility:F1}/5");
			
			// Ekipman durumu
			string equipment = "";
			if (security.HasRadio) equipment += "Telsiz ✓ ";
			if (security.HasTaser) equipment += "Şok Cihazı ✓ ";
			if (security.HasBodyArmor) equipment += "Koruyucu Yelek ✓ ";
			
			if (!string.IsNullOrEmpty(equipment))
			{
				AddInfoRow(specialGrid, "Ekipman:", equipment);
			}
			else
			{
				AddInfoRow(specialGrid, "Ekipman:", "Yok");
			}
			
			// Boşluk
			candidateInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		// Garsonlar için özel bilgiler
		private void AddWaiterSpecialInfo(Waiter waiter)
		{
			// Garson başlığı
			var title = new Label();
			title.Text = "Garson Özellikleri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			candidateInfoContainer.AddChild(title);
			
			// Özel bilgi grid'i
			var specialGrid = new GridContainer();
			specialGrid.Columns = 2;
			candidateInfoContainer.AddChild(specialGrid);
			
			// Garson değerleri
			AddInfoRow(specialGrid, "Servis Hızı:", $"{waiter.ServiceSpeed:F1}x");
			AddInfoRow(specialGrid, "Bahşiş Oranı:", $"%{waiter.TipRate * 100:F0}");
			AddInfoRow(specialGrid, "Dökme Olasılığı:", $"%{waiter.SpillChance * 100:F0}");
			AddInfoRow(specialGrid, "Masa Kapasitesi:", $"{waiter.MaxTables:F1}");
			
			// Özel yetenekler
			string abilities = "";
			if (waiter.CanMixDrinks) abilities += "Kokteyl Hazırlama ✓ ";
			if (waiter.HasTablet) abilities += "Tablet Kullanımı ✓ ";
			
			if (!string.IsNullOrEmpty(abilities))
			{
				AddInfoRow(specialGrid, "Yetenekler:", abilities);
			}
			
			// Boşluk
			candidateInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		// Müzisyenler için özel bilgiler
		private void AddMusicianSpecialInfo(Musician musician)
		{
			// Müzisyen başlığı
			var title = new Label();
			title.Text = "Müzisyen Özellikleri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			candidateInfoContainer.AddChild(title);
			
			// Özel bilgi grid'i
			var specialGrid = new GridContainer();
			specialGrid.Columns = 2;
			candidateInfoContainer.AddChild(specialGrid);
			
			// Müzisyen değerleri
			AddInfoRow(specialGrid, "Enstrüman:", musician.GetInstrumentName());
			AddInfoRow(specialGrid, "Müzik Türü:", musician.GetGenreName());
			AddInfoRow(specialGrid, "Performans Kalitesi:", $"{musician.PerformanceQuality:F1}/5");
			AddInfoRow(specialGrid, "Kalabalık Etkisi:", $"{musician.CrowdExcitementFactor:F1}x");
			AddInfoRow(specialGrid, "Dayanıklılık:", $"{musician.StaminaLevel:F0}/100");
			AddInfoRow(specialGrid, "Repertuar:", $"{musician.RepertoireSize} parça");
			
			// Özel ekipman
			string equipment = "";
			if (musician.HasOwnInstrument) equipment += "Özel Enstrüman ✓ ";
			if (musician.HasWirelessMic) equipment += "Kablosuz Mikrofon ✓ ";
			if (musician.HasCustomOutfit) equipment += "Sahne Kıyafeti ✓ ";
			
			if (!string.IsNullOrEmpty(equipment))
			{
				AddInfoRow(specialGrid, "Ekipman:", equipment);
			}
			else
			{
				AddInfoRow(specialGrid, "Ekipman:", "Standart");
			}
			
			// Boşluk
			candidateInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		// Aşçılar için özel bilgiler
		private void AddCookSpecialInfo(Cook cook)
		{
			// Aşçı başlığı
			var title = new Label();
			title.Text = "Aşçı Özellikleri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			candidateInfoContainer.AddChild(title);
			
			// Özel bilgi grid'i
			var specialGrid = new GridContainer();
			specialGrid.Columns = 2;
			candidateInfoContainer.AddChild(specialGrid);
			
			// Aşçı değerleri
			AddInfoRow(specialGrid, "Uzmanlık:", cook.GetSpecialtyName().Replace("_", " "));
			AddInfoRow(specialGrid, "Yemek Kalitesi:", $"{cook.CookingQuality:F1}/5");
			AddInfoRow(specialGrid, "Verimlilik:", $"{cook.EfficiencyRate:F1}x");
			AddInfoRow(specialGrid, "Malzeme Tasarrufu:", $"%{cook.WasteReduction * 100:F0}");
			AddInfoRow(specialGrid, "Yaratıcılık:", $"%{cook.CreativityLevel * 100:F0}");
			AddInfoRow(specialGrid, "Tarif Sayısı:", $"{cook.RecipeCount}");
			
			// Özel ekipman
			string equipment = "";
			if (cook.HasChefKnives) equipment += "Şef Bıçakları ✓ ";
			if (cook.HasSpecialSpices) equipment += "Özel Baharatlar ✓ ";
			if (cook.HasModernEquipment) equipment += "Modern Ekipman ✓ ";
			
			if (!string.IsNullOrEmpty(equipment))
			{
				AddInfoRow(specialGrid, "Ekipman:", equipment);
			}
			else
			{
				AddInfoRow(specialGrid, "Ekipman:", "Standart");
			}
			
			// Boşluk
			candidateInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		// Kaçak kat personeli için özel bilgiler
		private void AddIllegalStaffSpecialInfo(IllegalFloorStaff illegalStaff)
		{
			// Kaçak kat personeli başlığı
			var title = new Label();
			title.Text = "Kaçak Kat Personeli Özellikleri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			candidateInfoContainer.AddChild(title);
			
			// Özel bilgi grid'i
			var specialGrid = new GridContainer();
			specialGrid.Columns = 2;
			candidateInfoContainer.AddChild(specialGrid);
			
			// Kaçak kat personeli değerleri
			AddInfoRow(specialGrid, "Ana Faaliyet:", illegalStaff.GetActivityName(illegalStaff.PrimaryActivity));
			AddInfoRow(specialGrid, "Alt Faaliyet:", illegalStaff.GetActivityName(illegalStaff.SecondaryActivity));
			AddInfoRow(specialGrid, "Gizlilik:", $"{illegalStaff.DiscretionLevel:F1}/5");
			AddInfoRow(specialGrid, "Kâr Marjı:", $"{illegalStaff.ProfitMargin:F1}x");
			AddInfoRow(specialGrid, "Risk Azaltma:", $"%{illegalStaff.RiskReduction * 100:F0}");
			AddInfoRow(specialGrid, "Bağlantılar:", $"%{illegalStaff.ConnectionsLevel * 100:F0}");
			AddInfoRow(specialGrid, "Tespit Riski:", $"%{illegalStaff.DetectionRisk * 100:F0}");
			
			// Özel ekipman
			string equipment = "";
			if (illegalStaff.HasFakeID) equipment += "Sahte Kimlik ✓ ";
			if (illegalStaff.HasCustomSecuritySystem) equipment += "Özel Güvenlik Sistemi ✓ ";
			if (illegalStaff.HasVIPContactList) equipment += "VIP Bağlantı Listesi ✓ ";
			
			if (!string.IsNullOrEmpty(equipment))
			{
				AddInfoRow(specialGrid, "Ekipman:", equipment);
			}
			else
			{
				AddInfoRow(specialGrid, "Ekipman:", "Yok");
			}
			
			// Boşluk
			candidateInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		// İşe alma tavsiyesi ekle
		private void AddHiringRecommendation(StaffMember candidate)
		{
			// Tavsiye paneli
			PanelContainer recommendationPanel = new PanelContainer();
			recommendationPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
			{
				BgColor = new Color(0.2f, 0.2f, 0.3f, 0.5f),
				CornerRadius = new int[] { 4, 4, 4, 4 }
			});
			candidateInfoContainer.AddChild(recommendationPanel);
			
			// İçerik
			VBoxContainer content = new VBoxContainer();
			recommendationPanel.AddChild(content);
			
			// Tavsiye başlığı
			Label recTitle = new Label();
			recTitle.Text = "İşe Alma Değerlendirmesi";
			recTitle.AddThemeColorOverride("font_color", Colors.White);
			content.AddChild(recTitle);
			
			// Ayırıcı
			HSeparator separator = new HSeparator();
			content.AddChild(separator);
			
			// Değerlendirme metni
			Label recText = new Label();
			recText.Text = GenerateHiringRecommendation(candidate);
			recText.AutowrapMode = TextServer.AutowrapMode.Word;
			content.AddChild(recText);
			
			// Tavsiye seviyesi
			Label recLevel = new Label();
			float quality = CalculateCandidateQuality(candidate);
			recLevel.Text = GetRecommendationLevel(quality);
			recLevel.AddThemeColorOverride("font_color", GetQualityColor(quality));
			recLevel.HorizontalAlignment = HorizontalAlignment.Center;
			content.AddChild(recLevel);
		}
		
		// İşe alma tavsiyesi metni oluştur
		private string GenerateHiringRecommendation(StaffMember candidate)
		{
			if (candidate == null) return "";
			
			float quality = CalculateCandidateQuality(candidate);
			
			string recommendation = "";
			
			// Kalite puanına göre genel değerlendirme
			if (quality < 3f)
			{
				recommendation += "Bu aday çok zayıf performans gösterebilir. ";
			}
			else if (quality < 5f)
			{
				recommendation += "Bu aday vasat bir çalışan olabilir. ";
			}
			else if (quality < 7f)
			{
				recommendation += "Bu aday iyi bir seçim olabilir. ";
			}
			else if (quality < 9f)
			{
				recommendation += "Bu aday mükemmel bir çalışan adayı! ";
			}
			else
			{
				recommendation += "Bu aday istisnai bir yetenek, kaçırmayın! ";
			}
			
			// Maaş değerlendirmesi
			JobPosition jobPosition = null;
			if (staffManager?.GetJobPositions().TryGetValue(candidate.JobTitle, out jobPosition) == true)
			{
				float baseSalary = jobPosition.BaseSalary;
				float salaryCmp = candidate.Salary / baseSalary;
				
				if (salaryCmp < 0.9f)
				{
					recommendation += "Maaş beklentisi ortalamanın oldukça altında. ";
				}
				else if (salaryCmp < 1.0f)
				{
					recommendation += "Maaş beklentisi ortalamanın biraz altında. ";
				}
				else if (salaryCmp > 1.2f)
				{
					recommendation += "Maaş beklentisi ortalamanın oldukça üzerinde. ";
				}
				else if (salaryCmp > 1.1f)
				{
					recommendation += "Maaş beklentisi ortalamanın biraz üzerinde. ";
				}
			}
			
			// Personel türüne özgü öneriler
			if (candidate is Kons)
			{
				recommendation += "Müşteri memnuniyeti ve içki satışlarını artırmak için iyi bir seçim olabilir.";
			}
			else if (candidate is SecurityStaff)
			{
				recommendation += "Mekanın güvenliğini sağlamak ve sorunları çözmek için önemli bir pozisyon.";
			}
			else if (candidate is Waiter)
			{
				recommendation += "Hızlı servis ve müşteri memnuniyeti için önemli bir çalışan.";
			}
			else if (candidate is Musician)
			{
				recommendation += "Mekanın atmosferini iyileştirmek ve müşterilerin kalış süresini artırmak için etkili olabilir.";
			}
			else if (candidate is Cook)
			{
				recommendation += "Yemek kalitesi ve çeşitliliğini artırarak müşteri memnuniyetine katkıda bulunabilir.";
			}
			else if (candidate is IllegalFloorStaff)
			{
				recommendation += "Kaçak kat faaliyetlerini yönetmek ve ekstra gelir kaynakları sağlamak için gerekli bir pozisyon. Riskleri göz önünde bulundurun.";
			}
			
			return recommendation;
		}
		
		// İşe alma tavsiye seviyesi
		private string GetRecommendationLevel(float quality)
		{
			if (quality < 3f)
				return "Tavsiye Edilmez";
			else if (quality < 5f)
				return "Vasat";
			else if (quality < 7f)
				return "Tavsiye Edilir";
			else if (quality < 9f)
				return "Kesinlikle Tavsiye Edilir";
			else
				return "Olağanüstü Aday";
		}
		
		// Bilgi satırı ekleyen yardımcı metod
		private void AddInfoRow(GridContainer grid, string label, string value, Color? valueColor = null)
		{
			var labelNode = new Label();
			labelNode.Text = label;
			labelNode.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
			grid.AddChild(labelNode);
			
			var valueNode = new Label();
			valueNode.Text = value;
			if (valueColor.HasValue)
			{
				valueNode.AddThemeColorOverride("font_color", valueColor.Value);
			}
			grid.AddChild(valueNode);
		}
		
		// Maaş değerine göre renk
		private Color GetSalaryColor(float salary)
		{
			// İş pozisyonunun baz maaşını bul
			float baseSalary = 500f; // Varsayılan
			JobPosition jobPosition = null;
			
			if (selectedCandidate != null && 
				staffManager?.GetJobPositions().TryGetValue(selectedCandidate.JobTitle, out jobPosition) == true)
			{
				baseSalary = jobPosition.BaseSalary;
			}
			
			// Baz maaşa göre değerlendir
			float ratio = salary / baseSalary;
			
			if (ratio < 0.9f)
				return Colors.Green; // Ucuz
			else if (ratio < 1.0f)
				return Colors.LightGreen; // Biraz ucuz
			else if (ratio > 1.2f)
				return Colors.Red; // Pahalı
			else if (ratio > 1.1f)
				return Colors.Orange; // Biraz pahalı
			else
				return Colors.White; // Normal
		}
		
		// Özellik değerine göre renk
		private Color GetAttributeColor(float value)
		{
			if (value < 3f)
				return Colors.DarkRed;
			else if (value < 5f)
				return Colors.White;
			else if (value < 7f)
				return Colors.Yellow;
			else if (value < 9f)
				return Colors.Green;
			else
				return Colors.LightGreen;
		}
		
		// Kalite puanına göre renk
		private Color GetQualityColor(float quality)
		{
			if (quality < 3f)
				return Colors.DarkRed;
			else if (quality < 5f)
				return Colors.Orange;
			else if (quality < 7f)
				return Colors.Yellow;
			else if (quality < 9f)
				return Colors.Green;
			else
				return Colors.LightGreen;
		}
		
		// Buton olay işleyicileri
		
		private void OnHireButtonPressed()
		{
			if (selectedCandidate == null || staffManager == null) return;
			
			// Para kontrolü
			if (economyManager != null && economyManager.Money < staffManager.HiringCost)
			{
				// Para yetersiz
				var dialog = new AcceptDialog();
				dialog.Title = "Yetersiz Bakiye";
				dialog.DialogText = $"İşe alım maliyeti ({staffManager.HiringCost:F0}₺) için yeterli paranız yok!";
				AddChild(dialog);
				dialog.PopupCentered();
				return;
			}
			
			// Personeli işe al
			var hiredStaff = staffManager.HireStaff(selectedCandidate);
			
			if (hiredStaff != null)
			{
				GD.Print($"{hiredStaff.FullName} işe alındı!");
				
				// Adayı seçimden kaldır
				selectedCandidate = null;
				candidateDetailPanel.Visible = false;
				
				// İşe alma maliyeti bilgilerini güncelle
				UpdateCostInfo();
				
				// Mevcut listeyi güncelle
				RefreshCandidateList();
				
				// Başarı mesajı göster
				var dialog = new AcceptDialog();
				dialog.Title = "Personel İşe Alındı";
				dialog.DialogText = $"{hiredStaff.FullName} başarıyla işe alındı!";
				AddChild(dialog);
				dialog.PopupCentered();
			}
			else
			{
				GD.Print("Personel işe alınamadı!");
				
				// Hata mesajı göster
				var dialog = new AcceptDialog();
				dialog.Title = "İşe Alım Başarısız";
				dialog.DialogText = "Personel işe alınamadı! Bu pozisyon için maksimum personel sayısına ulaşmış olabilirsiniz.";
				AddChild(dialog);
				dialog.PopupCentered();
			}
		}
		
		private void OnRefreshButtonPressed()
		{
			if (staffManager == null) return;
			
			// Yeni aday listesi iste
			staffManager.RefreshAvailableStaffForHire();
			
			// Listeyi güncelle
			RefreshCandidateList();
			
			// Seçimi sıfırla
			selectedCandidate = null;
			candidateDetailPanel.Visible = false;
			hireButton.Disabled = true;
			
			// Maliyet bilgilerini güncelle
			UpdateCostInfo();
		}
		
		private void OnCloseButtonPressed()
		{
			// Panel'i kapat
			Visible = false;
			
			// Seçimi temizle
			selectedCandidate = null;
			candidateDetailPanel.Visible = false;
		}
		
		// Liste olayları
		
		private void OnCandidateSelected(long index)
		{
			// Aday detaylarını göster
			var candidate = (StaffMember)candidateList.GetItemMetadata((int)index);
			ShowCandidateDetails(candidate);
		}
		
		// Filtre olayları
		
		private void OnFilterChanged(long index)
		{
			RefreshCandidateList();
		}
	}
}
