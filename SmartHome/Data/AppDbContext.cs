using Microsoft.EntityFrameworkCore;
using SmartHome.Models;

namespace SmartHome.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ClassNode> ClassNodes => Set<ClassNode>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<MeasureUnit> MeasureUnits => Set<MeasureUnit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MeasureUnit>(entity =>
        {
            entity.ToTable("measure_unit");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ShortName).HasMaxLength(64).IsRequired();

            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.ShortName).IsUnique();
        });

        modelBuilder.Entity<ClassNode>(entity =>
        {
            entity.ToTable("class_node");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ShortName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.IsTerminal).IsRequired();
            entity.Property(x => x.SortOrder).IsRequired();

            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.ShortName).IsUnique();
            entity.HasIndex(x => x.ParentId);

            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.MeasureUnit)
                .WithMany(x => x.ClassNodes)
                .HasForeignKey(x => x.MeasureUnitId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("product");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ShortName).HasMaxLength(128).IsRequired();

            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.ShortName).IsUnique();

            entity.HasOne(x => x.ClassNode)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.ClassNodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MeasureUnit>().HasData(
            new MeasureUnit { Id = 1, Name = "Штука", ShortName = "шт." },
            new MeasureUnit { Id = 2, Name = "Комплект", ShortName = "компл." },
            new MeasureUnit { Id = 3, Name = "Зона", ShortName = "зона" },
            new MeasureUnit { Id = 4, Name = "Канал", ShortName = "канал" }
        );

        modelBuilder.Entity<ClassNode>().HasData(
            new ClassNode
            {
                Id = 1,
                Name = "Умный дом",
                ShortName = "smart-home",
                ParentId = null,
                SortOrder = 1,
                IsTerminal = false,
                MeasureUnitId = null
            },
            new ClassNode
            {
                Id = 2,
                Name = "Освещение",
                ShortName = "lighting",
                ParentId = 1,
                SortOrder = 1,
                IsTerminal = false,
                MeasureUnitId = null
            },
            new ClassNode
            {
                Id = 3,
                Name = "Климат-контроль",
                ShortName = "climate-control",
                ParentId = 1,
                SortOrder = 2,
                IsTerminal = false,
                MeasureUnitId = null
            },
            new ClassNode
            {
                Id = 4,
                Name = "Источники света",
                ShortName = "light-sources",
                ParentId = 2,
                SortOrder = 1,
                IsTerminal = false,
                MeasureUnitId = null
            },
            new ClassNode
            {
                Id = 5,
                Name = "Управление освещением",
                ShortName = "lighting-control",
                ParentId = 2,
                SortOrder = 2,
                IsTerminal = false,
                MeasureUnitId = null
            },
            new ClassNode
            {
                Id = 6,
                Name = "Датчики освещенности",
                ShortName = "light-sensors",
                ParentId = 2,
                SortOrder = 3,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 7,
                Name = "LED лампы",
                ShortName = "led-lamps",
                ParentId = 4,
                SortOrder = 1,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 8,
                Name = "RGB лампы",
                ShortName = "rgb-lamps",
                ParentId = 4,
                SortOrder = 2,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 9,
                Name = "Умные ленты",
                ShortName = "smart-strips",
                ParentId = 4,
                SortOrder = 3,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 10,
                Name = "Выключатели",
                ShortName = "switches",
                ParentId = 5,
                SortOrder = 1,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 11,
                Name = "Панели управления",
                ShortName = "control-panels",
                ParentId = 5,
                SortOrder = 2,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 12,
                Name = "Отопление",
                ShortName = "heating",
                ParentId = 3,
                SortOrder = 1,
                IsTerminal = false,
                MeasureUnitId = null
            },
            new ClassNode
            {
                Id = 13,
                Name = "Охлаждение",
                ShortName = "cooling",
                ParentId = 3,
                SortOrder = 2,
                IsTerminal = false,
                MeasureUnitId = null
            },
            new ClassNode
            {
                Id = 14,
                Name = "Вентиляция",
                ShortName = "ventilation",
                ParentId = 3,
                SortOrder = 3,
                IsTerminal = false,
                MeasureUnitId = null
            },
            new ClassNode
            {
                Id = 15,
                Name = "Датчики климата",
                ShortName = "climate-sensors",
                ParentId = 3,
                SortOrder = 4,
                IsTerminal = false,
                MeasureUnitId = null
            },
            new ClassNode
            {
                Id = 16,
                Name = "Термостаты",
                ShortName = "thermostats",
                ParentId = 12,
                SortOrder = 1,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 17,
                Name = "Радиаторные клапаны",
                ShortName = "radiator-valves",
                ParentId = 12,
                SortOrder = 2,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 18,
                Name = "Тёплый пол",
                ShortName = "floor-heating",
                ParentId = 12,
                SortOrder = 3,
                IsTerminal = true,
                MeasureUnitId = 2
            },
            new ClassNode
            {
                Id = 19,
                Name = "Кондиционеры",
                ShortName = "air-conditioners",
                ParentId = 13,
                SortOrder = 1,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 20,
                Name = "Вентиляторы",
                ShortName = "fans",
                ParentId = 13,
                SortOrder = 2,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 21,
                Name = "Рекуператоры",
                ShortName = "recuperators",
                ParentId = 14,
                SortOrder = 1,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 22,
                Name = "Вытяжные системы",
                ShortName = "exhaust-systems",
                ParentId = 14,
                SortOrder = 2,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 23,
                Name = "Датчики температуры",
                ShortName = "temperature-sensors",
                ParentId = 15,
                SortOrder = 1,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 24,
                Name = "Датчики влажности",
                ShortName = "humidity-sensors",
                ParentId = 15,
                SortOrder = 2,
                IsTerminal = true,
                MeasureUnitId = 1
            },
            new ClassNode
            {
                Id = 25,
                Name = "CO2 датчики",
                ShortName = "co2-sensors",
                ParentId = 15,
                SortOrder = 3,
                IsTerminal = true,
                MeasureUnitId = 1
            }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Xiaomi Smart LED Bulb",
                ShortName = "xiaomi-led-bulb",
                ClassNodeId = 7
            },
            new Product
            {
                Id = 2,
                Name = "Philips Hue White and Color",
                ShortName = "philips-hue-color",
                ClassNodeId = 8
            },
            new Product
            {
                Id = 3,
                Name = "Aqara Wall Switch H1",
                ShortName = "aqara-switch-h1",
                ClassNodeId = 10
            },
            new Product
            {
                Id = 4,
                Name = "Google Nest Thermostat",
                ShortName = "nest-thermostat",
                ClassNodeId = 16
            },
            new Product
            {
                Id = 5,
                Name = "Xiaomi Temperature and Humidity Sensor",
                ShortName = "xiaomi-temp-humidity",
                ClassNodeId = 23
            }
        );
    }
}