// src/Customers/CustomerGroupData.cs
using Godot;
using System;

namespace PavyonTycoon.Customers
{
	public class CustomerGroupData
	{
		// Temel özellikler
		public string Id { get; set; }
		public string Name { get; set; }
		public Vector2I GroupSize { get; set; } = new Vector2I(1, 4); // Min-Max kişi sayısı
		public string Description { get; set; }
		
		// Müşteri davranışları
		public float SpendingPower { get; set; } = 50.0f; // Temel harcama gücü (kişi başı/saat)
		public float StayDuration { get; set; } = 3.0f; // Ortalama kalış süresi (saat)
		public float LoyaltyChance { get; set; } = 0.3f; // Tekrar gelme olasılığı (0-1)
		
		// Spawn özellikleri
		public int SpawnWeight { get; set; } = 50; // Gelme olasılığı ağırlığı (yüksek değer = daha yaygın)
		public int[] SpawnHours { get; set; } = null; // Eğer null değilse, sadece belirtilen saatlerde spawn olur
		
		// Tercihler
		public string[] PreferredFurniture { get; set; } // Tercih edilen mobilya türleri
		public string[] PreferredDrinks { get; set; } // Tercih edilen içecek türleri
		
		// Özel özellikler
		public bool BlackmailPotential { get; set; } = false; // Şantaj potansiyeli var mı
		public float AggressionLevel { get; set; } = 0.1f; // Kavga çıkarma olasılığı (0-1)
	}
}
