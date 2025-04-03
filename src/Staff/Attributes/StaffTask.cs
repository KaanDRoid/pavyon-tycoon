// src/Staff/Attributes/StaffTask.cs
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PavyonTycoon.Staff
{
	public class StaffTask
	{
		// Basic task information
		public string Name { get; set; }
		public string Type { get; set; }
		public string Description { get; set; }
		
		// Duration in game minutes (or -1 for continuous)
		public int Duration { get; set; } = 60;
		
		// Location where task is performed
		public Vector2 Location { get; set; }
		
		// Task status
		public enum TaskStatus { Pending, InProgress, Completed, Failed }
		public TaskStatus Status { get; set; } = TaskStatus.Pending;
		
		// Progress tracking (0.0-1.0)
		public float Progress { get; private set; } = 0f;
		
		// Time tracking
		public DateTime StartTime { get; private set; }
		public DateTime EndTime { get; private set; }
		
		// Associated entities (customer, furniture, etc.)
		public object TargetEntity { get; set; }
		
		// Required attributes to perform this task
		public Dictionary<string, float> RequiredAttributes { get; private set; } = new Dictionary<string, float>();
		
		// Attributes that are relevant for performance calculation
		public List<string> RelevantAttributes { get; private set; } = new List<string>();
		
		// Attribute weights for performance calculation
		private Dictionary<string, float> attributeWeights = new Dictionary<string, float>();
		
		// Task results and rewards
		public Dictionary<string, float> Results { get; private set; } = new Dictionary<string, float>();
		
		// Constructor
		public StaffTask(string name, string type, string description = "")
		{
			Name = name;
			Type = type;
			Description = description;
			
			// If description is empty, get from StaffData
			if (string.IsNullOrEmpty(Description))
			{
				Description = StaffData.GetTaskDescription(Type);
			}
		}
		
		// Attribute requirements and performance factors
		public void AddRequiredAttribute(string attributeName, float minimumValue)
		{
			RequiredAttributes[attributeName] = minimumValue;
			
			// Add to relevant attributes if not already there
			if (!RelevantAttributes.Contains(attributeName))
			{
				RelevantAttributes.Add(attributeName);
				attributeWeights[attributeName] = 1.0f; // Default weight
			}
		}
		
		public void AddRelevantAttribute(string attributeName, float weight = 1.0f)
		{
			if (!RelevantAttributes.Contains(attributeName))
			{
				RelevantAttributes.Add(attributeName);
			}
			attributeWeights[attributeName] = weight;
		}
		
		public float GetAttributeWeight(string attributeName)
		{
			if (attributeWeights.ContainsKey(attributeName))
			{
				return attributeWeights[attributeName];
			}
			return 0f;
		}
		
		// Task execution methods
		public void StartTask(DateTime gameTime)
		{
			Status = TaskStatus.InProgress;
			StartTime = gameTime;
			EndTime = Duration > 0 ? gameTime.AddMinutes(Duration) : DateTime.MaxValue;
			Progress = 0f;
			
			GD.Print($"Görev başladı: {Name}");
		}
		
		public void UpdateProgress(DateTime currentTime)
		{
			if (Status != TaskStatus.InProgress) return;
			
			if (Duration > 0)
			{
				// Calculate progress based on time
				TimeSpan elapsed = currentTime - StartTime;
				TimeSpan total = EndTime - StartTime;
				Progress = (float)(elapsed.TotalMinutes / total.TotalMinutes);
				
				// Clamp and check for completion
				Progress = Mathf.Clamp(Progress, 0f, 1f);
				
				if (Progress >= 1f)
				{
					CompleteTask();
				}
			}
			else
			{
				// For continuous tasks, progress stays at a low value
				Progress = 0.1f;
			}
		}
		
		public void CompleteTask()
		{
			Status = TaskStatus.Completed;
			Progress = 1f;
			
			GD.Print($"Görev tamamlandı: {Name}");
		}
		
		public void FailTask(string reason = "")
		{
			Status = TaskStatus.Failed;
			
			if (!string.IsNullOrEmpty(reason))
			{
				GD.Print($"Görev başarısız: {Name} - {reason}");
			}
			else
			{
				GD.Print($"Görev başarısız: {Name}");
			}
		}
		
		// Set task results and rewards
		public void SetResult(string key, float value)
		{
			Results[key] = value;
		}
		
		// Create various task types
		public static StaffTask CreateCustomerInteractionTask(object customer)
		{
			StaffTask task = new StaffTask("Müşteri Etkileşimi", "MüşteriEğlendirme", 
				"Müşteri ile ilgilenerek içki satışını ve memnuniyeti artırma");
			
			task.Duration = 30; // 30 game minutes
			task.TargetEntity = customer;
			
			// Required and relevant attributes
			task.AddRequiredAttribute("Karizma", 3f);
			task.AddRelevantAttribute("Karizma", 1.0f);
			task.AddRelevantAttribute("Sosyallik", 0.8f);
			task.AddRelevantAttribute("İkna", 0.6f);
			
			return task;
		}
		
		public static StaffTask CreateSecurityTask(Vector2 location)
		{
			StaffTask task = new StaffTask("Güvenlik Devriyesi", "GüvenlikSağlama",
				"Belirtilen alanda devriye gezerek güvenliği sağlama");
			
			task.Duration = -1; // Continuous task
			task.Location = location;
			
			// Required and relevant attributes
			task.AddRequiredAttribute("Güç", 3f);
			task.AddRelevantAttribute("Güç", 0.7f);
			task.AddRelevantAttribute("Tehdit", 0.9f);
			task.AddRelevantAttribute("Uyanıklık", 0.8f);
			
			return task;
		}
		
		public static StaffTask CreateDrinkServiceTask(object customer)
		{
			StaffTask task = new StaffTask("İçecek Servisi", "İçecekHazırlama",
				"Müşteriye içecek hazırlama ve servis etme");
			
			task.Duration = 15; // 15 game minutes
			task.TargetEntity = customer;
			
			// Required and relevant attributes
			task.AddRequiredAttribute("Hız", 2f);
			task.AddRelevantAttribute("Hız", 1.0f);
			task.AddRelevantAttribute("Dikkat", 0.7f);
			
			return task;
		}
		
		public static StaffTask CreateMusicPerformanceTask()
		{
			StaffTask task = new StaffTask("Müzik Performansı", "MüzikPerformansı",
				"Canlı müzik performansı sergileme");
			
			task.Duration = 45; // 45 game minutes
			
			// Required and relevant attributes
			task.AddRequiredAttribute("Müzik", 4f);
			task.AddRelevantAttribute("Müzik", 1.0f);
			task.AddRelevantAttribute("Performans", 0.8f);
			task.AddRelevantAttribute("Karizma", 0.5f);
			
			return task;
		}
		
		public static StaffTask CreateFoodPreparationTask()
		{
			StaffTask task = new StaffTask("Yemek Hazırlama", "YemekHazırlama",
				"Mezeler ve atıştırmalıklar hazırlama");
			
			task.Duration = 40; // 40 game minutes
			
			// Required and relevant attributes
			task.AddRequiredAttribute("Yemek", 3f);
			task.AddRelevantAttribute("Yemek", 1.0f);
			task.AddRelevantAttribute("Yaratıcılık", 0.6f);
			task.AddRelevantAttribute("Dikkat", 0.5f);
			
			return task;
		}
		
		public static StaffTask CreateIllegalActivityTask(string activityType)
		{
			string taskName = "Kaçak Kat Görevi";
			string description = "Kaçak katta özel bir görev yürütme";
			
			switch (activityType.ToLower())
			{
				case "kumar":
					taskName = "Kumar Masası Yönetimi";
					description = "Kumar masasını yönetme ve kasayı kontrol etme";
					break;
				case "şantaj":
					taskName = "Şantaj Materyali Toplama";
					description = "Önemli kişiler hakkında kullanılabilecek bilgiler toplama";
					break;
				case "uyuşturucu":
					taskName = "Yasadışı Madde Satışı";
					description = "Seçilmiş müşterilere yasa dışı madde satışı";
					break;
			}
			
			StaffTask task = new StaffTask(taskName, "YasadışıFaaliyet", description);
			
			task.Duration = 60; // 60 game minutes
			
			// Required and relevant attributes
			task.AddRequiredAttribute("Gizlilik", 4f);
			task.AddRequiredAttribute("Sadakat", 5f);
			task.AddRelevantAttribute("Gizlilik", 1.0f);
			task.AddRelevantAttribute("Sadakat", 1.0f);
			task.AddRelevantAttribute("Uyanıklık", 0.7f);
			task.AddRelevantAttribute("İkna", 0.5f);
			
			return task;
		}
	}
}
