// src/UI/Staff/StaffManagementUI.cs
using Godot;
using System;
using System.Collections.Generic;
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
		
		public override void _Ready()
		{
			// UI Referanslarını al
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
			
			jobTypeFilter = GetNode<OptionButton>("TabContainer/StaffListTab/VBoxContainer/FilterContainer/JobTypeFilter");
			sortByOption = GetNode<OptionButton>("TabContainer/StaffListTab/VBoxContainer/FilterContainer/SortByOption");
			showInactiveCheckbox = GetNode<CheckBox>("TabContainer/StaffListTab/VBoxContainer/FilterContainer/ShowInactiveCheckbox");
			
			hirePanel = GetNode<Panel>("HirePanel");
			trainingPanel = GetNode<Panel>("TrainingPanel");
			taskAssignmentPanel = GetNode<Panel>("TaskAssignmentPanel");
			
			// Panelleri başlangıçta gizle
			staffDetailPanel.Visible = false;
			hirePanel.Visible = false;
			trainingPanel.Visible = false;
			taskAssignmentPanel.Visible = false;
			
			// Buton sinyallerini bağla
			trainButton.Pressed += OnTrainButtonPressed;
			promoteButton.Pressed += OnPromoteButtonPressed;
			fireButton.Pressed += OnFireButtonPressed;
			assignTaskButton.Pressed += OnAssignTaskButtonPressed;
			viewTasksButton.Pressed += OnViewTasksButtonPressed;
			hireNewButton.Pressed += OnHireNewButtonPressed;
			
			// Liste sinyallerini bağla
			staffList.ItemSelected += OnStaffSelected;
			
			// Filtre sinyallerini bağla
			jobTypeFilter.ItemSelected += OnFilterChanged;
			sortByOption.ItemSelected += OnFilterChanged;
			showInactiveCheckbox.Toggled += OnShowInactiveToggled;
			
			// Yöneticilere referans al
			staffManager = GetNode<StaffManager>("/root/Main/GameManager/StaffManager");
			
			if (staffManager != null)
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
			else
			{
				GD.PrintErr("StaffManagementUI: StaffManager bulunamadı!");
			}
			
			GD.Print("Personel yönetim arayüzü başlatıldı");
		}
		
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
			
			// Varsayılan olarak "İsme Göre" seçili
			sortByOption.Selected = 0;
		}
		
		// Personel listesini güncelleyen ana metod
		private void RefreshStaffList()
		{
			if (staffManager == null) return;
			
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
				// Temel bilgiler
				string displayText = $"{staff.FullName} (Lvl {staff.Level} {staff.JobTitle})";
				
				// İkonu belirle (her tür için farklı ikon)
				Texture2D icon = GetStaffTypeIcon(staff);
				
				// Listeye ekle
				int index = staffList.AddItem(displayText, icon);
				
				// Metadata olarak personeli sakla
				staffList.SetItemMetadata(index, staff);
				
				// Ek bilgiler ve biçimlendirme
				staffList.SetItemTooltip(index, $"Sadakat: {staff.Loyalty:F0}%\nMaaş: {staff.Salary:F0}₺");
				
				// Düşük sadakat veya diğer sorunlara göre renklendirme
				if (staff.Loyalty < 30f)
				{
					staffList.SetItemCustomFgColor(index, Colors.Red);
				}
				else if (staff.Loyalty < 50f)
				{
					staffList.SetItemCustomFgColor(index, Colors.Orange);
				}
			}
			
			// Seçili personeli güncelle
			if (selectedStaff != null)
			{
				bool stillExists = false;
				
				for (int i = 0; i < staffList.ItemCount; i++)
				{
					var staff = (StaffMember)staffList.GetItemMetadata(i);
					if (staff.FullName == selectedStaff.FullName)
					{
						staffList.Select(i);
						stillExists = true;
						break;
					}
				}
				
				if (!stillExists)
				{
					// Seçili personel artık listede değilse, seçimi temizle
					selectedStaff = null;
					staffDetailPanel.Visible = false;
				}
			}
		}
		
		// Personel listesini filtrele
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
				// TODO: Aktif/inaktif mantığı eklendiğinde bunu güncelle
				
				// Filtreleri geçen personeli listeye ekle
				filtered.Add(staff);
			}
			
			return filtered;
		}
		
		// Personel listesini sırala
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
			}
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
		
		// Personel detaylarını göster
		private void ShowStaffDetails(StaffMember staff)
		{
			if (staff == null)
			{
				staffDetailPanel.Visible = false;
				return;
			}
			
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
			
			// Paneli göster
			staffDetailPanel.Visible = true;
		}
		
		// Personel bilgi başlığı ekle
		private void AddStaffInfoHeader(StaffMember staff)
		{
			var nameLabel = new Label();
			nameLabel.Text = staff.FullName;
			nameLabel.AddThemeColorOverride("font_color", Colors.White);
			nameLabel.AddThemeConstantOverride("font_size", 18);
			nameLabel.AddThemeFontSizeOverride("font_size", 18);
			staffInfoContainer.AddChild(nameLabel);
			
			var titleLabel = new Label();
			titleLabel.Text = $"Seviye {staff.Level} {staff.JobTitle}";
			titleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 1.0f));
			staffInfoContainer.AddChild(titleLabel);
			
			// Ayırıcı
			var separator = new HSeparator();
			staffInfoContainer.AddChild(separator);
		}
		
		// Temel personel bilgilerini ekle
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
			
			// Görev (eğer varsa)
			if (staff.CurrentTask != null)
			{
				AddInfoRow(infoGrid, "Görev:", staff.CurrentTask.Name);
			}
			else
			{
				AddInfoRow(infoGrid, "Görev:", "Boşta");
			}
			
			// Boşluk
			staffInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		// Personel özelliklerini ekle
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
			foreach (var attr in attributes)
			{
				// Özellik rengi (değere göre)
				Color attrColor = GetAttributeColor(attr.Value);
				
				AddInfoRow(attrGrid, $"{attr.Key}:", $"{attr.Value:F1}/10", attrColor);
			}
			
			// Boşluk
			staffInfoContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
		}
		
		// Personel yeteneklerini ekle
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
		
		// Özel personel türü bilgilerini ekle
		private void AddSpecialTypeInfo(StaffMember staff)
		{
			// Personel türüne göre özel bilgiler
			if (staff is Kons kons)
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
			}
			else if (staff is SecurityStaff security)
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
			}
			// Diğer personel türleri için benzer kod ekleyebilirsin
			// (Waiter, Musician, Cook, IllegalFloorStaff)
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
		
		// Sadakat değerine göre renk
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
		
		// Özellik değerine göre renk
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
		
		// Butonları personel durumuna göre güncelle
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
			trainButton.Disabled = false;
			
			// Terfi butonu (maksimum seviyeye ulaşmadıysa aktif)
			promoteButton.Disabled = (staff.Level >= 5);
			
			// İşten çıkarma butonu
			fireButton.Disabled = false;
			
			// Görev atama butonu
			assignTaskButton.Disabled = false;
			
			// Görev görüntüleme butonu (personelin görevi varsa aktif)
			viewTasksButton.Disabled = (staff.CurrentTask == null);
		}
		
		// Buton olay işleyicileri
		
		private void OnTrainButtonPressed()
		{
			if (selectedStaff == null) return;
			
			// Eğitim panelini göster
			ShowTrainingPanel(selectedStaff);
		}
		
		private void OnPromoteButtonPressed()
		{
			if (selectedStaff == null || staffManager == null) return;
			
			// Personeli terfi ettir
			if (staffManager.PromoteStaff(selectedStaff))
			{
				GD.Print($"{selectedStaff.FullName} terfi ettirildi!");
				
				// UI'ı güncelle
				RefreshStaffList();
				ShowStaffDetails(selectedStaff);
			}
			else
			{
				GD.Print($"{selectedStaff.FullName} terfi ettirilemedi!");
				// TODO: Hata mesajı göster
			}
		}
		
		private void OnFireButtonPressed()
		{
			if (selectedStaff == null || staffManager == null) return;
			
			// Onay diyalogu göster
			ConfirmationDialog confirmDialog = new ConfirmationDialog();
			confirmDialog.Title = "İşten Çıkarma Onayı";
			confirmDialog.DialogText = $"{selectedStaff.FullName} adlı personeli işten çıkarmak istediğinize emin misiniz?";
			confirmDialog.Confirmed += OnFireConfirmed;
			
			AddChild(confirmDialog);
			confirmDialog.PopupCentered();
		}
		
		private void OnFireConfirmed()
		{
			if (selectedStaff == null || staffManager == null) return;
			
			// Personeli işten çıkar
			if (staffManager.FireStaff(selectedStaff))
			{
				GD.Print($"{selectedStaff.FullName} işten çıkarıldı!");
				
				var firedStaff = selectedStaff;
				selectedStaff = null;
				
				// UI'ı güncelle
				RefreshStaffList();
				staffDetailPanel.Visible = false;
			}
			else
			{
				GD.Print($"{selectedStaff.FullName} işten çıkarılamadı!");
				// TODO: Hata mesajı göster
			}
		}
		
		private void OnAssignTaskButtonPressed()
		{
			if (selectedStaff == null) return;
			
			// Görev atama panelini göster
			ShowTaskAssignmentPanel(selectedStaff);
		}
		
		private void OnViewTasksButtonPressed()
		{
			if (selectedStaff == null || selectedStaff.CurrentTask == null) return;
			
			// Görev detaylarını göster
			ShowTaskDetails(selectedStaff.CurrentTask);
		}
		
		private void OnHireNewButtonPressed()
		{
			// İşe alım panelini göster
			ShowHiringPanel();
		}
		
		// Liste olayları
		
		private void OnStaffSelected(long index)
		{
			// Personel bilgilerini göster
			var staff = (StaffMember)staffList.GetItemMetadata((int)index);
			ShowStaffDetails(staff);
		}
		
		// Filtre olayları
		
		private void OnFilterChanged(long index)
		{
			RefreshStaffList();
		}
		
		private void OnShowInactiveToggled(bool toggled)
		{
			RefreshStaffList();
		}
		
		// StaffManager olayları
		
		private void OnStaffHired(StaffMember staff)
		{
			RefreshStaffList();
		}
		
		private void OnStaffFired(StaffMember staff)
		{
			// Eğer şu anda gösterilen personel ise paneli kapat
			if (selectedStaff == staff)
			{
				selectedStaff = null;
				staffDetailPanel.Visible = false;
			}
			
			RefreshStaffList();
		}
		
		private void OnStaffAttributeChanged(StaffMember staff, string attributeName, float newValue)
		{
			// Eğer şu anda gösterilen personel ise detayları güncelle
			if (selectedStaff == staff)
			{
				ShowStaffDetails(staff);
			}
			
			RefreshStaffList();
		}
		
		private void OnStaffLoyaltyChanged(StaffMember staff, float newLoyalty)
		{
			// Eğer şu anda gösterilen personel ise detayları güncelle
			if (selectedStaff == staff)
			{
				ShowStaffDetails(staff);
			}
			
			RefreshStaffList();
		}
		
		// Panel gösterme metodları (bu metodlar tasarıma göre düzenlenmeli)
		
		private void ShowHiringPanel()
		{
			if (hirePanel == null || staffManager == null) return;
			
			// İşe alınabilecek personel listesini al
			var availableStaff = staffManager.GetAvailableStaffForHire();
			
			// Panel içeriğini doldur (Panel'in içeriği sahne editöründe oluşturulmuş olmalı)
			var staffListControl = hirePanel.GetNode<ItemList>("VBoxContainer/AvailableStaffList");
			var detailPanel = hirePanel.GetNode<Panel>("VBoxContainer/CandidateDetailPanel");
			var hireButton = hirePanel.GetNode<Button>("VBoxContainer/ButtonContainer/HireButton");
			var cancelButton = hirePanel.GetNode<Button>("VBoxContainer/ButtonContainer/CancelButton");
			
			// Liste içeriğini temizle
			staffListControl.Clear();
			
			// Personel adaylarını listeye ekle
			foreach (var candidate in availableStaff)
			{
				string displayText = $"{candidate.FullName} ({candidate.JobTitle})";
				Texture2D icon = GetStaffTypeIcon(candidate);
				
				int idx = staffListControl.AddItem(displayText, icon);
				staffListControl.SetItemMetadata(idx, candidate);
			}
			
			// Buton olaylarını bağla
			hireButton.Pressed += () => {
				var selectedItems = staffListControl.GetSelectedItems();
				if (selectedItems.Length > 0)
				{
					var selectedCandidate = (StaffMember)staffListControl.GetItemMetadata(selectedItems[0]);
					HireCandidate(selectedCandidate);
				}
			};
			
			cancelButton.Pressed += () => {
				hirePanel.Visible = false;
			};
			
			// Liste seçim olayını bağla
			staffListControl.ItemSelected += (idx) => {
				var candidate = (StaffMember)staffListControl.GetItemMetadata((int)idx);
				ShowCandidateDetails(candidate, detailPanel);
				hireButton.Disabled = false;
			};
			
			// Başlangıçta detay panelini gizle ve işe alma butonunu devre dışı bırak
			detailPanel.Visible = false;
			hireButton.Disabled = true;
			
			// Paneli göster
			hirePanel.Visible = true;
		}
		
		private void ShowCandidateDetails(StaffMember candidate, Panel detailPanel)
		{
			if (candidate == null || detailPanel == null) return;
			
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
			
			// Özellikleri temizle
			foreach (Node child in attributesContainer.GetChildren())
			{
				child.QueueFree();
			}
			
			// Özellikleri listele
			var attributes = candidate.GetAllAttributes();
			foreach (var attr in attributes)
			{
				var attrLabel = new Label();
				attrLabel.Text = $"{attr.Key}: {attr.Value:F1}/10";
				
				// Yüksek değerleri renklendir
				if (attr.Value >= 7f)
					attrLabel.AddThemeColorOverride("font_color", Colors.Green);
				else if (attr.Value >= 5f)
					attrLabel.AddThemeColorOverride("font_color", Colors.YellowGreen);
				
				attributesContainer.AddChild(attrLabel);
			}
		}
		
		private void HireCandidate(StaffMember candidate)
		{
			if (candidate == null || staffManager == null) return;
			
			// Personeli işe al
			var hiredStaff = staffManager.HireStaff(candidate);
			
			if (hiredStaff != null)
			{
				GD.Print($"{hiredStaff.FullName} işe alındı!");
				
				// İşe alım panelini kapat
				hirePanel.Visible = false;
				
				// Listeyi güncelle
				RefreshStaffList();
			}
			else
			{
				GD.Print("Personel işe alınamadı!");
				// TODO: Hata mesajı göster
			}
		}
		
		private void ShowTrainingPanel(StaffMember staff)
		{
			if (trainingPanel == null || staff == null) return;
			
			// Eğitim panelini başlat
			var attributeList = trainingPanel.GetNode<ItemList>("VBoxContainer/AttributeList");
			var costLabel = trainingPanel.GetNode<Label>("VBoxContainer/CostInfoLabel");
			var trainButton = trainingPanel.GetNode<Button>("VBoxContainer/ButtonContainer/TrainButton");
			var cancelButton = trainingPanel.GetNode<Button>("VBoxContainer/ButtonContainer/CancelButton");
			
			// Liste içeriğini temizle
			attributeList.Clear();
			
			// Personelin mevcut özelliklerini listele
			var attributes = staff.GetAllAttributes();
			foreach (var attr in attributes)
			{
				// Maksimum değere ulaşmış özellikleri atlayabiliriz
				if (attr.Value >= 10f) continue;
				
				string displayText = $"{attr.Key}: {attr.Value:F1}/10";
				int idx = attributeList.AddItem(displayText);
				attributeList.SetItemMetadata(idx, attr.Key);
			}
			
			// Eğitim maliyetini göster
			float trainingCost = staffManager.TrainingCostPerLevel;
			costLabel.Text = $"Eğitim Maliyeti: {trainingCost:F0}₺";
			
			// Buton olaylarını bağla
			trainButton.Pressed += () => {
				var selectedItems = attributeList.GetSelectedItems();
				if (selectedItems.Length > 0)
				{
					var attributeName = (string)attributeList.GetItemMetadata(selectedItems[0]);
					TrainStaffAttribute(staff, attributeName);
					trainingPanel.Visible = false;
				}
			};
			
			cancelButton.Pressed += () => {
				trainingPanel.Visible = false;
			};
			
			// Başlangıçta eğitim butonunu devre dışı bırak
			trainButton.Disabled = true;
			
			// Liste seçim olayını bağla
			attributeList.ItemSelected += (idx) => {
				trainButton.Disabled = false;
			};
			
			// Paneli göster
			trainingPanel.Visible = true;
		}
		
		private void TrainStaffAttribute(StaffMember staff, string attributeName)
		{
			if (staff == null || staffManager == null) return;
			
			// Özelliği eğit
			if (staffManager.TrainStaff(staff, attributeName))
			{
				GD.Print($"{staff.FullName}'in {attributeName} özelliği geliştirildi!");
				
				// Detayları güncelle
				ShowStaffDetails(staff);
			}
			else
			{
				GD.Print($"{attributeName} özelliği geliştirilemedi!");
				// TODO: Hata mesajı göster
			}
		}
		
		private void ShowTaskAssignmentPanel(StaffMember staff)
		{
			if (taskAssignmentPanel == null || staff == null) return;
			
			// Görev panelini başlat
			var taskList = taskAssignmentPanel.GetNode<ItemList>("VBoxContainer/TaskList");
			var taskDescLabel = taskAssignmentPanel.GetNode<Label>("VBoxContainer/TaskDescriptionLabel");
			var assignButton = taskAssignmentPanel.GetNode<Button>("VBoxContainer/ButtonContainer/AssignButton");
			var cancelButton = taskAssignmentPanel.GetNode<Button>("VBoxContainer/ButtonContainer/CancelButton");
			
			// Liste içeriğini temizle
			taskList.Clear();
			
			// Uygun görevleri listele (personel türüne göre)
			List<StaffTask> availableTasks = GetAvailableTasksForStaff(staff);
			
			foreach (var task in availableTasks)
			{
				string displayText = task.Name;
				int idx = taskList.AddItem(displayText);
				taskList.SetItemMetadata(idx, task);
			}
			
			// Buton olaylarını bağla
			assignButton.Pressed += () => {
				var selectedItems = taskList.GetSelectedItems();
				if (selectedItems.Length > 0)
				{
					var task = (StaffTask)taskList.GetItemMetadata(selectedItems[0]);
					AssignTaskToStaff(staff, task);
					taskAssignmentPanel.Visible = false;
				}
			};
			
			cancelButton.Pressed += () => {
				taskAssignmentPanel.Visible = false;
			};
			
			// Başlangıçta atama butonunu devre dışı bırak
			assignButton.Disabled = true;
			
			// Liste seçim olayını bağla
			taskList.ItemSelected += (idx) => {
				var task = (StaffTask)taskList.GetItemMetadata((int)idx);
				taskDescLabel.Text = task.Description;
				assignButton.Disabled = false;
			};
			
			// Başlangıçta açıklama etiketini temizle
			taskDescLabel.Text = "";
			
			// Paneli göster
			taskAssignmentPanel.Visible = true;
		}
		
		private List<StaffTask> GetAvailableTasksForStaff(StaffMember staff)
		{
			List<StaffTask> tasks = new List<StaffTask>();
			
			// Personel türüne göre uygun görevleri oluştur
			if (staff is Kons)
			{
				tasks.Add(StaffTask.CreateCustomerInteractionTask(null));
				// Diğer kons görevleri
			}
			else if (staff is SecurityStaff)
			{
				tasks.Add(StaffTask.CreateSecurityTask(Vector2.Zero));
				// Diğer güvenlik görevleri
			}
			else if (staff is Waiter)
			{
				tasks.Add(StaffTask.CreateDrinkServiceTask(null));
				// Diğer garson görevleri
			}
			else if (staff is Musician)
			{
				tasks.Add(StaffTask.CreateMusicPerformanceTask());
				// Diğer müzisyen görevleri
			}
			else if (staff is Cook)
			{
				tasks.Add(StaffTask.CreateFoodPreparationTask());
				// Diğer aşçı görevleri
			}
			else if (staff is IllegalFloorStaff)
			{
				tasks.Add(StaffTask.CreateIllegalActivityTask("kumar"));
				tasks.Add(StaffTask.CreateIllegalActivityTask("şantaj"));
				// Diğer kaçak kat görevleri
			}
			
			return tasks;
		}
		
		private void AssignTaskToStaff(StaffMember staff, StaffTask task)
		{
			if (staff == null || task == null) return;
			
			// Görevi personele ata
			if (staff.AssignTask(task))
			{
				GD.Print($"{staff.FullName}'e '{task.Name}' görevi atandı!");
				
				// Geçerli oyun zamanıyla görevi başlat
				var gameManager = GetNode<GameManager>("/root/Main/GameManager");
				if (gameManager?.Time != null)
				{
					task.StartTask(gameManager.Time.CurrentTime);
				}
				
				// Detayları güncelle
				ShowStaffDetails(staff);
			}
			else
			{
				GD.Print($"{task.Name} görevi {staff.FullName}'e atanamadı!");
				// TODO: Hata mesajı göster
			}
		}
		
		private void ShowTaskDetails(StaffTask task)
		{
			if (task == null) return;
			
			// Görev detay diyalogu göster
			AcceptDialog dialog = new AcceptDialog();
			dialog.Title = "Görev Detayları";
			
			// Dialog içeriğini oluştur
			VBoxContainer content = new VBoxContainer();
			
			// Görev adı
			Label nameLabel = new Label();
			nameLabel.Text = task.Name;
			nameLabel.AddThemeConstantOverride("font_size", 16);
			content.AddChild(nameLabel);
			
			// Görev açıklaması
			Label descLabel = new Label();
			descLabel.Text = task.Description;
			content.AddChild(descLabel);
			
			// Ayırıcı
			HSeparator separator = new HSeparator();
			content.AddChild(separator);
			
			// Görev durumu
			Label statusLabel = new Label();
			statusLabel.Text = $"Durum: {GetTaskStatusText(task.Status)}";
			content.AddChild(statusLabel);
			
			// İlerleme
			Label progressLabel = new Label();
			progressLabel.Text = $"İlerleme: %{task.Progress * 100:F0}";
			content.AddChild(progressLabel);
			
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
			
			// İptal et butonu ekle
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
			
			// Dialog'a içeriği ekle
			dialog.DialogText = ""; // Boş metin ayarla
			dialog.AddChild(content);
			
			// Dialog'u göster
			AddChild(dialog);
			dialog.PopupCentered();
		}
		
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
	}
}
