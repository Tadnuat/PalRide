using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalService.DTOs;
using PalRepository.Models;
using PalRepository.PalRepository;
using PalRepository.UnitOfWork;
using PalService.Interface;

namespace PalService
{
    public class TripService : ITripService
    {
        private readonly PalRideContext _context;
        private readonly UserRepository _userRepo;
        private readonly GenericRepository<Trip> _tripRepo;
        private readonly GenericRepository<Vehicle> _vehicleRepo;
        private readonly GenericRepository<Route> _routeRepo;

        public TripService(PalRideContext context, UserRepository userRepo, GenericRepository<Trip> tripRepo, GenericRepository<Vehicle> vehicleRepo, GenericRepository<Route> routeRepo)
        {
            _context = context;
            _userRepo = userRepo;
            _tripRepo = tripRepo;
            _vehicleRepo = vehicleRepo;
            _routeRepo = routeRepo;
        }

        private static string MaskPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 4) return "";
            var last4 = phone[^4..];
            return new string('*', Math.Max(0, phone.Length - 4)) + last4;
        }

        public async Task<ResponseDto<TripDto>> CreateTripAsync(CreateTripDto dto, int driverId)
        {
            var response = new ResponseDto<TripDto>();
            try
            {
                // Check if vehicle exists and belongs to driver (if VehicleId is provided)
                Vehicle vehicle = null;
                if (dto.VehicleId.HasValue)
                {
                    vehicle = await _vehicleRepo.GetByIdAsync(dto.VehicleId.Value);
                    if (vehicle == null || vehicle.UserId != driverId)
                    {
                        response.IsSuccess = false;
                        response.Message = "Vehicle not found or does not belong to you";
                        return response;
                    }
                }

                // Check if driver has any pending trips
                var hasPendingTrip = await _context.Trips
                    .AnyAsync(t => t.DriverId == driverId && t.Status == "Pending");
                
                if (hasPendingTrip)
                {
                    response.IsSuccess = false;
                    response.Message = "You already have a pending trip";
                    return response;
                }

                // Create trip
                var trip = new Trip
                {
                    DriverId = driverId,
                    VehicleId = dto.VehicleId,
                    PickupLocation = dto.PickupLocation,
                    DropoffLocation = dto.DropoffLocation,
                    StartTime = dto.StartTime,
                    PricePerSeat = dto.PricePerSeat,
                    PriceFullRide = dto.PriceFullRide,
                    SeatTotal = (byte)dto.SeatTotal,
                    SeatAvailable = (byte)dto.SeatTotal,
                    Status = "Pending",
                    Note = dto.Note,
                    CreatedAt = DateTime.UtcNow
                };

                await _tripRepo.CreateAsync(trip);

                // Get driver and vehicle info
                var driver = await _userRepo.GetByIdAsync(driverId);
                var reviewsCountCreate = driver != null
                    ? await _context.Reviews.CountAsync(r => r.ToUserId == driver.UserId)
                    : 0;

                response.Result = new TripDto
                {
                    TripId = trip.TripId,
                    DriverId = trip.DriverId,
                    DriverName = driver?.FullName ?? "Unknown",
                    PickupLocation = trip.PickupLocation,
                    DropoffLocation = trip.DropoffLocation,
                    StartTime = trip.StartTime,
                    EndTime = trip.EndTime,
                    PricePerSeat = trip.PricePerSeat,
                    PriceFullRide = trip.PriceFullRide ?? 0,
                    SeatTotal = trip.SeatTotal,
                    SeatAvailable = trip.SeatAvailable,
                    Status = trip.Status,
                    Note = trip.Note,
                    CreatedAt = trip.CreatedAt,
                    Vehicle = vehicle != null ? new VehicleDto
                    {
                        VehicleId = vehicle.VehicleId,
                        LicensePlate = vehicle.LicensePlate,
                        Brand = vehicle.Brand,
                        Model = vehicle.Model,
                        Color = vehicle.Color,
                        Type = vehicle.Type,
                        SeatCount = vehicle.SeatCount
                    } : null,
                    Driver = driver != null ? new DriverInfoDto
                    {
                        DriverId = driver.UserId,
                        FullName = driver.FullName,
                        PhoneNumberMasked = MaskPhone(driver.PhoneNumber),
                        RatingAverage = driver.RatingAverage,
                        ReviewsCount = reviewsCountCreate,
                        GmailVerified = driver.GmailVerified
                    } : null
                };
                response.Message = "Trip created successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<TripDto>>> SearchTripsAsync(SearchTripsDto dto)
        {
            var response = new ResponseDto<List<TripDto>>();
            try
            {
                var query = _context.Trips
                    .Where(t => t.Status == "Pending" && t.SeatAvailable > 0);

                if (!string.IsNullOrEmpty(dto.PickupLocation))
                {
                    query = query.Where(t => t.PickupLocation.Contains(dto.PickupLocation));
                }

                if (!string.IsNullOrEmpty(dto.DropoffLocation))
                {
                    query = query.Where(t => t.DropoffLocation.Contains(dto.DropoffLocation));
                }

                if (dto.StartDate.HasValue)
                {
                    var startDate = dto.StartDate.Value.Date;
                    var endDate = startDate.AddDays(1);
                    query = query.Where(t => t.StartTime >= startDate && t.StartTime < endDate);
                }

                var trips = await query
                    .OrderBy(t => t.StartTime)
                    .ToListAsync();

                var tripDtos = new List<TripDto>();
                foreach (var trip in trips)
                {
                    var driver = await _userRepo.GetByIdAsync(trip.DriverId);
                    var vehicle = trip.VehicleId.HasValue ? await _vehicleRepo.GetByIdAsync(trip.VehicleId.Value) : null;
                    var reviewsCountList = driver != null
                        ? await _context.Reviews.CountAsync(r => r.ToUserId == driver.UserId)
                        : 0;

                    tripDtos.Add(new TripDto
                    {
                        TripId = trip.TripId,
                        DriverId = trip.DriverId,
                        DriverName = driver?.FullName ?? "Unknown",
                        PickupLocation = trip.PickupLocation,
                        DropoffLocation = trip.DropoffLocation,
                        StartTime = trip.StartTime,
                        EndTime = trip.EndTime,
                        PricePerSeat = trip.PricePerSeat,
                        PriceFullRide = trip.PriceFullRide ?? 0,
                        SeatTotal = trip.SeatTotal,
                        SeatAvailable = trip.SeatAvailable,
                        Status = trip.Status,
                        Note = trip.Note,
                        CreatedAt = trip.CreatedAt,
                        Vehicle = vehicle != null ? new VehicleDto
                        {
                            VehicleId = vehicle.VehicleId,
                            LicensePlate = vehicle.LicensePlate,
                            Brand = vehicle.Brand,
                            Model = vehicle.Model,
                            Color = vehicle.Color,
                            Type = vehicle.Type,
                            SeatCount = vehicle.SeatCount
                        } : null,
                        Driver = driver != null ? new DriverInfoDto
                        {
                            DriverId = driver.UserId,
                            FullName = driver.FullName,
                            PhoneNumberMasked = MaskPhone(driver.PhoneNumber),
                            RatingAverage = driver.RatingAverage,
                            ReviewsCount = reviewsCountList,
                            GmailVerified = driver.GmailVerified
                        } : null
                    });
                }

                response.Result = tripDtos;
                response.Message = "Trips retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<TripDto>>> GetDriverTripsAsync(int driverId)
        {
            var response = new ResponseDto<List<TripDto>>();
            try
            {
                var trips = await _context.Trips
                    .Where(t => t.DriverId == driverId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                var tripDtos = new List<TripDto>();
                foreach (var trip in trips)
                {
                    var driver = await _userRepo.GetByIdAsync(trip.DriverId);
                    var vehicle = trip.VehicleId.HasValue ? await _vehicleRepo.GetByIdAsync(trip.VehicleId.Value) : null;

                    tripDtos.Add(new TripDto
                    {
                        TripId = trip.TripId,
                        DriverId = trip.DriverId,
                        DriverName = driver?.FullName ?? "Unknown",
                        PickupLocation = trip.PickupLocation,
                        DropoffLocation = trip.DropoffLocation,
                        StartTime = trip.StartTime,
                        EndTime = trip.EndTime,
                        PricePerSeat = trip.PricePerSeat,
                        PriceFullRide = trip.PriceFullRide ?? 0,
                        SeatTotal = trip.SeatTotal,
                        SeatAvailable = trip.SeatAvailable,
                        Status = trip.Status,
                        Note = trip.Note,
                        CreatedAt = trip.CreatedAt,
                        Vehicle = vehicle != null ? new VehicleDto
                        {
                            VehicleId = vehicle.VehicleId,
                            LicensePlate = vehicle.LicensePlate,
                            Brand = vehicle.Brand,
                            Model = vehicle.Model,
                            Color = vehicle.Color,
                            Type = vehicle.Type,
                            SeatCount = vehicle.SeatCount
                        } : null
                    });
                }

                response.Result = tripDtos;
                response.Message = "Driver trips retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<TripDto>> GetTripByIdAsync(int tripId)
        {
            var response = new ResponseDto<TripDto>();
            try
            {
                var trip = await _tripRepo.GetByIdAsync(tripId);
                if (trip == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Trip not found";
                    return response;
                }

                var driver = await _userRepo.GetByIdAsync(trip.DriverId);
                var vehicle = trip.VehicleId.HasValue ? await _vehicleRepo.GetByIdAsync(trip.VehicleId.Value) : null;
                var reviewsCountDetail = driver != null
                    ? await _context.Reviews.CountAsync(r => r.ToUserId == driver.UserId)
                    : 0;

                response.Result = new TripDto
                {
                    TripId = trip.TripId,
                    DriverId = trip.DriverId,
                    DriverName = driver?.FullName ?? "Unknown",
                    PickupLocation = trip.PickupLocation,
                    DropoffLocation = trip.DropoffLocation,
                    StartTime = trip.StartTime,
                    EndTime = trip.EndTime,
                    PricePerSeat = trip.PricePerSeat,
                    PriceFullRide = trip.PriceFullRide ?? 0,
                    SeatTotal = trip.SeatTotal,
                    SeatAvailable = trip.SeatAvailable,
                    Status = trip.Status,
                    Note = trip.Note,
                    CreatedAt = trip.CreatedAt,
                    Vehicle = vehicle != null ? new VehicleDto
                    {
                        VehicleId = vehicle.VehicleId,
                        LicensePlate = vehicle.LicensePlate,
                        Brand = vehicle.Brand,
                        Model = vehicle.Model,
                        Color = vehicle.Color,
                        Type = vehicle.Type,
                        SeatCount = vehicle.SeatCount
                    } : null,
                    Driver = driver != null ? new DriverInfoDto
                    {
                        DriverId = driver.UserId,
                        FullName = driver.FullName,
                        PhoneNumberMasked = MaskPhone(driver.PhoneNumber),
                        RatingAverage = driver.RatingAverage,
                        ReviewsCount = reviewsCountDetail,
                        GmailVerified = driver.GmailVerified
                    } : null
                };
                response.Message = "Trip retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> CancelTripAsync(int tripId, int driverId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var trip = await _tripRepo.GetByIdAsync(tripId);
                if (trip == null || trip.DriverId != driverId)
                {
                    response.IsSuccess = false;
                    response.Message = "Trip not found or you are not the driver";
                    return response;
                }

                if (trip.Status == "Cancelled")
                {
                    response.IsSuccess = false;
                    response.Message = "Trip is already cancelled";
                    return response;
                }

                // Cancel all pending bookings for this trip
                var bookings = await _context.Bookings
                    .Where(b => b.TripId == tripId && b.Status == "Pending")
                    .ToListAsync();

                foreach (var booking in bookings)
                {
                    booking.Status = "Cancelled";
                }

                trip.Status = "Cancelled";
                await _tripRepo.UpdateAsync(trip);

                response.Result = true;
                response.Message = "Trip cancelled successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> CompleteTripAsync(int tripId, int driverId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var trip = await _tripRepo.GetByIdAsync(tripId);
                if (trip == null || trip.DriverId != driverId)
                {
                    response.IsSuccess = false;
                    response.Message = "Trip not found or you are not the driver";
                    return response;
                }

                if (trip.Status != "Pending")
                {
                    response.IsSuccess = false;
                    response.Message = "Trip is not in pending status";
                    return response;
                }

                trip.Status = "Completed";
                await _tripRepo.UpdateAsync(trip);

                response.Result = true;
                response.Message = "Trip completed successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<SearchHistoryItemDto>>> GetSearchHistoryAsync(int userId, int limit = 5)
        {
            var response = new ResponseDto<List<SearchHistoryItemDto>>();
            try
            {
                var routes = await _context.Routes
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                response.Result = routes.Select(r => new SearchHistoryItemDto
                {
                    Id = r.RouteId,
                    PickupLocation = r.PickupLocation,
                    DropoffLocation = r.DropoffLocation,
                    CreatedAt = r.CreatedAt
                }).ToList();
                response.Message = "Search history retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> SaveSearchHistoryAsync(int userId, SearchTripsDto dto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                if (string.IsNullOrWhiteSpace(dto.PickupLocation) || string.IsNullOrWhiteSpace(dto.DropoffLocation))
                {
                    response.IsSuccess = false;
                    response.Message = "Pickup and dropoff are required";
                    return response;
                }

                // Optional: deduplicate recent identical entry
                var last = await _context.Routes
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

                if (last != null && last.PickupLocation == dto.PickupLocation && last.DropoffLocation == dto.DropoffLocation)
                {
                    response.Result = true;
                    response.Message = "Search history already up to date";
                    return response;
                }

                var route = new Route
                {
                    UserId = userId,
                    PickupLocation = dto.PickupLocation!,
                    DropoffLocation = dto.DropoffLocation!,
                    IsRoundTrip = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _routeRepo.CreateAsync(route);

                response.Result = true;
                response.Message = "Search saved";
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
