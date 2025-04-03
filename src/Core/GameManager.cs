// src/Core/GameManager.cs
using Godot;
using System;
using PavyonTycoon.Staff;
using PavyonTycoon.Economy;

namespace PavyonTycoon.Core
{
	public partial class GameManager : Node
	{
		// Singleton instance
		private static GameManager _instance;
		public static GameManager Instance => _instance;
		
		// Game state
		public enum GameState { MainMenu, NightMode, MorningMode, Paused }
		public GameState CurrentState { get; private set; } = GameState.MainMenu;
		
		// Manager references
		public TimeManager Time { get; private set; }
		public EconomyManager Economy { get; private set; }
		public StaffManager Staff { get; private set; }
		public CustomerManager Customers { get; private set; }
		public ReputationManager Reputation { get; private set; }
		public EventManager Events { get; private set; }
		public FurnitureManager Furniture { get; private set; }
		public IllegalActivitiesManager IllegalActivities { get; private set; }
		
		public override void _Ready()
		{
			// Set up singleton
			if (_instance != null)
			{
				QueueFree();
				return;
			}
			_instance = this;
			
			// Initialize managers
			InitializeManagers();
			GD.Print("GameManager initialized");
		}
		
		private void InitializeManagers()
		{
			// Get references to manager nodes
			Time = GetNode<TimeManager>("TimeManager");
			if (Time == null) GD.PrintErr("TimeManager not found!");

			Economy = GetNode<EconomyManager>("EconomyManager");
			if (Economy == null) GD.PrintErr("EconomyManager not found!");
			
			Staff = GetNode<StaffManager>("StaffManager");
			if (Staff == null) GD.PrintErr("StaffManager not found!");

			Customers = GetNodeOrNull<CustomerManager>("CustomerManager") as CustomerManager;
			Reputation = GetNodeOrNull<ReputationManager>("ReputationManager") as ReputationManager;
			Events = GetNodeOrNull<EventManager>("EventManager") as EventManager;
			Furniture = GetNodeOrNull<FurnitureManager>("FurnitureManager") as FurnitureManager;
			IllegalActivities = GetNodeOrNull<IllegalActivitiesManager>("IllegalActivitiesManager") as IllegalActivitiesManager;

			// Log the status
			GD.Print("📋 Manager initialization status:");
			GD.Print($"  - Time: {(Time != null ? "OK ✓" : "Missing ✗")}");
			GD.Print($"  - Economy: {(Economy != null ? "OK ✓" : "Missing ✗")}");
			GD.Print($"  - Staff: {(Staff != null ? "OK ✓" : "Missing ✗")}");
			GD.Print($"  - Customers: {(Customers != null ? "OK ✓" : "Missing ✗")}");
			GD.Print($"  - Events: {(Events != null ? "OK ✓" : "Missing ✗")}");
			GD.Print($"  - Furniture: {(Furniture != null ? "OK ✓" : "Missing ✗")}");
			GD.Print($"  - Reputation: {(Reputation != null ? "OK ✓" : "Missing ✗")}");
			GD.Print($"  - IllegalActivities: {(IllegalActivities != null ? "OK ✓" : "Missing ✗")}");
		}
		
		public void ChangeGameState(GameState newState)
		{
			GameState oldState = CurrentState;
			CurrentState = newState;
			
			GD.Print($"Game state changing: {oldState} -> {newState}");
			
			// Handle state transition
			switch (newState)
			{
				case GameState.MainMenu:
					// TODO: Show main menu
					break;
					
				case GameState.NightMode:
					StartNightMode();
					break;
					
				case GameState.MorningMode:
					StartMorningMode();
					break;
					
				case GameState.Paused:
					// Pause game systems
					if (Time != null) Time.PauseTime();
					break;
			}
			
			// Emit signal for UI updates etc.
			EmitSignal(SignalName.GameStateChanged, (int)oldState, (int)newState);
		}
		
		private void StartNightMode()
		{
			GD.Print("🌙 Starting Night Mode");
			
			// Initialize night business
			if (Time != null) 
			{
				Time.StartNightMode();
			}
			
			if (Economy != null)
			{
				// Process nightly setup expenses
				Economy.AddExpense(50f, EconomyManager.ExpenseCategory.Utilities, "Nightly setup costs");
			}
			
			// Assign staff to night shifts
			if (Staff != null)
			{
				Staff.StartNightShift();
			}
			
			// Initialize customer spawning
			if (Customers != null) 
			{
				Customers.StartSpawning();
			}
			
			// Other night mode setup...
		}
		
		private void StartMorningMode()
		{
			GD.Print("☀️ Starting Morning Mode");
			
			// End night operations
			if (Time != null)
			{
				Time.PauseTime();
			}
			
			// End staff night shifts
			if (Staff != null)
			{
				Staff.EndNightShift();
			}
			
			// Generate morning events
			if (Events != null) 
			{
				Events.GenerateDailyEvents();
			}
			
			// TODO: Show daily report
		}
		
		// Save and load game methods
		public void SaveGame(string saveSlot = "default")
		{
			// Implementation will come later
			GD.Print($"Game save requested for slot: {saveSlot}");
		}
		
		public void LoadGame(string saveSlot = "default")
		{
			// Implementation will come later
			GD.Print($"Game load requested for slot: {saveSlot}");
		}
		
		// Signal definitions
		[Signal] public delegate void GameStateChangedEventHandler(int oldState, int newState);
	}
}
