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
    public DbSet<ParameterGroup> ParameterGroups => Set<ParameterGroup>();
    public DbSet<ParameterDefinition> ParameterDefinitions => Set<ParameterDefinition>();
    public DbSet<ClassParameter> ClassParameters => Set<ClassParameter>();
    public DbSet<ProductParameterValue> ProductParameterValues => Set<ProductParameterValue>();

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
        
        modelBuilder.Entity<ParameterGroup>(entity =>
        {
            entity.ToTable("parameter_group");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(x => x.ShortName)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(x => x.SortOrder)
                .IsRequired();

            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.ShortName).IsUnique();
            entity.HasIndex(x => x.SortOrder);
        });
        
        modelBuilder.Entity<ParameterDefinition>(entity =>
        {
            entity.ToTable("parameter_definition");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(x => x.ShortName)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(x => x.ValueType)
                .IsRequired();

            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.ShortName).IsUnique();
            entity.HasIndex(x => x.ValueType);

            entity.HasOne(x => x.MeasureUnit)
                .WithMany(x => x.ParameterDefinitions)
                .HasForeignKey(x => x.MeasureUnitId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.EnumClass)
                .WithMany(x => x.ParameterDefinitions)
                .HasForeignKey(x => x.EnumClassId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<ClassParameter>(entity =>
        {
            entity.ToTable("class_parameter");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.SortOrder)
                .IsRequired();

            entity.Property(x => x.IsRequired)
                .IsRequired();

            entity.Property(x => x.IsInherited)
                .IsRequired();

            entity.Property(x => x.MinNumberValue)
                .HasColumnType("numeric(18, 4)");

            entity.Property(x => x.MaxNumberValue)
                .HasColumnType("numeric(18, 4)");

            entity.HasIndex(x => new { x.ClassNodeId, x.ParameterDefinitionId })
                .IsUnique();

            entity.HasIndex(x => new { x.ClassNodeId, x.SortOrder });

            entity.HasOne(x => x.ClassNode)
                .WithMany(x => x.ClassParameters)
                .HasForeignKey(x => x.ClassNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ParameterDefinition)
                .WithMany(x => x.ClassParameters)
                .HasForeignKey(x => x.ParameterDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ParameterGroup)
                .WithMany(x => x.ClassParameters)
                .HasForeignKey(x => x.ParameterGroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        modelBuilder.Entity<ProductParameterValue>(entity =>
        {
            entity.ToTable("product_parameter_value");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.NumberValue)
                .HasColumnType("numeric(18, 4)");

            entity.Property(x => x.StringValue)
                .HasMaxLength(1024);

            entity.HasIndex(x => new { x.ProductId, x.ClassParameterId })
                .IsUnique();

            entity.HasIndex(x => x.EnumValueId);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.ParameterValues)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ClassParameter)
                .WithMany(x => x.ProductValues)
                .HasForeignKey(x => x.ClassParameterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EnumValue)
                .WithMany(x => x.ProductParameterValues)
                .HasForeignKey(x => x.EnumValueId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        
        modelBuilder.Entity<MeasureUnit>().HasData(
            new MeasureUnit { Id = 1, Name = "Штука", ShortName = "шт." },
            new MeasureUnit { Id = 2, Name = "Комплект", ShortName = "компл." },
            new MeasureUnit { Id = 3, Name = "Зона", ShortName = "зона" },
            new MeasureUnit { Id = 4, Name = "Канал", ShortName = "канал" },
            new MeasureUnit { Id = 5, Name = "Ватт", ShortName = "Вт" }
        );
        
        modelBuilder.Entity<ParameterGroup>().HasData(
            new ParameterGroup
            {
                Id = 1,
                Name = "Подключение",
                ShortName = "connection",
                SortOrder = 1
            },
            new ParameterGroup
            {
                Id = 2,
                Name = "Электропитание",
                ShortName = "power",
                SortOrder = 2
            },
            new ParameterGroup
            {
                Id = 3,
                Name = "Технические характеристики",
                ShortName = "technical",
                SortOrder = 3
            },
            new ParameterGroup
            {
                Id = 4,
                Name = "Климатические параметры",
                ShortName = "climate",
                SortOrder = 4
            },
            new ParameterGroup
            {
                Id = 5,
                Name = "Внешний вид",
                ShortName = "appearance",
                SortOrder = 5
            }
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
        modelBuilder.Entity<ParameterDefinition>().HasData(
            new ParameterDefinition
            {
                Id = 1,
                Name = "Тип подключения",
                ShortName = "connection-kind-param",
                ValueType = ParameterValueType.Enum,
                MeasureUnitId = null,
                EnumClassId = 1
            },
            new ParameterDefinition
            {
                Id = 2,
                Name = "Протокол связи",
                ShortName = "communication-protocol-param",
                ValueType = ParameterValueType.Enum,
                MeasureUnitId = null,
                EnumClassId = 2
            },
            new ParameterDefinition
            {
                Id = 3,
                Name = "Мощность",
                ShortName = "power-watt",
                ValueType = ParameterValueType.Number,
                MeasureUnitId = 5,
                EnumClassId = null
            },
            new ParameterDefinition
            {
                Id = 4,
                Name = "Мощность лампы из списка",
                ShortName = "lamp-power-enum-param",
                ValueType = ParameterValueType.Enum,
                MeasureUnitId = null,
                EnumClassId = 3
            },
            new ParameterDefinition
            {
                Id = 5,
                Name = "Иконка устройства",
                ShortName = "device-icon-param",
                ValueType = ParameterValueType.Enum,
                MeasureUnitId = null,
                EnumClassId = 4
            },
            new ParameterDefinition
            {
                Id = 6,
                Name = "Количество каналов",
                ShortName = "channel-count",
                ValueType = ParameterValueType.Integer,
                MeasureUnitId = 4,
                EnumClassId = null
            },
            new ParameterDefinition
            {
                Id = 7,
                Name = "Рабочая температура",
                ShortName = "working-temperature",
                ValueType = ParameterValueType.Number,
                MeasureUnitId = null,
                EnumClassId = null
            },
            new ParameterDefinition
            {
                Id = 8,
                Name = "Описание устройства",
                ShortName = "device-description",
                ValueType = ParameterValueType.String,
                MeasureUnitId = null,
                EnumClassId = null
            },
            new ParameterDefinition
            {
                Id = 9,
                Name = "Дата ввода в эксплуатацию",
                ShortName = "commissioning-date",
                ValueType = ParameterValueType.DateTime,
                MeasureUnitId = null,
                EnumClassId = null
            }
        );
        modelBuilder.Entity<ClassParameter>().HasData(
            new ClassParameter
            {
                Id = 1,
                ClassNodeId = 7, // LED лампы
                ParameterDefinitionId = 1, // Тип подключения
                ParameterGroupId = 1,
                SortOrder = 1,
                IsRequired = true,
                IsInherited = false,
                MinNumberValue = null,
                MaxNumberValue = null
            },
            new ClassParameter
            {
                Id = 2,
                ClassNodeId = 7, // LED лампы
                ParameterDefinitionId = 2, // Протокол связи
                ParameterGroupId = 1,
                SortOrder = 2,
                IsRequired = true,
                IsInherited = false,
                MinNumberValue = null,
                MaxNumberValue = null
            },
            new ClassParameter
            {
                Id = 3,
                ClassNodeId = 7, // LED лампы
                ParameterDefinitionId = 3, // Мощность
                ParameterGroupId = 2,
                SortOrder = 3,
                IsRequired = true,
                IsInherited = false,
                MinNumberValue = 3,
                MaxNumberValue = 20
            },
            new ClassParameter
            {
                Id = 4,
                ClassNodeId = 8, // RGB лампы
                ParameterDefinitionId = 2, // Протокол связи
                ParameterGroupId = 1,
                SortOrder = 1,
                IsRequired = true,
                IsInherited = false,
                MinNumberValue = null,
                MaxNumberValue = null
            },
            new ClassParameter
            {
                Id = 5,
                ClassNodeId = 8, // RGB лампы
                ParameterDefinitionId = 4, // Мощность лампы из списка
                ParameterGroupId = 2,
                SortOrder = 2,
                IsRequired = true,
                IsInherited = false,
                MinNumberValue = null,
                MaxNumberValue = null
            },
            new ClassParameter
            {
                Id = 6,
                ClassNodeId = 10, // Выключатели
                ParameterDefinitionId = 1, // Тип подключения
                ParameterGroupId = 1,
                SortOrder = 1,
                IsRequired = true,
                IsInherited = false,
                MinNumberValue = null,
                MaxNumberValue = null
            },
            new ClassParameter
            {
                Id = 7,
                ClassNodeId = 10, // Выключатели
                ParameterDefinitionId = 6, // Количество каналов
                ParameterGroupId = 3,
                SortOrder = 2,
                IsRequired = true,
                IsInherited = false,
                MinNumberValue = 1,
                MaxNumberValue = 6
            },
            new ClassParameter
            {
                Id = 8,
                ClassNodeId = 16, // Термостаты
                ParameterDefinitionId = 2, // Протокол связи
                ParameterGroupId = 1,
                SortOrder = 1,
                IsRequired = true,
                IsInherited = false,
                MinNumberValue = null,
                MaxNumberValue = null
            },
            new ClassParameter
            {
                Id = 9,
                ClassNodeId = 16, // Термостаты
                ParameterDefinitionId = 7, // Рабочая температура
                ParameterGroupId = 4,
                SortOrder = 2,
                IsRequired = true,
                IsInherited = false,
                MinNumberValue = 5,
                MaxNumberValue = 35
            },
            new ClassParameter
            {
                Id = 10,
                ClassNodeId = 23, // Датчики температуры
                ParameterDefinitionId = 2, // Протокол связи
                ParameterGroupId = 1,
                SortOrder = 1,
                IsRequired = true,
                IsInherited = false,
                MinNumberValue = null,
                MaxNumberValue = null
            },
            new ClassParameter
            {
                Id = 11,
                ClassNodeId = 23, // Датчики температуры
                ParameterDefinitionId = 8, // Описание устройства
                ParameterGroupId = 3,
                SortOrder = 2,
                IsRequired = false,
                IsInherited = false,
                MinNumberValue = null,
                MaxNumberValue = null
            },
            new ClassParameter
            {
                Id = 12,
                ClassNodeId = 23, // Датчики температуры
                ParameterDefinitionId = 9, // Дата ввода в эксплуатацию
                ParameterGroupId = 3,
                SortOrder = 3,
                IsRequired = false,
                IsInherited = false,
                MinNumberValue = null,
                MaxNumberValue = null
            }
        );
        modelBuilder.Entity<ProductParameterValue>().HasData(
            new ProductParameterValue
            {
                Id = 1,
                ProductId = 1, // Xiaomi Smart LED Bulb
                ClassParameterId = 1, // Тип подключения
                IntegerValue = null,
                NumberValue = null,
                StringValue = null,
                DateTimeValue = null,
                EnumValueId = 2 // Беспроводное
            },
            new ProductParameterValue
            {
                Id = 2,
                ProductId = 1, // Xiaomi Smart LED Bulb
                ClassParameterId = 2, // Протокол связи
                IntegerValue = null,
                NumberValue = null,
                StringValue = null,
                DateTimeValue = null,
                EnumValueId = 4 // Wi-Fi
            },
            new ProductParameterValue
            {
                Id = 3,
                ProductId = 1, // Xiaomi Smart LED Bulb
                ClassParameterId = 3, // Мощность
                IntegerValue = null,
                NumberValue = 9,
                StringValue = null,
                DateTimeValue = null,
                EnumValueId = null
            },
            new ProductParameterValue
            {
                Id = 4,
                ProductId = 2, // Philips Hue White and Color
                ClassParameterId = 4, // Протокол связи
                IntegerValue = null,
                NumberValue = null,
                StringValue = null,
                DateTimeValue = null,
                EnumValueId = 5 // Zigbee
            },
            new ProductParameterValue
            {
                Id = 5,
                ProductId = 2, // Philips Hue White and Color
                ClassParameterId = 5, // Мощность лампы из списка
                IntegerValue = null,
                NumberValue = null,
                StringValue = null,
                DateTimeValue = null,
                EnumValueId = 8 // 9 Вт
            },
            new ProductParameterValue
            {
                Id = 6,
                ProductId = 3, // Aqara Wall Switch H1
                ClassParameterId = 6, // Тип подключения
                IntegerValue = null,
                NumberValue = null,
                StringValue = null,
                DateTimeValue = null,
                EnumValueId = 2 // Беспроводное
            },
            new ProductParameterValue
            {
                Id = 7,
                ProductId = 3, // Aqara Wall Switch H1
                ClassParameterId = 7, // Количество каналов
                IntegerValue = 2,
                NumberValue = null,
                StringValue = null,
                DateTimeValue = null,
                EnumValueId = null
            },
            new ProductParameterValue
            {
                Id = 8,
                ProductId = 4, // Google Nest Thermostat
                ClassParameterId = 8, // Протокол связи
                IntegerValue = null,
                NumberValue = null,
                StringValue = null,
                DateTimeValue = null,
                EnumValueId = 4 // Wi-Fi
            },
            new ProductParameterValue
            {
                Id = 9,
                ProductId = 4, // Google Nest Thermostat
                ClassParameterId = 9, // Рабочая температура
                IntegerValue = null,
                NumberValue = 25,
                StringValue = null,
                DateTimeValue = null,
                EnumValueId = null
            },
            new ProductParameterValue
            {
                Id = 10,
                ProductId = 5, // Xiaomi Temperature and Humidity Sensor
                ClassParameterId = 10, // Протокол связи
                IntegerValue = null,
                NumberValue = null,
                StringValue = null,
                DateTimeValue = null,
                EnumValueId = 5 // Zigbee
            },
            new ProductParameterValue
            {
                Id = 11,
                ProductId = 5, // Xiaomi Temperature and Humidity Sensor
                ClassParameterId = 11, // Описание устройства
                IntegerValue = null,
                NumberValue = null,
                StringValue = "Датчик температуры и влажности для системы умного дома",
                DateTimeValue = null,
                EnumValueId = null
            }
        );
    }
}