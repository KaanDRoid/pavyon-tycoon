// src/Staff/StaffTypes/Kons.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Staff
{
	public class Kons : StaffMember
	{
		// Kons-specific properties
		public float CharmBonus { get; set; } = 0f;
		public float DrinkSalesMultiplier { get; set; } = 1.0f;
		public float TipPercentage { get; set; } = 0.5f; // 50% of tips go to the pavyon, 50% to the kons
		public List<object> RegularCustomers { get; private set; } = new List<object>();
		public int MaxRegularCustomers => 3 + Level; // Depends on level
		
		// Tracking stats
		public float TotalDrinkSales { get; private set; } = 0f;
		public float TotalTipsCollected { get; private set; } = 0f;
		public int TotalCustomersServed { get; private set; } = 0;
		
		// Constructor
		public Kons() : base()
		{
			// Set job title
			JobTitle = "Kons";
			
			// Initialize kons-specific attributes
			if (!HasAttribute("Karizma")) SetAttributeValue("Karizma", 4f);
			if (!HasAttribute("Sosyallik")) SetAttributeValue("Sosyallik", 4f);
			if (!HasAttribute("İkna")) SetAttributeValue("İkna", 3f);
			
			// Calculate bonus stats based on attributes
			RecalculateStats();
		}
		
		// Recalculate dependent stats based on attributes
		public void RecalculateStats()
		{
			// Charm bonus affects customer spending
			CharmBonus = (GetAttributeValue("Karizma") / 10f) * 0.3f; // Up to 30% bonus
			
			// Drink sales multiplier based on persuasion
			DrinkSalesMultiplier = 1.0f + (GetAttributeValue("İkna") / 10f) * 0.5f; // Up to 50% more
			
			// Tip percentage based on level and loyalty
			TipPercentage = 0.5f - (Level * 0.05f); // Higher level konslar keep more tips
			TipPercentage = Mathf.Clamp(TipPercentage, 0.2f, 0.5f); // Between 20-50%
		}
		
		// Main revenue-generating method
		public float EntertainCustomer(object customer, float baseDrinkSpend)
		{
			// Skip if no customer
			if (customer == null) return 0f;
			
			// Calculate drink sales
			float salesAmount = baseDrinkSpend * DrinkSalesMultiplier;
			
			// Apply charm bonus for regular customers
			if (RegularCustomers.Contains(customer))
			{
				salesAmount *= (1f + CharmBonus);
			}
			
			// Calculate tips (based on drink spend and kons charm)
			float tipAmount = salesAmount * (GetAttributeValue("Karizma") / 20f); // Up to 50% tip
			
			// Split tips
			float pavyonTips = tipAmount * TipPercentage;
			float konsTips = tipAmount - pavyonTips;
			
			// Update tracking stats
			TotalDrinkSales += salesAmount;
			TotalTipsCollected += konsTips;
			TotalCustomersServed++;
			
			// Check if customer becomes regular
			if (!RegularCustomers.Contains(customer) && RegularCustomers.Count < MaxRegularCustomers)
			{
				// Chance based on charm and service quality
				float regularChance = (GetAttributeValue("Karizma") + GetAttributeValue("Sosyallik")) / 20f;
				
				if (GD.Randf() < regularChance)
				{
					RegularCustomers.Add(customer);
					GD.Print($"{FullName} yeni bir müdavim kazandı!");
				}
			}
			
			// Return the amount for the pavyon (sales + pavyon's share of tips)
			return salesAmount + pavyonTips;
		}
		
		// Override task assignment for kons-specific behavior
		public override bool AssignTask(StaffTask task)
		{
			if (task.Type == "MüşteriEğlendirme" || task.Type == "ÖzelMüşteriAğırlama")
			{
				// Konslar bu görevlerde daha etkili
				return base.AssignTask(task);
			}
			else if (task.Type == "İçecekHazırlama")
			{
				// Konslar içecek hazırlayabilir ama ideal değil
				if (GetAttributeValue("Hız") >= 3.0f)
				{
					return base.AssignTask(task);
				}
				return false;
			}
			else if (task.Type == "MüşteriGözlemleme")
			{
				// Konslar müşteri gözlemleyebilir
				return base.AssignTask(task);
			}
			
			// Diğer görevler için uygun değil
			return false;
		}
		
		// Override task performance calculation
		protected override float CalculateTaskPerformance(StaffTask task)
		{
			float basePerformance = base.CalculateTaskPerformance(task);
			
			// Kons-specific performance boosts
			if (task.Type == "MüşteriEğlendirme")
			{
				// Konslar müşteri eğlendirmede daha iyi
				basePerformance *= 1.2f;
				
				// Bonus for regular customers
				if (task.TargetEntity != null && RegularCustomers.Contains(task.TargetEntity))
				{
					basePerformance *= 1.3f;
				}
			}
			else if (task.Type == "ÖzelMüşteriAğırlama")
			{
				// VIP müşteriler için ekstra bonus
				basePerformance *= 1.3f;
			}
			
			return Mathf.Clamp(basePerformance, 0f, 10f);
		}
		
		// Override special capabilities
		public override string[] GetSpecialCapabilities()
		{
			List<string> capabilities = new List<string>();
			
			// Basic kons capabilities
			capabilities.Add("Müşteri Eğlendirme");
			capabilities.Add("İçki Satışını Artırma");
			
			// Level-based capabilities
			if (Level >= 2) capabilities.Add("Müdavim Kazandırma");
			if (Level >= 3) capabilities.Add("VIP Müşteri Ağırlama");
			if (Level >= 4) capabilities.Add("Şantaj Materyali Toplama");
			if (Level >= 5) capabilities.Add("Yüksek Statülü Müşterileri Çekme");
			
			return capabilities.ToArray();
		}
		
		// Override clone method
		public override StaffMember Clone()
		{
			Kons clone = new Kons
			{
				FullName = this.FullName,
				JobTitle = this.JobTitle,
				Level = this.Level,
				Salary = this.Salary,
				Loyalty = this.Loyalty,
				CharmBonus = this.CharmBonus,
				DrinkSalesMultiplier = this.DrinkSalesMultiplier,
				TipPercentage = this.TipPercentage
			};
			
			// Copy attributes
			foreach (var attr in this.attributes)
			{
				clone.attributes[attr.Key] = attr.Value;
			}
			
			return clone;
		}
		
		// Override status display to include kons-specific stats
		public override string GetStatusDisplay()
		{
			string status = base.GetStatusDisplay();
			
			// Add kons-specific info
			status += $"\nİçki Satış Çarpanı: +{(DrinkSalesMultiplier - 1f) * 100:F0}%\n";
			status += $"Bahşiş Payı: {(1f - TipPercentage) * 100:F0}%\n";
			status += $"Müdavim Sayısı: {RegularCustomers.Count}/{MaxRegularCustomers}\n";
			
			return status;
		}
	}
}
