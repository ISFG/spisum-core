using ISFG.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ISFG.Data.Database
{
    public class SpisumContext : DbContext
    {
        #region Constructors

        public SpisumContext()
        {
        }

        public SpisumContext(DbContextOptions<SpisumContext> options) : base(options)
        {
        }

        #endregion

        #region Properties

        public virtual DbSet<EventType> EventType { get; set; }
        public virtual DbSet<NodeType> NodeType { get; set; }
        public virtual DbSet<TransactionHistory> TransactionHistory { get; set; }
        public virtual DbSet<SystemLogin> SystemLogin { get; set; }

        #endregion

        #region Override of DbContext

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          modelBuilder.Entity<EventType>(entity =>
            {
                entity.HasKey(e => e.Code)
                    .HasName("pk_event_type");

                entity.ToTable("event_type");

                entity.Property(e => e.Code).HasColumnName("code");

                entity.Property(e => e.Description).HasColumnName("description");
            });

            modelBuilder.Entity<NodeType>(entity =>
            {
                entity.HasKey(e => e.Code)
                    .HasName("pk_node_type");

                entity.ToTable("node_type");

                entity.Property(e => e.Code).HasColumnName("code");

                entity.Property(e => e.Description).HasColumnName("description");
            });

            modelBuilder.Entity<TransactionHistory>(entity =>
            {
                entity.ToTable("transaction_history");

                entity.HasIndex(e => e.FkEventTypeCode)
                    .HasName("ix_transaction_history_fk_event_type_code");

                entity.HasIndex(e => e.FkNodeTypeCode)
                    .HasName("ix_transaction_history_fk_object_type_code");

                entity.HasIndex(e => e.NodeId)
                    .HasName("ix_transaction_history_node_id");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasDefaultValueSql("nextval('transaction_history_seq'::regclass)");

                entity.Property(e => e.EventParameters)
                    .HasColumnName("event_parameters")
                    .HasColumnType("jsonb");

                entity.Property(e => e.EventSource)
                    .IsRequired()
                    .HasColumnName("event_source");

                entity.Property(e => e.FkEventTypeCode)
                    .IsRequired()
                    .HasColumnName("fk_event_type_code");

                entity.Property(e => e.FkNodeTypeCode)
                    .IsRequired()
                    .HasColumnName("fk_node_type_code");

                entity.Property(e => e.NodeId)
                    .IsRequired()
                    .HasColumnName("node_id");

                entity.Property(e => e.OccuredAt).HasColumnName("occured_at");

                entity.Property(e => e.Pid)
                    .IsRequired()
                    .HasColumnName("pid");

                entity.Property(e => e.SslNodeType)
                    .IsRequired()
                    .HasColumnName("ssl_node_type");

                entity.Property(e => e.ProcessedAt)
                    .HasColumnName("processed_at")
                    .HasDefaultValueSql("clock_timestamp()");

                entity.Property(e => e.ProcessedBy)
                    .IsRequired()
                    .HasColumnName("processed_by")
                    .HasDefaultValueSql("(SESSION_USER)::text");

                entity.Property(e => e.RowHash)
                    .IsRequired()
                    .HasColumnName("row_hash");

                entity.Property(e => e.UserGroupId)
                    .IsRequired()
                    .HasColumnName("user_group_id");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasColumnName("user_id");

                entity.HasOne(d => d.FkEventTypeCodeNavigation)
                    .WithMany(p => p.TransactionHistory)
                    .HasForeignKey(d => d.FkEventTypeCode)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_transaction_history_event_type_code");

                entity.HasOne(d => d.FkNodeTypeCodeNavigation)
                    .WithMany(p => p.TransactionHistory)
                    .HasForeignKey(d => d.FkNodeTypeCode)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_transaction_history_node_type_code");
            });
            
            modelBuilder.Entity<SystemLogin>(entity =>
            {
                entity.HasKey(e => e.Username)
                    .HasName("pk_system_login");

                entity.ToTable("system_login", "system");

                entity.Property(e => e.Username).HasColumnName("username");

                entity.Property(e => e.Password).HasColumnName("password");
            });

            modelBuilder.HasSequence("transaction_history_seq");
        }

        #endregion
    }
}
