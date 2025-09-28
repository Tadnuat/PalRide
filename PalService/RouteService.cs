using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.Models;
using PalRepository.PalRepository;
using PalRepository.UnitOfWork;
using PalService.DTOs;
using PalService.Interface;

namespace PalService
{
    public class RouteService : IRouteService
    {
        private readonly PalRideContext _context;
        private readonly GenericRepository<Route> _routeRepo;

        public RouteService(PalRideContext context, GenericRepository<Route> routeRepo)
        {
            _context = context;
            _routeRepo = routeRepo;
        }

        public async Task<ResponseDto<RouteDto>> RegisterRouteAsync(int userId, CreateRouteDto dto)
        {
            var response = new ResponseDto<RouteDto>();
            try
            {
                if (string.IsNullOrWhiteSpace(dto.PickupLocation) || string.IsNullOrWhiteSpace(dto.DropoffLocation))
                    throw new InvalidOperationException("Pickup and dropoff are required");
                if (dto.PickupLocation.Length > 255) throw new InvalidOperationException("Pickup location must be <= 255 characters");
                if (dto.DropoffLocation.Length > 255) throw new InvalidOperationException("Dropoff location must be <= 255 characters");

                var exists = await _context.Routes.AnyAsync(r => r.UserId == userId && r.PickupLocation == dto.PickupLocation && r.DropoffLocation == dto.DropoffLocation && r.IsRoundTrip == dto.IsRoundTrip);
                if (exists)
                {
                    var last = await _context.Routes.Where(r => r.UserId == userId && r.PickupLocation == dto.PickupLocation && r.DropoffLocation == dto.DropoffLocation && r.IsRoundTrip == dto.IsRoundTrip)
                        .OrderByDescending(r => r.CreatedAt).FirstAsync();
                    response.Result = new RouteDto
                    {
                        RouteId = last.RouteId,
                        UserId = last.UserId,
                        PickupLocation = last.PickupLocation,
                        DropoffLocation = last.DropoffLocation,
                        IsRoundTrip = last.IsRoundTrip,
                        CreatedAt = last.CreatedAt
                    };
                    response.Message = "Route already registered";
                    return response;
                }

                var route = new Route
                {
                    UserId = userId,
                    PickupLocation = dto.PickupLocation.Trim(),
                    DropoffLocation = dto.DropoffLocation.Trim(),
                    IsRoundTrip = dto.IsRoundTrip,
                    CreatedAt = DateTime.UtcNow
                };
                await _routeRepo.CreateAsync(route);

                response.Result = new RouteDto
                {
                    RouteId = route.RouteId,
                    UserId = route.UserId,
                    PickupLocation = route.PickupLocation,
                    DropoffLocation = route.DropoffLocation,
                    IsRoundTrip = route.IsRoundTrip,
                    CreatedAt = route.CreatedAt
                };
                response.Message = "Route registered";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<RouteDto>> UpdateRouteAsync(int userId, int routeId, UpdateRouteDto dto)
        {
            var response = new ResponseDto<RouteDto>();
            try
            {
                var route = await _context.Routes.FirstOrDefaultAsync(r => r.RouteId == routeId && r.UserId == userId);
                if (route == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Route not found or you don't have permission to update it";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(dto.PickupLocation) || string.IsNullOrWhiteSpace(dto.DropoffLocation))
                    throw new InvalidOperationException("Pickup and dropoff are required");
                if (dto.PickupLocation.Length > 255) throw new InvalidOperationException("Pickup location must be <= 255 characters");
                if (dto.DropoffLocation.Length > 255) throw new InvalidOperationException("Dropoff location must be <= 255 characters");

                // Check if the new route already exists
                var exists = await _context.Routes.AnyAsync(r => r.UserId == userId && 
                    r.RouteId != routeId && 
                    r.PickupLocation == dto.PickupLocation && 
                    r.DropoffLocation == dto.DropoffLocation && 
                    r.IsRoundTrip == dto.IsRoundTrip);
                
                if (exists)
                {
                    response.IsSuccess = false;
                    response.Message = "A route with these details already exists";
                    return response;
                }

                route.PickupLocation = dto.PickupLocation.Trim();
                route.DropoffLocation = dto.DropoffLocation.Trim();
                route.IsRoundTrip = dto.IsRoundTrip;

                await _routeRepo.UpdateAsync(route);

                response.Result = new RouteDto
                {
                    RouteId = route.RouteId,
                    UserId = route.UserId,
                    PickupLocation = route.PickupLocation,
                    DropoffLocation = route.DropoffLocation,
                    IsRoundTrip = route.IsRoundTrip,
                    CreatedAt = route.CreatedAt
                };
                response.Message = "Route updated successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> DeleteRouteAsync(int userId, int routeId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var route = await _context.Routes.FirstOrDefaultAsync(r => r.RouteId == routeId && r.UserId == userId);
                if (route == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Route not found or you don't have permission to delete it";
                    return response;
                }

                // Check if route is being used in any trips
                var hasTrips = await _context.Trips.AnyAsync(t => t.PickupLocation == route.PickupLocation && t.DropoffLocation == route.DropoffLocation);
                if (hasTrips)
                {
                    response.IsSuccess = false;
                    response.Message = "Cannot delete route that is being used in trips";
                    return response;
                }

                await _routeRepo.RemoveAsync(route);
                response.Result = true;
                response.Message = "Route deleted successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<RouteDto>>> GetUserRoutesAsync(int userId)
        {
            var response = new ResponseDto<List<RouteDto>>();
            try
            {
                var routes = await _context.Routes
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new RouteDto
                    {
                        RouteId = r.RouteId,
                        UserId = r.UserId,
                        PickupLocation = r.PickupLocation,
                        DropoffLocation = r.DropoffLocation,
                        IsRoundTrip = r.IsRoundTrip,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                response.Result = routes;
                response.Message = $"Found {routes.Count} routes";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<RouteDto>> GetRouteByIdAsync(int routeId)
        {
            var response = new ResponseDto<RouteDto>();
            try
            {
                var route = await _context.Routes
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.RouteId == routeId);

                if (route == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Route not found";
                    return response;
                }

                response.Result = new RouteDto
                {
                    RouteId = route.RouteId,
                    UserId = route.UserId,
                    PickupLocation = route.PickupLocation,
                    DropoffLocation = route.DropoffLocation,
                    IsRoundTrip = route.IsRoundTrip,
                    CreatedAt = route.CreatedAt,
                    UserName = route.User?.FullName ?? "Unknown"
                };
                response.Message = "Route found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
    }
}









