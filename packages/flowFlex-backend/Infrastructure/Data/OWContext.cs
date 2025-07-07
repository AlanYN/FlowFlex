using Microsoft.EntityFrameworkCore;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Infrastructure.Data;

public class OWContext : DbContext
{
    public OWContext(DbContextOptions<OWContext> options) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
    }

    public DbSet<Onboarding> Onboardings { get; set; }
    public DbSet<Stage> Stages { get; set; }
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<Questionnaire> Questionnaires { get; set; }
    public DbSet<QuestionnaireAnswer> QuestionnaireAnswers { get; set; }
    public DbSet<Checklist> Checklists { get; set; }
    public DbSet<ChecklistTask> ChecklistTasks { get; set; }
    public DbSet<InternalNote> InternalNotes { get; set; }
    public DbSet<OnboardingFile> OnboardingFiles { get; set; }
    public DbSet<OperationChangeLog> OperationChangeLogs { get; set; }
    public DbSet<StaticFieldValue> StaticFieldValues { get; set; }
    public DbSet<StageCompletionLog> StageCompletionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names to snake_case
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (!string.IsNullOrWhiteSpace(entity.GetTableName()))
            {
                entity.SetTableName(entity.GetTableName()!.ToSnakeCase());
            }

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }
        }
    }
}

public static class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
    }
} 
