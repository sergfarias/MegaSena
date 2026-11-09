using MegaSena.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace MegaSena.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<JogoEntity> Jogos { get; set; }
    public DbSet<JogoItemEntity> JogoItens { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Perfil> Perfis { get; set; }
    public DbSet<Concurso> Concursos { get; set; }
    public DbSet<MensagemContato> MensagensContato { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<JogoEntity>(entity =>
        {
            entity.ToTable("Jogo"); // O nome da tabela no banco continua "Jogo"
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Hash).IsUnique();
            entity.HasMany(e => e.Itens)
                  .WithOne(e => e.Jogo)
                  .HasForeignKey(e => e.JogoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JogoItemEntity>(entity =>
        {
            entity.ToTable("JogoItem");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuario");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Login).IsUnique();
            entity.HasIndex(e => e.ResetToken);
            // SEÇÃO 3.2: Índice único para o e-mail
            entity.Property(e => e.Email).HasMaxLength(150); // nvarchar(max) não pode ser chave de índice
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL"); // vários NULL permitidos
        });

        modelBuilder.Entity<Perfil>(entity =>
        {
            entity.ToTable("Perfil");
            entity.HasKey(e => e.Id);

            // SEED DO PERFIL ADMIN
            entity.HasData(new Perfil
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Descricao = "Administrador",
                Sigla = "Admin",
                Ativo = true,
                DataCriacao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

        });

        modelBuilder.Entity<Concurso>(entity =>
        {
            entity.ToTable("Concurso");
            entity.HasKey(e => e.Id);

            // Número do concurso é único
            entity.HasIndex(e => e.NumeroConcurso).IsUnique();

            // Precisão dos valores monetários
            entity.Property(e => e.ValorArrecadado).HasPrecision(15, 2);
            entity.Property(e => e.ValorAcumulado).HasPrecision(15, 2);
            entity.Property(e => e.ValorPremioSena).HasPrecision(15, 2);
            entity.Property(e => e.ValorPremioQuina).HasPrecision(15, 2);
            entity.Property(e => e.ValorPremioQuadra).HasPrecision(15, 2);

            // Strings longas
            entity.Property(e => e.DezenasSorteadas).HasMaxLength(50);
            entity.Property(e => e.DezenasOrdemSorteio).HasMaxLength(50);
            entity.Property(e => e.CidadeSorteio).HasMaxLength(100);
            entity.Property(e => e.UfSorteio).HasMaxLength(2);

            // Datas como datetime2
            entity.Property(e => e.DataHoraSorteio).HasColumnType("datetime2");
            entity.Property(e => e.DataProximoConcurso).HasColumnType("datetime2");
            entity.Property(e => e.DataImportacao).HasColumnType("datetime2");
        });

        modelBuilder.Entity<MensagemContato>(entity =>
        {
            entity.ToTable("MensagemContato");
            entity.HasKey(e => e.Id);

            // Index para busca rápida
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Lida);

            // Limites de tamanho
            entity.Property(e => e.Nome).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Mensagem).HasMaxLength(2000).IsRequired();

            entity.Property(e => e.DataEnvio).HasColumnType("datetime2");
        });

    }
}


