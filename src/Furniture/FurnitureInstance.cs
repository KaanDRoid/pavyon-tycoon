// src/Furniture/FurnitureInstance.cs
using Godot;
using System;

namespace PavyonTycoon.Furniture
{
	public partial class FurnitureInstance : Node2D
	{
		// Benzersiz örnek kimliği
		public string InstanceId { get; private set; }
		
		// Mobilya verileri
		public FurnitureData FurnitureData { get; private set; }
		
		// Görsel bileşenler
		private Sprite2D sprite;
		
		// Durumlar
		private bool isHighlighted = false;
		
		public override void _Ready()
		{
			// Görsel bileşenleri oluştur
			sprite = new Sprite2D();
			AddChild(sprite);
			
			// Tıklanabilirlik için gerekli
			MouseFilter = MouseFilterEnum.Stop;
		}
		
		public void Initialize(FurnitureData data)
		{
			FurnitureData = data;
			
			// Sprite'ı ayarla
			if (sprite != null && !string.IsNullOrEmpty(data.TexturePath))
			{
				Texture2D texture = ResourceLoader.Load<Texture2D>(data.TexturePath);
				if (texture != null)
				{
					sprite.Texture = texture;
				}
				else
				{
					// Texture yoksa default kare kullan
					sprite.Texture = GenerateDefaultTexture();
					GD.PrintErr($"FurnitureInstance: Texture yüklenemedi: {data.TexturePath}");
				}
			}
			
			// Tooltip ayarla
			TooltipText = $"{data.Name}\n{data.Description}\nFiyat: {data.Cost}₺\nBakım: {data.UpkeepCost}₺/gün";
		}
		
		// Benzersiz ID belirle
		public void SetId(string id)
		{
			InstanceId = id;
		}
		
		// Mobilyayı vurgula
		public void SetHighlighted(bool highlight)
		{
			isHighlighted = highlight;
			
			if (highlight)
			{
				sprite.Modulate = new Color(1.2f, 1.2f, 0.8f);
			}
			else
			{
				sprite.Modulate = new Color(1, 1, 1);
			}
		}
		
		// Default texture oluştur (gerçek assetler olmadığında)
		private Texture2D GenerateDefaultTexture()
		{
			// Basit bir renkli kare oluştur
			var image = Image.Create(64, 64, false, Image.Format.Rgba8);
			
			// Mobilya kategorisine göre renk belirle
			Color color;
			switch (FurnitureData.Category)
			{
				case FurnitureManager.FurnitureCategory.Table:
					color = new Color(0.8f, 0.6f, 0.4f); // Kahverengi
					break;
				case FurnitureManager.FurnitureCategory.ConsTable:
					color = new Color(0.9f, 0.3f, 0.3f); // Kırmızı
					break;
				case FurnitureManager.FurnitureCategory.BarEquipment:
					color = new Color(0.3f, 0.3f, 0.9f); // Mavi
					break;
				case FurnitureManager.FurnitureCategory.LightingEquipment:
					color = new Color(0.9f, 0.9f, 0.3f); // Sarı
					break;
				default:
					color = new Color(0.7f, 0.7f, 0.7f); // Gri
					break;
			}
			
			// Kareyi renklendir
			image.Fill(color);
			
			// Kenar çizgileri ekle
			for (int x = 0; x < 64; x++)
			{
				for (int y = 0; y < 64; y++)
				{
					if (x == 0 || y == 0 || x == 63 || y == 63)
					{
						image.SetPixel(x, y, Colors.Black);
					}
				}
			}
			
			// Texture oluştur
			var texture = ImageTexture.CreateFromImage(image);
			return texture;
		}
		
		// Input olaylarını yakalama - _GuiInputInputEvent yerine _GuiInput olarak düzeltildi
		public override void _GuiInput(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
			{
				if (mouseButton.ButtonIndex == MouseButton.Left)
				{
					// Mobilyayı seç
					EmitSignal(SignalName.FurnitureSelected, InstanceId);
				}
				else if (mouseButton.ButtonIndex == MouseButton.Right)
				{
					// Sağ tıklama menüsü (context menu) göster
					ShowContextMenu();
				}
			}
			else if (@event is InputEventMouseMotion)
			{
				// Fare üzerine geldiğinde vurgula
				if (!isHighlighted)
				{
					SetHighlighted(true);
				}
			}
		}
		
		public override void _MouseExit()
		{
			// Fare üzerinden ayrıldığında vurgulamayı kaldır
			if (isHighlighted)
			{
				SetHighlighted(false);
			}
		}
		
		// Sağ tıklama menüsünü göster
		private void ShowContextMenu()
		{
			PopupMenu menu = new PopupMenu();
			menu.AddItem("Bilgi", 0);
			menu.AddItem("Kaldır", 1);
			
			// İleride uygulanacak özellikler
			if (FurnitureData.PossibleUpgrades != null && FurnitureData.PossibleUpgrades.Length > 0)
			{
				menu.AddSeparator();
				menu.AddItem("Yükselt", 2);
			}
			
			// Popupu göster
			menu.PopupCentered();
			
			// Seçim sinyalini bağla
			menu.IdPressed += (id) => {
				HandleContextMenuSelection((int)id);
			};
			
			AddChild(menu);
		}
		
		// Menü seçimlerini işle
		private void HandleContextMenuSelection(int id)
		{
			switch (id)
			{
				case 0: // Bilgi
					ShowFurnitureInfo();
					break;
				case 1: // Kaldır
					var manager = GetNode<FurnitureManager>("/root/Main/GameManager/FurnitureManager");
					if (manager != null)
					{
						manager.RemoveFurniture(InstanceId);
					}
					break;
				case 2: // Yükselt (ileride uygulanacak)
					GD.Print("Mobilya yükseltme özelliği henüz uygulanmadı");
					break;
			}
		}
		
		// Mobilya bilgilerini göster
		private void ShowFurnitureInfo()
		{
			// İleride daha ayrıntılı bir bilgi penceresi olacak
			GD.Print($"Mobilya Bilgisi: {FurnitureData.Name}");
			GD.Print($"Açıklama: {FurnitureData.Description}");
			GD.Print($"Fiyat: {FurnitureData.Cost}₺");
			GD.Print($"Günlük bakım: {FurnitureData.UpkeepCost}₺");
			GD.Print($"Atmosfer bonusu: +{FurnitureData.AtmosphereBonus}");
			GD.Print($"Gelir çarpanı: x{FurnitureData.IncomeMultiplier}");
		}
		
		// Signal tanımlamaları
		[Signal] public delegate void FurnitureSelectedEventHandler(string instanceId);
	}
}
