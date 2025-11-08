using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCBack.Models.Tracker;

public enum MoodLevel { None=0, VeryBad=1, Bad=2, Neutral=3, Good=4, Great=5 }
public enum AppetiteLevel { None=0, Poor=1, Normal=2, Good=3 }
public enum ToiletingStatus { None=0, Dry=1, Accident=2, Success=3 }

public class DailyEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid ChildId { get; set; }
    public Child? Child { get; set; }

    // день в UTC (дата без времени). Храним как DateOnly + EF-конверсия или DateTime.Date (00:00Z)
    [Required] public DateOnly Date { get; set; }

    // Сон
    [Range(0,16)] public double? SleepTotalHours { get; set; }
    [Range(0,600)] public int? SleepLatencyMin { get; set; }   // время засыпания
    [Range(0,20)]  public int? NightWakings { get; set; }
    [Range(1,5)]   public int? SleepQuality { get; set; }      // 1..5

    // Настроение/регуляция
    public MoodLevel Mood { get; set; } = MoodLevel.None;
    [Range(0,3)] public int? Anxiety { get; set; }             // 0..3
    public bool SensoryOverload { get; set; }

    // Питание
    [Range(0,10)] public int? MealsCount { get; set; }
    public AppetiteLevel Appetite { get; set; } = AppetiteLevel.None;
    [MaxLength(400)] public string? DietNotes { get; set; }

    // Коммуникация/навыки
    [MaxLength(200)] public string? CommunicationLevel { get; set; } // "PECS", "слова", "фразы"...
    [MaxLength(300)] public string? NewSkillObserved { get; set; }

    // Туалет/самообслуживание
    public ToiletingStatus ToiletingStatus { get; set; } = ToiletingStatus.None;
    public bool? SelfCareDressing { get; set; }  // одевание
    public bool? SelfCareHygiene  { get; set; }  // умывание/зубы

    // Флаги
    public bool? HomeTasksDone { get; set; }
    public bool? RewardUsed { get; set; }

    // Теги окружения/триггеры — храним в JSON (строка), чтобы не городить M2M в MVP
    public string? TriggersJson { get; set; }            // ["шум","ожидание"]
    public string? EnvironmentChangesJson { get; set; }  // ["поездка","гости"]

    [MaxLength(1200)] public string? ParentNote { get; set; }

    public int IncidentsCount { get; set; } // денормаль, пересчитывается из Incidents

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public List<DailyIncident> Incidents { get; set; } = new();
    public List<DailyMedIntake> MedIntakes { get; set; } = new();
    public List<DailySession> Sessions { get; set; } = new();
}
