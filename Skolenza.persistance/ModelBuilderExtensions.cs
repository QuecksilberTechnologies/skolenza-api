using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using skolenza.domain.Entity;
using skolenza.domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace skolenza.persistance
{

    public static class ModelBuilderExtensions
    {
        public static void ConfigureBaseEntity<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
        {
            builder.Property(e => e.IsSoftDeleted)
                .HasDefaultValue(false);
            builder.Property(e => e.AddedById)
                   .HasColumnType("bigint");

            builder.Property(e => e.AddedDateTime)
                   .HasDefaultValueSql("(getdate())")
                   .HasColumnType("datetime");

            builder.Property(e => e.UpdatedDateTime)
                   .HasColumnType("datetime");

            builder.Property(e => e.UpdatedById)
                   .HasColumnType("bigint");

            builder.Property(e => e.SoftDeletedById)
                   .HasColumnType("bigint");

            builder.Property(e => e.DeletedDateTime)
                   .HasColumnType("datetime");

            builder.Property(e => e.IsActive)
                   .HasDefaultValue(true);

        }
    }

}


