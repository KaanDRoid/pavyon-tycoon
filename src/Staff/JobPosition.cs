// src/Staff/JobPosition.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Staff
{
	public class JobPosition
	{
		// Basic position information
		public string Title { get; set; }
		public string Description { get; set; }
		public float BaseSalary { get; set; }
		public int MaxPositions { get; set; }
		
		// Whether this position is involved in legal activities
		public bool IsLegal { get; set; } = true;
		
		// Attributes required for this position
		public Dictionary<string, float> RequiredAttributes { get; set; } = new Dictionary<string, float>();
		
		// Attributes that can enhance performance in this position
		public Dictionary<string, float> BonusAttributes { get; set; } = new Dictionary<string, float>();
		
		// Types of tasks this position can perform
		public List<string> AllowedTaskTypes { get; set; } = new List<string>();
		
		// Potential bonuses or commission structures
		public bool HasCommission { get; set; } = false;
		public float CommissionPercentage { get; set; } = 0f;
		
		// Level requirements and bonuses
		public Dictionary<int, Dictionary<string, float>> LevelBonuses { get; set; } = 
			new Dictionary<int, Dictionary<string, float>>();
		
		// Working schedule (which hours this position works, 0-23)
		public int[] WorkingHours { get; set; } = { 18, 19, 20, 21, 22, 23, 0, 1, 2, 3, 4, 5 };
		
		// Unlocked job benefits by level
		public Dictionary<int, string[]> LevelBenefits { get; set; } = new Dictionary<int, string[]>();
		
		public JobPosition()
		{
			// Initialize default level bonuses
			LevelBonuses[2] = new Dictionary<string, float>() { { "BaseSalary", 1.1f } };
			LevelBonuses[3] = new Dictionary<string, float>() { { "BaseSalary", 1.2f }, { "Performance", 1.1f } };
			LevelBonuses[4] = new Dictionary<string, float>() { { "BaseSalary", 1.3f }, { "Performance", 1.2f } };
			LevelBonuses[5] = new Dictionary<string, float>() { { "BaseSalary", 1.5f }, { "Performance", 1.3f } };
			
			// Initialize default benefits
			LevelBenefits[2] = new string[] { "Fazla Mesai Primi" };
			LevelBenefits[3] = new string[] { "Fazla Mesai Primi", "Haftalık İzin" };
			LevelBenefits[4] = new string[] { "Fazla Mesai Primi", "Haftalık İzin", "Terfi İmkanı" };
			LevelBenefits[5] = new string[] { "Fazla Mesai Primi", "Haftalık İzin", "Terfi İmkanı", "Ekstra Bonus" };
		}
		
		// Get job title with level indicator
		public string GetFormattedTitle(int level)
		{
			string prefix = "";
			
			switch (level)
			{
				case 1:
					prefix = "Jr. ";
					break;
				case 2:
					prefix = "";  // No prefix for standard level
					break;
				case 3:
					prefix = "Sr. ";
					break;
				case 4:
					prefix = "Baş ";
					break;
				case 5:
					prefix = "Uzman ";
					break;
				default:
					prefix = "";
					break;
			}
			
			return prefix + Title;
		}
		
		// Get minimum acceptable loyalty for higher risk positions
		public float GetMinimumLoyalty()
		{
			if (!IsLegal)
			{
				return 70f; // Illegal positions require high loyalty
			}
			return 40f; // Standard positions
		}
		
		// Calculate adjusted salary based on level
		public float GetSalaryForLevel(int level)
		{
			float multiplier = 1.0f;
			
			// Apply level bonuses if exist
			if (LevelBonuses.ContainsKey(level) && LevelBonuses[level].ContainsKey("BaseSalary"))
			{
				multiplier = LevelBonuses[level]["BaseSalary"];
			}
			
			return BaseSalary * multiplier;
		}
		
		// Get performance multiplier based on level
		public float GetPerformanceMultiplier(int level)
		{
			float multiplier = 1.0f;
			
			// Apply level bonuses if exist
			if (LevelBonuses.ContainsKey(level) && LevelBonuses[level].ContainsKey("Performance"))
			{
				multiplier = LevelBonuses[level]["Performance"];
			}
			
			return multiplier;
		}
		
		// Check if a staff member can be promoted to next level
		public bool CanPromote(StaffMember staff)
		{
			if (staff == null || staff.Level >= 5)
				return false;
				
			// Check minimum loyalty requirement
			if (staff.Loyalty < 50f + staff.Level * 5f)
				return false;
				
			// Check if meets attribute requirements for next level
			foreach (var req in RequiredAttributes)
			{
				float minValue = req.Value + staff.Level * 0.5f;
				if (!staff.HasAttribute(req.Key) || staff.GetAttributeValue(req.Key) < minValue)
				{
					return false;
				}
			}
			
			return true;
		}
		
		// Get benefits for current level
		public string[] GetBenefitsForLevel(int level)
		{
			if (LevelBenefits.ContainsKey(level))
			{
				return LevelBenefits[level];
			}
			return new string[0];
		}
		
		// Check if this position is allowed to work at specific hour
		public bool IsWorkingHour(int hour)
		{
			return Array.IndexOf(WorkingHours, hour) >= 0;
		}
	}
}
