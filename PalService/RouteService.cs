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
    }
}





