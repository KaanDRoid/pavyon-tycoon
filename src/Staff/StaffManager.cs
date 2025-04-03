// src/Staff/StaffManager.cs
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PavyonTycoon.Core;
using PavyonTycoon.Economy;

namespace PavyonTycoon.Staff
{
	public partial class StaffManager : Node
	{
		// Singleton pattern (optional, can also be accessed through GameManager)
		private static StaffManager _instance;
		public static StaffManager Instance => _instance;

		// Lists of staff members by type
		private List<StaffMember> staffMembers = new List<StaffMember>();
		private List<Kons> konslar = new List<Kons>();
		private List<SecurityStaff> securityStaff = new List<SecurityStaff>();
		private List<Waiter> waiters = new List<Waiter>();
		private List<Musician> musicians = new List<Musician>();
		private List<Cook> cooks = new List<Cook>();
		private List<IllegalFloorStaff> illegalFloorStaff = new List<IllegalFloorStaff>();
		
		// Job positions with open positions, required counts, etc.
		private Dictionary<string, JobPosition> jobPositions = new Dictionary<string, JobPosition>();

		// Available staff for hire (refreshes daily)
		private List<StaffMember> availableStaffForHire = new List<StaffMember>();
		
		// Staff hiring and management costs
		public float HiringCost { get; private set; } = 500f;
		public float FiringCost { get; private set; } = 200f;
		public float TrainingCostPerLevel { get; private set; } = 300f;
		
		// Total daily salary cost
		public float DailySalaryCost => staffMembers.Sum(s => s.Salary);
		
		// References to other systems
		private EconomyManager economyManager;
		private TimeManager timeManager;
		
		// Vardiya yönetimi için değişkenler
		private List<StaffMember> activeShiftStaff = new List<StaffMember>();
		private Dictionary<string, List<StaffMember>> shiftAssignments = new Dictionary<string, List<StaffMember>>();
		
		// Signals
		[Signal] public delegate void StaffHiredEventHandler(StaffMember staff);
		[Signal] public delegate void StaffFiredEventHandler(StaffMember staff);
		[Signal] public delegate void StaffAttributeChangedEventHandler(StaffMember staff, string attributeName, float newValue);
		[Signal] public delegate void StaffLoyaltyChangedEventHandler(StaffMember staff, float newLoyalty);
		[Signal] public delegate void SalariesPaidEventHandler(float totalAmount);
		[Signal] public delegate void AvailableStaffUpdatedEventHandler();
		[Signal] public delegate void ShiftStartedEventHandler();
		[Signal] public delegate void ShiftEndedEventHandler();
		[Signal] public delegate void StaffTaskCompletedEventHandler(StaffMember staff, StaffTask task);

		public override void _Ready()
		{
			// Set up singleton
			if (_instance != null)
			{
				GD.PrintErr("Multiple StaffManager instances detected!");
				QueueFree();
				return;
			}
			_instance = this;
			
			// Get references to other systems
			var gameManager = GetParent<GameManager>();
			if (gameManager != null)
			{
				economyManager = gameManager.Economy;
				timeManager = gameManager.Time;
				
				// Connect to signals
				if (timeManager != null)
				{
					timeManager.Connect(TimeManager.SignalName.DayEnded, Callable.From(OnDayEnded));
					timeManager.Connect(TimeManager.SignalName.NewDayStarted, Callable.From(OnNewDayStarted));
					timeManager.Connect(TimeManager.SignalName.HourChanged, Callable.From(OnHourChanged));
				}
			}
			
			// Initialize job positions
			InitializeJobPositions();
			
			// Generate initial staff for hire
			RefreshAvailableStaffForHire();
			
			GD.Print("👥 Personel yönetim sistemi başlatıldı");
		}

		// Vardiya başlatma
		public void StartNightShift()
		{
			GD.Print("🌃 Personel vardiyası başlatılıyor...");
			
			// Aktif vardiya listesini temizle
			activeShiftStaff.Clear();
			
			// İş pozisyonlarını kontrol et ve vardiyaya ekle
			foreach (var staff in staffMembers)
			{
				// Personel görevini kontrol et
				bool isAssigned = false;
				
				// Eğer önceden atanmış görev varsa, kontrol et
				if (staff.CurrentTask != null)
				{
					// Görevi başlat
					var gameManager = GetNode<GameManager>("/root/Main/GameManager");
					if (gameManager?.Time != null)
					{
						staff.CurrentTask.StartTask(gameManager.Time.CurrentTime);
						isAssigned = true;
					}
				}
				
				// Eğer bir görev atanmamışsa, personelin iş tanımına göre varsayılan görev ata
				if (!isAssigned)
				{
					AssignDefaultTask(staff);
				}
				
				// Aktif vardiyaya ekle
				activeShiftStaff.Add(staff);
			}
			
			// Vardiya başlangıç sinyali gönder
			EmitSignal(SignalName.ShiftStarted);
			
			GD.Print($"🌃 Vardiya başlatıldı. Aktif personel: {activeShiftStaff.Count}");
		}
		
		// Vardiya sonlandırma
		public void EndNightShift()
		{
			GD.Print("🌅 Personel vardiyası sonlandırılıyor...");
			
			// Personelin görevlerini tamamla ve performans değerlendirmesi yap
			foreach (var staff in activeShiftStaff)
			{
				if (staff.CurrentTask != null)
				{
					// Görevin durumunu kontrol et
					if (staff.CurrentTask.Status == StaffTask.TaskStatus.InProgress)
					{
						// Görevi tamamla
						staff.CurrentTask.CompleteTask();
						
						// Personelin görevi bitirme işlemlerini yap
						staff.CompleteTask();
						
						// Görev tamamlandı sinyali gönder
						EmitSignal(SignalName.StaffTaskCompleted, staff, staff.CurrentTask);
					}
				}
			}
			
			// Aktif vardiya listesini temizle
			activeShiftStaff.Clear();
			
			// Vardiya bitiş sinyali gönder
			EmitSignal(SignalName.ShiftEnded);
			
			GD.Print("🌅 Vardiya sonlandırıldı.");
		}
		
		// Varsayılan görev atama
		private void AssignDefaultTask(StaffMember staff)
		{
			if (staff == null) return;
			
			StaffTask defaultTask = null;
			
			// Personel türüne göre varsayılan görev oluştur
			if (staff is Kons)
			{
				defaultTask = StaffTask.CreateCustomerInteractionTask(null);
			}
			else if (staff is SecurityStaff)
			{
				defaultTask = StaffTask.CreateSecurityTask(Vector2.Zero);
			}
			else if (staff is Waiter)
			{
				defaultTask = StaffTask.CreateDrinkServiceTask(null);
			}
			else if (staff is Musician)
			{
				defaultTask = StaffTask.CreateMusicPerformanceTask();
			}
			else if (staff is Cook)
			{
				defaultTask = StaffTask.CreateFoodPreparationTask();
			}
			else if (staff is IllegalFloorStaff illegalStaff)
			{
				// Yasadışı personelin birincil faaliyetine göre görev ata
				string activityType = illegalStaff.GetActivityName(illegalStaff.PrimaryActivity).ToLower();
				defaultTask = StaffTask.CreateIllegalActivityTask(activityType);
			}
			
			// Oluşturulan görevi personele ata
			if (defaultTask != null && staff.AssignTask(defaultTask))
			{
				// Görevi başlat
				var gameManager = GetNode<GameManager>("/root/Main/GameManager");
				if (gameManager?.Time != null)
				{
					defaultTask.StartTask(gameManager.Time.CurrentTime);
					GD.Print($"{staff.FullName}'e varsayılan görev atandı: {defaultTask.Name}");
				}
			}
		}
		
		// Personel görevlerinin güncel durumunu işleme
		public void ProcessStaffTasks(DateTime currentTime)
		{
			foreach (var staff in activeShiftStaff)
			{
				if (staff.CurrentTask != null && staff.CurrentTask.Status == StaffTask.TaskStatus.InProgress)
				{
					// Görevin ilerlemesini güncelle
					staff.CurrentTask.UpdateProgress(currentTime);
					
					// Eğer görev tamamlandıysa işle
					if (staff.CurrentTask.Status == StaffTask.TaskStatus.Completed)
					{
						staff.CompleteTask();
						EmitSignal(SignalName.StaffTaskCompleted, staff, staff.CurrentTask);
						
						// Yeni görev ata (sürekli çalışma için)
						AssignDefaultTask(staff);
					}
				}
			}
		}
		
		// Vardiya çalışanlarını belirli bir pozisyon için filtreleme
		public List<StaffMember> GetActiveStaffByPosition(string positionTitle)
		{
			return activeShiftStaff.Where(s => s.JobTitle == positionTitle).ToList();
		}
		
		// Personele özel bir görev atayıp hemen başlatma
		public bool AssignAndStartTask(StaffMember staff, StaffTask task)
		{
			if (staff == null || task == null) return false;
			
			// Eğer mevcut bir görev varsa iptal et
			if (staff.CurrentTask != null && staff.CurrentTask.Status == StaffTask.TaskStatus.InProgress)
			{
				staff.CurrentTask.FailTask("Yeni görev atandı");
			}
			
			// Yeni görevi ata
			bool success = staff.AssignTask(task);
			
			if (success)
			{
				// Görevi başlat
				var gameManager = GetNode<GameManager>("/root/Main/GameManager");
				if (gameManager?.Time != null)
				{
					task.StartTask(gameManager.Time.CurrentTime);
					GD.Print($"{staff.FullName}'e yeni görev atandı ve başlatıldı: {task.Name}");
				}
			}
			
			return success;
		}
		
		// TimeManager ile entegrasyon - her saat başında çağrılır
		private void OnHourChanged(int hour)
		{
			// Şu anki zamanı al
			var gameManager = GetNode<GameManager>("/root/Main/GameManager");
			if (gameManager?.Time != null)
			{
				// Personel görevlerini işle
				ProcessStaffTasks(gameManager.Time.CurrentTime);
				
				// İş saati kontrolü
				bool isBusinessHour = hour >= 18 || hour < 6; // 18:00-06:00 arası çalışma saatleri
				
				if (isBusinessHour)
				{
					// Çalışma saatlerindeki işlemler
					UpdateStaffStates();
				}
			}
		}
		
		// Personel durumlarını güncelle (yorgunluk, morale vb.)
		private void UpdateStaffStates()
		{
			foreach (var staff in activeShiftStaff)
			{
				// Müzisyenler için dayanıklılık azaltma
				if (staff is Musician musician)
				{
					musician.StaminaLevel = Mathf.Max(10f, musician.StaminaLevel - 5f);
				}
				
				// Sadakat değişimleri ve olayları kontrol et
				float randomChance = GD.Randf();
				
				// %5 şansla rastgele olay
				if (randomChance < 0.05f)
				{
					TriggerRandomStaffEvent(staff);
				}
			}
		}
		
		// Rastgele personel olayı tetikleme
		private void TriggerRandomStaffEvent(StaffMember staff)
		{
			if (staff == null) return;
			
			// Personel türüne göre farklı olay türleri
			if (staff is Kons kons)
			{
				// Konslar için özel olaylar
				float eventRoll = GD.Randf();
				
				if (eventRoll < 0.3f)
				{
					// Önemli müşteri bağlantısı
					GD.Print($"🌟 {kons.FullName} önemli bir müşteriyle bağlantı kurdu!");
					kons.IncreaseLoyalty(GD.RandRange(1f, 3f));
				}
				else if (eventRoll < 0.5f)
				{
					// Müşteri şikayeti
					GD.Print($"⚠️ {kons.FullName} hakkında müşteri şikayeti var.");
					kons.ReduceLoyalty(GD.RandRange(0.5f, 2f));
				}
			}
			else if (staff is SecurityStaff security)
			{
				// Güvenlik personeli için özel olaylar
				float eventRoll = GD.Randf();
				
				if (eventRoll < 0.4f)
				{
					// Kavga önleme
					GD.Print($"💪 {security.FullName} bir kavgayı önledi!");
					security.IncreaseLoyalty(GD.RandRange(1f, 2f));
				}
				else if (eventRoll < 0.6f)
				{
					// Şüpheli müşteri durumu
					GD.Print($"👀 {security.FullName} şüpheli bir müşteriyi takip ediyor...");
				}
			}
			// Diğer personel türleri için de benzer olaylar eklenebilir
		}

		private void InitializeJobPositions()
		{
			// Create each job position with its requirements and base salary
			jobPositions.Add("Kons", new JobPosition
			{
				Title = "Kons",
				Description = "Müşterilerle etkileşime girerek içki satışını artırır.",
				BaseSalary = 500f,
				MaxPositions = 8,
				RequiredAttributes = new Dictionary<string, float>
				{
					{ "Karizma", 3f },
					{ "Sosyallik", 4f }
				}
			});
			
			jobPositions.Add("Güvenlik", new JobPosition
			{
				Title = "Güvenlik",
				Description = "Pavyondaki düzeni sağlar ve sorunları çözer.",
				BaseSalary = 600f,
				MaxPositions = 4,
				RequiredAttributes = new Dictionary<string, float>
				{
					{ "Güç", 4f },
					{ "Tehdit", 3f }
				}
			});
			
			jobPositions.Add("Garson", new JobPosition
			{
				Title = "Garson",
				Description = "Müşterilere hızlı ve etkili hizmet sunar.",
				BaseSalary = 400f,
				MaxPositions = 6,
				RequiredAttributes = new Dictionary<string, float>
				{
					{ "Hız", 4f },
					{ "Dikkat", 3f }
				}
			});
			
			jobPositions.Add("Müzisyen", new JobPosition
			{
				Title = "Müzisyen",
				Description = "Canlı müzik performansı ile atmosferi yükseltir.",
				BaseSalary = 550f,
				MaxPositions = 5,
				RequiredAttributes = new Dictionary<string, float>
				{
					{ "Müzik", 5f },
					{ "Performans", 4f }
				}
			});
			
			jobPositions.Add("Aşçı", new JobPosition
			{
				Title = "Aşçı",
				Description = "Lezzetli mezeler ve yemekler hazırlar.",
				BaseSalary = 450f,
				MaxPositions = 3,
				RequiredAttributes = new Dictionary<string, float>
				{
					{ "Yemek", 4f },
					{ "Yaratıcılık", 3f }
				}
			});
			
			jobPositions.Add("Kaçak Kat Görevlisi", new JobPosition
			{
				Title = "Kaçak Kat Görevlisi",
				Description = "Kaçak kattaki yasadışı faaliyetleri yürütür.",
				BaseSalary = 700f,
				MaxPositions = 4,
				RequiredAttributes = new Dictionary<string, float>
				{
					{ "Gizlilik", 4f },
					{ "Sadakat", 5f }
				},
				IsLegal = false
			});
			
			GD.Print("💼 İş pozisyonları tanımlandı");
		}

		// Staff hiring and management methods
		public StaffMember HireStaff(StaffMember staffToHire)
		{
			if (staffToHire == null) return null;
			
			// Check if we can afford to hire
			if (economyManager == null || economyManager.Money < HiringCost)
			{
				GD.Print("❌ İşe alma için yeterli paranız yok!");
				return null;
			}
			
			// Check if position is available
			string jobTitle = staffToHire.JobTitle;
			if (!jobPositions.ContainsKey(jobTitle))
			{
				GD.PrintErr($"İş pozisyonu bulunamadı: {jobTitle}");
				return null;
			}
			
			JobPosition position = jobPositions[jobTitle];
			int currentCount = GetStaffCountByJobTitle(jobTitle);
			
			if (currentCount >= position.MaxPositions)
			{
				GD.Print($"❌ {jobTitle} pozisyonu için maksimum çalışan sayısına ulaşıldı!");
				return null;
			}
			
			// Pay hiring cost
			economyManager.AddExpense(HiringCost, EconomyManager.ExpenseCategory.Salaries, $"{staffToHire.FullName} işe alım maliyeti");
			
			// Clone the staff member (to avoid modifying the one in availableStaffForHire)
			StaffMember newStaff = staffToHire.Clone();
			
			// Add to appropriate list based on type
			staffMembers.Add(newStaff);
			AddStaffToTypeList(newStaff);
			
			// Remove from available for hire
			availableStaffForHire.Remove(staffToHire);
			
			// Log and emit signal
			GD.Print($"👋 {newStaff.FullName} işe alındı! ({newStaff.JobTitle})");
			EmitSignal(SignalName.StaffHired, newStaff);
			EmitSignal(SignalName.AvailableStaffUpdated);
			
			return newStaff;
		}
		
		public bool FireStaff(StaffMember staffToFire)
		{
			if (staffToFire == null || !staffMembers.Contains(staffToFire)) return false;
			
			// Pay firing cost if economy exists
			if (economyManager != null)
			{
				economyManager.AddExpense(FiringCost, EconomyManager.ExpenseCategory.Salaries, $"{staffToFire.FullName} işten çıkarma maliyeti");
			}
			
			// Remove from appropriate lists
			staffMembers.Remove(staffToFire);
			RemoveStaffFromTypeList(staffToFire);
			
			// Apply random loyalty penalty to other staff (staff morale hit)
			foreach (var staff in staffMembers)
			{
				// 30% chance to affect loyalty
				if (GD.Randf() < 0.3f)
				{
					float loyaltyPenalty = GD.RandRange(3f, 10f);
					staff.ReduceLoyalty(loyaltyPenalty);
					GD.Print($"⚠️ {staff.FullName} sadakati düştü (işten çıkarma etkisi): -{loyaltyPenalty}");
				}
			}
			
			// Log and emit signal
			GD.Print($"🚪 {staffToFire.FullName} işten çıkarıldı.");
			EmitSignal(SignalName.StaffFired, staffToFire);
			
			return true;
		}
		
		public bool TrainStaff(StaffMember staff, string attributeName)
		{
			if (staff == null || !staffMembers.Contains(staff)) return false;
			
			// Check if we can afford training
			if (economyManager == null || economyManager.Money < TrainingCostPerLevel)
			{
				GD.Print("❌ Eğitim için yeterli paranız yok!");
				return false;
			}
			
			// Check if attribute exists and can be improved
			if (!staff.HasAttribute(attributeName) || staff.GetAttributeValue(attributeName) >= 10f)
			{
				GD.Print($"❌ {attributeName} özelliği iyileştirilemez.");
				return false;
			}
			
			// Pay training cost
			economyManager.AddExpense(TrainingCostPerLevel, EconomyManager.ExpenseCategory.Salaries, 
				$"{staff.FullName} eğitim: {attributeName}");
			
			// Improve attribute
			float oldValue = staff.GetAttributeValue(attributeName);
			float improvement = GD.RandRange(0.5f, 1.5f);
			staff.SetAttributeValue(attributeName, oldValue + improvement);
			
			// Improve loyalty slightly
			staff.IncreaseLoyalty(GD.RandRange(2f, 5f));
			
			// Log and emit signal
			GD.Print($"📚 {staff.FullName} eğitimi tamamlandı: {attributeName} +{improvement:F1}");
			EmitSignal(SignalName.StaffAttributeChanged, staff, attributeName, staff.GetAttributeValue(attributeName));
			
			return true;
		}
		
		public bool PromoteStaff(StaffMember staff)
		{
			if (staff == null || !staffMembers.Contains(staff)) return false;
			
			// Calculate promotion cost - double the training cost
			float promotionCost = TrainingCostPerLevel * 2;
			
			// Check if we can afford promotion
			if (economyManager == null || economyManager.Money < promotionCost)
			{
				GD.Print("❌ Terfi için yeterli paranız yok!");
				return false;
			}
			
			// Check if already at max level
			if (staff.Level >= 5)
			{
				GD.Print($"❌ {staff.FullName} maksimum seviyeye ulaştı!");
				return false;
			}
			
			// Pay promotion cost
			economyManager.AddExpense(promotionCost, EconomyManager.ExpenseCategory.Salaries, 
				$"{staff.FullName} terfi maliyeti");
			
			// Promote staff
			int oldLevel = staff.Level;
			staff.Level++;
			
			// Increase salary by 15%
			staff.Salary *= 1.15f;
			
			// Improve random attributes
			int attributeCount = GD.RandRange(1, 3);
			for (int i = 0; i < attributeCount; i++)
			{
				string attribute = staff.GetRandomAttribute();
				float oldValue = staff.GetAttributeValue(attribute);
				float improvement = GD.RandRange(0.8f, 2.0f);
				staff.SetAttributeValue(attribute, oldValue + improvement);
				GD.Print($"⬆️ {staff.FullName} terfi özellik artışı: {attribute} +{improvement:F1}");
			}
			
			// Large loyalty boost
			staff.IncreaseLoyalty(GD.RandRange(10f, 25f));
			
			// Log promotion
			GD.Print($"🌟 {staff.FullName} terfi etti! Seviye {oldLevel} -> {staff.Level}");
			
			return true;
		}
		
		public void PaySalaries()
		{
			if (economyManager == null || staffMembers.Count == 0) return;
			
			float totalSalary = DailySalaryCost;
			
			// Pay the salaries
			economyManager.AddExpense(totalSalary, EconomyManager.ExpenseCategory.Salaries, "Günlük maaş ödemesi");
			
			// Update staff loyalty based on payment
			foreach (var staff in staffMembers)
			{
				// Paying on time slightly increases loyalty
				staff.IncreaseLoyalty(GD.RandRange(1f, 3f));
			}
			
			GD.Print($"💰 Günlük maaş ödemesi yapıldı: {totalSalary:F0}₺");
			EmitSignal(SignalName.SalariesPaid, totalSalary);
		}
		
		// Event handlers
		private void OnDayEnded(int day)
		{
			// Pay salaries at the end of each day
			PaySalaries();
			
			// Process staff behavior and events
			ProcessDailyStaffBehavior();
		}
		
		private void OnNewDayStarted(int day)
		{
			// Refresh the available staff for hire
			RefreshAvailableStaffForHire();
		}
		
		private void ProcessDailyStaffBehavior()
		{
			// Process each staff member's daily behavior and events
			foreach (var staff in staffMembers.ToList()) // ToList to avoid modification issues during iteration
			{
				// Random loyalty decay (life happens)
				float loyaltyDecay = GD.RandRange(0.5f, 1.5f);
				staff.ReduceLoyalty(loyaltyDecay);
				
				// Check for random events based on loyalty
				CheckForStaffEvents(staff);
			}
		}
		
		private void CheckForStaffEvents(StaffMember staff)
		{
			// Skip if staff is null
			if (staff == null) return;
			
			float loyalty = staff.Loyalty;
			float eventChance = 0f;
			
			// Very low loyalty: high chance of negative events
			if (loyalty < 20f)
			{
				eventChance = 0.25f; // 25% chance of event
				if (GD.Randf() < eventChance)
				{
					// Dangerous events: betrayal, theft, sabotage
					int eventType = GD.RandRange(0, 3);
					switch (eventType)
					{
						case 0:
							// Betrayal - staff might leak info to rivals or authorities
							GD.Print($"⚠️ {staff.FullName} bilgileri rakip işletmeye sızdırdı!");
							// TODO: Create a negative reputation event or penalty
							break;
							
						case 1:
							// Theft - staff steals money
							float stolenAmount = GD.RandRange(100f, 500f);
							economyManager?.AddExpense(stolenAmount, EconomyManager.ExpenseCategory.Unexpected, $"{staff.FullName} kasadan para çaldı");
							GD.Print($"⚠️ {staff.FullName} kasadan {stolenAmount:F0}₺ çaldı!");
							break;
							
						case 2:
							// Sabotage - staff causes damage or problems
							GD.Print($"⚠️ {staff.FullName} işletmeye zarar verdi!");
							// TODO: Create a sabotage event with consequences
							break;
							
						case 3:
							// Quitting without notice
							GD.Print($"⚠️ {staff.FullName} istifa etti ve gitti!");
							FireStaff(staff); // Remove without benefits or firing cost
							break;
					}
				}
			}
			// Low loyalty: moderate chance of negative events
			else if (loyalty < 40f)
			{
				eventChance = 0.15f; // 15% chance of event
				if (GD.Randf() < eventChance)
				{
					// Negative events: complaints, arguments, poor performance
					int eventType = GD.RandRange(0, 2);
					switch (eventType)
					{
						case 0:
							// Complaint - staff complains to others, affecting morale
							GD.Print($"⚠️ {staff.FullName} diğer çalışanlara şikayet ediyor!");
							// Affect other staff loyalty
							foreach (var otherStaff in staffMembers)
							{
								if (otherStaff != staff && GD.Randf() < 0.3f)
								{
									otherStaff.ReduceLoyalty(GD.RandRange(1f, 5f));
								}
							}
							break;
							
						case 1:
							// Argument - staff gets into argument with customers
							GD.Print($"⚠️ {staff.FullName} bir müşteriyle tartıştı!");
							// TODO: Create a customer satisfaction penalty
							break;
							
						case 2:
							// Poor performance - staff performs poorly
							GD.Print($"⚠️ {staff.FullName} performansı çok düşük!");
							// Temporarily reduce a random attribute
							string attribute = staff.GetRandomAttribute();
							float oldValue = staff.GetAttributeValue(attribute);
							float reduction = GD.RandRange(0.5f, 1.5f);
							staff.SetAttributeValue(attribute, Mathf.Max(1f, oldValue - reduction));
							break;
					}
				}
			}
			// High loyalty: chance of positive events
			else if (loyalty > 80f)
			{
				eventChance = 0.15f; // 15% chance of positive event
				if (GD.Randf() < eventChance)
				{
					// Positive events: extra effort, good ideas, recruiting
					int eventType = GD.RandRange(0, 2);
					switch (eventType)
					{
						case 0:
							// Extra effort - staff goes above and beyond
							GD.Print($"✨ {staff.FullName} ekstra çaba gösteriyor!");
							// Improve a random attribute temporarily
							string attribute = staff.GetRandomAttribute();
							float oldValue = staff.GetAttributeValue(attribute);
							float improvement = GD.RandRange(0.5f, 1.5f);
							staff.SetAttributeValue(attribute, oldValue + improvement);
							break;
							
						case 1:
							// Good idea - staff has a good idea for improvement
							GD.Print($"✨ {staff.FullName} işletme için harika bir fikir önerdi!");
							// TODO: Create a small boost to income or customer satisfaction
							break;
							
						case 2:
							// Recruiting - staff refers a friend for hire
							GD.Print($"✨ {staff.FullName} işe alınabilecek bir arkadaşını önerdi!");
							// Add a new potential hire with slightly better stats
							StaffMember referral = GenerateStaffMember(staff.JobTitle, 1.1f);
							if (referral != null)
							{
								availableStaffForHire.Add(referral);
								EmitSignal(SignalName.AvailableStaffUpdated);
							}
							break;
					}
				}
			}
		}
		
		// Utility methods
		private void RefreshAvailableStaffForHire()
		{
			// Clear previous staff list
			availableStaffForHire.Clear();
			
			// Generate new staff for each position
			foreach (var position in jobPositions.Values)
			{
				// Generate 1-3 candidates for each job type
				int candidateCount = GD.RandRange(1, 3);
				for (int i = 0; i < candidateCount; i++)
				{
					StaffMember staff = GenerateStaffMember(position.Title);
					if (staff != null)
					{
						availableStaffForHire.Add(staff);
					}
				}
			}
			
			GD.Print($"👥 İşe alınabilecek {availableStaffForHire.Count} yeni personel adayı mevcut");
			EmitSignal(SignalName.AvailableStaffUpdated);
		}
		
		private StaffMember GenerateStaffMember(string jobTitle, float qualityMultiplier = 1.0f)
		{
			if (!jobPositions.ContainsKey(jobTitle)) return null;
			
			JobPosition position = jobPositions[jobTitle];
			
			// Create appropriate staff type
			StaffMember staff = null;
			
			switch (jobTitle)
			{
				case "Kons":
					staff = new Kons();
					break;
					
				case "Güvenlik":
					staff = new SecurityStaff();
					break;
					
				case "Garson":
					staff = new Waiter();
					break;
					
				case "Müzisyen":
					staff = new Musician();
					break;
					
				case "Aşçı":
					staff = new Cook();
					break;
					
				case "Kaçak Kat Görevlisi":
					staff = new IllegalFloorStaff();
					break;
					
				default:
					staff = new StaffMember();
					break;
			}
			
			// Set base properties
			staff.JobTitle = jobTitle;
			staff.Level = 1;
			staff.Salary = position.BaseSalary * GD.RandRange(0.9f, 1.1f); // Slight salary variation
			staff.Loyalty = GD.RandRange(40f, 60f); // Initial loyalty
			
			// Generate a random name from StaffData class
			staff.FullName = StaffData.GetRandomName(jobTitle);
			
			// Configure attributes based on job requirements and randomness
			foreach (var reqAttribute in position.RequiredAttributes)
			{
				float baseValue = reqAttribute.Value;
				float randomVariation = GD.RandRange(-1.0f, 3.0f);
				float finalValue = Mathf.Clamp((baseValue + randomVariation) * qualityMultiplier, 1f, 10f);
				staff.SetAttributeValue(reqAttribute.Key, finalValue);
			}
			
			// Add some random auxiliary attributes
			int extraAttributeCount = GD.RandRange(1, 3);
			for (int i = 0; i < extraAttributeCount; i++)
			{
				string attrName = StaffData.GetRandomAttributeName();
				if (!staff.HasAttribute(attrName))
				{
					float value = GD.RandRange(1f, 5f) * qualityMultiplier;
					staff.SetAttributeValue(attrName, Mathf.Clamp(value, 1f, 10f));
				}
			}
			
			return staff;
		}
		
		private void AddStaffToTypeList(StaffMember staff)
		{
			if (staff == null) return;
			
			switch (staff)
			{
				case Kons kons:
					konslar.Add(kons);
					break;
				case SecurityStaff security:
					securityStaff.Add(security);
					break;
				case Waiter waiter:
					waiters.Add(waiter);
					break;
				case Musician musician:
					musicians.Add(musician);
					break;
				case Cook cook:
					cooks.Add(cook);
					break;
				case IllegalFloorStaff illegalStaff:
					illegalFloorStaff.Add(illegalStaff);
					break;
			}
		}
		
		private void RemoveStaffFromTypeList(StaffMember staff)
		{
			if (staff == null) return;
			
			switch (staff)
			{
				case Kons kons:
					konslar.Remove(kons);
					break;
				case SecurityStaff security:
					securityStaff.Remove(security);
					break;
				case Waiter waiter:
					waiters.Remove(waiter);
					break;
				case Musician musician:
					musicians.Remove(musician);
					break;
				case Cook cook:
					cooks.Remove(cook);
					break;
				case IllegalFloorStaff illegalStaff:
					illegalFloorStaff.Remove(illegalStaff);
					break;
			}
		}
		
		private int GetStaffCountByJobTitle(string jobTitle)
		{
			return staffMembers.Count(s => s.JobTitle == jobTitle);
		}
		
		// Public getters for staff lists
		public List<StaffMember> GetAllStaff() => new List<StaffMember>(staffMembers);
		public List<Kons> GetKonslar() => new List<Kons>(konslar);
		public List<SecurityStaff> GetSecurityStaff() => new List<SecurityStaff>(securityStaff);
		public List<Waiter> GetWaiters() => new List<Waiter>(waiters);
		public List<Musician> GetMusicians() => new List<Musician>(musicians);
		public List<Cook> GetCooks() => new List<Cook>(cooks);
		public List<IllegalFloorStaff> GetIllegalFloorStaff() => new List<IllegalFloorStaff>(illegalFloorStaff);
		public List<StaffMember> GetAvailableStaffForHire() => new List<StaffMember>(availableStaffForHire);
		public Dictionary<string, JobPosition> GetJobPositions() => new Dictionary<string, JobPosition>(jobPositions);
	}
}
