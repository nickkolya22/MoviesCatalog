using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Movies.Api.Auth;
using Movies.Application.Services;
using Movies.Auth;
using Movies.Contracts.Requests;
using Movies.Mapping;

namespace Movies.Controller;

[ApiController]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IOutputCacheStore _outputCacheStore;

    public MoviesController(IMovieService movieService, IOutputCacheStore outputCacheStore)
    {
        _movieService = movieService;
        _outputCacheStore = outputCacheStore;
    }

    [Authorize(AuthConstants.AdminUserPolicyName)]
    [HttpPost(ApiEndpoints.Movies.Create)]
    public async Task<IActionResult> Create([FromBody]CreateMovieRequest request, CancellationToken token)
    {
        var movie = request.MapToMovie();
         await _movieService.CreateAsync(movie,token);
         await _outputCacheStore.EvictByTagAsync("movies", token);
         return Created($"/api/movies/{movie.Id}", movie);
    }

    [HttpGet(ApiEndpoints.Movies.Get)]
    [OutputCache(PolicyName = "MovieCache ")]
    public async Task<IActionResult> Get([FromRoute] string idOrSlug,CancellationToken token )
    {
        var userId = HttpContext.GetUserId();
        
        var movie = Guid.TryParse(idOrSlug, out var id) 
            ? await _movieService.GetByIdAsync(id, userId, token)
            : await _movieService.GetBySlugAsync(idOrSlug,userId, token);
        if (movie is null)
        {
            return NotFound(); 
        }
        return Ok(movie.MapToResponse());
    }

    [HttpGet(ApiEndpoints.Movies.GetAll)]
    [OutputCache(PolicyName = "MovieCache ")]
    public async Task<IActionResult> GetAll([FromQuery]GetAllMoviesRequest request,CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var options = request.MapToOptions().WithUserId(userId);
        var movies = await _movieService.GetAllAsync(options,token);
        var movieCount = await _movieService.GetCountAsync(options.Title, options.YearOfRelease, token);
        return Ok(movies.MapToResponse(request.Page, request.PageSize, movieCount));
    }

    [Authorize(AuthConstants.AdminUserPolicyName)]
    [HttpPut(ApiEndpoints.Movies.Update)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieRequest request,CancellationToken token)
    {
        var userId = HttpContext.GetUserId();

        var movie = request.MapToMovie(id);
        var updatedMovie = await _movieService.UpdateAsync(movie,userId,token);

        if (updatedMovie is null) 
        {
            return NotFound();
        }
        await _outputCacheStore.EvictByTagAsync("movies", token);
        var response = updatedMovie.MapToResponse();
        return Ok(response);
    }

    [Authorize(AuthConstants.AdminUserPolicyName)]
    [HttpDelete(ApiEndpoints.Movies.Delete)]
    public async Task<IActionResult> Delete([FromRoute]Guid id,CancellationToken token)
    {
        var deleted = await _movieService.DeleteByIdAsync(id, token);

        if (!deleted)
        {
            return NotFound();
        }
        await _outputCacheStore.EvictByTagAsync("movies", token);
        return Ok(); 
    }
}