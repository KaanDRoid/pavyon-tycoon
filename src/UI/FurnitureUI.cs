// src/UI/FurnitureUI.cs
using Godot;
using System;
using System.Collections.Generic;
using PavyonTycoon.Furniture;

namespace PavyonTycoon.UI
{
	public partial class FurnitureUI : Control
	{
		// UI Elemanları
		private ItemList furnitureList;
		private Button cancelButton;
		private TabContainer categoryTabs;
		private Label moneyLabel;
		
		// Kategori mapping
		private Dictionary<FurnitureManager.FurnitureCategory, string> categoryNames = new Dictionary<FurnitureManager.FurnitureCategory, string>
		{
			{ FurnitureManager.FurnitureCategory.Table, "Masalar" },
			{ FurnitureManager.FurnitureCategory.Chair, "Sandalyeler" },
			{ FurnitureManager.FurnitureCategory.ConsTable, "Kons Masaları" },
			{ FurnitureManager.FurnitureCategory.BarEquipment, "Bar Ekipmanları" },
			{ FurnitureManager.FurnitureCategory.Decoration, "Dekorasyon" },
			{ FurnitureManager.FurnitureCategory.LightingEquipment, "Aydınlatma" },
			{ FurnitureManager.FurnitureCategory.FloorCovering, "Zemin Kaplamaları" },
			{ FurnitureManager.FurnitureCategory.Entertainment, "Eğlence Ekipmanları" }
		};
		
		// Manager referansları
		private FurnitureManager furnitureManager;
		private EconomyManager economyManager;
		
		public override void _Ready()
		{
			// UI referanslarını al
			furnitureList = GetNode<ItemList>("CategoryTabs/TabContent/FurnitureList");
			cancelButton = GetNode<Button>("CancelButton");
			categoryTabs = GetNode<TabContainer>("CategoryTabs");
			moneyLabel = GetNode<Label>("MoneyLabel");
			
			// Manager referanslarını al
			furnitureManager = GetNode<FurnitureManager>("/root/Main/GameManager/FurnitureManager");
			economyManager = GetNode<EconomyManager>("/root/Main/GameManager/EconomyManager");
			
			// Sinyalleri bağla
			furnitureList.ItemSelected += OnFurnitureItemSelected;
			cancelButton.Pressed += OnCancelButtonPressed;
			categoryTabs.TabChanged += OnCategoryTabChanged;
			
			if (economyManager != null)
			{
				economyManager.Connect(EconomyManager.SignalName.MoneyChanged, Callable.From(UpdateMoneyLabel));
				UpdateMoneyLabel(economyManager.Money);
			}
			
			// İlk sekmeyi yükle
			LoadFurnitureCategory(FurnitureManager.FurnitureCategory.Table);
		}
		
		private void OnFurnitureItemSelected(long index)
		{
			if (furnitureManager == null)
				return;
				
			string furnitureId = furnitureList.GetItemMetadata((int)index).AsString();
			furnitureManager.StartPlacementMode(furnitureId);
		}
		
		private void OnCancelButtonPressed()
		{
			if (furnitureManager != null)
			{
				furnitureManager.CancelPlacementMode();
			}
		}
		
		private void OnCategoryTabChanged(long tabIndex)
		{
			// Kategori sekmesi değiştiğinde mobilya listesini güncelle
			var categories = Enum.GetValues(typeof(FurnitureManager.FurnitureCategory));
			if (tabIndex >= 0 && tabIndex < categories.Length)
			{
				FurnitureManager.FurnitureCategory category = (FurnitureManager.FurnitureCategory)tabIndex;
				LoadFurnitureCategory(category);
			}
		}
		
		private void LoadFurnitureCategory(FurnitureManager.FurnitureCategory category)
		{
			if (furnitureManager == null || furnitureList == null)
				return;
				
			// Listeyi temizle
			furnitureList.Clear();
			
			// Seçilen kategorideki mobilyaları al
			var furnitureItems = furnitureManager.GetFurnitureByCategory(category);
			
			// Listeye ekle
			foreach (var item in furnitureItems)
			{
				FurnitureData data = item.Value;
				
				// Öğeyi ekle
				int index = furnitureList.AddItem($"{data.Name} - {data.Cost}₺");
				furnitureList.SetItemMetadata(index, data.Id);
				
				// Eğer texture varsa, ikon ekle
				if (!string.IsNullOrEmpty(data.TexturePath))
				{
					var texture = ResourceLoader.Load<Texture2D>(data.TexturePath);
					if (texture != null)
					{
						furnitureList.SetItemIcon(index, texture);
					}
				}
				
				// Eğer yeteri kadar para yoksa, öğeyi devre dışı bırak
				if (economyManager != null && economyManager.Money < data.Cost)
				{
					furnitureList.SetItemDisabled(index, true);
					furnitureList.SetItemCustomFgColor(index, Colors.Gray);
				}
			}
		}
		
		private void UpdateMoneyLabel(float money)
		{
			if (moneyLabel != null)
			{
				moneyLabel.Text = $"Para: {EconomyManager.FormatMoney(money)}";
			}
			
			// Para değiştiğinde öğelerin aktifliğini güncelle
			RefreshItemStates();
		}
		
		private void RefreshItemStates()
		{
			if (furnitureList == null || economyManager == null)
				return;
				
			for (int i = 0; i < furnitureList.ItemCount; i++)
			{
				string furnitureId = furnitureList.GetItemMetadata(i).AsString();
				var furniture = furnitureManager.GetFurnitureByID(furnitureId);
				
				if (furniture != null)
				{
					bool canAfford = economyManager.Money >= furniture.Cost;
					furnitureList.SetItemDisabled(i, !canAfford);
					furnitureList.SetItemCustomFgColor(i, canAfford ? Colors.White : Colors.Gray);
				}
			}
		}
		
		// Mobilya UI'ını göster - "new" anahtar kelimesi ile base sınıfın metodunu gizliyoruz
		public new void Show()
		{
			Visible = true;
			
			// Listeyi güncelle
			var currentCategory = (FurnitureManager.FurnitureCategory)categoryTabs.CurrentTab;
			LoadFurnitureCategory(currentCategory);
		}
		
		// Mobilya UI'ını gizle - "new" anahtar kelimesi ile base sınıfın metodunu gizliyoruz
		public new void Hide()
		{
			Visible = false;
			
			// Yerleştirme modunu iptal et
			if (furnitureManager != null)
			{
				furnitureManager.CancelPlacementMode();
			}
		}
	}
}
