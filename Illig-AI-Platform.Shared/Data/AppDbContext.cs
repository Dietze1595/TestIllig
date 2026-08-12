using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Auftragsinformationen;
using Illig_AI_Platform.Shared.Lieferantenassistent;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;

namespace Illig_AI_Platform.Shared.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserProfileRole> UserProfileRoles => Set<UserProfileRole>();
    public DbSet<StuecklistenPosition> StuecklistenPositionen => Set<StuecklistenPosition>();
    public DbSet<StuecklistenpruefungVerlaufEintrag> StuecklistenpruefungVerlaufEintraege => Set<StuecklistenpruefungVerlaufEintrag>();
    public DbSet<MaschinentypStueckliste> MaschinentypStuecklisten => Set<MaschinentypStueckliste>();
    public DbSet<MaximalstuecklistenPosition> MaximalstuecklistenPositionen => Set<MaximalstuecklistenPosition>();
    public DbSet<VerlaufMerkmal> VerlaufMerkmale => Set<VerlaufMerkmal>();
    public DbSet<Angebot> Angebote => Set<Angebot>();
    public DbSet<Auftragsbestaetigung> Auftragsbestaetigungen => Set<Auftragsbestaetigung>();
    public DbSet<Lieferant> Lieferanten => Set<Lieferant>();
    public DbSet<LieferantEmailAdresse> LieferantEmailAdressen => Set<LieferantEmailAdresse>();
    public DbSet<Dispositionsposition> Dispositionspositionen => Set<Dispositionsposition>();
    public DbSet<Kunde> Kunden => Set<Kunde>();
    public DbSet<KundenQuelle> KundenQuellen => Set<KundenQuelle>();
    public DbSet<KundenPartneradresse> KundenPartneradressen => Set<KundenPartneradresse>();
    public DbSet<SharePointSynchronisationsstand> SharePointSynchronisationsstaende => Set<SharePointSynchronisationsstand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(e => e.ObjectId).IsUnique();
            entity.Property(e => e.ObjectId).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.DisplayName).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Vorname).IsRequired();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired();

            entity.HasData(AppRoles.Seed.Select(r => new Role { Id = r.Id, Name = r.Name }));
        });

        modelBuilder.Entity<UserProfileRole>(entity =>
        {
            entity.HasKey(e => new { e.UserProfileId, e.RoleId });
            entity.HasOne(e => e.UserProfile)
                .WithMany(u => u.UserProfileRoles)
                .HasForeignKey(e => e.UserProfileId);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserProfileRoles)
                .HasForeignKey(e => e.RoleId);
        });

        modelBuilder.Entity<StuecklistenPosition>(entity =>
        {
            entity.HasIndex(e => new { e.Auftragsnummer, e.Auftragsposition });
            entity.HasIndex(e => e.NodeId);
            entity.Property(e => e.BomTyp).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Auftragsnummer).IsRequired();
            entity.Property(e => e.Auftragsposition).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RootNodeId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.NodeId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ParentNodeId).HasMaxLength(255);
            entity.Property(e => e.SapPosition).HasMaxLength(50);
            entity.Property(e => e.Typ).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Artikelnummer).IsRequired();
            entity.Property(e => e.Menge).HasPrecision(18, 4);
        });

        modelBuilder.Entity<StuecklistenpruefungVerlaufEintrag>(entity =>
        {
            entity.HasIndex(e => e.UserProfileId);
            entity.Property(e => e.Dateiname).IsRequired();
            entity.Property(e => e.Auftragsnummer).HasMaxLength(100);
            entity.Property(e => e.Kundennummer).HasMaxLength(100);
            entity.Property(e => e.Kundenname).HasMaxLength(500);
            entity.Property(e => e.Kundenadresse).HasMaxLength(1000);
            entity.Property(e => e.Maschinentyp).HasMaxLength(500);
            entity.Property(e => e.StuecklisteJson).HasColumnType("longtext");
            entity.Property(e => e.VergleichsErgebnisJson).HasColumnType("longtext");
            entity.Property(e => e.SharePointDriveId).HasMaxLength(255);
            entity.Property(e => e.SharePointItemId).HasMaxLength(255);
            entity.Property(e => e.ETag).HasMaxLength(512);
            entity.Property(e => e.WebUrl).HasMaxLength(2048);
            entity.Property(e => e.AnalyseFehler).HasColumnType("longtext");
            entity.HasIndex(e => e.KundeId);
            entity.HasIndex(e => new { e.SharePointDriveId, e.SharePointItemId }).IsUnique();
            entity.HasIndex(e => e.Auftragsnummer);
            entity.HasIndex(e => e.Kundennummer);
            entity.HasIndex(e => new { e.AnalyseStatus, e.GeloeschtAm });
            entity.HasIndex(e => e.Quelle);
            entity.HasOne<Kunde>().WithMany().HasForeignKey(e => e.KundeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VerlaufMerkmal>(entity =>
        {
            entity.Property(e => e.Position).IsRequired();
            entity.Property(e => e.Merkmalsnummer).IsRequired();
            entity.Property(e => e.Beschreibung).IsRequired();
            entity.HasIndex(e => e.VerlaufEintragId);
            entity.HasIndex(e => new { e.Kategorie, e.Merkmalsnummer });
            entity.HasOne<StuecklistenpruefungVerlaufEintrag>()
                .WithMany()
                .HasForeignKey(e => e.VerlaufEintragId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MaschinentypStueckliste>(entity =>
        {
            entity.Property(e => e.BomTyp).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MaschinentypSchluessel).IsRequired();
            entity.Property(e => e.Beschreibung).IsRequired();
            entity.Property(e => e.Werk).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Stuecklistenverwendung).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Stuecklistenalternative).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.MaschinentypSchluessel).IsUnique();
        });

        modelBuilder.Entity<MaximalstuecklistenPosition>(entity =>
        {
            entity.Property(e => e.Artikelnummer).IsRequired();
            entity.Property(e => e.Menge).HasPrecision(18, 4);
            entity.Property(e => e.Gesamtmenge).HasPrecision(18, 4);
            entity.Property(e => e.Bedingung).HasColumnType("longtext");
            entity.Property(e => e.SapNodeId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SapPosition).HasMaxLength(50);
            entity.Property(e => e.Typ).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.MaschinentypStuecklisteId);
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => new { e.MaschinentypStuecklisteId, e.SapNodeId }).IsUnique();
            // Kein HasOne/WithMany auf sich selbst mit Cascade — Full-Replace-Import löscht
            // per MaschinentypStuecklisteId, nicht über Kaskade.
            entity.HasOne<MaschinentypStueckliste>()
                .WithMany()
                .HasForeignKey(e => e.MaschinentypStuecklisteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Angebot>(entity =>
        {
            entity.Property(e => e.Angebotsnummer).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Dateiname).IsRequired();
            entity.Property(e => e.BlobPfad).IsRequired();
            entity.Property(e => e.Volltext).IsRequired().HasColumnType("longtext");
            entity.HasIndex(e => new { e.Angebotsnummer, e.Version }).IsUnique();
            entity.HasIndex(e => e.UserProfileId);
            entity.HasIndex(e => e.FreigegebenVonUserProfileId);
            entity.HasIndex(e => e.KundeId);
            entity.HasOne<Kunde>().WithMany().HasForeignKey(e => e.KundeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Auftragsbestaetigung>(entity =>
        {
            entity.Property(e => e.Nummer).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Dateiname).IsRequired();
            entity.Property(e => e.BlobPfad).IsRequired();
            entity.Property(e => e.Volltext).IsRequired().HasColumnType("longtext");
            entity.Property(e => e.SonstigeAbweichungen).HasColumnType("longtext");
            entity.HasIndex(e => e.AngebotId);
            entity.HasIndex(e => e.UserProfileId);
            entity.HasIndex(e => e.KundeId);
            entity.HasOne<Kunde>().WithMany().HasForeignKey(e => e.KundeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Angebot>()
                .WithMany()
                .HasForeignKey(e => e.AngebotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lieferant>(entity =>
        {
            entity.HasKey(e => e.Kreditor);
            entity.Property(e => e.Kreditor).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.Entity<LieferantEmailAdresse>(entity =>
        {
            entity.Property(e => e.AdressNummer).IsRequired();
            entity.Property(e => e.EmailAdresse).IsRequired();
            entity.Property(e => e.KontaktTyp).HasMaxLength(100);
            entity.HasIndex(e => e.AdressNummer);
        });

        modelBuilder.Entity<Dispositionsposition>(entity =>
        {
            entity.Property(e => e.Einkaufsbeleg).IsRequired();
            entity.Property(e => e.Position).IsRequired();
            entity.Property(e => e.Schluessel).IsRequired();
            entity.Property(e => e.LieferantKreditor).IsRequired();
            entity.Property(e => e.Kurztext).IsRequired();
            entity.Property(e => e.Bestellmenge).HasPrecision(18, 4);
            entity.Property(e => e.Nettopreis).HasPrecision(18, 2);
            entity.Property(e => e.Preiseinheit).HasPrecision(18, 4);
            entity.Property(e => e.Einteilungsmenge).HasPrecision(18, 4);
            entity.Property(e => e.GelieferteMenge).HasPrecision(18, 4);
            entity.Property(e => e.NochZuLiefernMenge).HasPrecision(18, 4);
            entity.Property(e => e.MengeInLagerME).HasPrecision(18, 4);
            // Bewusst nicht IsUnique(): Einkaufsbeleg+Position+Lieferdatum ist der additive
            // Import-Schlüssel, aber ein harter DB-Unique-Constraint würde den Import bei einer
            // erneuten, echten Kollision abbrechen statt sie additiv zu ersetzen (siehe
            // Schluessel-Kommentar auf der Entity).
            entity.HasIndex(e => e.Schluessel);
            entity.HasIndex(e => e.LieferantKreditor);
        });

        modelBuilder.Entity<Kunde>(entity =>
        {
            entity.Property(e => e.Kundennummer).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.Adresse).HasMaxLength(1000);
            entity.Property(e => e.NormalisierterName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.NormalisierteAdresse).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Kundennummer).IsUnique();
            entity.HasIndex(e => new { e.NormalisierterName, e.NormalisierteAdresse });
        });

        modelBuilder.Entity<KundenQuelle>(entity =>
        {
            entity.Property(e => e.Kundennummer).HasMaxLength(100);
            entity.Property(e => e.Kundenname).HasMaxLength(500);
            entity.Property(e => e.Kundenadresse).HasMaxLength(1000);
            entity.HasIndex(e => new { e.Quelltyp, e.QuellId }).IsUnique();
            entity.HasIndex(e => e.KundeId);
            entity.HasOne<Kunde>().WithMany().HasForeignKey(e => e.KundeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KundenPartneradresse>(entity =>
        {
            entity.Property(e => e.Hauptkundennummer).IsRequired();
            entity.Property(e => e.Partnerrolle).IsRequired();
            entity.Property(e => e.PartnerId).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => new { e.Hauptkundennummer, e.Partnerrolle }).IsUnique();
        });

        modelBuilder.Entity<SharePointSynchronisationsstand>(entity =>
        {
            entity.Property(e => e.Quelle).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DriveId).HasMaxLength(255);
            entity.Property(e => e.OrdnerItemId).HasMaxLength(255);
            entity.Property(e => e.DeltaLink).HasColumnType("longtext");
            entity.Property(e => e.LetzterFehler).HasColumnType("longtext");
            entity.HasIndex(e => e.Quelle).IsUnique();
        });
    }
}
