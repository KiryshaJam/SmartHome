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
    public DbSet<EnumClass> EnumClasses => Set<EnumClass>();
    public DbSet<EnumValue> EnumValues => Set<EnumValue>();

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
        
        modelBuilder.Entity<EnumClass>(entity =>
        {
            entity.ToTable("enum_class");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ShortName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ValueType).IsRequired();
            entity.Property(x => x.SortOrder).IsRequired();

            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.ShortName).IsUnique();
            entity.HasIndex(x => x.SortOrder);

            entity.HasOne(x => x.MeasureUnit)
                .WithMany(x => x.EnumClasses)
                .HasForeignKey(x => x.MeasureUnitId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EnumValue>(entity =>
        {
            entity.ToTable("enum_value");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.StringValue).HasMaxLength(512);
            entity.Property(x => x.IconValue).HasMaxLength(512);
            entity.Property(x => x.DisplayName).HasMaxLength(512);
            entity.Property(x => x.NumberValue).HasColumnType("numeric(18, 4)");
            entity.Property(x => x.SortOrder).IsRequired();

            entity.HasIndex(x => x.EnumClassId);
            entity.HasIndex(x => new { x.EnumClassId, x.SortOrder }).IsUnique();

            entity.HasOne(x => x.EnumClass)
                .WithMany(x => x.Values)
                .HasForeignKey(x => x.EnumClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MeasureUnit>().HasData(
            new MeasureUnit { Id = 1, Name = "Штука", ShortName = "шт." },
            new MeasureUnit { Id = 2, Name = "Комплект", ShortName = "компл." },
            new MeasureUnit { Id = 3, Name = "Зона", ShortName = "зона" },
            new MeasureUnit { Id = 4, Name = "Канал", ShortName = "канал" },
            new MeasureUnit { Id = 5, Name = "Ватт", ShortName = "Вт" }
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

        modelBuilder.Entity<EnumClass>().HasData(
            new EnumClass
            {
                Id = 1,
                Name = "Тип подключения",
                ShortName = "connection-kind",
                ValueType = EnumValueType.String,
                SortOrder = 1,
                MeasureUnitId = null
            },
            new EnumClass
            {
                Id = 2,
                Name = "Протокол связи",
                ShortName = "communication-protocol",
                ValueType = EnumValueType.String,
                SortOrder = 2,
                MeasureUnitId = null
            },
            new EnumClass
            {
                Id = 3,
                Name = "Мощность лампы",
                ShortName = "lamp-power",
                ValueType = EnumValueType.Number,
                SortOrder = 3,
                MeasureUnitId = 5
            },
            new EnumClass
            {
                Id = 4,
                Name = "Иконка устройства",
                ShortName = "device-icon",
                ValueType = EnumValueType.Icon,
                SortOrder = 4,
                MeasureUnitId = null
            }
        );

        modelBuilder.Entity<EnumValue>().HasData(
            new EnumValue
            {
                Id = 1,
                EnumClassId = 1,
                StringValue = "Проводное",
                NumberValue = null,
                IconValue = null,
                DisplayName = "Проводное подключение",
                SortOrder = 1
            },
            new EnumValue
            {
                Id = 2,
                EnumClassId = 1,
                StringValue = "Беспроводное",
                NumberValue = null,
                IconValue = null,
                DisplayName = "Беспроводное подключение",
                SortOrder = 2
            },
            new EnumValue
            {
                Id = 3,
                EnumClassId = 1,
                StringValue = "Гибридное",
                NumberValue = null,
                IconValue = null,
                DisplayName = "Гибридное подключение",
                SortOrder = 3
            },

            new EnumValue
            {
                Id = 4,
                EnumClassId = 2,
                StringValue = "Wi-Fi",
                NumberValue = null,
                IconValue = null,
                DisplayName = "Wi-Fi",
                SortOrder = 1
            },
            new EnumValue
            {
                Id = 5,
                EnumClassId = 2,
                StringValue = "Zigbee",
                NumberValue = null,
                IconValue = null,
                DisplayName = "Zigbee",
                SortOrder = 2
            },
            new EnumValue
            {
                Id = 6,
                EnumClassId = 2,
                StringValue = "Bluetooth",
                NumberValue = null,
                IconValue = null,
                DisplayName = "Bluetooth",
                SortOrder = 3
            },

            new EnumValue
            {
                Id = 7,
                EnumClassId = 3,
                StringValue = null,
                NumberValue = 5,
                IconValue = null,
                DisplayName = "5 Вт",
                SortOrder = 1
            },
            new EnumValue
            {
                Id = 8,
                EnumClassId = 3,
                StringValue = null,
                NumberValue = 9,
                IconValue = null,
                DisplayName = "9 Вт",
                SortOrder = 2
            },
            new EnumValue
            {
                Id = 9,
                EnumClassId = 3,
                StringValue = null,
                NumberValue = 12,
                IconValue = null,
                DisplayName = "12 Вт",
                SortOrder = 3
            },

            new EnumValue
            {
                Id = 10,
                EnumClassId = 4,
                StringValue = null,
                NumberValue = null,
                IconValue = "lightbulb",
                DisplayName = "Лампочка",
                SortOrder = 1
            },
            new EnumValue
            {
                Id = 11,
                EnumClassId = 4,
                StringValue = null,
                NumberValue = null,
                IconValue = "thermostat",
                DisplayName = "Термостат",
                SortOrder = 2
            },
            new EnumValue
            {
                Id = 12,
                EnumClassId = 4,
                StringValue = null,
                NumberValue = null,
                IconValue = "camera",
                DisplayName = "Камера",
                SortOrder = 3
            }
        );
    }
}