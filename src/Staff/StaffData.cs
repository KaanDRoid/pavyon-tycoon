// src/Staff/StaffData.cs
using Godot;
using System;
using System.Collections.Generic;

namespace PavyonTycoon.Staff
{
	public static class StaffData
	{
		// Random name generation data
		private static string[] maleFirstNames = new string[]
		{
			"Ahmet", "Mehmet", "Ali", "Mustafa", "Hasan", "Hüseyin", "İbrahim", "Osman",
			"Murat", "Yusuf", "Recep", "Halil", "Cem", "Serkan", "Emre", "Burak", "Onur",
			"Sinan", "Erkan", "Volkan", "Doğan", "Cengiz", "Tolga", "Koray", "Uğur", "Ayhan",
			"Özgür", "Mete", "Selim", "Gökhan", "Baran", "Deniz", "Ercan", "Kemal", "Ufuk",
			"Tarık", "Selçuk", "Alper", "Mert", "Tuncay", "İlker", "Barış", "Levent", "Orhan"
		};
		
		private static string[] femaleFirstNames = new string[]
		{
			"Ayşe", "Fatma", "Zeynep", "Elif", "Emine", "Merve", "Esra", "Ebru", "Seda",
			"Derya", "Gül", "Hülya", "Özge", "Ceyda", "Serap", "Şule", "Deniz", "Filiz",
			"Pınar", "Yasemin", "Sibel", "Beyza", "Yeliz", "Melek", "Çiğdem", "Gamze",
			"Burcu", "Demet", "Tuğba", "Sevim", "Melis", "Asena", "Berna", "Sedef", "Duygu",
			"Nurgül", "Gizem", "Gülşen", "Buket", "Emel", "Meltem", "Zehra", "Selin", "Elvan"
		};
		
		private static string[] lastNames = new string[]
		{
			"Yılmaz", "Kaya", "Demir", "Çelik", "Şahin", "Yıldız", "Yıldırım", "Öztürk",
			"Aydın", "Özdemir", "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin", "Kara", "Kurt",
			"Koç", "Turan", "Polat", "Yalçın", "Şen", "Güneş", "Şimşek", "Özkan", "Korkmaz",
			"Çakır", "Ateş", "Tekin", "Yüksel", "Deniz", "Bulut", "Esen", "Can", "Aktaş",
			"Demirci", "Bozkurt", "Taş", "Aksoy", "Erdem", "Yavuz", "Altın", "Keskin", "Gül"
		};
		
		private static string[] konsNicknames = new string[]
		{
			"Kraliçe", "Sultan", "Prenses", "Melek", "İnci", "Yıldız", "Rüya", "Ceylan",
			"Peri", "Nazlı", "Bal", "Buse", "Gülistan", "Bahar", "Fidan", "Tatlı",
			"Kiraz", "Zambak", "Gümüş", "Şeker", "Papatya", "Leyla", "Berna", "Çiçek"
		};
		
		private static string[] securityNicknames = new string[]
		{
			"Beton", "Kaya", "Tank", "Dağ", "Avcı", "Kurt", "Kaplan", "Çelik",
			"Şahin", "Kartal", "Kasırga", "Pala", "Kanun", "Aslan", "Kral", "Ejder",
			"Yumruk", "Barut", "Kabadayı", "Duvar", "Tosun", "Yıldırım", "Balyoz"
		};
		
		private static string[] musicianInstruments = new string[]
		{
			"Udi", "Kanuncu", "Kemancı", "Cümbüşçü", "Klarnetçi", "Hanende", "Solist",
			"Bağlamacı", "Darbukacı", "Virtüöz", "Davulcu", "Zurna", "Tamburcu"
		};
		
		// Attribute definitions
		public static Dictionary<string, string> AttributeDefinitions = new Dictionary<string, string>
		{
			{"Karizma", "Müşterileri ve diğer çalışanları etkileme ve yönlendirme yeteneği"},
			{"Hız", "İşleri hızlı bir şekilde tamamlama becerisi"},
			{"Güç", "Fiziksel kuvvet ve dayanıklılık"},
			{"Dikkat", "Detaylara odaklanma ve hata yapmadan çalışabilme"},
			{"Sosyallik", "İnsanlarla etkileşim kurma ve iletişim becerisi"},
			{"Müzik", "Müzikal yetenek ve performans kalitesi"},
			{"Tehdit", "İnsanları korkutma ve caydırma yeteneği"},
			{"Lezzet", "Yemek hazırlama ve sunum kalitesi"},
			{"Performans", "Sahne şovu ve eğlence kalitesi"},
			{"Yaratıcılık", "Yenilikçi fikirler ve özgün çözümler üretebilme"},
			{"Gizlilik", "Sırları saklama ve fark edilmeden hareket edebilme"},
			{"Sadakat", "Zor zamanlarda bile işletmeye bağlı kalma"},
			{"İkna", "Başkalarını kendi bakış açısına getirme yeteneği"},
			{"Uyanıklık", "Fırsatları görme ve değerlendirme becerisi"},
			{"Dayanıklılık", "Stresli durumlarda performansını koruyabilme"},
			{"Organizasyon", "İşleri düzenli ve planlı bir şekilde yürütebilme"},
			{"Pazarlık", "Daha iyi anlaşmalar yapabilme becerisi"},
			{"Yemek", "Lezzetli ve kaliteli yemekler hazırlama becerisi"}
		};
		
		// Task types definitions
		public static Dictionary<string, string> TaskTypeDefinitions = new Dictionary<string, string>
		{
			{"ÖzelMüşteriAğırlama", "VIP müşterileri ağırlama ve memnun etme"},
			{"MüşteriEğlendirme", "Standart müşterileri eğlendirme ve içki satışını artırma"},
			{"SorunÇözme", "Müşteri veya personel sorunlarını çözme"},
			{"GüvenlikSağlama", "Mekanın güvenliğini sağlama ve olaylara müdahale etme"},
			{"İçecekHazırlama", "İçecek hazırlama ve servis etme"},
			{"YemekHazırlama", "Yemek hazırlama ve sunum"},
			{"TemizlikYapma", "Mekanın temizliğini ve düzenini sağlama"},
			{"MüzikPerformansı", "Canlı müzik performansı sergileme"},
			{"YasadışıFaaliyet", "Kaçak kat ve diğer yasadışı faaliyetleri yürütme"},
			{"MafyaBağlantısı", "Mafya ile ilişkileri yönetme"},
			{"MüşteriGözlemleme", "Müşterileri gözlemleme ve bilgi toplama"}
		};
		
		// Methods for random name generation
		public static string GetRandomName(string jobTitle)
		{
			string fullName = "";
			
			switch (jobTitle)
			{
				case "Kons":
					// Konslar kadın ve takma isimli
					string femaleFirstName = femaleFirstNames[GD.RandRange(0, femaleFirstNames.Length - 1)];
					string konsNick = konsNicknames[GD.RandRange(0, konsNicknames.Length - 1)];
					fullName = femaleFirstName + " '" + konsNick + "'";
					break;
					
				case "Güvenlik":
					// Güvenlikçiler genellikle erkek ve takma isimli
					string securityFirstName = maleFirstNames[GD.RandRange(0, maleFirstNames.Length - 1)];
					string securityNick = securityNicknames[GD.RandRange(0, securityNicknames.Length - 1)];
					fullName = securityFirstName + " '" + securityNick + "'";
					break;
					
				case "Müzisyen":
					// Müzisyenler için enstrüman belirtilir
					string musicianName;
					if (GD.Randf() < 0.8f) // 80% erkek müzisyen
						musicianName = maleFirstNames[GD.RandRange(0, maleFirstNames.Length - 1)];
					else
						musicianName = femaleFirstNames[GD.RandRange(0, femaleFirstNames.Length - 1)];
					
					string lastName = lastNames[GD.RandRange(0, lastNames.Length - 1)];
					string instrument = musicianInstruments[GD.RandRange(0, musicianInstruments.Length - 1)];
					fullName = musicianName + " " + lastName + " (" + instrument + ")";
					break;
					
				case "Kaçak Kat Görevlisi":
					// Kaçak kat görevlileri daha gizemli
					if (GD.Randf() < 0.7f) // 70% erkek
					{
						string firstName = maleFirstNames[GD.RandRange(0, maleFirstNames.Length - 1)];
						fullName = firstName;
					}
					else
					{
						string firstName = femaleFirstNames[GD.RandRange(0, femaleFirstNames.Length - 1)];
						fullName = firstName;
					}
					break;
					
				default:
					// Diğer personel türleri için normal isim
					string randomFirstName;
					if (GD.Randf() < 0.5f) // 50-50 erkek/kadın dağılımı
						randomFirstName = maleFirstNames[GD.RandRange(0, maleFirstNames.Length - 1)];
					else
						randomFirstName = femaleFirstNames[GD.RandRange(0, femaleFirstNames.Length - 1)];
					
					string randomLastName = lastNames[GD.RandRange(0, lastNames.Length - 1)];
					fullName = randomFirstName + " " + randomLastName;
					break;
			}
			
			return fullName;
		}
		
		// Get a random attribute name
		public static string GetRandomAttributeName()
		{
			string[] attributeKeys = new string[AttributeDefinitions.Count];
			AttributeDefinitions.Keys.CopyTo(attributeKeys, 0);
			
			return attributeKeys[GD.RandRange(0, attributeKeys.Length - 1)];
		}
		
		// Get a random task type
		public static string GetRandomTaskType()
		{
			string[] taskKeys = new string[TaskTypeDefinitions.Count];
			TaskTypeDefinitions.Keys.CopyTo(taskKeys, 0);
			
			return taskKeys[GD.RandRange(0, taskKeys.Length - 1)];
		}
		
		// Get attribute description
		public static string GetAttributeDescription(string attributeName)
		{
			if (AttributeDefinitions.ContainsKey(attributeName))
			{
				return AttributeDefinitions[attributeName];
			}
			return "Tanımsız özellik";
		}
		
		// Get task description
		public static string GetTaskDescription(string taskType)
		{
			if (TaskTypeDefinitions.ContainsKey(taskType))
			{
				return TaskTypeDefinitions[taskType];
			}
			return "Tanımsız görev";
		}
	}
}
