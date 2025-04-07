// PavyonTycoon/Scripts/Economy/EconomyManager.cs
using Godot;
using System;
using System.Collections.Generic;


public partial class EconomyManager : Node
{
	// Temel ekonomik değişkenler
	public float CurrentMoney { get; private set; } = 0f;
	public float DailyRevenue { get; private set; } = 0f;
	public float DailyExpenses { get; private set; } = 0f;
	public float DailyProfit => DailyRevenue - DailyExpenses;
	public float TotalRevenue { get; private set; } = 0f;
	public float TotalExpenses { get; private set; } = 0f;
	public float TotalProfit => TotalRevenue - TotalExpenses;
	
	// Kredi sistemi değişkenleri
	public float CurrentLoanAmount { get; private set; } = 0f;
	public float CurrentLoanInterestRate { get; private set; } = 0.05f;  // %5 başlangıç faiz oranı
	public int LoanTermInDays { get; private set; } = 0;                 // Ödeme süresi
	public int RemainingLoanDays { get; private set; } = 0;              // Kalan ödeme günü
	public float DailyLoanPayment { get; private set; } = 0f;            // Günlük ödeme miktarı
	public bool HasActiveLoan => CurrentLoanAmount > 0;
	public int MissedPayments { get; private set; } = 0;                 // Kaçırılan ödeme sayısı
	public float LatePaymentPenaltyRate = 0.1f;                          // Gecikme cezası oranı
	public int MaxMissedPayments = 5;                                    // Maksimum kaçırılabilecek ödeme sayısı
	
	// Maliyet ve fiyat faktörleri
	public float AlcoholPriceMultiplier { get; private set; } = 1.0f;    // İçki fiyat çarpanı
	public float FoodPriceMultiplier { get; private set; } = 1.0f;       // Yemek fiyat çarpanı
	public float EntertainmentPriceMultiplier { get; private set; } = 1.0f; // Eğlence fiyat çarpanı
	
	// Gelir ve gider takibi
	private Dictionary<string, float> _incomeCategories = new Dictionary<string, float>();
	private Dictionary<string, float> _expenseCategories = new Dictionary<string, float>();
	
	// Finansal geçmiş
	private List<DailyFinancialRecord> _financialHistory = new List<DailyFinancialRecord>();
	
	// Kredi tipleri
	public enum LoanType
	{
		Small,      // Küçük kredi - düşük miktar, düşük faiz
		Medium,     // Orta kredi - orta miktar, orta faiz
		Large,      // Büyük kredi - yüksek miktar, yüksek faiz
		Emergency   // Acil kredi - düşük miktar, çok yüksek faiz
	}
	
	// Kredi bilgileri
	private Dictionary<LoanType, LoanInfo> _loanTypes = new Dictionary<LoanType, LoanInfo>();
	
	public class LoanInfo
	{
		public float Amount;
		public float BaseInterestRate;
		public int TermInDays;
		
		public LoanInfo(float amount, float baseInterestRate, int termInDays)
		{
			Amount = amount;
			BaseInterestRate = baseInterestRate;
			TermInDays = termInDays;
		}
	}
	
	// Günlük finansal kayıt sınıfı
	public class DailyFinancialRecord
	{
		public int Day;
		public float Revenue;
		public float Expenses;
		public float Profit;
		public Dictionary<string, float> IncomeBreakdown;
		public Dictionary<string, float> ExpenseBreakdown;

		public DailyFinancialRecord(int day, float revenue, float expenses, 
			Dictionary<string, float> incomeBreakdown, Dictionary<string, float> expenseBreakdown)
		{
			Day = day;
			Revenue = revenue;
			Expenses = expenses;
			Profit = revenue - expenses;
			IncomeBreakdown = new Dictionary<string, float>(incomeBreakdown);
			ExpenseBreakdown = new Dictionary<string, float>(expenseBreakdown);
		}
	}
	
	// Sinyaller (Events)
	[Signal]
	public delegate void MoneyChangedEventHandler(float newAmount);
	
	[Signal]
	public delegate void DailyFinanceProcessedEventHandler(float revenue, float expenses, float profit);
	
	[Signal]
	public delegate void LoanTakenEventHandler(float amount, float interestRate, int termInDays);
	
	[Signal]
	public delegate void LoanPaidEventHandler(float amount);
	
	[Signal]
	public delegate void LoanPaymentMissedEventHandler(float amount, float penalty);
	
	[Signal]
	public delegate void LoanDefaultedEventHandler(float remainingAmount);
	
	[Signal]
	public delegate void PriceMultiplierChangedEventHandler(string category, float multiplier);
	
	[Signal]
	public delegate void BankruptcyEventHandler();

	public override void _Ready()
	{
		// Gelir kategorilerini başlat
		_incomeCategories.Add("drinks", 0f);            // İçecek satışları
		_incomeCategories.Add("food", 0f);              // Yemek satışları
		_incomeCategories.Add("entertainment", 0f);     // Eğlence gelirleri (dans, müzik vb.)
		_incomeCategories.Add("vip", 0f);               // VIP hizmetler
		_incomeCategories.Add("gambling", 0f);          // Kumar gelirleri
		_incomeCategories.Add("drugs", 0f);             // Uyuşturucu satışları
		_incomeCategories.Add("other", 0f);             // Diğer gelirler
		
		// Gider kategorilerini başlat
		_expenseCategories.Add("staff", 0f);            // Personel maaşları
		_expenseCategories.Add("supplies", 0f);         // Malzeme alımları (içki, yemek)
		_expenseCategories.Add("maintenance", 0f);      // Bakım ve tamir
		_expenseCategories.Add("utilities", 0f);        // Faturalar
		_expenseCategories.Add("bribe", 0f);            // Rüşvet ödemeleri
		_expenseCategories.Add("loan", 0f);             // Kredi ödemeleri
		_expenseCategories.Add("fine", 0f);             // Cezalar
		_expenseCategories.Add("other", 0f);            // Diğer giderler
		
		// Kredi türlerini başlat
		_loanTypes.Add(LoanType.Small, new LoanInfo(5000f, 0.05f, 30));         // 5.000 TL, %5 faiz, 30 gün
		_loanTypes.Add(LoanType.Medium, new LoanInfo(20000f, 0.08f, 60));       // 20.000 TL, %8 faiz, 60 gün
		_loanTypes.Add(LoanType.Large, new LoanInfo(50000f, 0.12f, 90));        // 50.000 TL, %12 faiz, 90 gün
		_loanTypes.Add(LoanType.Emergency, new LoanInfo(10000f, 0.25f, 15));    // 10.000 TL, %25 faiz, 15 gün
		
		GD.Print("EconomyManager initialized.");
	}

	public void SetupInitialFunds()
	{
		// Oyun başlangıcında verilen başlangıç parası
		AddMoney(10000f, "initial_funds");
		GD.Print("Initial funds added: 10000 TL");
	}
	
	// Para ekleme metodu
	public void AddMoney(float amount, string category = "other")
	{
		if (amount <= 0) return;
		
		CurrentMoney += amount;
		DailyRevenue += amount;
		TotalRevenue += amount;
		
		// Kategori bazlı gelir takibi
		if (_incomeCategories.ContainsKey(category))
		{
			_incomeCategories[category] += amount;
		}
		else
		{
			_incomeCategories["other"] += amount;
		}
		
		EmitSignal(SignalName.MoneyChanged, CurrentMoney);
		GD.Print($"Added {amount} TL to {category}. Current money: {CurrentMoney} TL");
	}
	
	// Para çıkarma metodu
	public bool SpendMoney(float amount, string category = "other")
	{
		if (amount <= 0) return true;
		
		if (CurrentMoney >= amount)
		{
			CurrentMoney -= amount;
			DailyExpenses += amount;
			TotalExpenses += amount;
			
			// Kategori bazlı gider takibi
			if (_expenseCategories.ContainsKey(category))
			{
				_expenseCategories[category] += amount;
			}
			else
			{
				_expenseCategories["other"] += amount;
			}
			
			EmitSignal(SignalName.MoneyChanged, CurrentMoney);
			GD.Print($"Spent {amount} TL on {category}. Current money: {CurrentMoney} TL");
			return true;
		}
		else
		{
			GD.Print($"Not enough money to spend {amount} TL. Current money: {CurrentMoney} TL");
			return false;
		}
	}
	
	// Günlük finansal işlem
	public void ProcessDailyFinances()
	{
		// Kredi ödemesi kontrolü
		ProcessLoanPayment();
		
		// Günlük finansal kaydı oluştur
		var record = new DailyFinancialRecord(
			GameManager.Instance.CurrentDay,
			DailyRevenue,
			DailyExpenses,
			new Dictionary<string, float>(_incomeCategories),
			new Dictionary<string, float>(_expenseCategories)
		);
		
		_financialHistory.Add(record);
		
		// Sinyal gönder
		EmitSignal(SignalName.DailyFinanceProcessed, DailyRevenue, DailyExpenses, DailyProfit);
		
		// Günlük verileri sıfırla
		ResetDailyFinances();
		
		// Gelir ve giderleri GameManager'a bildir
		GameManager.Instance.UpdateGameStats(DailyRevenue, DailyExpenses, 0, false);
		
		GD.Print($"Daily finances processed. Revenue: {DailyRevenue} TL, Expenses: {DailyExpenses} TL, Profit: {DailyProfit} TL");
	}
	
	// Günlük finansal verileri sıfırla
	private void ResetDailyFinances()
	{
		DailyRevenue = 0f;
		DailyExpenses = 0f;
		
		// Kategori bazlı gelir/gider takibi sıfırla
		foreach (var key in _incomeCategories.Keys)
		{
			_incomeCategories[key] = 0f;
		}
		
		foreach (var key in _expenseCategories.Keys)
		{
			_expenseCategories[key] = 0f;
		}
		
		GD.Print("Daily finances reset.");
	}
	
	// Kredi al
	public bool TakeLoan(LoanType loanType)
	{
		if (HasActiveLoan)
		{
			GD.Print("Cannot take a new loan while having an active loan.");
			return false;
		}
		
		if (!_loanTypes.ContainsKey(loanType))
		{
			GD.Print($"Invalid loan type: {loanType}");
			return false;
		}
		
		var loanInfo = _loanTypes[loanType];
		float effectiveInterestRate = CalculateEffectiveInterestRate(loanInfo.BaseInterestRate);
		
		// Kredi bilgilerini ayarla
		CurrentLoanAmount = loanInfo.Amount;
		CurrentLoanInterestRate = effectiveInterestRate;
		LoanTermInDays = loanInfo.TermInDays;
		RemainingLoanDays = loanInfo.TermInDays;
		
		// Günlük ödeme miktarını hesapla (basit faiz ile)
		float totalPayment = CurrentLoanAmount * (1 + CurrentLoanInterestRate);
		DailyLoanPayment = totalPayment / LoanTermInDays;
		
		// Parayı oyuncuya ekle
		AddMoney(CurrentLoanAmount, "loan");
		
		// Sinyal gönder
		EmitSignal(SignalName.LoanTaken, CurrentLoanAmount, CurrentLoanInterestRate, LoanTermInDays);
		
		GD.Print($"Loan taken: {CurrentLoanAmount} TL with {CurrentLoanInterestRate*100}% interest rate for {LoanTermInDays} days. Daily payment: {DailyLoanPayment} TL");
		return true;
	}
	
	// Etkili faiz oranını hesapla (itibar, geçmiş ödeme davranışı vb. faktörlere göre)
	private float CalculateEffectiveInterestRate(float baseRate)
	{
		float effectiveRate = baseRate;
		
		// İtibar etkisi
		if (GameManager.Instance.ReputationManager != null)
		{
			float reputationModifier = 1.0f - (GameManager.Instance.ReputationManager.CurrentReputation / 100f) * 0.3f;
			effectiveRate *= reputationModifier;
		}
		
		// Geçmiş ödeme davranışı
		if (MissedPayments > 0)
		{
			effectiveRate *= (1.0f + MissedPayments * 0.2f);
		}
		
		return Mathf.Clamp(effectiveRate, 0.01f, 0.5f); // %1 ile %50 arasında sınırla
	}
	
	// Kredi ödemesi işle
	private void ProcessLoanPayment()
	{
		if (!HasActiveLoan || RemainingLoanDays <= 0)
			return;
		
		RemainingLoanDays--;
		
		// Ödeme yapabilecek durumdaysa öde
		if (CurrentMoney >= DailyLoanPayment)
		{
			SpendMoney(DailyLoanPayment, "loan");
			CurrentLoanAmount -= (DailyLoanPayment / (1 + CurrentLoanInterestRate)) * (1 + CurrentLoanInterestRate/LoanTermInDays);
			
			// Kredi tamamen ödendiyse
			if (RemainingLoanDays <= 0 || CurrentLoanAmount <= 0)
			{
				float finalPayment = CurrentLoanAmount;
				
				if (finalPayment > 0)
				{
					SpendMoney(finalPayment, "loan");
				}
				
				EmitSignal(SignalName.LoanPaid, CurrentLoanAmount + finalPayment);
				ResetLoan();
				GD.Print("Loan fully paid off!");
			}
		}
		else
		{
			// Ödeme yapılamadı, ceza uygula
			MissedPayments++;
			float penaltyAmount = DailyLoanPayment * LatePaymentPenaltyRate;
			CurrentLoanAmount += penaltyAmount;
			
			EmitSignal(SignalName.LoanPaymentMissed, DailyLoanPayment, penaltyAmount);
			GD.Print($"Loan payment missed! Penalty: {penaltyAmount} TL. Total loan amount increased to: {CurrentLoanAmount} TL");
			
			// Maksimum kaçırılabilecek ödeme sayısını aştıysa, kredi temerrüdü (default)
			if (MissedPayments >= MaxMissedPayments)
			{
				HandleLoanDefault();
			}
		}
	}
	
	// Kredi temerrüdü (default) işle
	private void HandleLoanDefault()
	{
		EmitSignal(SignalName.LoanDefaulted, CurrentLoanAmount);
		GD.Print($"Loan defaulted! Amount: {CurrentLoanAmount} TL");
		
		// Mafya veya başka bir alacaklı devreye girebilir
		if (GameManager.Instance.ReputationManager != null)
		{
			GameManager.Instance.ReputationManager.DecreaseMafiaReputation(50); // Mafya itibarını ciddi şekilde düşür
			GameManager.Instance.ReputationManager.DecreaseCityReputation(30);  // Şehir itibarını düşür
		}
		
		// Özel bir olay tetiklenebilir
		if (GameManager.Instance.EventManager != null)
		{
			GameManager.Instance.EventManager.TriggerEvent("LoanDefault");
		}
		
		ResetLoan();
		
		// Eğer oyuncu iflasa sürüklenecekse
		CheckForBankruptcy();
	}
	
	// Kredi sıfırla
	private void ResetLoan()
	{
		CurrentLoanAmount = 0f;
		CurrentLoanInterestRate = 0.05f;
		LoanTermInDays = 0;
		RemainingLoanDays = 0;
		DailyLoanPayment = 0f;
		GD.Print("Loan reset.");
	}
	
	// İflas kontrolü
	private void CheckForBankruptcy()
	{
		if (CurrentMoney <= -10000f) // Belirli bir borç limitini aşarsa
		{
			EmitSignal(SignalName.Bankruptcy);
			GD.Print("BANKRUPTCY! Game over condition triggered.");
			
			// Oyun bitişi ekranı vs. gösterilebilir
			if (GameManager.Instance != null)
			{
				GameManager.Instance.ShowGameNotification("İFLAS ETTİNİZ!", GameManager.NotificationType.Error);
				// Özel iflas ekranını göstermek için event tetikle
				GameManager.Instance.EventManager.TriggerEvent("Bankruptcy");
			}
		}
	}
	
	// Erken kredi ödemesi
	public bool MakeEarlyLoanPayment(float amount)
	{
		if (!HasActiveLoan || amount <= 0)
			return false;
		
		if (CurrentMoney < amount)
		{
			GD.Print($"Not enough money to make early payment. Current money: {CurrentMoney} TL");
			return false;
		}
		
		// Ödenecek miktarı hesapla (erken ödeme için küçük bir indirim)
		float effectivePayment = amount * 0.95f; // %5 erken ödeme indirimi
		
		// Parayı harca
		SpendMoney(amount, "loan");
		
		// Kredi tutarını azalt
		CurrentLoanAmount -= effectivePayment;
		
		// Kredi tamamen ödendiyse sıfırla
		if (CurrentLoanAmount <= 0)
		{
			EmitSignal(SignalName.LoanPaid, CurrentLoanAmount + effectivePayment);
			ResetLoan();
			GD.Print("Loan fully paid off with early payment!");
		}
		else
		{
			// Günlük ödeme miktarını güncelle
			float remainingPayment = CurrentLoanAmount * (1 + CurrentLoanInterestRate);
			DailyLoanPayment = remainingPayment / RemainingLoanDays;
			GD.Print($"Early payment made. Remaining loan: {CurrentLoanAmount} TL. New daily payment: {DailyLoanPayment} TL");
		}
		
		return true;
	}
	
	// Fiyat çarpanlarını güncelle
	public void UpdatePriceMultiplier(string category, float multiplier)
	{
		if (multiplier < 0.5f) multiplier = 0.5f; // Minimum %50
		if (multiplier > 2.0f) multiplier = 2.0f; // Maksimum %200
		
		switch (category.ToLower())
		{
			case "alcohol":
			case "drinks":
				AlcoholPriceMultiplier = multiplier;
				break;
			case "food":
				FoodPriceMultiplier = multiplier;
				break;
			case "entertainment":
				EntertainmentPriceMultiplier = multiplier;
				break;
			default:
				GD.Print($"Unknown price multiplier category: {category}");
				return;
		}
		
		EmitSignal(SignalName.PriceMultiplierChanged, category, multiplier);
		GD.Print($"{category} price multiplier updated to: {multiplier}");
		
		// Müşteri memnuniyetini etkileyebilir
		UpdateCustomerSatisfactionForPriceChange(category, multiplier);
	}
	
	// Fiyat değişikliğine bağlı müşteri memnuniyeti güncelleme
	private void UpdateCustomerSatisfactionForPriceChange(string category, float multiplier)
	{
		if (GameManager.Instance?.CustomerManager == null)
			return;
		
		float satisfactionModifier = 0f;
		
		// Fiyat yüksekse memnuniyet düşer, düşükse artar
		if (multiplier > 1.0f)
		{
			satisfactionModifier = -(multiplier - 1.0f) * 10f; // %10 artış için -%1 memnuniyet
		}
		else
		{
			satisfactionModifier = (1.0f - multiplier) * 5f;   // %10 düşüş için %0.5 memnuniyet
		}
		
		// Farklı müşteri tiplerini etkile
		switch (category.ToLower())
		{
			case "alcohol":
			case "drinks":
				GameManager.Instance.CustomerManager.ModifyAllCustomerSatisfaction(satisfactionModifier, "alcohol_price");
				break;
			case "food":
				GameManager.Instance.CustomerManager.ModifyAllCustomerSatisfaction(satisfactionModifier, "food_price");
				break;
			case "entertainment":
				GameManager.Instance.CustomerManager.ModifyAllCustomerSatisfaction(satisfactionModifier, "entertainment_price");
				break;
		}
	}
	
	// Günlük gelir tahminini hesapla
	public float EstimateDailyIncome()
	{
		// Son 7 günün ortalaması
		if (_financialHistory.Count == 0)
			return 0f;
		
		int daysToConsider = Mathf.Min(7, _financialHistory.Count);
		float totalRevenue = 0f;
		
		for (int i = _financialHistory.Count - 1; i >= _financialHistory.Count - daysToConsider; i--)
		{
			totalRevenue += _financialHistory[i].Revenue;
		}
		
		return totalRevenue / daysToConsider;
	}
	
	// Günlük gider tahminini hesapla
	public float EstimateDailyExpenses()
	{
		// Son 7 günün ortalaması
		if (_financialHistory.Count == 0)
			return 0f;
		
		int daysToConsider = Mathf.Min(7, _financialHistory.Count);
		float totalExpenses = 0f;
		
		for (int i = _financialHistory.Count - 1; i >= _financialHistory.Count - daysToConsider; i--)
		{
			totalExpenses += _financialHistory[i].Expenses;
		}
		
		return totalExpenses / daysToConsider;
	}
	
	// Belirli bir kategorideki geliri al
	public float GetIncomeByCategory(string category)
	{
		if (_incomeCategories.ContainsKey(category))
		{
			return _incomeCategories[category];
		}
		return 0f;
	}
	
	// Belirli bir kategorideki gideri al
	public float GetExpenseByCategory(string category)
	{
		if (_expenseCategories.ContainsKey(category))
		{
			return _expenseCategories[category];
		}
		return 0f;
	}
	
	// Finansal geçmişi al
	public List<DailyFinancialRecord> GetFinancialHistory()
	{
		return _financialHistory;
	}
	
	// Son X günün finansal geçmişini al
	public List<DailyFinancialRecord> GetRecentFinancialHistory(int days)
	{
		int recordCount = Mathf.Min(days, _financialHistory.Count);
		List<DailyFinancialRecord> recentHistory = new List<DailyFinancialRecord>();
		
		for (int i = _financialHistory.Count - recordCount; i < _financialHistory.Count; i++)
		{
			recentHistory.Add(_financialHistory[i]);
		}
		
		return recentHistory;
	}
	
	// Finansal durumu kaydet (SaveSystem için)
	public Dictionary<string, object> SaveFinancialState()
	{
		var data = new Dictionary<string, object>
		{
			{ "current_money", CurrentMoney },
			{ "total_revenue", TotalRevenue },
			{ "total_expenses", TotalExpenses },
			{ "current_loan_amount", CurrentLoanAmount },
			{ "current_loan_interest_rate", CurrentLoanInterestRate },
			{ "loan_term_in_days", LoanTermInDays },
			{ "remaining_loan_days", RemainingLoanDays },
			{ "daily_loan_payment", DailyLoanPayment },
			{ "missed_payments", MissedPayments },
			{ "alcohol_price_multiplier", AlcoholPriceMultiplier },
			{ "food_price_multiplier", FoodPriceMultiplier },
			{ "entertainment_price_multiplier", EntertainmentPriceMultiplier }
		};
		
		return data;
	}
	
	// Finansal durumu yükle (SaveSystem için)
	public void LoadFinancialState(Dictionary<string, object> data)
	{
		if (data.ContainsKey("current_money")) CurrentMoney = (float)data["current_money"];
		if (data.ContainsKey("total_revenue")) TotalRevenue = (float)data["total_revenue"];
		if (data.ContainsKey("total_expenses")) TotalExpenses = (float)data["total_expenses"];
		if (data.ContainsKey("current_loan_amount")) CurrentLoanAmount = (float)data["current_loan_amount"];
		if (data.ContainsKey("current_loan_interest_rate")) CurrentLoanInterestRate = (float)data["current_loan_interest_rate"];
		if (data.ContainsKey("loan_term_in_days")) LoanTermInDays = (int)data["loan_term_in_days"];
		if (data.ContainsKey("remaining_loan_days")) RemainingLoanDays = (int)data["remaining_loan_days"];
		if (data.ContainsKey("daily_loan_payment")) DailyLoanPayment = (float)data["daily_loan_payment"];
		if (data.ContainsKey("missed_payments")) MissedPayments = (int)data["missed_payments"];
		if (data.ContainsKey("alcohol_price_multiplier")) AlcoholPriceMultiplier = (float)data["alcohol_price_multiplier"];
		if (data.ContainsKey("food_price_multiplier")) FoodPriceMultiplier = (float)data["food_price_multiplier"];
		if (data.ContainsKey("entertainment_price_multiplier")) EntertainmentPriceMultiplier = (float)data["entertainment_price_multiplier"];
		
		GD.Print("Financial state loaded.");
	}
}
