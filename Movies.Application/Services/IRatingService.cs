using Movies.Application.Models;

namespace Movies.Application.Services;

public interface IRatingService
{   
    Task<bool> RateMovieAsync(Guid id, int rating, Guid userId, CancellationToken token = default);
    
    Task<bool> DeleteRatingAsync(Guid movieId, Guid? userId, CancellationToken token = default);

    Task<IEnumerable<MovieRating>> GetRatingForUserAsync(Guid userId, CancellationToken token = default);

}