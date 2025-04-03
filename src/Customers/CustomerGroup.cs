// src/Customers/CustomerGroup.cs
using Godot;
using System;
using System.Collections.Generic;
using PavyonTycoon.Furniture;

namespace PavyonTycoon.Customers
{
	public partial class CustomerGroup : Node2D
	{
		// Temel özellikler
		public string GroupId { get; private set; }
		public CustomerGroupData GroupData { get; private set; }
		public int GroupSize { get; private set; }
		public float Satisfaction { get; private set; } = 100.0f;
		
		// Durum bilgileri
		public bool IsSitting { get; private set; } = false;
		public bool IsLeaving { get; private set; } = false;
		public string AssignedFurnitureId { get; private set; } = null;
		
		// Takip değişkenleri
		private float timeInPavyon = 0.0f;
		private float timeSinceLastSpending = 0.0f;
		private float spendingInterval = 15.0f; // Her 15 saniyede bir harcama
		private float spendingMultiplier = 1.0f;
		private float totalSpent = 0.0f;
		
		// Görselleştirme bileşenleri
		private Label groupLabel;
		private Sprite2D groupSprite;
		
		// Hareket kontrolleri
		private Vector2 targetPosition;
		private float moveSpeed = 100.0f;
		private readonly float pavyonExitY = 600.0f;
		
		// Ekonomi kategorileri
		private static readonly string[] spendingCategories = new string[] {
			"İçecekler", "Yiyecekler", "Kons Hizmeti", "Bahşiş"
		};
		
		public override void _Ready()
		{
			// Görsel bileşenleri oluştur
			groupSprite = new Sprite2D();
			AddChild(groupSprite);
			
			groupLabel = new Label();
			groupLabel.Position = new Vector2(0, -40);
			groupLabel.HorizontalAlignment = HorizontalAlignment.Center;
			AddChild(groupLabel);
			
			// Benzersiz ID oluştur
			GroupId = Guid.NewGuid().ToString();
		}
		
		public void Initialize(CustomerGroupData data, int size, Vector2 startPosition)
		{
			GroupData = data;
			GroupSize = size;
			Position = startPosition;
			
			// Başlangıçta rastgele bir hedef belirle
			SetRandomTargetPosition();
			
			// Grup etiketini güncelle
			UpdateGroupLabel();
			
			// Sprite'ı yapılandır (gerçek texture olmadığı için şimdilik renk bloğu kullan)
			groupSprite.Texture = GenerateGroupTexture();
		}
		
		public override void _Process(double delta)
		{
			if (IsLeaving)
			{
				// Çıkışa doğru hareket et
				MoveTowardsExit((float)delta);
				return;
			}
			
			if (!IsSitting)
			{
				// Hedef konuma doğru hareket et
				MoveTowardsTarget((float)delta);
			}
			else
			{
				// Oturuyorsa, düzenli olarak harcama yap ve memnuniyeti güncelle
				ProcessCustomerActivity((float)delta);
			}
		}
		
		private void ProcessCustomerActivity(float delta)
		{
			// Pavyonda geçirilen zamanı artır
			timeInPavyon += delta;
			timeSinceLastSpending += delta;
			
			// Periyodik harcama
			if (timeSinceLastSpending >= spendingInterval)
			{
				MakeRandomSpending();
				timeSinceLastSpending = 0.0f;
				
				// Harcama aralığını rastgele ayarla (10-20 saniye)
				spendingInterval = (float)GD.RandRange(10, 20);
			}
			
			// Memnuniyeti zamanla azalt
			DecreaseCustomerSatisfaction(delta);
			
			// Ayrılma zamanı geldi mi kontrol et
			CheckIfShouldLeave();
		}
		
		private void MakeRandomSpending()
		{
			// Rastgele bir harcama kategorisi seç
			string category = spendingCategories[GD.RandRange(0, spendingCategories.Length - 1)];
			
			// Harcama miktarını hesapla (kişi başı harcama gücü * çarpan * grup boyutu * rastgele faktör)
			float baseAmount = GroupData.SpendingPower * 0.1f; // Her harcamada, saatlik harcamanın %10'u
			float randomFactor = (float)GD.RandRange(0.7, 1.3); // %70 - %130 arası rastgele değişim
			float amount = baseAmount * spendingMultiplier * GroupSize * randomFactor;
			
			// Toplam harcamayı güncelle
			totalSpent += amount;
			
			// Memnuniyeti biraz arttır
			Satisfaction = Mathf.Min(Satisfaction + 2.0f, 100.0f);
			
			// Signal gönder
			EmitSignal(SignalName.CustomerGroupSpent, GroupId, amount, category);
			
			// Label'ı güncelle
			UpdateGroupLabel();
		}
		
		private void DecreaseCustomerSatisfaction(float delta)
		{
			// Zamanla azalan memnuniyet (saatte yaklaşık %10)
			float decreaseRate = 10.0f / 3600.0f; // %10 / saat
			Satisfaction -= decreaseRate * 100.0f * delta;
			
			// Minimum %0 memnuniyet
			Satisfaction = Mathf.Max(Satisfaction, 0.0f);
			
			// Her 10 saniyede bir label'ı güncelle
			if (Mathf.FloorToInt(timeInPavyon) % 10 == 0)
			{
				UpdateGroupLabel();
			}
		}
		
		private void CheckIfShouldLeave()
		{
			// Memnuniyet çok düşükse veya yeterince uzun süre kaldıysa ayrıl
			float stayHours = GroupData.StayDuration * (1.0f + (Satisfaction / 100.0f)); // Memnunsa daha uzun kal
			float maxStayTime = stayHours * 3600.0f; // Saat -> saniye
			
			if (timeInPavyon >= maxStayTime)
			{
				StartLeaving("Normal ayrılma");
			}
		}
		
		public void StartLeaving(string reason)
		{
			if (IsLeaving)
				return;
				
			IsLeaving = true;
			IsSitting = false;
			AssignedFurnitureId = null;
			
			// Hedefi çıkışa ayarla
			targetPosition = new Vector2(Position.X, pavyonExitY);
			
			GD.Print($"👥 '{GroupData.Name}' ayrılmaya başladı: {reason}");
		}
		
		private void MoveTowardsTarget(float delta)
		{
			Vector2 direction = targetPosition - Position;
			float distance = direction.Length();
			
			if (distance < 5.0f)
			{
				// Hedefe ulaşıldı
				if (!IsSitting)
				{
					// Uygun mobilya bul ve otur
					FindAndSitAtFurniture();
				}
			}
			else
			{
				// Hedefe doğru hareket et
				direction = direction.Normalized();
				Position += direction * moveSpeed * delta;
			}
		}
		
		private void MoveTowardsExit(float delta)
		{
			Vector2 direction = targetPosition - Position;
			float distance = direction.Length();
			
			if (distance < 5.0f)
			{
				// Çıkışa ulaşıldı, müşteriyi kaldır
				EmitSignal(SignalName.CustomerGroupLeft, GroupId, GroupSize, totalSpent, "Ayrıldı");
				QueueFree();
			}
			else
			{
				// Çıkışa doğru hareket et
				direction = direction.Normalized();
				Position += direction * moveSpeed * delta;
			}
		}
		
		private void SetRandomTargetPosition()
		{
			// Pavyonun içinde rastgele bir konum belirle
			targetPosition = new Vector2(
				GD.RandRange(100, 500),
				GD.RandRange(100, 300)
			);
		}
		
		private void FindAndSitAtFurniture()
		{
			// Furniture Manager'dan tercih edilen mobilya türlerini al
			var furnitureManager = GetNode<FurnitureManager>("/root/Main/GameManager/FurnitureManager");
			if (furnitureManager != null)
			{
				// Tercih edilen mobilyalardan birinin konumunu al
				targetPosition = furnitureManager.GetAvailableFurniturePosition(GroupData.PreferredFurniture);
				Position = targetPosition;
			}
			
			IsSitting = true;
			GD.Print($"👥 '{GroupData.Name}' oturdu ve sipariş vermeye başladı");
			
			// Memnuniyeti biraz arttır
			Satisfaction = Mathf.Min(Satisfaction + 10.0f, 100.0f);
			
			// Label'ı güncelle
			UpdateGroupLabel();
		}
		
		public void AdjustSpendingMultiplier(float multiplier)
		{
			spendingMultiplier = multiplier;
		}
		
		private void UpdateGroupLabel()
		{
			if (groupLabel != null)
			{
				string satisfactionEmoji = Satisfaction > 80 ? "😄" : (Satisfaction > 50 ? "😐" : "😠");
				groupLabel.Text = $"{GroupData.Name} x{GroupSize}\n{satisfactionEmoji} {Satisfaction:F0}%";
			}
		}
		
		private Texture2D GenerateGroupTexture()
		{
			// Grubun rengini ID'sine göre belirle
			Color color;
			
			switch (GroupData.Id)
			{
				case "regular_low":
					color = new Color(0.2f, 0.7f, 0.2f); // Yeşil
					break;
				case "regular_mid":
					color = new Color(0.2f, 0.5f, 0.7f); // Mavi
					break;
				case "businessman":
					color = new Color(0.7f, 0.5f, 0.2f); // Turuncu
					break;
				case "rich":
					color = new Color(0.7f, 0.2f, 0.7f); // Mor
					break;
				case "vip_blackmailable":
					color = new Color(0.9f, 0.1f, 0.1f); // Kırmızı
					break;
				default:
					color = new Color(0.5f, 0.5f, 0.5f); // Gri
					break;
			}
			
			// Grup boyutuna göre boyutu belirle
			int size = 20 + (GroupSize * 5);
			
			// Basit bir daire oluştur
			var image = Image.Create(size, size, false, Image.Format.Rgba8);
			image.Fill(new Color(0, 0, 0, 0)); // Şeffaf arkaplan
			
			// Daireyi çiz
			int centerX = size / 2;
			int centerY = size / 2;
			int radius = size / 2;
			
			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					float distance = Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2));
					if (distance <= radius)
					{
						image.SetPixel(x, y, color);
					}
				}
			}
			
			// Texture oluştur
			var texture = ImageTexture.CreateFromImage(image);
			return texture;
		}
		
		// Signal tanımlamaları
		[Signal] public delegate void CustomerGroupLeftEventHandler(string groupId, int groupSize, float totalSpent, string reason);
		[Signal] public delegate void CustomerGroupSpentEventHandler(string groupId, float amount, string category);
	}
}
