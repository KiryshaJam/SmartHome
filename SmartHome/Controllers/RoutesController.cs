using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs.Routes;
using SmartHome.Models.Routes;
using SmartHome.Services.Routes;
namespace SmartHome.Controllers;

[ApiController, Route("api/routes")]
public class RoutesController(AppDbContext db) : ControllerBase
{
    private readonly RouteService service = new(db);
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? productId) => Ok(await (
        from r in db.Set<TechRouteSetting>().AsNoTracking()
        join p in db.Products on r.ProductId equals p.Id
        join b in db.Set<SmartHome.Models.Bom.BomRevision>() on r.RevisionId equals b.Id into revisions
        from b in revisions.DefaultIfEmpty()
        where productId == null || r.ProductId == productId
        orderby r.Id descending
        select new { r.Id, r.Name, r.ProductId, productName = p.Name, revisionName = b.Name, r.RevisionId, r.Kind, r.FinalStepId, r.ParameterId, r.ParameterValue }).ToListAsync());
    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups() => Ok(new
    {
        products = await db.Products.Include(x => x.ClassNode).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, unitId = x.ClassNode.MeasureUnitId }).ToListAsync(),
        units = await db.MeasureUnits.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.ShortName }).ToListAsync(),
        operations = await db.ClassNodes.FromSqlRaw("SELECT * FROM class_node WHERE route_branch(\"Id\",'route_operations') AND \"IsTerminal\"").Select(x => new { x.Id, x.Name }).ToListAsync(),
        professions = await db.ClassNodes.FromSqlRaw("SELECT * FROM class_node WHERE route_branch(\"Id\",'route_professions') AND \"IsTerminal\"").Select(x => new { x.Id, x.Name }).ToListAsync(),
        qualifications = await db.EnumValues.Where(x => x.EnumClass.ShortName == "route_qualification").Select(x => new { x.Id, name = x.DisplayName ?? x.StringValue }).ToListAsync(),
        departmentTypes = await db.ClassNodes.FromSqlRaw("SELECT * FROM class_node WHERE route_branch(\"Id\",'route_departments') AND \"IsTerminal\"").OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(),
        centerTypes = await db.ClassNodes.FromSqlRaw("SELECT * FROM class_node WHERE route_branch(\"Id\",'route_centers') AND \"IsTerminal\"").OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(),
        departments = await db.Set<Department>().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.TypeId }).ToListAsync(),
        centers = await (from c in db.Set<WorkCenter>() join d in db.Set<Department>() on c.DepartmentId equals d.Id orderby d.Name, c.Name select new { c.Id, c.Name, c.TypeId, c.DepartmentId, department = d.Name }).ToListAsync(),
        parameters = await db.ParameterDefinitions.Where(x => (int)x.ValueType == 1 || (int)x.ValueType == 2).Select(x => new { x.Id, x.Name, unitId = x.MeasureUnitId }).ToListAsync(),
        variants = await db.Set<SmartHome.Models.Bom.BomRevision>().Where(x => x.IsConfigured && x.IsFinalized && x.Name != null).Select(x => new { x.Id, x.Name, x.ProductId }).ToListAsync(),
        sources = await (from s in db.Set<TechStep>()
                         join r in db.Set<TechRouteSetting>() on s.Id equals r.FinalStepId
                         where r.Kind != "application"
                         select new { s.Id, resultName = r.Name + " — " + s.ResultName, s.ProductId, s.UnitId }).ToListAsync()
    });
    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment(DepartmentWrite input) => Ok(await new RouteResourceService(db).CreateDepartment(input));
    [HttpPost("work-centers")]
    public async Task<IActionResult> CreateCenter(WorkCenterWrite input) => Ok(await new RouteResourceService(db).CreateCenter(input));
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id) => Ok(await service.Read(id));
    [HttpPost] public async Task<IActionResult> Create(RouteWrite input) => Ok(await service.Save(null, input));
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, RouteWrite input) => Ok(await service.Save(id, input));
    [HttpPost("{id:int}/apply")] public async Task<IActionResult> Apply(int id, ApplyRoute input) => Ok(await service.Apply(id, input));
    [HttpGet("{id:int}/calculate")] public async Task<IActionResult> Calculate(int id, [FromQuery] decimal quantity = 1, [FromQuery] int? operationId = null) => Ok(await service.Calculate(id, quantity, operationId));
}
