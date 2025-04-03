// src/Economy/EconomyManager.cs
using Godot;
using System;
using System.Collections.Generic;
using PavyonTycoon.Core;

namespace PavyonTycoon
{
	public partial class EconomyManager : Node
	{
		// Money and financial data
		public float Money { get; private set; }
		public float DailyIncome { get; private set; }
		public float DailyExpenses { get; private set; }
		
		// Income tracking
		private Dictionary<string, float> incomeBySource = new Dictionary<string, float>();
		private Dictionary<string, float> expensesByCategory = new Dictionary<string, float>();
		
		// Transaction history
		private List<Transaction> transactions = new List<Transaction>();
		private List<Transaction> dailyTransactions = new List<Transaction>();

		// Income categories
		public static class IncomeCategory
		{
			public const string Drinks = "İçecekler";
			public const string Food = "Yiyecekler";
			public const string Entertainment = "Eğlence";
			public const string Tips = "Bahşişler";
			public const string VIP = "VIP Hizmetler";
			public const string IllegalFloor = "Kaçak Kat";
			public const string Blackmail = "Şantaj";
			public const string Other = "Diğer";
		}

		// Expense categories
		public static class ExpenseCategory
		{
			public const string Salaries = "Maaşlar";
			public const string Supplies = "Malzemeler";
			public const string Utilities = "Genel Giderler";
			public const string Maintenance = "Bakım";
			public const string Bribes = "Rüşvetler";
			public const string Furnishing = "Mobilya";
			public const string Marketing = "Pazarlama";
			public const string MafiaPayments = "Mafya Ödemeleri";
			public const string Other = "Diğer";
		}

		// Public property to access current report
		public DailyFinancialReport CurrentReport { get; private set; }

		public override void _Ready()
		{
			// Set initial money
			Money = GameManager.STARTING_MONEY;
			
			// Subscribe to signals
			var timeManager = GetParent().GetNode<TimeManager>("TimeManager");
			if (timeManager != null)
			{
				timeManager.Connect(TimeManager.SignalName.DayEnded, Callable.From(ProcessEndOfDay));
			}
			else
			{
				GD.PrintErr("EconomyManager: TimeManager not found for connecting signals");
			}
			
			GD.Print($"💰 Economy initialized with {Money}₺");
		}

		public void ProcessTransaction(Transaction transaction)
		{
			// Update money
			Money += transaction.Amount;
			
			// Record transaction
			transactions.Add(transaction);
			dailyTransactions.Add(transaction);
			
			// Track by category
			if (transaction.Amount > 0)
			{
				// Income
				if (!incomeBySource.ContainsKey(transaction.Category))
				{
					incomeBySource[transaction.Category] = 0;
				}
				incomeBySource[transaction.Category] += transaction.Amount;
				DailyIncome += transaction.Amount;
				
				GD.Print($"💸 Gelir: {transaction.Amount}₺ - {transaction.Description} ({transaction.Category})");
			}
			else
			{
				// Expense (convert to positive for tracking)
				float expense = -transaction.Amount;
				if (!expensesByCategory.ContainsKey(transaction.Category))
				{
					expensesByCategory[transaction.Category] = 0;
				}
				expensesByCategory[transaction.Category] += expense;
				DailyExpenses += expense;
				
				GD.Print($"💸 Gider: {expense}₺ - {transaction.Description} ({transaction.Category})");
			}
			
			// Emit signal for UI updates
			EmitSignal(SignalName.MoneyChanged, Money);
			EmitSignal(SignalName.TransactionProcessed, transaction.Description, transaction.Amount);
		}

		private void ProcessEndOfDay(int day)
		{
			// Calculate daily summary
			float dailyProfit = DailyIncome - DailyExpenses;
			
			GD.Print($"📊 Günlük Finansal Rapor (Gün {day}):");
			GD.Print($"  Toplam Gelir: {DailyIncome}₺");
			GD.Print($"  Toplam Gider: {DailyExpenses}₺");
			GD.Print($"  Net Kar/Zarar: {dailyProfit}₺");
			
			// Create daily report
			CurrentReport = new DailyFinancialReport
			{
				Day = day,
				TotalIncome = DailyIncome,
				TotalExpenses = DailyExpenses,
				Profit = dailyProfit,
				IncomeBySource = new Dictionary<string, float>(incomeBySource),
				ExpensesByCategory = new Dictionary<string, float>(expensesByCategory),
				Transactions = new List<Transaction>(dailyTransactions)
			};
			
			// Emit signal without complex parameter
			EmitSignal(SignalName.DailyReportGenerated);
			
			// Reset daily tracking
			DailyIncome = 0;
			DailyExpenses = 0;
			incomeBySource.Clear();
			expensesByCategory.Clear();
			dailyTransactions.Clear();
		}

		// Convenience methods for common transactions
		public void AddIncome(float amount, string source, string description)
		{
			ProcessTransaction(new Transaction
			{
				Amount = Mathf.Abs(amount), // Ensure positive
				Category = source,
				Description = description,
				Timestamp = DateTime.Now
			});
		}

		public void AddExpense(float amount, string category, string description)
		{
			ProcessTransaction(new Transaction
			{
				Amount = -Mathf.Abs(amount), // Ensure negative
				Category = category,
				Description = description,
				Timestamp = DateTime.Now
			});
		}

		// Money formatting helper
		public static string FormatMoney(float amount)
		{
			return $"{amount:N0}₺";
		}

		// Export report data to a simple format for UI
		public Godot.Collections.Dictionary GetReportDictionary()
		{
			if (CurrentReport == null)
				return new Godot.Collections.Dictionary();

			var result = new Godot.Collections.Dictionary();
			result["day"] = CurrentReport.Day;
			result["income"] = CurrentReport.TotalIncome;
			result["expenses"] = CurrentReport.TotalExpenses;
			result["profit"] = CurrentReport.Profit;
			
			// Convert income categories to format suitable for signal
			var incomeDict = new Godot.Collections.Dictionary();
			foreach (var item in CurrentReport.IncomeBySource)
			{
				incomeDict[item.Key] = item.Value;
			}
			result["income_by_source"] = incomeDict;
			
			// Convert expense categories to format suitable for signal
			var expenseDict = new Godot.Collections.Dictionary();
			foreach (var item in CurrentReport.ExpensesByCategory)
			{
				expenseDict[item.Key] = item.Value;
			}
			result["expenses_by_category"] = expenseDict;
			
			return result;
		}

		// Signal definitions
		[Signal] public delegate void MoneyChangedEventHandler(float newAmount);
		[Signal] public delegate void TransactionProcessedEventHandler(string description, float amount);
		[Signal] public delegate void DailyReportGeneratedEventHandler(); // Changed this to not use complex parameter
	}

	public class Transaction
	{
		public float Amount { get; set; } // Positive for income, negative for expenses
		public string Category { get; set; } // e.g., "Drinks", "Staff Salary", "Furniture"
		public string Description { get; set; }
		public DateTime Timestamp { get; set; }
	}

	public class DailyFinancialReport
	{
		public int Day { get; set; }
		public float TotalIncome { get; set; }
		public float TotalExpenses { get; set; }
		public float Profit { get; set; }
		public Dictionary<string, float> IncomeBySource { get; set; }
		public Dictionary<string, float> ExpensesByCategory { get; set; }
		public List<Transaction> Transactions { get; set; }
	}
}
