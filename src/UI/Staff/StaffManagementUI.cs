// src/UI/Staff/StaffManagementUI.cs
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PavyonTycoon.Staff;
using PavyonTycoon.Core;

namespace PavyonTycoon.UI.Staff
{
	public partial class StaffManagementUI : Control
	{
		// UI Referansları
		private TabContainer tabContainer;
		private ItemList staffList;
		private Panel staffDetailPanel;
		private VBoxContainer staffInfoContainer;
		private Button trainButton;
		private Button promoteButton;
		private Button fireButton;
		private Button assignTaskButton;
		private Button viewTasksButton;
		private Button hireNewButton;
		private Button closeButton;
		
		// Filtre UI'ları
		private OptionButton jobTypeFilter;
		private OptionButton sortByOption;
		private CheckBox showInactiveCheckbox;
		
		// Pop-up paneller
		private Panel hirePanel;
		private Panel trainingPanel;
		private Panel taskAssignmentPanel;
		
		// Geçerli seçili personel
		private StaffMember selectedStaff;
		
		// Yöneticiler
		private StaffManager staffManager;
		private GameManager gameManager;
		
		// UI Renkleri ve Stiller
		private static readonly Color LowLoyaltyColor = new Color(0.9f, 0.3f, 0.3f);
		private static readonly Color MediumLoyaltyColor = new Color(0.9f, 0.6f, 0.1f);
		private static readonly Color HighLoyaltyColor = new Color(0.3f, 0.8f, 0.3f);
		
		// Animasyon için tween nesnesi
		private Tween currentTween;
		
		public override void _Ready()
		{
			// UI Referanslarını al
			GetUIReferences();
			
			// Panelleri başlangıçta gizle
			HideAllPanels();
			
			// Buton sinyallerini bağla
			ConnectButtonSignals();
			
			// Liste ve filtre sinyallerini bağla
			ConnectListAndFilterSignals();
			
			// Yöneticilere referans al
			GetManagers();
			
			// StaffManager varsa başlangıç işlemlerini yap
			if (staffManager != null)
			{
				InitializeStaffManager();
			}
			else
			{
				GD.PrintErr("StaffManagementUI: StaffManager bulunamadı!");
			}
			
			GD.Print("Personel yönetim arayüzü başlatıldı");
		}
		
		public override void _ExitTree()
		{
			// Mevcut tween'i temizle
			if (currentTween != null && currentTween.IsValid())
			{
				currentTween.Kill();
			}
			
			// StaffManager'dan sinyalleri ayır
			if (staffManager != null)
			{
				staffManager.Disconnect(StaffManager.SignalName.StaffHired, Callable.From(OnStaffHired));
				staffManager.Disconnect(StaffManager.SignalName.StaffFired, Callable.From(OnStaffFired));
				staffManager.Disconnect(StaffManager.SignalName.StaffAttributeChanged, Callable.From(OnStaffAttributeChanged));
				staffManager.Disconnect(StaffManager.SignalName.StaffLoyaltyChanged, Callable.From(OnStaffLoyaltyChanged));
			}
		}
		
		/// <summary>
		/// UI bileşenlerinin referanslarını alır
		/// </summary>
		private void GetUIReferences()
		{
			tabContainer = GetNode<TabContainer>("TabContainer");
			staffList = GetNode<ItemList>("TabContainer/StaffListTab/VBoxContainer/StaffList");
			staffDetailPanel = GetNode<Panel>("TabContainer/StaffListTab/StaffDetailPanel");
			staffInfoContainer = GetNode<VBoxContainer>("TabContainer/StaffListTab/StaffDetailPanel/ScrollContainer/StaffInfo");
			
			trainButton = GetNode<Button>("TabContainer/StaffListTab/StaffDetailPanel/ButtonContainer/TrainButton");
			promoteButton = GetNode<Button>("TabContainer/StaffListTab/StaffDetailPanel/ButtonContainer/PromoteButton");
			fireButton = GetNode<Button>("TabContainer/StaffListTab/StaffDetailPanel/ButtonContainer/FireButton");
			assignTaskButton = GetNode<Button>("TabContainer/StaffListTab/StaffDetailPanel/ButtonContainer/AssignTaskButton");
			viewTasksButton = GetNode<Button>("TabContainer/StaffListTab/StaffDetailPanel/ButtonContainer/ViewTasksButton");
			hireNewButton = GetNode<Button>("TabContainer/StaffListTab/VBoxContainer/FilterContainer/HireNewButton");
			closeButton = GetNode<Button>("TitleBar/CloseButton");
			
			jobTypeFilter = GetNode<OptionButton>("TabContainer/StaffListTab/VBoxContainer/FilterContainer/JobTypeFilter");
			sortByOption = GetNode<OptionButton>("TabContainer/StaffListTab/VBoxContainer/FilterContainer/SortByOption");
			showInactiveCheckbox = GetNode<CheckBox>("TabContainer/StaffListTab/VBoxContainer/FilterContainer/ShowInactiveCheckbox");
			
			hirePanel = GetNode<Panel>("HirePanel");
			trainingPanel = GetNode<Panel>("TrainingPanel");
			taskAssignmentPanel = GetNode<Panel>("TaskAssignmentPanel");
		}
		
		/// <summary>
		/// Tüm panelleri gizler inş
		/// </summary>
		private void HideAllPanels()
		{
			staffDetailPanel.Visible = false;
			hirePanel.Visible = false;
			trainingPanel.Visible = false;
			taskAssignmentPanel.Visible = false;
		}
		
		/// <summary>
		/// Buton sinyallerini bağlar
		/// </summary>
		private void ConnectButtonSignals()
		{
			trainButton.Pressed += OnTrainButtonPressed;
			promoteButton.Pressed += OnPromoteButtonPressed;
			fireButton.Pressed += OnFireButtonPressed;
			assignTaskButton.Pressed += OnAssignTaskButtonPressed;
			viewTasksButton.Pressed += OnViewTasksButtonPressed;
			hireNewButton.Pressed += OnHireNewButtonPressed;
			closeButton.Pressed += OnCloseButtonPressed;
		}
		
		/// <summary>
		/// Liste ve filtre sinyallerini bağlar
		/// </summary>
		private void ConnectListAndFilterSignals()
		{
			staffList.ItemSelected += OnStaffSelected;
			
			jobTypeFilter.ItemSelected += OnFilterChanged;
			sortByOption.ItemSelected += OnFilterChanged;
			showInactiveCheckbox.Toggled += OnShowInactiveToggled;
		}
		
		/// <summary>
		/// Gerekli yöneticilerin referanslarını alır
		/// </summary>
		private void GetManagers()
		{
			// Ana node path'iniz farklıysa bu kısmı güncelleyin
			staffManager = GetNode<StaffManager>("/root/Main/GameManager/StaffManager");
			gameManager = GetNode<GameManager>("/root/Main/GameManager");
		}
		
		/// <summary>
		/// StaffManager ile ilgili başlangıç işlemlerini yapar
		/// </summary>
		private void InitializeStaffManager()
		{
			// StaffManager olaylarını dinle
			staffManager.Connect(StaffManager.SignalName.StaffHired, Callable.From(OnStaffHired));
			staffManager.Connect(StaffManager.SignalName.StaffFired, Callable.From(OnStaffFired));
			staffManager.Connect(StaffManager.SignalName.StaffAttributeChanged, Callable.From(OnStaffAttributeChanged));
			staffManager.Connect(StaffManager.SignalName.StaffLoyaltyChanged, Callable.From(OnStaffLoyaltyChanged));
			
			// İş pozisyonlarını filtre menüsüne ekle
			InitializeJobTypeFilter();
			
			// Sıralama seçeneklerini başlat
			InitializeSortOptions();
			
			// Tüm personeli listele
			RefreshStaffList();
		}
		
		/// <summary>
		/// İş tipi filtresini başlatır
		/// </summary>
		private void InitializeJobTypeFilter()
		{
			// Filtre seçeneklerini temizle
			jobTypeFilter.Clear();
			
			// "Tümü" seçeneği
			jobTypeFilter.AddItem("Tüm Personel");
			
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
		
		/// <summary>
		/// Sıralama seçeneklerini başlatır
		/// </summary>
		private void InitializeSortOptions()
		{
			// Sıralama seçeneklerini temizle
			sortByOption.Clear();
			
			// Sıralama seçeneklerini ekle
			sortByOption.AddItem("İsme Göre");
			sortByOption.AddItem("Pozisyona Göre");
			sortByOption.AddItem("Seviyeye Göre");
			sortByOption.AddItem("Sadakate Göre");
			sortByOption.AddItem("Maaşa Göre");
			sortByOption.AddItem("Performansa Göre"); // Yeni eklenen sıralama seçeneği
			
			// Varsayılan olarak "İsme Göre" seçili
			sortByOption.Selected = 0;
		}
		
		/// <summary>
		/// Personel listesini güncelleyen ana metod
		/// </summary>
		private void RefreshStaffList()
		{
			if (staffManager == null) return;
			
			try
			{
				// Listeyi temizle
				staffList.Clear();
				
				// Tüm personeli al
				var allStaff = staffManager.GetAllStaff();
				
				// Filtreleme ve sıralama
				List<StaffMember> filteredStaff = FilterStaffList(allStaff);
				SortStaffList(filteredStaff);
				
				// Listeye ekle
				foreach (var staff in filteredStaff)
				{
					AddStaffToList(staff);
				}
				
				// Seçili personeli güncelle
				UpdateSelectedStaffInList();
				
				// Personel sayısını güncelle
				UpdateStaffCountLabel(filteredStaff.Count, allStaff.Count);
			}
			catch (Exception ex)
			{
				GD.PrintErr($"RefreshStaffList hatası: {ex.Message}");
				ShowErrorMessage("Personel listesi güncellenirken bir hata oluştu.");
			}
		}
		
		/// <summary>
		/// Personeli listeye ekler
		/// </summary>
		private void AddStaffToList(StaffMember staff)
		{
			// Temel bilgiler
			string displayText = $"{staff.FullName} (Lvl {staff.Level} {staff.JobTitle})";
			
			// İkonu belirle (her tür için farklı ikon)
			Texture2D icon = GetStaffTypeIcon(staff);
			
			// Listeye ekle
			int index = staffList.AddItem(displayText, icon);
			
			// Metadata olarak personeli sakla
			staffList.SetItemMetadata(index, staff);
			
			// Ek bilgiler ve biçimlendirme
			string tooltipText = $"Sadakat: {staff.Loyalty:F0}%\nMaaş: {staff.Salary:F0}₺";
			
			// Aktif görev varsa göster
			if (staff.CurrentTask != null)
			{
				tooltipText += $"\nGörev: {staff.CurrentTask.Name}";
			}
			
			// İnaktif durumu belirt (Personel sınıfına IsActive özelliği eklenmeli)
			if (staff.IsActive != null && !staff.IsActive)
			{
				tooltipText += "\nDURUM: İNAKTİF";
				staffList.SetItemCustomBgColor(index, new Color(0.3f, 0.3f, 0.3f));
			}
			
			staffList.SetItemTooltip(index, tooltipText);
			
			// Düşük sadakat veya diğer sorunlara göre renklendirme
			if (staff.Loyalty < 30f)
			{
				staffList.SetItemCustomFgColor(index, LowLoyaltyColor);
			}
			else if (staff.Loyalty < 50f)
			{
				staffList.SetItemCustomFgColor(index, MediumLoyaltyColor);
			}
		}
		
		/// <summary>
		/// Seçili personeli listede günceller
		/// </summary>
		private void UpdateSelectedStaffInList()
		{
			if (selectedStaff != null)
			{
				bool stillExists = false;
				
				for (int i = 0; i < staffList.ItemCount; i++)
				{
					var staff = (StaffMember)staffList.GetItemMetadata(i);
					if (staff.Id == selectedStaff.Id) // ID tabanlı karşılaştırma
					{
						staffList.Select(i);
						selectedStaff = staff; // Referansı güncelle
						stillExists = true;
						break;
					}
				}
				
				if (!stillExists)
				{
					// Seçili personel artık listede değilse, seçimi temizle
					selectedStaff = null;
					AnimateHidePanel(staffDetailPanel);
				}
			}
		}
		
		/// <summary>
		/// Personel sayısı etiketini günceller
		/// </summary>
		private void UpdateStaffCountLabel(int visibleCount, int totalCount)
		{
			// Not: Eğer böyle bir etiket yoksa eklemeniz gerekir
			var countLabel = GetNodeOrNull<Label>("StaffCountLabel");
			if (countLabel != null)
			{
				countLabel.Text = $"Görüntülenen: {visibleCount} / Toplam: {totalCount}";
			}
		}
		
		/// <summary>
		/// Personel listesini filtrele
		/// </summary>
		private List<StaffMember> FilterStaffList(List<StaffMember> allStaff)
		{
			List<StaffMember> filtered = new List<StaffMember>();
			
			// İş pozisyonu filtresi
			string selectedJobType = jobTypeFilter.GetItemText(jobTypeFilter.Selected);
			bool filterByJobType = selectedJobType != "Tüm Personel";
			
			// İnaktif personeli gösterme filtresi
			bool showInactive = showInactiveCheckbox.ButtonPressed;
			
			foreach (var staff in allStaff)
			{
				// İş pozisyonu filtresi
				if (filterByJobType && staff.JobTitle != selectedJobType)
					continue;
				
				// Aktif/İnaktif filtresi
				if (!showInactive && staff.IsActive != null && !staff.IsActive)
					continue;
				
				// Filtreleri geçen personeli listeye ekle
				filtered.Add(staff);
			}
			
			return filtered;
		}
		
		/// <summary>
		/// Personel listesini sırala
		/// </summary>
		private void SortStaffList(List<StaffMember> staffList)
		{
			int sortOption = sortByOption.Selected;
			
			switch (sortOption)
			{
				case 0: // İsme göre
					staffList.Sort((a, b) => a.FullName.CompareTo(b.FullName));
					break;
					
				case 1: // Pozisyona göre
					staffList.Sort((a, b) => a.JobTitle.CompareTo(b.JobTitle));
					break;
					
				case 2: // Seviyeye göre (azalan)
					staffList.Sort((a, b) => b.Level.CompareTo(a.Level));
					break;
					
				case 3: // Sadakate göre (azalan)
					staffList.Sort((a, b) => b.Loyalty.CompareTo(a.Loyalty));
					break;
					
				case 4: // Maaşa göre (azalan)
					staffList.Sort((a, b) => b.Salary.CompareTo(a.Salary));
					break;
					
				case 5: // Performansa göre (azalan)
					staffList.Sort((a, b) => b.Performance.CompareTo(a.Performance));
					break;
			}
		}
		
		/// <summary>
		/// Personel türüne göre ikon 
		/// </summary>
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
			
			// İkon dosyası varsa yükle, yoksa varsayılan ikon kullan
			if (ResourceLoader.Exists(iconPath))
				return ResourceLoader.Load<Texture2D>(iconPath);
			else 
			{
				GD.PrintErr($"Personel ikonu bulunamadı: {iconPath}");
				return ResourceLoader.Load<Texture2D>("res://assets/icons/staff/generic_staff_icon.png");
			}
		}
		
		/// <summary>
		/// Personel detaylarını göster
		/// </summary>
		private void ShowStaffDetails(StaffMember staff)
		{
			if (staff == null)
			{
				AnimateHidePanel(staffDetailPanel);
				return;
			}
			
			try
			{
				// Referansı sakla
				selectedStaff = staff;
				
				// Personel bilgilerini temizle
				foreach (Node child in staffInfoContainer.GetChildren())
				{
					child.QueueFree();
				}
				
				// Temel bilgileri ekle
				AddStaffInfoHeader(staff);
				AddStaffBasicInfo(staff);
				AddStaffAttributes(staff);
				AddStaffCapabilities(staff);
				AddSpecialTypeInfo(staff);
				
				// Butonları duruma göre ayarla
				UpdateActionButtons(staff);
				
				// Paneli animasyonla göster
				if (!staffDetailPanel.Visible)
				{
					AnimateShowPanel(staffDetailPanel);
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"ShowStaffDetails hatası: {ex.Message}");
				ShowErrorMessage("Personel detayları gösterilirken bir hata oluştu.");
			}
		}
		
		/// <summary>
		/// Personel bilgi başlığı ekle
		/// </summary>
		private void AddStaffInfoHeader(StaffMember staff)
		{
			var nameLabel = new Label();
			nameLabel.Text = staff.FullName;
			nameLabel.AddThemeColorOverride("font_color", Colors.White);
			nameLabel.AddThemeFontSizeOverride("font_size", 18);
			staffInfoContainer.AddChild(nameLabel);
			
			var titleLabel = new Label();
			titleLabel.Text = $"Seviye {staff.Level} {staff.JobTitle}";
			titleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 1.0f));
			staffInfoContainer.AddChild(titleLabel);
			
			// Aktif/İnaktif durumu
			if (staff.IsActive != null && !staff.IsActive)
			{
				var statusLabel = new Label();
				statusLabel.Text = "DURUM: İNAKTİF";
				statusLabel.AddThemeColorOverride("font_color", Colors.Red);
				staffInfoContainer.AddChild(statusLabel);
			}
			
			// Ayırıcı
			var separator = new HSeparator();
			staffInfoContainer.AddChild(separator);
		}
		
		/// <summary>
		/// Temel personel bilgilerini ekle
		/// </summary>
		private void AddStaffBasicInfo(StaffMember staff)
		{
			// Ana bilgi paneli
			var infoGrid = new GridContainer();
			infoGrid.Columns = 2;
			staffInfoContainer.AddChild(infoGrid);
			
			// Sadakat
			AddInfoRow(infoGrid, "Sadakat:", $"{staff.Loyalty:F0}%", GetLoyaltyColor(staff.Loyalty));
			
			// Maaş
			AddInfoRow(infoGrid, "Maaş:", $"{staff.Salary:F0}₺");
			
			// Performans (yeni eklenen)
			AddInfoRow(infoGrid, "Performans:", $"{staff.Performance:F0}%", GetPerformanceColor(staff.Performance));
			
			// İşe alınma tarihi (yeni eklenen)
			if (staff.HireDate != DateTime.MinValue)
			{
				AddInfoRow(infoGrid, "İşe Alınma:", $"{staff.HireDate:dd.MM.yyyy}");
			}
			
			// Görev (eğer varsa)
			if (staff.CurrentTask != null)
			{
				AddInfoRow(infoGrid, "Görev:", staff.CurrentTask.Name);
				
				// Görev ilerlemesi
				if (staff.CurrentTask.Status == StaffTask.TaskStatus.InProgress)
				{
					AddInfoRow(infoGrid, "İlerleme:", $"%{staff.CurrentTask.Progress * 100:F0}");
				}
			}
			else
			{
				AddInfoRow(infoGrid, "Görev:", "Boşta");
			}
			
			// Boşluk
			staffInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		/// <summary>
		/// Personel özelliklerini ekle
		/// </summary>
		private void AddStaffAttributes(StaffMember staff)
		{
			// Başlık
			var attrTitle = new Label();
			attrTitle.Text = "Özellikler";
			attrTitle.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			staffInfoContainer.AddChild(attrTitle);
			
			// Özellik grid'i
			var attrGrid = new GridContainer();
			attrGrid.Columns = 2;
			staffInfoContainer.AddChild(attrGrid);
			
			// Tüm özellikleri ekle
			var attributes = staff.GetAllAttributes();
			
			// Özellikleri sırala (daha güzel görünüm için)
			var sortedAttributes = attributes.OrderByDescending(attr => attr.Value)
				.ThenBy(attr => attr.Key)
				.ToList();
			
			foreach (var attr in sortedAttributes)
			{
				// Özellik rengi (değere göre)
				Color attrColor = GetAttributeColor(attr.Value);
				
				// Eğer eğitilebilirse yükseltme işareti ekle
				string suffix = staff.CanTrain(attr.Key) ? " ↑" : "";
				AddInfoRow(attrGrid, $"{attr.Key}:", $"{attr.Value:F1}/10{suffix}", attrColor);
			}
			
			// Boşluk
			staffInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		/// <summary>
		/// Personel yeteneklerini ekle
		/// </summary>
		private void AddStaffCapabilities(StaffMember staff)
		{
			// Yetenekler başlığı
			var capTitle = new Label();
			capTitle.Text = "Yetenekler";
			capTitle.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			staffInfoContainer.AddChild(capTitle);
			
			// Yetenekler listesi
			var capabilities = staff.GetSpecialCapabilities();
			if (capabilities.Length > 0)
			{
				foreach (var cap in capabilities)
				{
					var capLabel = new Label();
					capLabel.Text = $"• {cap}";
					staffInfoContainer.AddChild(capLabel);
				}
			}
			else
			{
				var noCapsLabel = new Label();
				noCapsLabel.Text = "Özel yetenek yok";
				noCapsLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
				staffInfoContainer.AddChild(noCapsLabel);
			}
			
			// Boşluk
			staffInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		/// <summary>
		/// Özel personel türü bilgilerini ekle
		/// </summary>
		private void AddSpecialTypeInfo(StaffMember staff)
		{
			// Personel türüne göre özel bilgiler
			if (staff is Kons kons)
			{
				AddKonsInfo(kons);
			}
			else if (staff is SecurityStaff security)
			{
				AddSecurityStaffInfo(security);
			}
			else if (staff is Waiter waiter)
			{
				AddWaiterInfo(waiter);
			}
			else if (staff is Musician musician) 
			{
				AddMusicianInfo(musician);
			}
			else if (staff is Cook cook)
			{
				AddCookInfo(cook);
			}
			else if (staff is IllegalFloorStaff illegalStaff)
			{
				AddIllegalFloorStaffInfo(illegalStaff);
			}
		}
		
		/// <summary>
		/// Kons personeli için özel bilgiler
		/// </summary>
		private void AddKonsInfo(Kons kons)
		{
			// Kons özel bilgileri
			var title = new Label();
			title.Text = "Kons Bilgileri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			staffInfoContainer.AddChild(title);
			
			var infoGrid = new GridContainer();
			infoGrid.Columns = 2;
			staffInfoContainer.AddChild(infoGrid);
			
			AddInfoRow(infoGrid, "İçki Satış Çarpanı:", $"+{(kons.DrinkSalesMultiplier - 1f) * 100:F0}%");
			AddInfoRow(infoGrid, "Bahşiş Payı:", $"{(1f - kons.TipPercentage) * 100:F0}%");
			AddInfoRow(infoGrid, "Müdavim Sayısı:", $"{kons.RegularCustomers.Count}/{kons.MaxRegularCustomers}");
			
			// Müdavimler
			if (kons.RegularCustomers.Count > 0)
			{
				var vipTitle = new Label();
				vipTitle.Text = "Müdavimler:";
				staffInfoContainer.AddChild(vipTitle);
				
				var vipBox = new VBoxContainer();
				staffInfoContainer.AddChild(vipBox);
				
				foreach (var customer in kons.RegularCustomers.Take(3)) // En önemli 3 müdavim
				{
					var customerLabel = new Label();
					customerLabel.Text = $"• {customer.Name} ({customer.SpendingPower:F0}₺)";
					vipBox.AddChild(customerLabel);
				}
				
				if (kons.RegularCustomers.Count > 3)
				{
					var moreLabel = new Label();
					moreLabel.Text = $"... ve {kons.RegularCustomers.Count - 3} kişi daha";
					moreLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
					vipBox.AddChild(moreLabel);
				}
			}
		}
		
		/// <summary>
		/// Güvenlik personeli için özel bilgiler
		/// </summary>
		private void AddSecurityStaffInfo(SecurityStaff security)
		{
			// Güvenlik özel bilgileri
			var title = new Label();
			title.Text = "Güvenlik Bilgileri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			staffInfoContainer.AddChild(title);
			
			var infoGrid = new GridContainer();
			infoGrid.Columns = 2;
			staffInfoContainer.AddChild(infoGrid);
			
			AddInfoRow(infoGrid, "Tehdit Seviyesi:", $"{security.ThreatLevel:F1}");
			AddInfoRow(infoGrid, "Tespit Şansı:", $"%{security.DetectionChance * 100:F0}");
			AddInfoRow(infoGrid, "Dövüş Yeteneği:", $"{security.FightingAbility:F1}");
			
			// Ekipman
			string equipText = "";
			if (security.HasRadio) equipText += "Telsiz, ";
			if (security.HasTaser) equipText += "Şok Cihazı, ";
			if (security.HasBodyArmor) equipText += "Koruyucu Yelek, ";
			
			if (!string.IsNullOrEmpty(equipText))
			{
				equipText = equipText.TrimEnd(',', ' ');
				AddInfoRow(infoGrid, "Ekipman:", equipText);
			}
			
			// Güvenlik olayları istatistiği
			AddInfoRow(infoGrid, "Kavga Önleme:", $"{security.FightsPrevented}");
			AddInfoRow(infoGrid, "Hırsız Yakalama:", $"{security.ThievesCaught}");
		}
		
		/// <summary>
		/// Garson personeli için özel bilgiler
		/// </summary>
		private void AddWaiterInfo(Waiter waiter)
		{
			var title = new Label();
			title.Text = "Garson Bilgileri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			staffInfoContainer.AddChild(title);
			
			var infoGrid = new GridContainer();
			infoGrid.Columns = 2;
			staffInfoContainer.AddChild(infoGrid);
			
			AddInfoRow(infoGrid, "Servis Hızı:", $"{waiter.ServiceSpeed:F1}/10");
			AddInfoRow(infoGrid, "Sipariş Doğruluğu:", $"%{waiter.OrderAccuracy * 100:F0}");
			AddInfoRow(infoGrid, "Taşıma Kapasitesi:", $"{waiter.CarryCapacity}");
			AddInfoRow(infoGrid, "Toplam Bahşiş:", $"{waiter.TotalTipsEarned:F0}₺");
		}
		
		/// <summary>
		/// Müzisyen personeli için özel bilgiler
		/// </summary>
		private void AddMusicianInfo(Musician musician)
		{
			var title = new Label();
			title.Text = "Müzisyen Bilgileri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			staffInfoContainer.AddChild(title);
			
			var infoGrid = new GridContainer();
			infoGrid.Columns = 2;
			staffInfoContainer.AddChild(infoGrid);
			
			AddInfoRow(infoGrid, "Müzik Tarzı:", musician.MusicStyle);
			AddInfoRow(infoGrid, "Enstrüman:", musician.Instrument);
			AddInfoRow(infoGrid, "Performans Etkisi:", $"+%{musician.PerformanceMultiplier * 100:F0}");
			AddInfoRow(infoGrid, "Popülerlik:", $"{musician.Popularity:F1}/10");
		}
		
		/// <summary>
		/// Aşçı personeli için özel bilgiler
		/// </summary>
		private void AddCookInfo(Cook cook)
		{
			var title = new Label();
			title.Text = "Aşçı Bilgileri";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			staffInfoContainer.AddChild(title);
			
			var infoGrid = new GridContainer();
			infoGrid.Columns = 2;
			staffInfoContainer.AddChild(infoGrid);
			
			AddInfoRow(infoGrid, "Uzmanlık:", cook.Specialty);
			AddInfoRow(infoGrid, "Yemek Kalitesi:", $"{cook.FoodQuality:F1}/10");
			AddInfoRow(infoGrid, "Hazırlama Hızı:", $"{cook.PreparationSpeed:F1}/10");
			AddInfoRow(infoGrid, "Özel Menü:", cook.HasSignatureDish ? "Var" : "Yok");
		}
		
		/// <summary>
		/// Kaçak kat personeli için özel bilgiler
		/// </summary>
		private void AddIllegalFloorStaffInfo(IllegalFloorStaff illegalStaff)
		{
			var title = new Label();
			title.Text = "Kaçak Kat Bilgileri";
			title.AddThemeColorOverride("font_color", Colors.DarkRed);
			staffInfoContainer.AddChild(title);
			
			var infoGrid = new GridContainer();
			infoGrid.Columns = 2;
			staffInfoContainer.AddChild(infoGrid);
			
			AddInfoRow(infoGrid, "Risk Seviyesi:", $"%{illegalStaff.RiskLevel * 100:F0}", GetRiskColor(illegalStaff.RiskLevel));
			AddInfoRow(infoGrid, "Operasyon Tipi:", illegalStaff.OperationType);
			AddInfoRow(infoGrid, "Kâr Payı:", $"%{illegalStaff.ProfitShare * 100:F0}");
			
			// Hukuki geçmiş
			AddInfoRow(infoGrid, "Sabıka:", illegalStaff.HasCriminalRecord ? "Var" : "Yok", 
				illegalStaff.HasCriminalRecord ? Colors.Red : Colors.Green);
			
			// Bağlantılar
			if (illegalStaff.Connections != null && illegalStaff.Connections.Count > 0)
			{
				var connectionsTitle = new Label();
				connectionsTitle.Text = "Bağlantılar:";
				staffInfoContainer.AddChild(connectionsTitle);
				
				var connectionsBox = new VBoxContainer();
				staffInfoContainer.AddChild(connectionsBox);
				
				foreach (var connection in illegalStaff.Connections)
				{
					var connLabel = new Label();
					connLabel.Text = $"• {connection}";
					connectionsBox.AddChild(connLabel);
				}
			}
		}
		
		/// <summary>
		/// Bilgi satırı ekleyen yardımcı metod
		/// </summary>
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
		
		/// <summary>
		/// Sadakat değerine göre renk
		/// </summary>
		private Color GetLoyaltyColor(float loyalty)
		{
			if (loyalty < 30f)
				return Colors.Red;
			else if (loyalty < 50f)
				return Colors.Orange;
			else if (loyalty < 70f)
				return Colors.Yellow;
			else if (loyalty < 90f)
				return Colors.Green;
			else
				return Colors.LightGreen;
		}
		
		/// <summary>
		/// Performans değerine göre renk
		/// </summary>
		private Color GetPerformanceColor(float performance)
		{
			if (performance < 40f)
				return Colors.Red;
			else if (performance < 60f)
				return Colors.Orange;
			else if (performance < 80f)
				return Colors.Yellow;
			else
				return Colors.Green;
		}
		
		/// <summary>
		/// Risk seviyesine göre renk
		/// </summary>
		private Color GetRiskColor(float risk)
		{
			if (risk > 0.7f)
				return Colors.Red;
			else if (risk > 0.4f)
				return Colors.Orange;
			else
				return Colors.Green;
		}
		
		/// <summary>
		/// Özellik değerine göre renk
		/// </summary>
		private Color GetAttributeColor(float value)
		{
			if (value < 3f)
				return Colors.DarkRed;
			else if (value < 5f)
				return Colors.Yellow;
			else if (value < 8f)
				return Colors.Green;
			else
				return Colors.LightGreen;
		}
		
		/// <summary>
		/// Butonları personel durumuna göre güncelle
		/// </summary>
		private void UpdateActionButtons(StaffMember staff)
		{
			if (staff == null)
			{
				trainButton.Disabled = true;
				promoteButton.Disabled = true;
				fireButton.Disabled = true;
				assignTaskButton.Disabled = true;
				viewTasksButton.Disabled = true;
				return;
			}
			
			// Eğitim butonu
			bool canTrain = false;
			var attributes = staff.GetAllAttributes();
			foreach (var attr in attributes)
			{
				if (staff.CanTrain(attr.Key))
				{
					canTrain = true;
					break;
				}
			}
			trainButton.Disabled = !canTrain;
			
			// Terfi butonu (maksimum seviyeye ulaşmadıysa aktif)
			promoteButton.Disabled = (staff.Level >= staff.MaxLevel || !staff.CanPromote);
			
			// İşten çıkarma butonu
			fireButton.Disabled = false;
			
			// Görev atama butonu (inaktif personel görev alamaz)
			assignTaskButton.Disabled = (staff.IsActive != null && !staff.IsActive);
			
			// Görev görüntüleme butonu (personelin görevi varsa aktif)
			viewTasksButton.Disabled = (staff.CurrentTask == null);
			
			// Buton ipuçları
			trainButton.TooltipText = trainButton.Disabled ? "Eğitilebilecek özellik yok" : "Personeli eğit";
			promoteButton.TooltipText = promoteButton.Disabled ? "Terfi için gerekli şartlar sağlanmıyor" : "Personeli terfi ettir";
			assignTaskButton.TooltipText = assignTaskButton.Disabled ? "İnaktif personele görev atanamaz" : "Görev ata";
		}
		
		#region Panel Animasyonları
		
		/// <summary>
		/// Paneli animasyonla göster
		/// </summary>
		private void AnimateShowPanel(Control panel)
		{
			if (panel == null) return;
			
			// Mevcut tween'i temizle
			if (currentTween != null && currentTween.IsValid())
			{
				currentTween.Kill();
			}
			
			// Animasyon başlangıç ayarları
			panel.Visible = true;
			panel.Modulate = new Color(1, 1, 1, 0);
			panel.Scale = new Vector2(0.95f, 0.95f);
			
			// Tween oluştur
			currentTween = CreateTween();
			currentTween.SetEase(Tween.EaseType.Out);
			currentTween.SetTrans(Tween.TransitionType.Quint);
			
			// Animasyonu ekle
			currentTween.TweenProperty(panel, "modulate:a", 1.0f, 0.3f);
			currentTween.Parallel().TweenProperty(panel, "scale", new Vector2(1.0f, 1.0f), 0.3f);
		}
		
		/// <summary>
		/// Paneli animasyonla gizle
		/// </summary>
		private void AnimateHidePanel(Control panel)
		{
			if (panel == null || !panel.Visible) return;
			
			// Mevcut tween'i temizle
			if (currentTween != null && currentTween.IsValid())
			{
				currentTween.Kill();
			}
			
			// Tween oluştur
			currentTween = CreateTween();
			currentTween.SetEase(Tween.EaseType.Out);
			currentTween.SetTrans(Tween.TransitionType.Quint);
			
			// Animasyonu ekle
			currentTween.TweenProperty(panel, "modulate:a", 0.0f, 0.2f);
			currentTween.Parallel().TweenProperty(panel, "scale", new Vector2(0.95f, 0.95f), 0.2f);
			
			// Animasyon bitince gizle
			currentTween.TweenCallback(Callable.From(() => {
				panel.Visible = false;
			}));
		}
		
		#endregion
		
		#region Buton Olay İşleyicileri
		
		/// <summary>
		/// Eğitim butonuna basıldığında
		/// </summary>
		private void OnTrainButtonPressed()
		{
			if (selectedStaff == null) return;
			
			// Eğitim panelini göster
			ShowTrainingPanel(selectedStaff);
		}
		
		/// <summary>
		/// Terfi butonuna basıldığında
		/// </summary>
		private void OnPromoteButtonPressed()
		{
			if (selectedStaff == null || staffManager == null) return;
			
			// Terfi maliyetini kontrol et
			float promotionCost = staffManager.GetPromotionCost(selectedStaff);
			
			if (gameManager?.Money < promotionCost)
			{
				ShowErrorMessage($"Terfi için yeterli paranız yok. Gerekli miktar: {promotionCost:F0}₺");
				return;
			}
			
			// Personeli terfi ettir
			if (staffManager.PromoteStaff(selectedStaff))
			{
				GD.Print($"{selectedStaff.FullName} terfi ettirildi!");
				
				// Başarı mesajı göster
				ShowSuccessMessage($"{selectedStaff.FullName} başarıyla terfi ettirildi!");
				
				// UI'ı güncelle
				RefreshStaffList();
				ShowStaffDetails(selectedStaff);
			}
			else
			{
				GD.Print($"{selectedStaff.FullName} terfi ettirilemedi!");
				ShowErrorMessage($"{selectedStaff.FullName} terfi ettirilemedi. Gerekli şartlar sağlanmıyor.");
			}
		}
		
		/// <summary>
		/// İşten çıkarma butonuna basıldığında
		/// </summary>
		private void OnFireButtonPressed()
		{
			if (selectedStaff == null || staffManager == null) return;
			
			// Onay diyalogu göster
			ConfirmationDialog confirmDialog = new ConfirmationDialog();
			confirmDialog.Title = "İşten Çıkarma Onayı";
			confirmDialog.DialogText = $"{selectedStaff.FullName} adlı personeli işten çıkarmak istediğinize emin misiniz?";
			
			// Tazminat tutarını hesapla ve göster
			float severancePay = staffManager.CalculateSeverancePay(selectedStaff);
			if (severancePay > 0)
			{
				confirmDialog.DialogText += $"\n\nTazminat Bedeli: {severancePay:F0}₺";
			}
			
			// Yüksek sadakatte ise uyarı ekle
			if (selectedStaff.Loyalty > 70f)
			{
				confirmDialog.DialogText += "\n\nUyarı: Bu personelin sadakati yüksek. İşten çıkarma diğer personelin sadakatini etkileyebilir!";
			}
			
			confirmDialog.Confirmed += OnFireConfirmed;
			
			AddChild(confirmDialog);
			confirmDialog.PopupCentered();
		}
		
		/// <summary>
		/// İşten çıkarma onaylandığında
		/// </summary>
		private void OnFireConfirmed()
		{
			if (selectedStaff == null || staffManager == null) return;
			
			try
			{
				// Personeli işten çıkar
				if (staffManager.FireStaff(selectedStaff))
				{
					string staffName = selectedStaff.FullName;
					
					var firedStaff = selectedStaff;
					selectedStaff = null;
					
					// UI'ı güncelle
					RefreshStaffList();
					AnimateHidePanel(staffDetailPanel);
					
					// Başarı mesajı göster
					ShowSuccessMessage($"{staffName} işten çıkarıldı.");
				}
				else
				{
					GD.Print($"{selectedStaff.FullName} işten çıkarılamadı!");
					ShowErrorMessage("İşten çıkarma işlemi başarısız oldu.");
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"İşten çıkarma hatası: {ex.Message}");
				ShowErrorMessage("İşten çıkarma sırasında bir hata oluştu.");
			}
		}
		
		/// <summary>
		/// Görev atama butonuna basıldığında
		/// </summary>
		private void OnAssignTaskButtonPressed()
		{
			if (selectedStaff == null) return;
			
			// Görev atama panelini göster
			ShowTaskAssignmentPanel(selectedStaff);
		}
		
		/// <summary>
		/// Görev görüntüleme butonuna basıldığında
		/// </summary>
		private void OnViewTasksButtonPressed()
		{
			if (selectedStaff == null || selectedStaff.CurrentTask == null) return;
			
			// Görev detaylarını göster
			ShowTaskDetails(selectedStaff.CurrentTask);
		}
		
		/// <summary>
		/// Yeni personel alım butonuna basıldığında
		/// </summary>
		private void OnHireNewButtonPressed()
		{
			// İşe alım panelini göster
			ShowHiringPanel();
		}
		
		/// <summary>
		/// Kapatma butonuna basıldığında
		/// </summary>
		private void OnCloseButtonPressed()
		{
			// Tüm panelleri kapat
			HideAllPanels();
			
			// UI'ı gizle
			Visible = false;
		}
		
		#endregion
		
		#region Liste ve Filtre Olayları
		
		/// <summary>
		/// Personel seçildiğinde
		/// </summary>
		private void OnStaffSelected(long index)
		{
			// Personel bilgilerini göster
			var staff = (StaffMember)staffList.GetItemMetadata((int)index);
			ShowStaffDetails(staff);
		}
		
		/// <summary>
		/// Filtre değiştiğinde
		/// </summary>
		private void OnFilterChanged(long index)
		{
			RefreshStaffList();
		}
		
		/// <summary>
		/// İnaktif gösterme seçeneği değiştiğinde
		/// </summary>
		private void OnShowInactiveToggled(bool toggled)
		{
			RefreshStaffList();
		}
		
		#endregion
		
		#region StaffManager Olayları
		
		/// <summary>
		/// Personel işe alındığında
		/// </summary>
		private void OnStaffHired(StaffMember staff)
		{
			RefreshStaffList();
			
			// Yeni personel mesajı göster
			ShowSuccessMessage($"{staff.FullName} işe alındı!");
		}
		
		/// <summary>
		/// Personel işten çıkarıldığında
		/// </summary>
		private void OnStaffFired(StaffMember staff)
		{
			// Eğer şu anda gösterilen personel ise paneli kapat
			if (selectedStaff?.Id == staff.Id)
			{
				selectedStaff = null;
				AnimateHidePanel(staffDetailPanel);
			}
			
			RefreshStaffList();
		}
		
		/// <summary>
		/// Personel özelliği değiştiğinde
		/// </summary>
		private void OnStaffAttributeChanged(StaffMember staff, string attributeName, float newValue)
		{
			// Eğer şu anda gösterilen personel ise detayları güncelle
			if (selectedStaff?.Id == staff.Id)
			{
				selectedStaff = staff; // Referansı güncelle
				ShowStaffDetails(staff);
			}
			
			RefreshStaffList();
		}
		
		/// <summary>
		/// Personel sadakati değiştiğinde
		/// </summary>
		private void OnStaffLoyaltyChanged(StaffMember staff, float newLoyalty)
		{
			// Eğer şu anda gösterilen personel ise detayları güncelle
			if (selectedStaff?.Id == staff.Id)
			{
				selectedStaff = staff; // Referansı güncelle
				ShowStaffDetails(staff);
			}
			
			RefreshStaffList();
		}
		
		#endregion
		
		#region Panel İşlevleri
		
		/// <summary>
		/// İşe alım panelini göster
		/// </summary>
		private void ShowHiringPanel()
		{
			if (hirePanel == null || staffManager == null) return;
			
			try
			{
				// İşe alınabilecek personel listesini al
				var availableStaff = staffManager.GetAvailableStaffForHire();
				
				// Panel içeriğini doldur
				var staffListControl = hirePanel.GetNode<ItemList>("VBoxContainer/AvailableStaffList");
				var detailPanel = hirePanel.GetNode<Panel>("VBoxContainer/CandidateDetailPanel");
				var hireButton = hirePanel.GetNode<Button>("VBoxContainer/ButtonContainer/HireButton");
				var cancelButton = hirePanel.GetNode<Button>("VBoxContainer/ButtonContainer/CancelButton");
				
				// Liste içeriğini temizle
				staffListControl.Clear();
				
				// Personel adaylarını listeye ekle
				foreach (var candidate in availableStaff)
				{
					string displayText = $"{candidate.FullName} ({candidate.JobTitle}) - {candidate.Salary:F0}₺";
					Texture2D icon = GetStaffTypeIcon(candidate);
					
					int idx = staffListControl.AddItem(displayText, icon);
					staffListControl.SetItemMetadata(idx, candidate);
					
					// Özellikleri ipucu olarak göster
					string tooltip = "";
					var mainAttributes = candidate.GetAllAttributes()
						.OrderByDescending(a => a.Value)
						.Take(3)
						.ToList();
					
					foreach (var attr in mainAttributes)
					{
						tooltip += $"{attr.Key}: {attr.Value:F1}/10\n";
					}
					
					staffListControl.SetItemTooltip(idx, tooltip.TrimEnd('\n'));
				}
				
				// Buton olaylarını bağla
				hireButton.Pressed = null; // Önceki bağlantıları temizle
				hireButton.Pressed += () => 
				{
					var selectedItems = staffListControl.GetSelectedItems();
					if (selectedItems.Length > 0)
					{
						var selectedCandidate = (StaffMember)staffListControl.GetItemMetadata(selectedItems[0]);
						HireCandidate(selectedCandidate);
					}
				};
				
				cancelButton.Pressed = null; // Önceki bağlantıları temizle
				cancelButton.Pressed += () => 
				{
					AnimateHidePanel(hirePanel);
				};
				
				// Liste seçim olayını bağla
				staffListControl.ItemSelected = null; // Önceki bağlantıları temizle
				staffListControl.ItemSelected += (idx) => 
				{
					var candidate = (StaffMember)staffListControl.GetItemMetadata((int)idx);
					ShowCandidateDetails(candidate, detailPanel);
					hireButton.Disabled = false;
				};
				
				// Başlangıçta detay panelini gizle ve işe alma butonunu devre dışı bırak
				detailPanel.Visible = false;
				hireButton.Disabled = true;
				
				// Paneli animasyonla göster
				AnimateShowPanel(hirePanel);
			}
			catch (Exception ex)
			{
				GD.PrintErr($"ShowHiringPanel hatası: {ex.Message}");
				ShowErrorMessage("İşe alım paneli gösterilirken bir hata oluştu.");
			}
		}
		
		/// <summary>
		/// Aday detaylarını göster
		/// </summary>
		private void ShowCandidateDetails(StaffMember candidate, Panel detailPanel)
		{
			if (candidate == null || detailPanel == null) return;
			
			try
			{
				// Detay panelini göster
				detailPanel.Visible = true;
				
				// Panel içeriğini doldur
				var nameLabel = detailPanel.GetNode<Label>("VBoxContainer/NameLabel");
				var jobLabel = detailPanel.GetNode<Label>("VBoxContainer/JobLabel");
				var attributesContainer = detailPanel.GetNode<VBoxContainer>("VBoxContainer/AttributesContainer");
				var salaryLabel = detailPanel.GetNode<Label>("VBoxContainer/SalaryLabel");
				
				// Temel bilgileri güncelle
				nameLabel.Text = candidate.FullName;
				jobLabel.Text = candidate.JobTitle;
				salaryLabel.Text = $"İstenen Maaş: {candidate.Salary:F0}₺";
				
				// Maaşın uygunluğunu kontrol et
				if (gameManager != null && candidate.Salary > gameManager.Money * 0.3f)
				{
					salaryLabel.AddThemeColorOverride("font_color", Colors.Red);
					salaryLabel.Text += " (Yüksek!)";
				}
				
				// Özellikleri temizle
				foreach (Node child in attributesContainer.GetChildren())
				{
					child.QueueFree();
				}
				
				// Özellikleri listele
				var attributes = candidate.GetAllAttributes();
				
				// Özellikleri sırala
				var sortedAttributes = attributes.OrderByDescending(attr => attr.Value)
					.ThenBy(attr => attr.Key)
					.ToList();
				
				foreach (var attr in sortedAttributes)
				{
					var attrLabel = new Label();
					attrLabel.Text = $"{attr.Key}: {attr.Value:F1}/10";
					
					// Yüksek değerleri renklendir
					if (attr.Value >= 7f)
						attrLabel.AddThemeColorOverride("font_color", Colors.Green);
					else if (attr.Value >= 5f)
						attrLabel.AddThemeColorOverride("font_color", Colors.YellowGreen);
					else if (attr.Value < 3f)
						attrLabel.AddThemeColorOverride("font_color", Colors.DarkRed);
					
					attributesContainer.AddChild(attrLabel);
				}
				
				// Özel yetenekleri göster
				var capabilities = candidate.GetSpecialCapabilities();
				if (capabilities.Length > 0)
				{
					var capLabel = new Label();
					capLabel.Text = "Özel Yetenekler:";
					attributesContainer.AddChild(capLabel);
					
					foreach (var cap in capabilities)
					{
						var capItem = new Label();
						capItem.Text = $"• {cap}";
						capItem.AddThemeColorOverride("font_color", Colors.LightBlue);
						attributesContainer.AddChild(capItem);
					}
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"ShowCandidateDetails hatası: {ex.Message}");
			}
		}
		
		/// <summary>
		/// Adayı işe al
		/// </summary>
		private void HireCandidate(StaffMember candidate)
		{
			if (candidate == null || staffManager == null) return;
			
			try
			{
				// Yeterli para kontrolü
				if (gameManager != null && gameManager.Money < candidate.Salary * 2)
				{
					ShowErrorMessage($"İşe almak için yeterli paranız yok. En az {candidate.Salary * 2:F0}₺ gerekiyor.");
					return;
				}
				
				// Personeli işe al
				var hiredStaff = staffManager.HireStaff(candidate);
				
				if (hiredStaff != null)
				{
					GD.Print($"{hiredStaff.FullName} işe alındı!");
					
					// Başarı mesajı göster
					ShowSuccessMessage($"{hiredStaff.FullName} başarıyla işe alındı!");
					
					// İşe alım panelini kapat
					AnimateHidePanel(hirePanel);
					
					// Listeyi güncelle
					RefreshStaffList();
				}
				else
				{
					GD.Print("Personel işe alınamadı!");
					ShowErrorMessage("Personel işe alınamadı. Yeterli fonunuz olmayabilir veya başka bir sorun oluşmuş olabilir.");
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"HireCandidate hatası: {ex.Message}");
				ShowErrorMessage("İşe alma sırasında bir hata oluştu.");
			}
		}
		
		/// <summary>
		/// Eğitim panelini göster
		/// </summary>
		private void ShowTrainingPanel(StaffMember staff)
		{
			if (trainingPanel == null || staff == null) return;
			
			try
			{
				// Eğitim panelini başlat
				var attributeList = trainingPanel.GetNode<ItemList>("VBoxContainer/AttributeList");
				var costLabel = trainingPanel.GetNode<Label>("VBoxContainer/CostInfoLabel");
				var trainButton = trainingPanel.GetNode<Button>("VBoxContainer/ButtonContainer/TrainButton");
				var cancelButton = trainingPanel.GetNode<Button>("VBoxContainer/ButtonContainer/CancelButton");
				
				// Liste içeriğini temizle
				attributeList.Clear();
				
				// Personelin mevcut özelliklerini listele
				var attributes = staff.GetAllAttributes();
				
				// Eğitilebilir özellikler var mı kontrol et
				bool hasTrainableAttribute = false;
				
				foreach (var attr in attributes)
				{
					// Maksimum değere ulaşmış özellikleri atlayabiliriz
					if (attr.Value >= 10f || !staff.CanTrain(attr.Key)) continue;
					
					hasTrainableAttribute = true;
					
					string displayText = $"{attr.Key}: {attr.Value:F1}/10";
					
					// Eğitim sonrası yeni değeri göster
					float potentialValue = Math.Min(10f, attr.Value + staffManager.TrainingAttributeGain);
					displayText += $" → {potentialValue:F1}";
					
					int idx = attributeList.AddItem(displayText);
					attributeList.SetItemMetadata(idx, attr.Key);
					
					// Düşük değerleri vurgula (daha çok fayda sağlar)
					if (attr.Value < 5f)
					{
						attributeList.SetItemCustomFgColor(idx, Colors.Orange);
					}
				}
				
				// Eğitilebilir özellik yoksa bildir
				if (!hasTrainableAttribute)
				{
					ShowErrorMessage("Bu personelin eğitilebilecek özelliği bulunmuyor.");
					return;
				}
				
				// Eğitim maliyetini göster
				float trainingCost = staffManager.CalculateTrainingCost(staff);
				costLabel.Text = $"Eğitim Maliyeti: {trainingCost:F0}₺";
				
				// Yeterli para yoksa uyarı göster
				if (gameManager != null && gameManager.Money < trainingCost)
				{
					costLabel.AddThemeColorOverride("font_color", Colors.Red);
					costLabel.Text += " (Yetersiz!)";
				}
				
				// Buton olaylarını bağla
				trainButton.Pressed = null; // Önceki bağlantıları temizle
				trainButton.Pressed += () => 
				{
					var selectedItems = attributeList.GetSelectedItems();
					if (selectedItems.Length > 0)
					{
						var attributeName = (string)attributeList.GetItemMetadata(selectedItems[0]);
						TrainStaffAttribute(staff, attributeName);
						AnimateHidePanel(trainingPanel);
					}
				};
				
				cancelButton.Pressed = null; // Önceki bağlantıları temizle
				cancelButton.Pressed += () => 
				{
					AnimateHidePanel(trainingPanel);
				};
				
				// Başlangıçta eğitim butonunu devre dışı bırak
				trainButton.Disabled = true;
				
				// Liste seçim olayını bağla
				attributeList.ItemSelected = null; // Önceki bağlantıları temizle
				attributeList.ItemSelected += (idx) => 
				{
					trainButton.Disabled = false;
					
					// Yeterli para yoksa butonu devre dışı bırak
					if (gameManager != null && gameManager.Money < trainingCost)
					{
						trainButton.Disabled = true;
						trainButton.TooltipText = "Eğitim için yeterli paranız yok!";
					}
					else
					{
						trainButton.TooltipText = "";
					}
				};
				
				// Paneli animasyonla göster
				AnimateShowPanel(trainingPanel);
			}
			catch (Exception ex)
			{
				GD.PrintErr($"ShowTrainingPanel hatası: {ex.Message}");
				ShowErrorMessage("Eğitim paneli gösterilirken bir hata oluştu.");
			}
		}
		
		/// <summary>
		/// Personel özelliğini eğit
		/// </summary>
		private void TrainStaffAttribute(StaffMember staff, string attributeName)
		{
			if (staff == null || staffManager == null) return;
			
			try
			{
				// Özelliği eğit
				if (staffManager.TrainStaff(staff, attributeName))
				{
					GD.Print($"{staff.FullName}'in {attributeName} özelliği geliştirildi!");
					
					// Başarı mesajı göster
					ShowSuccessMessage($"{staff.FullName}'in {attributeName} özelliği geliştirildi!");
					
					// Detayları güncelle
					ShowStaffDetails(staff);
				}
				else
				{
					GD.Print($"{attributeName} özelliği geliştirilemedi!");
					ShowErrorMessage($"{attributeName} özelliği geliştirilemedi. Yeterli fonunuz olmayabilir.");
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"TrainStaffAttribute hatası: {ex.Message}");
				ShowErrorMessage("Eğitim sırasında bir hata oluştu.");
			}
		}
		
		/// <summary>
		/// Görev atama panelini göster
		/// </summary>
		private void ShowTaskAssignmentPanel(StaffMember staff)
		{
			if (taskAssignmentPanel == null || staff == null) return;
			
			try
			{
				// Görev panelini başlat
				var taskList = taskAssignmentPanel.GetNode<ItemList>("VBoxContainer/TaskList");
				var taskDescLabel = taskAssignmentPanel.GetNode<Label>("VBoxContainer/TaskDescriptionLabel");
				var assignButton = taskAssignmentPanel.GetNode<Button>("VBoxContainer/ButtonContainer/AssignButton");
				var cancelButton = taskAssignmentPanel.GetNode<Button>("VBoxContainer/ButtonContainer/CancelButton");
				
				// Liste içeriğini temizle
				taskList.Clear();
				
				// Uygun görevleri listele (personel türüne göre)
				List<StaffTask> availableTasks = GetAvailableTasksForStaff(staff);
				
				// Uygun görev yoksa bildir
				if (availableTasks.Count == 0)
				{
					ShowErrorMessage("Bu personel tipi için uygun görev bulunamadı.");
					return;
				}
				
				foreach (var task in availableTasks)
				{
					string displayText = task.Name;
					
					// Görevin süresini göster
					if (task.EstimatedDuration.TotalMinutes > 0)
					{
						int hours = (int)task.EstimatedDuration.TotalHours;
						int minutes = (int)task.EstimatedDuration.TotalMinutes % 60;
						displayText += $" ({hours}s {minutes}dk)";
					}
					
					int idx = taskList.AddItem(displayText);
					taskList.SetItemMetadata(idx, task);
				}
				
				// Buton olaylarını bağla
				assignButton.Pressed = null; // Önceki bağlantıları temizle
				assignButton.Pressed += () => 
				{
					var selectedItems = taskList.GetSelectedItems();
					if (selectedItems.Length > 0)
					{
						var task = (StaffTask)taskList.GetItemMetadata(selectedItems[0]);
						AssignTaskToStaff(staff, task);
						AnimateHidePanel(taskAssignmentPanel);
					}
				};
				
				cancelButton.Pressed = null; // Önceki bağlantıları temizle
				cancelButton.Pressed += () => 
				{
					AnimateHidePanel(taskAssignmentPanel);
				};
				
				// Başlangıçta atama butonunu devre dışı bırak
				assignButton.Disabled = true;
				
				// Liste seçim olayını bağla
				taskList.ItemSelected = null; // Önceki bağlantıları temizle
				taskList.ItemSelected += (idx) => 
				{
					var task = (StaffTask)taskList.GetItemMetadata((int)idx);
					taskDescLabel.Text = task.Description;
					assignButton.Disabled = false;
				};
				
				// Başlangıçta açıklama etiketini temizle
				taskDescLabel.Text = "";
				
				// Paneli animasyonla göster
				AnimateShowPanel(taskAssignmentPanel);
			}
			catch (Exception ex)
			{
				GD.PrintErr($"ShowTaskAssignmentPanel hatası: {ex.Message}");
				ShowErrorMessage("Görev atama paneli gösterilirken bir hata oluştu.");
			}
		}
		
		/// <summary>
		/// Personel türüne göre uygun görevleri al
		/// </summary>
		private List<StaffTask> GetAvailableTasksForStaff(StaffMember staff)
		{
			List<StaffTask> tasks = new List<StaffTask>();
			
			// Personel türüne göre uygun görevleri oluştur
			if (staff is Kons)
			{
				tasks.Add(StaffTask.CreateCustomerInteractionTask(null));
				tasks.Add(StaffTask.CreatePromotionalTask("VIP hizmet"));
				tasks.Add(StaffTask.CreateSpecialTask("Çiçek getir", "Özel müşteri için çiçek getir", TimeSpan.FromHours(1)));
				tasks.Add(StaffTask.CreateSpecialTask("Sürpriz hazırla", "Doğum günü kutlaması için sürpriz hazırla", TimeSpan.FromHours(2)));
			}
			else if (staff is SecurityStaff)
			{
				tasks.Add(StaffTask.CreateSecurityTask(Vector2.Zero));
				tasks.Add(StaffTask.CreatePatrolTask());
				tasks.Add(StaffTask.CreateSpecialTask("VIP Koruma", "Önemli bir müşteriyi koru", TimeSpan.FromHours(3)));
				tasks.Add(StaffTask.CreateSpecialTask("Güvenlik taraması", "Mekanı şüpheli eşyalar için tara", TimeSpan.FromHours(1)));
			}
			else if (staff is Waiter)
			{
				tasks.Add(StaffTask.CreateDrinkServiceTask(null));
				tasks.Add(StaffTask.CreateFoodServiceTask());
				tasks.Add(StaffTask.CreateSpecialTask("İçki Stokla", "Bar için içki stokla", TimeSpan.FromHours(2)));
				tasks.Add(StaffTask.CreateCleanupTask());
			}
			else if (staff is Musician)
			{
				tasks.Add(StaffTask.CreateMusicPerformanceTask());
				tasks.Add(StaffTask.CreateSpecialTask("Özel Şarkı", "Müşteri isteği üzerine özel şarkı", TimeSpan.FromHours(1)));
				tasks.Add(StaffTask.CreateSpecialTask("Ekipman Bakımı", "Müzik ekipmanlarının bakımını yap", TimeSpan.FromHours(2)));
				tasks.Add(StaffTask.CreateSpecialTask("Repertuar Geliştir", "Yeni şarkılar ekle", TimeSpan.FromHours(4)));
			}
			else if (staff is Cook)
			{
				tasks.Add(StaffTask.CreateFoodPreparationTask());
				tasks.Add(StaffTask.CreateInventoryTask("Yemek malzemeleri"));
				tasks.Add(StaffTask.CreateSpecialTask("Özel Menü Hazırla", "VIP müşteriler için özel menü", TimeSpan.FromHours(3)));
				tasks.Add(StaffTask.CreateCleanupTask());
			}
			else if (staff is IllegalFloorStaff)
			{
				tasks.Add(StaffTask.CreateIllegalActivityTask("kumar"));
				tasks.Add(StaffTask.CreateIllegalActivityTask("şantaj"));
				tasks.Add(StaffTask.CreateIllegalActivityTask("bilgi toplama"));
				tasks.Add(StaffTask.CreateSpecialTask("Baskını Atalt", "Olası baskını atlat", TimeSpan.FromHours(1)));
			}
			
			return tasks;
		}
		
		/// <summary>
		/// Görevi personele ata
		/// </summary>
		private void AssignTaskToStaff(StaffMember staff, StaffTask task)
		{
			if (staff == null || task == null) return;
			
			try
			{
				// Mevcut görevi iptal et
				if (staff.CurrentTask != null)
				{
					staff.CurrentTask.FailTask("Yeni görev nedeniyle iptal edildi");
				}
				
				// Görevi personele ata
				if (staff.AssignTask(task))
				{
					GD.Print($"{staff.FullName}'e '{task.Name}' görevi atandı!");
					
					// Geçerli oyun zamanıyla görevi başlat
					if (gameManager?.Time != null)
					{
						task.StartTask(gameManager.Time.CurrentTime);
					}
					
					// Başarı mesajı göster
					ShowSuccessMessage($"{staff.FullName}'e '{task.Name}' görevi atandı!");
					
					// Detayları güncelle
					ShowStaffDetails(staff);
				}
				else
				{
					GD.Print($"{task.Name} görevi {staff.FullName}'e atanamadı!");
					ShowErrorMessage($"{task.Name} görevi atanamadı. Personel başka bir görevde olabilir.");
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"AssignTaskToStaff hatası: {ex.Message}");
				ShowErrorMessage("Görev atama sırasında bir hata oluştu.");
			}
		}
		
		/// <summary>
		/// Görev detaylarını göster
		/// </summary>
		private void ShowTaskDetails(StaffTask task)
		{
			if (task == null) return;
			
			try
			{
				// Görev detay diyalogu göster
				AcceptDialog dialog = new AcceptDialog();
				dialog.Title = "Görev Detayları";
				
				// Dialog içeriğini oluştur
				VBoxContainer content = new VBoxContainer();
				content.CustomMinimumSize = new Vector2(350, 0);
				
				// Görev adı
				Label nameLabel = new Label();
				nameLabel.Text = task.Name;
				nameLabel.AddThemeFontSizeOverride("font_size", 16);
				nameLabel.AddThemeColorOverride("font_color", Colors.White);
				content.AddChild(nameLabel);
				
				// Görev açıklaması
				Label descLabel = new Label();
				descLabel.Text = task.Description;
				descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
				content.AddChild(descLabel);
				
				// Ayırıcı
				HSeparator separator = new HSeparator();
				content.AddChild(separator);
				
				// Görev durumu
				Label statusLabel = new Label();
				string statusText = GetTaskStatusText(task.Status);
				Color statusColor = GetTaskStatusColor(task.Status);
				statusLabel.Text = $"Durum: {statusText}";
				statusLabel.AddThemeColorOverride("font_color", statusColor);
				content.AddChild(statusLabel);
				
				// İlerleme
				Label progressLabel = new Label();
				progressLabel.Text = $"İlerleme: %{task.Progress * 100:F0}";
				content.AddChild(progressLabel);
				
				// İlerleme çubuğu
				ProgressBar progressBar = new ProgressBar();
				progressBar.Value = task.Progress * 100;
				progressBar.MinValue = 0;
				progressBar.MaxValue = 100;
				content.AddChild(progressBar);
				
				// Başlangıç/bitiş zamanları
				if (task.StartTime != DateTime.MinValue)
				{
					Label timeLabel = new Label();
					timeLabel.Text = $"Başlangıç: {task.StartTime.ToShortTimeString()}";
					content.AddChild(timeLabel);
				}
				
				if (task.EndTime != DateTime.MaxValue)
				{
					Label endLabel = new Label();
					endLabel.Text = $"Tahmini Bitiş: {task.EndTime.ToShortTimeString()}";
					content.AddChild(endLabel);
				}
				
				// İptal et butonu ekle (duruma göre)
				if (task.Status == StaffTask.TaskStatus.Pending || task.Status == StaffTask.TaskStatus.InProgress)
				{
					Button cancelTaskButton = new Button();
					cancelTaskButton.Text = "Görevi İptal Et";
					cancelTaskButton.Pressed += () => {
						if (selectedStaff != null && selectedStaff.CurrentTask == task)
						{
							task.FailTask("Kullanıcı tarafından iptal edildi");
							selectedStaff.CurrentTask = null;
							ShowStaffDetails(selectedStaff);
							dialog.Hide();
						}
					};
					content.AddChild(cancelTaskButton);
				}
				
				// Dialog'a içeriği ekle
				dialog.DialogText = ""; // Boş metin ayarla
				dialog.AddChild(content);
				
				// Dialog'u göster
				AddChild(dialog);
				dialog.PopupCentered();
			}
			catch (Exception ex)
			{
				GD.PrintErr($"ShowTaskDetails hatası: {ex.Message}");
				ShowErrorMessage("Görev detayları gösterilirken bir hata oluştu.");
			}
		}
		
		/// <summary>
		/// Görev durumunun metin karşılığını al
		/// </summary>
		private string GetTaskStatusText(StaffTask.TaskStatus status)
		{
			switch (status)
			{
				case StaffTask.TaskStatus.Pending:
					return "Beklemede";
				case StaffTask.TaskStatus.InProgress:
					return "Devam Ediyor";
				case StaffTask.TaskStatus.Completed:
					return "Tamamlandı";
				case StaffTask.TaskStatus.Failed:
					return "Başarısız";
				default:
					return "Bilinmiyor";
			}
		}
		
		/// <summary>
		/// Görev durumunun renk karşılığını al
		/// </summary>
		private Color GetTaskStatusColor(StaffTask.TaskStatus status)
		{
			switch (status)
			{
				case StaffTask.TaskStatus.Pending:
					return Colors.LightBlue;
				case StaffTask.TaskStatus.InProgress:
					return Colors.YellowGreen;
				case StaffTask.TaskStatus.Completed:
					return Colors.Green;
				case StaffTask.TaskStatus.Failed:
					return Colors.Red;
				default:
					return Colors.White;
			}
		}
		
		#endregion
		
		#region UI Yardımcı Metodları
		
		/// <summary>
		/// Hata mesajı göster
		/// </summary>
		private void ShowErrorMessage(string message)
		{
			AcceptDialog dialog = new AcceptDialog();
			dialog.Title = "Hata";
			dialog.DialogText = message;
			dialog.AddThemeColorOverride("font_color", Colors.White);
			dialog.AddThemeColorOverride("font_color_hover", Colors.White);
			
			// Dialog'a stil ayarları (kırmızı)
			dialog.GetOkButton().AddThemeColorOverride("font_color", Colors.White);
			dialog.GetOkButton().AddThemeColorOverride("font_color_hover", Colors.White);
			
			AddChild(dialog);
			dialog.PopupCentered();
		}
		
		/// <summary>
		/// Başarı mesajı göster
		/// </summary>
		private void ShowSuccessMessage(string message)
		{
			AcceptDialog dialog = new AcceptDialog();
			dialog.Title = "Başarılı";
			dialog.DialogText = message;
			dialog.AddThemeColorOverride("font_color", Colors.White);
			dialog.AddThemeColorOverride("font_color_hover", Colors.White);
			
			// Dialog'a stil ayarları (yeşil)
			dialog.GetOkButton().AddThemeColorOverride("font_color", Colors.White);
			dialog.GetOkButton().AddThemeColorOverride("font_color_hover", Colors.White);
			
			AddChild(dialog);
			dialog.PopupCentered();
		}
		
		/// <summary>
		/// Onay mesajı göster
		/// </summary>
		private void ShowConfirmDialog(string title, string message, Action onConfirm)
		{
			ConfirmationDialog dialog = new ConfirmationDialog();
			dialog.Title = title;
			dialog.DialogText = message;
			dialog.Confirmed += onConfirm;
			
			AddChild(dialog);
			dialog.PopupCentered();
		}
		
		#endregion
	}
}
