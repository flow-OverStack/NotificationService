using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.DAL.Configurations;

public class UserEventConfiguration : IEntityTypeConfiguration<UserEvent>
{
    public void Configure(EntityTypeBuilder<UserEvent> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.EventId).IsRequired();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.InitiatorId).IsRequired();
        builder.Property(x => x.RecipientId).IsRequired();
        builder.Property(x => x.EventType).IsRequired();
        builder.Property(x => x.EntityType).IsRequired();
        builder.Property(x => x.IsRead).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        
        builder.HasIndex(x => new { x.RecipientId, x.IsRead, x.CreatedAt });
    }
}