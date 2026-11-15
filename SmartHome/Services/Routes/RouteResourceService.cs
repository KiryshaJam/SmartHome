using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs.Routes;
using SmartHome.Models.Routes;

namespace SmartHome.Services.Routes;

public class RouteResourceService(AppDbContext db)
{
    private async Task Validate(string name, int typeId, string branch)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Укажите название.");
        if (!await db.Database.SqlQuery<int>($"SELECT \"Id\" AS \"Value\" FROM class_node WHERE \"Id\"={typeId} AND \"IsTerminal\" AND route_branch(\"Id\",{branch})").AnyAsync())
            throw new ArgumentException("Выберите конечный тип из соответствующей ветви классификатора.");
    }

    public async Task<Department> CreateDepartment(DepartmentWrite input)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(734021,1)");
        await Validate(input.Name, input.TypeId, "route_departments");
        var name = input.Name.Trim();
        if (await db.Set<Department>().AnyAsync(x => x.Name.ToLower() == name.ToLower()))
            throw new ArgumentException("Подразделение с таким названием уже существует.");
        var department = new Department { Name = name, TypeId = input.TypeId };
        db.Add(department);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return department;
    }

    public async Task<WorkCenter> CreateCenter(WorkCenterWrite input)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(734021,1)");
        await Validate(input.Name, input.TypeId, "route_centers");
        if (!await db.Set<Department>().AnyAsync(x => x.Id == input.DepartmentId))
            throw new ArgumentException("Выберите существующее подразделение.");
        var name = input.Name.Trim();
        if (await db.Set<WorkCenter>().AnyAsync(x => x.DepartmentId == input.DepartmentId && x.Name.ToLower() == name.ToLower()))
            throw new ArgumentException("Рабочий центр с таким названием уже существует в подразделении.");
        var center = new WorkCenter { Name = name, TypeId = input.TypeId, DepartmentId = input.DepartmentId };
        db.Add(center);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return center;
    }
}
