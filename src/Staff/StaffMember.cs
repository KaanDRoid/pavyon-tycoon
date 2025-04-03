// src/Staff/StaffMember.cs
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PavyonTycoon.Staff
{
	public class StaffMember
	{
		// Basic properties
		public string FullName { get; set; }
		public string JobTitle { get; set; }
		public int Level { get; set; } = 1;
		public float Salary { get; set; }
		
		// Loyalty represents job satisfaction and reliability (0-100)
		private float _loyalty = 50f;
		public float Loyalty
		{
			get => _loyalty;
			set => _loyalty = Mathf.Clamp(value, 0f, 100f);
		}
		
		// Staff attributes (different for each staff type)
		protected Dictionary<string, float> attributes = new Dictionary<string, float>();
		
		// Current task or assignment
		public StaffTask CurrentTask { get; set; }
		
		// Historical performance metrics
		public Dictionary<string, float> PerformanceHistory { get; private set; } = new Dictionary<string, float>();
		
		// Constructor
		public StaffMember()
		{
			// Initialize default attributes
			attributes.Add("Karizma", 1f);
			attributes.Add("Hız", 1f);
			attributes.Add("Dikkat", 1f);
		}
		
		// Attribute methods
		public bool HasAttribute(string name)
		{
			return attributes.ContainsKey(name);
		}
		
		public float GetAttributeValue(string name)
		{
			if (HasAttribute(name))
			{
				return attributes[name];
			}
			return 0f;
		}
		
		public void SetAttributeValue(string name, float value)
		{
			attributes[name] = Mathf.Clamp(value, 1f, 10f); // Attributes range from 1-10
		}
		
		public Dictionary<string, float> GetAllAttributes()
		{
			return new Dictionary<string, float>(attributes);
		}
		
		public string GetRandomAttribute()
		{
			if (attributes.Count == 0) return null;
			return attributes.Keys.ElementAt(GD.RandRange(0, attributes.Count - 1));
		}
		
		// Loyalty methods
		public void IncreaseLoyalty(float amount)
		{
			if (amount <= 0) return;
			float oldLoyalty = Loyalty;
			Loyalty += amount;
			
			// Log only significant changes
			if (Loyalty - oldLoyalty >= 1f)
			{
				GD.Print($"😊 {FullName} sadakati arttı: {oldLoyalty:F1} -> {Loyalty:F1}");
			}
			
			// Notify listeners
			StaffManager.Instance?.EmitSignal(StaffManager.SignalName.StaffLoyaltyChanged, this, Loyalty);
		}
		
		public void ReduceLoyalty(float amount)
		{
			if (amount <= 0) return;
			float oldLoyalty = Loyalty;
			Loyalty -= amount;
			
			// Log only significant changes
			if (oldLoyalty - Loyalty >= 1f)
			{
				GD.Print($"😟 {FullName} sadakati azaldı: {oldLoyalty:F1} -> {Loyalty:F1}");
			}
			
			// Notify listeners
			StaffManager.Instance?.EmitSignal(StaffManager.SignalName.StaffLoyaltyChanged, this, Loyalty);
		}
		
		// Task management
		public virtual bool AssignTask(StaffTask task)
		{
			// Check if task is valid for this staff type
			if (task == null || !CanPerformTask(task))
			{
				return false;
			}
			
			CurrentTask = task;
			return true;
		}
		
		public virtual void CompleteTask()
		{
			if (CurrentTask == null) return;
			
			// Calculate performance based on relevant attributes
			float performanceScore = CalculateTaskPerformance(CurrentTask);
			
			// Record performance
			string taskType = CurrentTask.Type;
			if (!PerformanceHistory.ContainsKey(taskType))
			{
				PerformanceHistory[taskType] = performanceScore;
			}
			else
			{
				// Weighted average (more recent performances count more)
				PerformanceHistory[taskType] = PerformanceHistory[taskType] * 0.7f + performanceScore * 0.3f;
			}
			
			// Adjust loyalty based on task difficulty and success
			float loyaltyChange = 0;
			
			if (performanceScore > 7f) // Excellent performance
			{
				loyaltyChange = GD.RandRange(1f, 3f);
				GD.Print($"✨ {FullName} görevi mükemmel tamamladı! ({performanceScore:F1}/10)");
			}
			else if (performanceScore > 5f) // Good performance
			{
				loyaltyChange = GD.RandRange(0.2f, 1f);
			}
			else if (performanceScore < 3f) // Poor performance
			{
				loyaltyChange = -GD.RandRange(0.5f, 2f);
				GD.Print($"❌ {FullName} görevde zorlandı... ({performanceScore:F1}/10)");
			}
			
			if (loyaltyChange != 0)
			{
				if (loyaltyChange > 0)
					IncreaseLoyalty(loyaltyChange);
				else
					ReduceLoyalty(-loyaltyChange);
			}
			
			// Clear current task
			CurrentTask = null;
		}
		
		protected virtual bool CanPerformTask(StaffTask task)
		{
			// Check if staff has required attributes for the task
			foreach (var req in task.RequiredAttributes)
			{
				if (!HasAttribute(req.Key) || GetAttributeValue(req.Key) < req.Value)
				{
					return false;
				}
			}
			return true;
		}
		
		protected virtual float CalculateTaskPerformance(StaffTask task)
		{
			if (task == null) return 0f;
			
			// Base performance score
			float score = 5f; // Start at middle
			
			// Add points for each relevant attribute
			foreach (var attr in task.RelevantAttributes)
			{
				if (HasAttribute(attr))
				{
					// Attribute weight is how important this attribute is for the task (0.0-1.0)
					float attrValue = GetAttributeValue(attr);
					float attrWeight = task.GetAttributeWeight(attr);
					score += (attrValue / 10f) * attrWeight * 5f; // Scale contribution
				}
			}
			
			// Loyalty factor (higher loyalty = better performance)
			score += (Loyalty / 100f) * 2f; // Max +2 points for 100% loyalty
			
			// Experience factor (higher level = better performance)
			score += (Level - 1) * 0.5f; // +0.5 points per level above 1
			
			// Randomness factor (good/bad day)
			score += GD.RandRange(-1.0f, 1.0f);
			
			// Clamp to valid range
			return Mathf.Clamp(score, 0f, 10f);
		}
		
		// Helper methods
		public virtual StaffMember Clone()
		{
			StaffMember clone = new StaffMember
			{
				FullName = this.FullName,
				JobTitle = this.JobTitle,
				Level = this.Level,
				Salary = this.Salary,
				Loyalty = this.Loyalty
			};
			
			// Copy attributes
			foreach (var attr in this.attributes)
			{
				clone.attributes[attr.Key] = attr.Value;
			}
			
			// Copy performance history
			foreach (var perf in this.PerformanceHistory)
			{
				clone.PerformanceHistory[perf.Key] = perf.Value;
			}
			
			return clone;
		}
		
		// Get a formatted display of the staff's current state
		public virtual string GetStatusDisplay()
		{
			string status = $"{FullName} (Lvl {Level} {JobTitle})\n";
			status += $"Sadakat: {Loyalty:F0}%\n";
			status += $"Maaş: {Salary:F0}₺\n";
			
			// Display top 3 attributes
			var sortedAttrs = attributes.OrderByDescending(a => a.Value).Take(3);
			status += "En İyi Özellikler:\n";
			foreach (var attr in sortedAttrs)
			{
				status += $"- {attr.Key}: {attr.Value:F1}/10\n";
			}
			
			return status;
		}
		
		// Get any special capabilities this staff has
		public virtual string[] GetSpecialCapabilities()
		{
			return new string[0]; // Base staff has no special capabilities
		}
		
		// Get risk level for illegal activities (0-10)
		public virtual float GetIllegalActivityRisk()
		{
			// Base risk calculation: 
			// - Higher loyalty = lower risk
			// - Lower loyalty = higher risk
			return Mathf.Max(0, 10f - (Loyalty / 10f));
		}
	}
}
