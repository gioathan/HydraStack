using Hydra.Api.Caching;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Data;
using Hydra.Api.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICache _cache;

    public VenuesController(AppDbContext db, ICache cache)
    {
        _db = db;
        _cache = cache;
    }

    
}