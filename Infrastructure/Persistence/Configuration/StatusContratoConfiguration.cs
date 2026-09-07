using Application.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Persistence.Configuration
{
    public class StatusContratoConfiguration : IEntityTypeConfiguration<StatusContrato>
    {
        public void Configure(EntityTypeBuilder<StatusContrato> builder)
        {
            builder.ToTable("status_contrato");

            builder.HasKey(c => c.IdContrato);
            builder.Property(c => c.IdContrato).HasMaxLength(100);

            builder.Property(c => c.UltimoValor).HasPrecision(18, 2);

            builder.Property(c => c.StatusPagamento)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            builder.Property<uint>("Version").IsRowVersion();

            builder.HasOne<EventoWebhook>()
                   .WithMany()
                   .HasForeignKey(c => c.UltimoEventoId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
