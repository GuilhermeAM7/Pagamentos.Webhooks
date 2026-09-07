using Application.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Persistence.Configuration
{
    public class EventoWebhookConfiguration : IEntityTypeConfiguration<EventoWebhook>
    {
        public void Configure(EntityTypeBuilder<EventoWebhook> builder)
        {
            builder.ToTable("eventos_webhook");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.IdTransacao).IsRequired().HasMaxLength(100);
            builder.Property(e => e.IdContrato).HasMaxLength(100);

            builder.Property(e => e.Valor).HasPrecision(18, 2);

            builder.Property(e => e.StatusOrigem).HasMaxLength(50);

            builder.Property(e => e.StatusPagamento)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            builder.Property(e => e.Status)              
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(e => e.PayloadJson)
                   .IsRequired()
                   .HasColumnType("jsonb");

            builder.Property(e => e.UltimoErro).HasMaxLength(2000);

            builder.HasIndex(e => e.IdTransacao)
                .IsUnique()
                .HasDatabaseName("ux_eventos_webhook_id_transacao"); ;

            builder.HasIndex(e => e.IdContrato);

            // Dashboard (status + mais recentes) e reconciliador (status + parados).
            builder.HasIndex(e => new { e.Status, e.DataRecebido })
                   .IsDescending(false, true);
        }
    }
}
