// src/Furniture/FurnitureManager.cs
using Godot;
using System;
using System.Collections.Generic;
using PavyonTycoon.Core;
using System.Linq;

namespace PavyonTycoon.Furniture
{
	public partial class FurnitureManager : Node
	{
		// Mobilya koleksiyonları
		private Dictionary<string, FurnitureData> furnitureDatabase = new Dictionary<string, FurnitureData>();
		private List<FurnitureInstance> placedFurniture = new List<FurnitureInstance>();
		
		// Yerleştirme modu değişkenleri
		private bool placementModeActive = false;
		private FurnitureData currentFurnitureToBePlaced = null;
		private FurnitureInstance previewInstance = null;
		
		// Node referansları
		private Node2D furnitureContainer;
		private Node2D previewContainer;
		
		// Mobilya kategorileri
		public enum FurnitureCategory
		{
			Table,
			Chair,
			ConsTable,
			BarEquipment,
			Decoration,
			LightingEquipment,
			FloorCovering,
			Entertainment
		}
		
		public override void _Ready()
		{
			// Node referanslarını al
			var gameNode = GetTree().Root.GetNode("Main");
			furnitureContainer = gameNode.GetNodeOrNull<Node2D>("FurnitureContainer");
			previewContainer = gameNode.GetNodeOrNull<Node2D>("PreviewContainer");
			
			if (furnitureContainer == null)
			{
				GD.PrintErr("FurnitureManager: FurnitureContainer node bulunamadı!");
				furnitureContainer = new Node2D();
				gameNode.AddChild(furnitureContainer);
				furnitureContainer.Name = "FurnitureContainer";
			}
			
			if (previewContainer == null)
			{
				previewContainer = new Node2D();
				gameNode.AddChild(previewContainer);
				previewContainer.Name = "PreviewContainer";
			}
			
			// Veritabanını yükle
			LoadFurnitureDatabase();
			
			// Gün sonu sinyalini yakalama
			var timeManager = GetNode<TimeManager>("/root/Main/GameManager/TimeManager");
			if (timeManager != null)
			{
				timeManager.Connect(TimeManager.SignalName.DayEnded, Callable.From(ProcessDayEnd));
			}
			
			GD.Print("🪑 Mobilya sistemi başlatıldı");
		}
		
		// Gün sonu işlemleri
		private void ProcessDayEnd(int day)
		{
			// Bakım maliyetlerini hesapla
			float upkeepCost = CalculateDailyUpkeepCost();
			
			// Ekonomiye ekle
			var economy = GetNode<EconomyManager>("/root/Main/GameManager/EconomyManager");
			if (economy != null && upkeepCost > 0)
			{
				economy.AddExpense(upkeepCost, 
					EconomyManager.ExpenseCategory.Maintenance, 
					"Günlük mobilya bakım maliyeti");
			}
			
			GD.Print($"📋 Mobilya günlük bakım maliyeti: {upkeepCost}₺");
		}
		
		public override void _Input(InputEvent @event)
		{
			if (!placementModeActive || previewInstance == null)
				return;
				
			if (@event is InputEventMouseMotion mouseMotion)
			{
				// Mobilya önizlemesini mouse konumuna taşı
				Vector2 position = GetGlobalMousePosition();
				previewInstance.GlobalPosition = new Vector2(
					Mathf.Round(position.X / 10) * 10,  // 10 birimlik grid'e snap
					Mathf.Round(position.Y / 10) * 10
				);
			}
			else if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
			{
				if (mouseButton.ButtonIndex == MouseButton.Left)
				{
					// Sol tıklama ile mobilyayı yerleştir
					PlaceFurnitureAtPreviewPosition();
				}
				else if (mouseButton.ButtonIndex == MouseButton.Right)
				{
					// Sağ tıklama ile yerleştirme modundan çık
					CancelPlacementMode();
				}
			}
		}
		
		// Mobilya veritabanını yükle
		private void LoadFurnitureDatabase()
		{
			// TODO: İleride JSON dosyasından yüklenecek
			// Şimdilik örnek mobilyaları direkt kodda tanımlayalım
			
			// Masalar
			AddFurnitureToDatabase(new FurnitureData
			{
				Id = "table_basic",
				Name = "Standart Masa",
				Category = FurnitureCategory.Table,
				Cost = 150,
				UpkeepCost = 5,
				AtmosphereBonus = 1,
				IncomeMultiplier = 1.0f,
				TexturePath = "res://assets/sprites/furniture/table_basic.png",
				Description = "Temel bir masa. Müşteriler için yeterli ancak lüks değil."
			});
			
			AddFurnitureToDatabase(new FurnitureData
			{
				Id = "table_vip",
				Name = "VIP Masa",
				Category = FurnitureCategory.Table,
				Cost = 500,
				UpkeepCost = 15,
				AtmosphereBonus = 5,
				IncomeMultiplier = 1.4f,
				TexturePath = "res://assets/sprites/furniture/table_vip.png",
				Description = "Lüks bir masa. Zengin müşterileri çeker ve daha fazla harcama yapmalarını sağlar."
			});
			
			// Kons Masaları
			AddFurnitureToDatabase(new FurnitureData
			{
				Id = "cons_table_basic",
				Name = "Standart Kons Masası",
				Category = FurnitureCategory.ConsTable,
				Cost = 300,
				UpkeepCost = 10,
				AtmosphereBonus = 2,
				IncomeMultiplier = 1.2f,
				TexturePath = "res://assets/sprites/furniture/cons_table_basic.png",
				Description = "Kons hizmeti için temel bir masa."
			});
			
			AddFurnitureToDatabase(new FurnitureData
			{
				Id = "cons_table_deluxe",
				Name = "Deluxe Kons Masası",
				Category = FurnitureCategory.ConsTable,
				Cost = 800,
				UpkeepCost = 25,
				AtmosphereBonus = 8,
				IncomeMultiplier = 1.7f,
				TexturePath = "res://assets/sprites/furniture/cons_table_deluxe.png",
				Description = "Lüks kons masası. Zengin müşteriler burada saatlerce kalır ve çok para harcar."
			});
			
			// Bar Ekipmanları
			AddFurnitureToDatabase(new FurnitureData
			{
				Id = "bar_counter",
				Name = "Bar Tezgahı",
				Category = FurnitureCategory.BarEquipment,
				Cost = 1000,
				UpkeepCost = 20,
				AtmosphereBonus = 5,
				IncomeMultiplier = 1.3f,
				TexturePath = "res://assets/sprites/furniture/bar_counter.png",
				Description = "Temel bar tezgahı. İçeceklerin hazırlanıp servis edildiği yer."
			});
			
			// Aydınlatma
			AddFurnitureToDatabase(new FurnitureData
			{
				Id = "light_neon",
				Name = "Neon Aydınlatma",
				Category = FurnitureCategory.LightingEquipment,
				Cost = 200,
				UpkeepCost = 8,
				AtmosphereBonus = 3,
				IncomeMultiplier = 1.1f,
				TexturePath = "res://assets/sprites/furniture/light_neon.png",
				Description = "Renkli neon ışıklar. Otantik pavyon atmosferi yaratır."
			});
			
			GD.Print($"📋 {furnitureDatabase.Count} mobilya veritabanına yüklendi");
		}
		
		private void AddFurnitureToDatabase(FurnitureData data)
		{
			if (furnitureDatabase.ContainsKey(data.Id))
			{
				GD.PrintErr($"FurnitureManager: Bu ID'ye sahip mobilya zaten var: {data.Id}");
				return;
			}
			
			furnitureDatabase[data.Id] = data;
		}
		
		// GetFurnitureByID metodu - FurnitureUI için gerekli
		public FurnitureData GetFurnitureByID(string id)
		{
			if (furnitureDatabase.ContainsKey(id))
			{
				return furnitureDatabase[id];
			}
			
			return null;
		}
		
		// Mobilya yerleştirme modunu başlat
		public void StartPlacementMode(string furnitureId)
		{
			if (!furnitureDatabase.ContainsKey(furnitureId))
			{
				GD.PrintErr($"FurnitureManager: Bilinmeyen mobilya ID'si: {furnitureId}");
				return;
			}
			
			// Aktif yerleştirme modunu temizle
			CancelPlacementMode();
			
			// Yeni yerleştirme modunu başlat
			currentFurnitureToBePlaced = furnitureDatabase[furnitureId];
			placementModeActive = true;
			
			// Önizleme örneğini oluştur
			previewInstance = CreateFurnitureInstance(currentFurnitureToBePlaced);
			previewInstance.Modulate = new Color(1, 1, 1, 0.5f); // Yarı saydam
			previewContainer.AddChild(previewInstance);
			
			GD.Print($"🪑 Yerleştirme modu başlatıldı: {currentFurnitureToBePlaced.Name}");
			
			// Signal gönder
			EmitSignal(SignalName.PlacementModeStarted, furnitureId);
		}
		
		// Yerleştirme modunu iptal et
		public void CancelPlacementMode()
		{
			if (previewInstance != null)
			{
				previewInstance.QueueFree();
				previewInstance = null;
			}
			
			placementModeActive = false;
			currentFurnitureToBePlaced = null;
			
			// Signal gönder
			EmitSignal(SignalName.PlacementModeCanceled);
		}
		
		// Önizleme konumuna mobilyayı yerleştir
		private void PlaceFurnitureAtPreviewPosition()
		{
			if (previewInstance == null || currentFurnitureToBePlaced == null)
				return;
				
			// Önce mobilya için para kontrolü yap
			var economy = GetNode<EconomyManager>("/root/Main/GameManager/EconomyManager");
			if (economy != null && economy.Money < currentFurnitureToBePlaced.Cost)
			{
				GD.Print("❌ Yeterli para yok!");
				EmitSignal(SignalName.PlacementFailed, "not_enough_money");
				return;
			}
			
			// Çakışma kontrolü yap (ileride geliştirilecek)
			// TODO: Mobilyaların çakışmasını engelle
			
			// Yeni mobilya örneği oluştur
			FurnitureInstance newFurniture = CreateFurnitureInstance(currentFurnitureToBePlaced);
			newFurniture.GlobalPosition = previewInstance.GlobalPosition;
			newFurniture.SetId(GenerateUniqueId());
			
			// Mobilyayı sahneye ekle
			furnitureContainer.AddChild(newFurniture);
			placedFurniture.Add(newFurniture);
			
			// Para harcama işlemini gerçekleştir
			if (economy != null)
			{
				economy.AddExpense(currentFurnitureToBePlaced.Cost, 
					EconomyManager.ExpenseCategory.Furnishing, 
					$"{currentFurnitureToBePlaced.Name} satın alındı");
			}
			
			GD.Print($"✅ Mobilya yerleştirildi: {currentFurnitureToBePlaced.Name}");
			
			// Signal gönder
			EmitSignal(SignalName.FurniturePlaced, newFurniture.InstanceId, currentFurnitureToBePlaced.Id);
			
			// Yerleştirme modunu sürdür (yeni bir mobilya daha yerleştirebilmek için)
			previewInstance.GlobalPosition = GetGlobalMousePosition();
		}
		
		// Yeni mobilya örneği oluştur
		private FurnitureInstance CreateFurnitureInstance(FurnitureData data)
		{
			FurnitureInstance instance = new FurnitureInstance();
			instance.Initialize(data);
			return instance;
		}
		
		// Benzersiz ID oluştur
		private string GenerateUniqueId()
		{
			return Guid.NewGuid().ToString();
		}
		
		// Yerleştirilmiş mobilyayı kaldır
		public void RemoveFurniture(string instanceId)
		{
			FurnitureInstance furnitureToRemove = null;
			
			foreach (var furniture in placedFurniture)
			{
				if (furniture.InstanceId == instanceId)
				{
					furnitureToRemove = furniture;
					break;
				}
			}
			
			if (furnitureToRemove != null)
			{
				// Kaldırmadan önce satış geliri ekle (mobilya değerinin %50'si)
				var data = furnitureToRemove.FurnitureData;
				float resaleValue = data.Cost * 0.5f;
				
				var economy = GetNode<EconomyManager>("/root/Main/GameManager/EconomyManager");
				if (economy != null)
				{
					economy.AddIncome(resaleValue, 
						EconomyManager.IncomeCategory.Other, 
						$"{data.Name} satıldı");
				}
				
				// Listeden ve sahneden kaldır
				placedFurniture.Remove(furnitureToRemove);
				furnitureToRemove.QueueFree();
				
				GD.Print($"🗑️ Mobilya kaldırıldı: {data.Name}");
				
				// Signal gönder
				EmitSignal(SignalName.FurnitureRemoved, instanceId, data.Id);
			}
			else
			{
				GD.PrintErr($"FurnitureManager: Kaldırılacak mobilya bulunamadı: {instanceId}");
			}
		}
		
		// Belirli kategorideki tüm mobilyalar
		public Dictionary<string, FurnitureData> GetFurnitureByCategory(FurnitureCategory category)
		{
			Dictionary<string, FurnitureData> result = new Dictionary<string, FurnitureData>();
			
			foreach (var pair in furnitureDatabase)
			{
				if (pair.Value.Category == category)
				{
					result.Add(pair.Key, pair.Value);
				}
			}
			
			return result;
		}
		
		// Günlük bakım maliyetlerini hesapla
		public float CalculateDailyUpkeepCost()
		{
			float totalUpkeep = 0f;
			
			foreach (var furniture in placedFurniture)
			{
				totalUpkeep += furniture.FurnitureData.UpkeepCost;
			}
			
			return totalUpkeep;
		}
		
		// Tüm mobilyaların atmosfer bonusunu hesapla
		public float CalculateTotalAtmosphereBonus()
		{
			float totalBonus = 0f;
			
			foreach (var furniture in placedFurniture)
			{
				totalBonus += furniture.FurnitureData.AtmosphereBonus;
			}
			
			return totalBonus;
		}
		
		// Müşteriler için uygun mobilya konumu bul
		public Vector2 GetAvailableFurniturePosition(string[] preferredTypes)
		{
			// Tercih edilen mobilya türlerini kontrol et
			foreach (var furniture in placedFurniture)
			{
				if (preferredTypes != null && preferredTypes.Contains(furniture.FurnitureData.Id))
				{
					// Şimdilik basit bir yaklaşım - daha sonra gerçek "müşteri oturuyor"
					// durumu takip edilecek
					return furniture.GlobalPosition;
				}
			}
			
			// Hiçbir tercih edilebilir mobilya yoksa, rastgele bir tanesi döndür
			if (placedFurniture.Count > 0)
			{
				int randomIndex = GD.RandRange(0, placedFurniture.Count - 1);
				return placedFurniture[randomIndex].GlobalPosition;
			}
			
			// Hiç mobilya yoksa, rastgele bir konum döndür
			return new Vector2(GD.RandRange(100, 500), GD.RandRange(100, 400));
		}
		
		// Signal tanımlamaları
		[Signal] public delegate void PlacementModeStartedEventHandler(string furnitureId);
		[Signal] public delegate void PlacementModeCanceledEventHandler();
		[Signal] public delegate void PlacementFailedEventHandler(string reason);
		[Signal] public delegate void FurniturePlacedEventHandler(string instanceId, string furnitureId);
		[Signal] public delegate void FurnitureRemovedEventHandler(string instanceId, string furnitureId);
	}
}
