// src/Furniture/FurnitureData.cs
using Godot;
using System;
using static PavyonTycoon.Furniture.FurnitureManager;

namespace PavyonTycoon.Furniture
{
	public class FurnitureData
	{
		// Temel özellikler
		public string Id { get; set; }
		public string Name { get; set; }
		public FurnitureCategory Category { get; set; }
		public float Cost { get; set; }
		public float UpkeepCost { get; set; }
		public string Description { get; set; }
		
		// Oynanış etkileri
		public float AtmosphereBonus { get; set; }
		public float IncomeMultiplier { get; set; }
		
		// Görsel veriler
		public string TexturePath { get; set; }
		
		// Yükseltme seçenekleri (ileride uygulanacak)
		public string[] PossibleUpgrades { get; set; }
		
		// Kolizyon verisi (ileride kullanılacak)
		public Vector2 CollisionSize { get; set; } = new Vector2(1, 1);
	}
}
