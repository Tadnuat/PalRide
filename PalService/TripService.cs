using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Globalization;
using PalRepository.DBContexts;
using PalService.DTOs;
using PalRepository.Models;
using PalRepository.PalRepository;
using PalRepository.UnitOfWork;
using PalService.Interface;
using Google.Apis.Http;

namespace PalService
{
    public class TripService : ITripService
    {
        private readonly PalRideContext _context;
        private readonly HttpClient _http;
        private readonly UserRepository _userRepo;
        private readonly GenericRepository<Trip> _tripRepo;
        private readonly GenericRepository<Vehicle> _vehicleRepo;
        private readonly GenericRepository<Route> _routeRepo;
        private readonly BookingRepository _bookingRepo;
        private readonly INotificationService _notificationService;

        public TripService(PalRideContext context, UserRepository userRepo, GenericRepository<Trip> tripRepo, GenericRepository<Vehicle> vehicleRepo, GenericRepository<Route> routeRepo, BookingRepository bookingRepo, INotificationService notificationService, System.Net.Http.IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _userRepo = userRepo;
            _tripRepo = tripRepo;
            _vehicleRepo = vehicleRepo;
            _routeRepo = routeRepo;
            _bookingRepo = bookingRepo;
            _notificationService = notificationService;
            _http = httpClientFactory.CreateClient(nameof(TripService));
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("PalRide/1.0 (contact: admin@palride.example)");
            _http.Timeout = TimeSpan.FromSeconds(15);
        }

        private static string MaskPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 4) return "";
            var last4 = phone[^4..];
            return new string('*', Math.Max(0, phone.Length - 4)) + last4;
        }

        private async Task<bool> HasTimeConflictAsync(int userId, DateTime startTime, int? excludeTripId = null)
        {
            // Define time buffer (e.g., 2 hours before and after)
            var timeBuffer = TimeSpan.FromHours(2);
            var startBuffer = startTime.Subtract(timeBuffer);
            var endBuffer = startTime.Add(timeBuffer);

            var query = _context.Trips
                .Where(t => t.DriverId == userId && 
                           t.Status != "Cancelled" && 
                           t.Status != "Completed" &&
                           t.Status != "Withdrawn" &&
                           t.Status != "Accepted" && // Don't conflict with accepted requests
                           t.StartTime >= startBuffer && 
                           t.StartTime <= endBuffer);

            if (excludeTripId.HasValue)
            {
                query = query.Where(t => t.TripId != excludeTripId.Value);
            }

            return await query.AnyAsync();
        }

        private async Task SendNotificationToSuitableDriversAsync(Trip requestTrip)
        {
            try
            {
                // Find suitable drivers based on:
                // 1. Role is Driver or Both
                // 2. No time conflict with the request time
                // 3. Have matching routes (optional - can be enhanced later)
                
                var suitableDriverIds = await _context.Users
                    .Where(u => (u.Role == "Driver" || u.Role == "Both") && u.IsActive)
                    .Select(u => u.UserId)
                    .ToListAsync();

                if (!suitableDriverIds.Any()) return;

                // Filter drivers with no time conflict and matching routes
                var availableDriverIds = new List<int>();
                foreach (var driverId in suitableDriverIds)
                {
                    // Check time conflict
                    var hasConflict = await HasTimeConflictAsync(driverId, requestTrip.StartTime);
                    if (hasConflict) continue;

                    // Check if driver has matching route (optional)
                    var hasMatchingRoute = await HasMatchingRouteAsync(driverId, requestTrip.PickupLocation, requestTrip.DropoffLocation);
                    if (hasMatchingRoute)
                    {
                        availableDriverIds.Add(driverId);
                    }
                }

                if (!availableDriverIds.Any()) return;

                // Send notification to each suitable driver
                foreach (var driverId in availableDriverIds)
                {
                    await _notificationService.SendAndSaveNotificationToUserAsync(
                        driverId,
                        "Đang tìm nửa kia cho chuyến đi",
                        $"Có yêu cầu chuyến đi mới từ {requestTrip.PickupLocation} đến {requestTrip.DropoffLocation} vào {requestTrip.StartTime:dd/MM/yyyy HH:mm}.",
                        "Important",
                        "Trip",
                        requestTrip.TripId);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking main flow
                Console.WriteLine($"Error sending notification to suitable drivers: {ex.Message}");
            }
        }

        private async Task<bool> HasMatchingRouteAsync(int driverId, string pickupLocation, string dropoffLocation)
        {
            try
            {
                // Check if driver has any saved routes that match the request
                var matchingRoutes = await _context.Routes
                    .Where(r => r.UserId == driverId)
                    .ToListAsync();

                if (!matchingRoutes.Any()) return true; // If no saved routes, consider all drivers

                // Simple string matching (can be enhanced with geolocation later)
                foreach (var route in matchingRoutes)
                {
                    // Check if pickup or dropoff locations contain similar keywords
                    if (IsLocationSimilar(pickupLocation, route.PickupLocation) ||
                        IsLocationSimilar(dropoffLocation, route.DropoffLocation) ||
                        IsLocationSimilar(pickupLocation, route.DropoffLocation) ||
                        IsLocationSimilar(dropoffLocation, route.PickupLocation))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                // If error, return true to not block notifications
                Console.WriteLine($"Error checking matching routes for driver {driverId}: {ex.Message}");
                return true;
            }
        }

        private bool IsLocationSimilar(string location1, string location2)
        {
            if (string.IsNullOrEmpty(location1) || string.IsNullOrEmpty(location2))
                return false;

            // Simple similarity check - can be enhanced with more sophisticated algorithms
            var words1 = location1.ToLower().Split(' ', ',', '-', '.');
            var words2 = location2.ToLower().Split(' ', ',', '-', '.');

            // Check if any significant words match
            var significantWords = words1.Where(w => w.Length > 2).ToList();
            var matchingWords = significantWords.Count(w => words2.Contains(w));

            // If at least 1 significant word matches, consider it similar
            return matchingWords > 0;
        }

        public async Task<ResponseDto<TripDto>> CreateTripAsync(CreateTripDto dto, int driverId)
        {
            var response = new ResponseDto<TripDto>();
            try
            {
                // Check if vehicle exists and belongs to driver (if VehicleId is provided)
                Vehicle vehicle = null!;
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

                // Check if driver has time conflict with existing trips
                var hasTimeConflict = await HasTimeConflictAsync(driverId, dto.StartTime);
                
                if (hasTimeConflict)
                {
                    response.IsSuccess = false;
                    response.Message = "You already have a trip scheduled around this time (within 2 hours)";
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
                    TripType = "Register",
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
                    TripType = trip.TripType,
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
                        GmailVerified = driver.GmailVerified,
                        Introduce = driver.Introduce
                    } : null
                };
                response.Message = "Trip created successfully";
            }
            catch (DbUpdateException dbex)
            {
                response.IsSuccess = false;
                var inner = dbex.InnerException?.Message ?? dbex.Message;
                if (inner.Contains("UQ__Vehicle") || inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                {
                    response.Message = "License plate already exists";
                }
                else
                {
                    response.Message = inner;
                }
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

                // Only trips posted by real drivers (role Driver or Both)
                query = query.Where(t => _context.Users.Any(u => u.UserId == t.DriverId && (u.Role == "Driver" || u.Role == "Both")));

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
                        TripType = trip.TripType,
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
                            GmailVerified = driver.GmailVerified,
                            Introduce = driver.Introduce
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

        public async Task<ResponseDto<List<TripDto>>> SearchPassengerRequestsAsync(SearchTripsDto dto)
        {
            var response = new ResponseDto<List<TripDto>>();
            try
            {
                // Get ALL passenger requests (no pickup/dropoff/date filtering)
                var query = _context.Trips.Where(t => t.Status == "Looking");

                // Requests posted by passengers (role Passenger or Both)
                query = query.Where(t => _context.Users.Any(u => u.UserId == t.DriverId && (u.Role == "Passenger" || u.Role == "Both")));

                var requests = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                var list = new List<TripDto>();
                foreach (var req in requests)
                {
                    var owner = await _userRepo.GetByIdAsync(req.DriverId);
                    list.Add(new TripDto
                    {
                        TripId = req.TripId,
                        DriverId = req.DriverId,
                        DriverName = owner?.FullName ?? "",
                        PickupLocation = req.PickupLocation,
                        DropoffLocation = req.DropoffLocation,
                        StartTime = req.StartTime,
                        EndTime = req.EndTime,
                        PricePerSeat = req.PricePerSeat,
                        PriceFullRide = req.PriceFullRide ?? 0,
                        SeatTotal = req.SeatTotal,
                        SeatAvailable = req.SeatAvailable,
                        Status = req.Status,
                        TripType = req.TripType,
                        Note = req.Note,
                        CreatedAt = req.CreatedAt,
                        Passenger = owner != null ? new PassengerInfoDto
                        {
                            PassengerId = owner.UserId,
                            FullName = owner.FullName,
                            PhoneNumberMasked = MaskPhone(owner.PhoneNumber),
                            RatingAverage = owner.RatingAverage,
                            ReviewsCount = await _context.Reviews.CountAsync(r => r.ToUserId == owner.UserId),
                            GmailVerified = owner.GmailVerified,
                            Introduce = owner.Introduce
                        } : null
                    });
                }

                response.Result = list;
                response.Message = "Passenger requests retrieved";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<TripDto>>> SearchPassengerRequestsFilteredAsync(SearchTripsDto dto)
        {
            var response = new ResponseDto<List<TripDto>>();
            try
            {
                var query = _context.Trips.Where(t => t.Status == "Looking");

                if (!string.IsNullOrEmpty(dto.PickupLocation))
                    query = query.Where(t => t.PickupLocation.Contains(dto.PickupLocation));
                if (!string.IsNullOrEmpty(dto.DropoffLocation))
                    query = query.Where(t => t.DropoffLocation.Contains(dto.DropoffLocation));
                if (dto.StartDate.HasValue)
                {
                    var startDate = dto.StartDate.Value.Date;
                    var endDate = startDate.AddDays(1);
                    query = query.Where(t => t.StartTime >= startDate && t.StartTime < endDate);
                }

                query = query.Where(t => _context.Users.Any(u => u.UserId == t.DriverId && (u.Role == "Passenger" || u.Role == "Both")));

                var requests = await query.OrderBy(t => t.StartTime).ToListAsync();

                var list = new List<TripDto>();
                foreach (var req in requests)
                {
                    var owner = await _userRepo.GetByIdAsync(req.DriverId);
                    list.Add(new TripDto
                    {
                        TripId = req.TripId,
                        DriverId = req.DriverId,
                        DriverName = owner?.FullName ?? "",
                        PickupLocation = req.PickupLocation,
                        DropoffLocation = req.DropoffLocation,
                        StartTime = req.StartTime,
                        EndTime = req.EndTime,
                        PricePerSeat = req.PricePerSeat,
                        PriceFullRide = req.PriceFullRide ?? 0,
                        SeatTotal = req.SeatTotal,
                        SeatAvailable = req.SeatAvailable,
                        Status = req.Status,
                        TripType = req.TripType,
                        Note = req.Note,
                        CreatedAt = req.CreatedAt,
                        Passenger = owner != null ? new PassengerInfoDto
                        {
                            PassengerId = owner.UserId,
                            FullName = owner.FullName,
                            PhoneNumberMasked = MaskPhone(owner.PhoneNumber),
                            RatingAverage = owner.RatingAverage,
                            ReviewsCount = await _context.Reviews.CountAsync(r => r.ToUserId == owner.UserId),
                            GmailVerified = owner.GmailVerified,
                            Introduce = owner.Introduce
                        } : null
                    });
                }

                response.Result = list;
                response.Message = "Passenger requests filtered";
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
                    .Where(t => t.DriverId == driverId && t.TripType != "Request" && t.Status != "Completed")
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                var tripDtos = new List<TripDto>();
                foreach (var trip in trips)
                {
                    var driver = await _userRepo.GetByIdAsync(trip.DriverId);
                    var vehicle = trip.VehicleId.HasValue ? await _vehicleRepo.GetByIdAsync(trip.VehicleId.Value) : null;
                    // Build passenger lists
                    var tripBookings = await _context.Bookings
                        .Where(b => b.TripId == trip.TripId)
                        .OrderByDescending(b => b.BookingTime)
                        .ToListAsync();
                    var accepted = new List<PassengerInfoDto>();
                    var pending = new List<PassengerInfoDto>();
                    foreach (var b in tripBookings)
                    {
                        var p = await _userRepo.GetByIdAsync(b.PassengerId);
                        if (p == null) continue;
                        var info = new PassengerInfoDto
                        {
                            BookingId = b.BookingId,
                            PassengerId = p.UserId,
                            FullName = p.FullName,
                            PhoneNumberMasked = MaskPhone(p.PhoneNumber),
                            RatingAverage = p.RatingAverage,
                            ReviewsCount = await _context.Reviews.CountAsync(r => r.ToUserId == p.UserId),
                            GmailVerified = p.GmailVerified,
                            Introduce = p.Introduce
                        };
                        if (string.Equals(b.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
                            accepted.Add(info);
                        else if (string.Equals(b.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                            pending.Add(info);
                    }

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
                        TripType = trip.TripType,
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
                        AcceptedPassengers = accepted,
                        PendingPassengers = pending
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

        public async Task<ResponseDto<List<TripDto>>> GetDriverTripHistoryAsync(int driverId)
        {
            var response = new ResponseDto<List<TripDto>>();
            try
            {
                var trips = await _context.Trips
                    .Where(t => t.DriverId == driverId && t.TripType != "Request" && t.Status == "Completed")
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                var tripDtos = new List<TripDto>();
                foreach (var trip in trips)
                {
                    var driver = await _userRepo.GetByIdAsync(trip.DriverId);
                    var vehicle = trip.VehicleId.HasValue ? await _vehicleRepo.GetByIdAsync(trip.VehicleId.Value) : null;
                    // Build passenger lists
                    var tripBookings = await _context.Bookings
                        .Where(b => b.TripId == trip.TripId)
                        .OrderByDescending(b => b.BookingTime)
                        .ToListAsync();
                    var accepted = new List<PassengerInfoDto>();
                    var pending = new List<PassengerInfoDto>();
                    foreach (var b in tripBookings)
                    {
                        var p = await _userRepo.GetByIdAsync(b.PassengerId);
                        if (p == null) continue;
                        var info = new PassengerInfoDto
                        {
                            BookingId = b.BookingId,
                            PassengerId = p.UserId,
                            FullName = p.FullName,
                            PhoneNumberMasked = MaskPhone(p.PhoneNumber),
                            RatingAverage = p.RatingAverage,
                            ReviewsCount = await _context.Reviews.CountAsync(r => r.ToUserId == p.UserId),
                            GmailVerified = p.GmailVerified,
                            Introduce = p.Introduce
                        };
                        if (string.Equals(b.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
                            accepted.Add(info);
                        else if (string.Equals(b.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                            pending.Add(info);
                    }

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
                        TripType = trip.TripType,
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
                        AcceptedPassengers = accepted,
                        PendingPassengers = pending
                    });
                }

                response.Result = tripDtos;
                response.Message = "Driver trip history retrieved successfully";
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

                // Passenger lists for detailed view
                var tripBookings = await _context.Bookings
                    .Where(b => b.TripId == trip.TripId)
                    .OrderByDescending(b => b.BookingTime)
                    .ToListAsync();
                var accepted = new List<PassengerInfoDto>();
                var pending = new List<PassengerInfoDto>();
                foreach (var b in tripBookings)
                {
                    var p = await _userRepo.GetByIdAsync(b.PassengerId);
                    if (p == null) continue;
                    var info = new PassengerInfoDto
                    {
                        BookingId = b.BookingId,
                        PassengerId = p.UserId,
                        FullName = p.FullName,
                        PhoneNumberMasked = MaskPhone(p.PhoneNumber),
                        RatingAverage = p.RatingAverage,
                        ReviewsCount = await _context.Reviews.CountAsync(r => r.ToUserId == p.UserId),
                        GmailVerified = p.GmailVerified,
                        Introduce = p.Introduce
                    };
                    if (string.Equals(b.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
                        accepted.Add(info);
                    else if (string.Equals(b.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                        pending.Add(info);
                }

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
                    TripType = trip.TripType,
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
                        GmailVerified = driver.GmailVerified,
                        Introduce = driver.Introduce
                    } : null,
                    AcceptedPassengers = accepted,
                    PendingPassengers = pending
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
                await _context.SaveChangesAsync();

                // Send notification to all passengers in the trip
                await _notificationService.SendAndSaveNotificationToTripAsync(
                    tripId, 
                    "Tài xế vừa hủy chuyến đi", 
                    $"Tài xế đã hủy chuyến đi vào ngày {trip.StartTime:dd/MM/yyyy HH:mm}.", 
                    "Important", 
                    "Trip", 
                    tripId);

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
                await _context.SaveChangesAsync();

                // Send notification to all passengers in the trip
                await _notificationService.SendAndSaveNotificationToTripAsync(
                    tripId, 
                    "Chuyến đi đã hoàn tất", 
                    "Chuyến đi của bạn đã hoàn tất, vui lòng đánh giá hành khách.", 
                    "Important", 
                    "Trip", 
                    tripId);

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

        private async Task<(double lat, double lon)> GeocodeAsync(string query)
        {
            var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(query)}&limit=1&addressdetails=0&countrycodes=vn";
            try
            {
                var res = await _http.GetFromJsonAsync<List<NominatimResult>>(url);
                if (res == null || res.Count == 0) throw new InvalidOperationException("Location not found: " + query);
                return (double.Parse(res[0].lat, CultureInfo.InvariantCulture), double.Parse(res[0].lon, CultureInfo.InvariantCulture));
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("Geocoding service timed out");
            }
        }

        private async Task<(double distanceKm, string polyline)> RouteAsync((double lat, double lon) from, (double lat, double lon) to)
        {
            var url = $"https://router.project-osrm.org/route/v1/driving/{from.lon},{from.lat};{to.lon},{to.lat}?overview=false&steps=false";
            try
            {
                var res = await _http.GetFromJsonAsync<OsrmResponse>(url);
                if (res == null || res.code != "Ok" || res.routes == null || res.routes.Count == 0)
                    throw new InvalidOperationException("Failed to get route");
                var r = res.routes[0];
                return (r.distance / 1000.0, string.Empty);
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("Routing service timed out");
            }
        }

        private static (decimal min, decimal max) CalculatePriceRange(double distanceKm)
        {
            // Exact price: up to 5km = 20,000; each additional 1km (rounded up) +3,000
            var price = 20000m;
            if (distanceKm > 5.0)
            {
                var extraKm = Math.Ceiling(distanceKm - 5.0);
                price += 3000m * (decimal)extraKm;
            }
            var min = Math.Max(0m, price - 5000m);
            var max = price + 5000m;
            return (min, max);
        }

        public async Task<ResponseDto<PriceRangeDto>> GetPriceRangeAsync(string pickup, string dropoff)
        {
            var response = new ResponseDto<PriceRangeDto>();
            try
            {
                var from = await GeocodeAsync(pickup);
                var to = await GeocodeAsync(dropoff);
                double distanceKm;
                string polyline;
                try
                {
                    (distanceKm, polyline) = await RouteAsync(from, to);
                }
                catch (Exception)
                {
                    // Fallback to Haversine straight-line distance if routing fails
                    distanceKm = HaversineKm(from.lat, from.lon, to.lat, to.lon);
                    polyline = string.Empty;
                }
                var (min, max) = CalculatePriceRange(distanceKm);
                response.Result = new PriceRangeDto
                {
                    DistanceKm = Math.Round(distanceKm, 2),
                    MinPrice = min,
                    MaxPrice = max,
                    Polyline = polyline
                };
                response.Message = "Price range calculated";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        // Route-specific methods moved to RouteService

        private class NominatimResult
        {
            public string lat { get; set; } = string.Empty;
            public string lon { get; set; } = string.Empty;
        }

        private class OsrmResponse
        {
            public string code { get; set; } = string.Empty;
            public List<OsrmRoute> routes { get; set; } = new List<OsrmRoute>();
        }

        private class OsrmRoute
        {
            public double distance { get; set; }
            public string geometry { get; set; } = string.Empty;
        }

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // km
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;

        public async Task<ResponseDto<VehicleDto>> AddVehicleAsync(int userId, AddVehicleDto dto)
        {
            var response = new ResponseDto<VehicleDto>();
            try
            {
                if (string.IsNullOrWhiteSpace(dto.LicensePlate)) throw new InvalidOperationException("License plate is required");
                if (string.IsNullOrWhiteSpace(dto.Type)) throw new InvalidOperationException("Vehicle type is required");
                if (dto.SeatCount <= 0) throw new InvalidOperationException("Seat count must be greater than 0");

                // Validate lengths to satisfy DB constraints
                if (dto.LicensePlate.Length > 20) throw new InvalidOperationException("License plate must be <= 20 characters");
                if (!string.IsNullOrEmpty(dto.Brand) && dto.Brand.Length > 50) throw new InvalidOperationException("Brand must be <= 50 characters");
                if (!string.IsNullOrEmpty(dto.Model) && dto.Model.Length > 50) throw new InvalidOperationException("Model must be <= 50 characters");
                if (!string.IsNullOrEmpty(dto.Color) && dto.Color.Length > 30) throw new InvalidOperationException("Color must be <= 30 characters");
                if (dto.Type.Length > 20) throw new InvalidOperationException("Type must be <= 20 characters");

                // Normalize
                var plate = dto.LicensePlate.Trim().ToUpperInvariant();

                // Check uniqueness
                var exists = await _context.Vehicles.AnyAsync(v => v.LicensePlate == plate);
                if (exists) throw new InvalidOperationException("License plate already exists");

                var vehicle = new Vehicle
                {
                    UserId = userId,
                    LicensePlate = plate,
                    Brand = dto.Brand?.Trim() ?? string.Empty,
                    Model = dto.Model?.Trim() ?? string.Empty,
                    Color = dto.Color?.Trim() ?? string.Empty,
                    Type = dto.Type.Trim(),
                    SeatCount = (byte)Math.Max(1, dto.SeatCount),
                    CreatedAt = DateTime.UtcNow
                };

                await _vehicleRepo.CreateAsync(vehicle);

                response.Result = new VehicleDto
                {
                    VehicleId = vehicle.VehicleId,
                    LicensePlate = vehicle.LicensePlate,
                    Brand = vehicle.Brand,
                    Model = vehicle.Model,
                    Color = vehicle.Color,
                    Type = vehicle.Type,
                    SeatCount = vehicle.SeatCount
                };
                response.Message = "Vehicle added";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> VerifyVehicleAsync(int vehicleId, bool verified)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var vehicle = await _vehicleRepo.GetByIdAsync(vehicleId) ?? throw new KeyNotFoundException("Vehicle not found");
                if (vehicle.Verified == verified)
                {
                    response.Result = true;
                    response.Message = verified ? "Vehicle already verified" : "Vehicle already unverified";
                    return response;
                }

                vehicle.Verified = verified;
                await _vehicleRepo.UpdateAsync(vehicle);

                response.Result = true;
                response.Message = verified ? "Vehicle verified" : "Vehicle unverified";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<VehicleDto>>> GetMyVehiclesAsync(int userId)
        {
            var response = new ResponseDto<List<VehicleDto>>();
            try
            {
                var vehicles = await _context.Vehicles
                    .Where(v => v.UserId == userId)
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();

                response.Result = vehicles.Select(v => new VehicleDto
                {
                    VehicleId = v.VehicleId,
                    LicensePlate = v.LicensePlate,
                    Brand = v.Brand,
                    Model = v.Model,
                    Color = v.Color,
                    Type = v.Type,
                    SeatCount = v.SeatCount
                }).ToList();
                response.Message = "Vehicles retrieved";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<VehicleDto>> UpdateVehicleAsync(int userId, int vehicleId, UpdateVehicleDto dto)
        {
            var response = new ResponseDto<VehicleDto>();
            try
            {
                var vehicle = await _vehicleRepo.GetByIdAsync(vehicleId) ?? throw new KeyNotFoundException("Vehicle not found");
                if (vehicle.UserId != userId) throw new UnauthorizedAccessException("You do not own this vehicle");

                if (!string.IsNullOrEmpty(dto.LicensePlate))
                {
                    var plate = dto.LicensePlate.Trim().ToUpperInvariant();
                    if (plate.Length > 20) throw new InvalidOperationException("License plate must be <= 20 characters");
                    var duplicate = await _context.Vehicles.AnyAsync(v => v.VehicleId != vehicleId && v.LicensePlate == plate);
                    if (duplicate) throw new InvalidOperationException("License plate already exists");
                    vehicle.LicensePlate = plate;
                }

                if (!string.IsNullOrEmpty(dto.Brand)) vehicle.Brand = dto.Brand.Trim();
                if (!string.IsNullOrEmpty(dto.Model)) vehicle.Model = dto.Model.Trim();
                if (!string.IsNullOrEmpty(dto.Color)) vehicle.Color = dto.Color.Trim();
                if (!string.IsNullOrEmpty(dto.Type)) vehicle.Type = dto.Type.Trim();
                if (dto.SeatCount > 0) vehicle.SeatCount = (byte)dto.SeatCount;

                await _vehicleRepo.UpdateAsync(vehicle);

                response.Result = new VehicleDto
                {
                    VehicleId = vehicle.VehicleId,
                    LicensePlate = vehicle.LicensePlate,
                    Brand = vehicle.Brand,
                    Model = vehicle.Model,
                    Color = vehicle.Color,
                    Type = vehicle.Type,
                    SeatCount = vehicle.SeatCount
                };
                response.Message = "Vehicle updated";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<TripDto>> CreatePassengerRequestAsync(int userId, CreatePassengerRequestDto dto)
        {
            var response = new ResponseDto<TripDto>();
            try
            {
                if (string.IsNullOrWhiteSpace(dto.PickupLocation) || string.IsNullOrWhiteSpace(dto.DropoffLocation))
                    throw new InvalidOperationException("Pickup and dropoff are required");
                if (dto.SeatCount <= 0)
                    throw new InvalidOperationException("Seat count must be greater than 0");
                if (dto.PickupLocation.Length > 255) throw new InvalidOperationException("Pickup location must be <= 255 characters");
                if (dto.DropoffLocation.Length > 255) throw new InvalidOperationException("Dropoff location must be <= 255 characters");
                if (!string.IsNullOrEmpty(dto.Note) && dto.Note.Length > 500) throw new InvalidOperationException("Note must be <= 500 characters");

                // Check if passenger has time conflict with existing requests
                var hasTimeConflict = await HasTimeConflictAsync(userId, dto.StartTime);
                
                if (hasTimeConflict)
                {
                    response.IsSuccess = false;
                    response.Message = "You already have a request scheduled around this time (within 2 hours)";
                    return response;
                }

                // Create a trip-like request where the passenger is looking for a driver
                var trip = new Trip
                {
                    DriverId = userId, // request owner
                    VehicleId = null,
                    PickupLocation = dto.PickupLocation.Trim(),
                    DropoffLocation = dto.DropoffLocation.Trim(),
                    StartTime = dto.StartTime,
                    PricePerSeat = dto.FullRide ? 0 : (dto.OfferedPrice ?? 0),
                    PriceFullRide = dto.FullRide ? (dto.OfferedPrice ?? 0) : null,
                    SeatTotal = (byte)Math.Max(1, dto.SeatCount),
                    SeatAvailable = 0, // For requests, no seats are available until driver accepts
                    Status = "Looking",
                    TripType = "Request",
                    Note = dto.Note,
                    CreatedAt = DateTime.UtcNow
                };

                await _tripRepo.CreateAsync(trip);

                // Send notification to suitable drivers only
                await SendNotificationToSuitableDriversAsync(trip);

                var passenger = await _userRepo.GetByIdAsync(userId);
                response.Result = new TripDto
                {
                    TripId = trip.TripId,
                    DriverId = trip.DriverId,
                    DriverName = passenger?.FullName ?? "",
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
                    CreatedAt = trip.CreatedAt
                };
                response.Message = "Passenger request created";
            }
            catch (DbUpdateException dbex)
            {
                response.IsSuccess = false;
                response.Message = dbex.InnerException?.Message ?? dbex.Message;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> WithdrawPassengerRequestAsync(int userId, int tripId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var trip = await _tripRepo.GetByIdAsync(tripId) ?? throw new KeyNotFoundException("Request not found");
                if (trip.DriverId != userId)
                    throw new UnauthorizedAccessException("You can only withdraw your own request");

                if (trip.Status == "Withdrawn")
                {
                    response.Result = true;
                    response.Message = "Request already withdrawn";
                    return response;
                }

                trip.Status = "Withdrawn";
                await _tripRepo.UpdateAsync(trip);

                response.Result = true;
                response.Message = "Request withdrawn";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<TripDto>> AcceptPassengerRequestAsync(int driverId, AcceptPassengerRequestDto dto)
        {
            var response = new ResponseDto<TripDto>();
            try
            {
                // Get the passenger request trip
                var requestTrip = await _tripRepo.GetByIdAsync(dto.RequestTripId) ?? 
                    throw new KeyNotFoundException("Passenger request not found");

                // Validate the request trip
                if (requestTrip.TripType != "Request")
                    throw new InvalidOperationException("This is not a passenger request");
                
                if (requestTrip.Status != "Looking")
                    throw new InvalidOperationException("This request is no longer available");

                // Check if driver has time conflict with existing trips
                var hasTimeConflict = await HasTimeConflictAsync(driverId, requestTrip.StartTime);
                
                if (hasTimeConflict)
                {
                    response.IsSuccess = false;
                    response.Message = "You already have a trip scheduled around this time (within 2 hours)";
                    return response;
                }

                // Validate vehicle if provided
                Vehicle vehicle = null!;
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

                // Create new trip based on passenger request
                var newTrip = new Trip
                {
                    DriverId = driverId,
                    VehicleId = dto.VehicleId,
                    PickupLocation = requestTrip.PickupLocation,
                    DropoffLocation = requestTrip.DropoffLocation,
                    StartTime = requestTrip.StartTime,
                    PricePerSeat = dto.PricePerSeat,
                    PriceFullRide = dto.PriceFullRide,
                    SeatTotal = requestTrip.SeatTotal,
                    SeatAvailable = 0, // Reserve all seats for the passenger group
                    Status = "Pending",
                    TripType = "Register",
                    Note = dto.Note,
                    CreatedAt = DateTime.UtcNow
                };

                await _tripRepo.CreateAsync(newTrip);
                await _context.SaveChangesAsync(); // Save trip first to get TripId

                // Create booking for the passenger group
                var totalPrice = requestTrip.PriceFullRide.HasValue ? 
                    (dto.PriceFullRide ?? 0) : 
                    (dto.PricePerSeat * requestTrip.SeatTotal);

                var booking = new Booking
                {
                    TripId = newTrip.TripId,
                    PassengerId = requestTrip.DriverId, // The original passenger who made the request
                    SeatCount = (byte)requestTrip.SeatTotal, // Use the actual number of seats requested
                    TotalPrice = totalPrice,
                    Status = "Accepted", // Auto-accept since driver is accepting the request
                    BookingTime = DateTime.UtcNow
                };

                await _bookingRepo.CreateAsync(booking);

                // Update seat available for the new trip
                newTrip.SeatAvailable = 0; // All seats are now taken by the booking
                await _tripRepo.UpdateAsync(newTrip);

                // Update the original request status
                requestTrip.Status = "Accepted";
                await _tripRepo.UpdateAsync(requestTrip);

                await _context.SaveChangesAsync(); // Save booking and request update
                
                // Send notification to passenger
                await _notificationService.SendAndSaveNotificationToUserAsync(
                    requestTrip.DriverId, 
                    "Tài xế đã nhận yêu cầu", 
                    "Tài xế đã nhận yêu cầu chuyến đi của bạn.", 
                    "Important", 
                    "Trip", 
                    newTrip.TripId);
                
                // Verify booking was created
                var createdBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.TripId == newTrip.TripId && b.PassengerId == requestTrip.DriverId);
                
                if (createdBooking == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to create booking";
                    return response;
                }

                // Get driver and vehicle info for response
                var driver = await _userRepo.GetByIdAsync(driverId);
                response.Result = new TripDto
                {
                    TripId = newTrip.TripId,
                    DriverId = newTrip.DriverId,
                    DriverName = driver?.FullName ?? "Unknown",
                    PickupLocation = newTrip.PickupLocation,
                    DropoffLocation = newTrip.DropoffLocation,
                    StartTime = newTrip.StartTime,
                    EndTime = newTrip.EndTime,
                    PricePerSeat = newTrip.PricePerSeat,
                    PriceFullRide = newTrip.PriceFullRide ?? 0,
                    SeatTotal = newTrip.SeatTotal,
                    SeatAvailable = newTrip.SeatAvailable,
                    Status = newTrip.Status,
                    TripType = newTrip.TripType,
                    Note = newTrip.Note,
                    CreatedAt = newTrip.CreatedAt,
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
                };
                response.Message = "Passenger request accepted and trip created successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<TripDto>> UpdateTripAsync(int tripId, int driverId, UpdateTripDto dto)
        {
            var response = new ResponseDto<TripDto>();
            try
            {
                // Get the trip to update
                var trip = await _tripRepo.GetByIdAsync(tripId);
                if (trip == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Trip not found";
                    return response;
                }

                // Check if the driver owns this trip
                if (trip.DriverId != driverId)
                {
                    response.IsSuccess = false;
                    response.Message = "You can only update your own trips";
                    return response;
                }

                // Check if trip can be updated (not completed or cancelled)
                if (trip.Status == "Completed" || trip.Status == "Cancelled")
                {
                    response.IsSuccess = false;
                    response.Message = "Cannot update completed or cancelled trips";
                    return response;
                }

                // Validate vehicle if provided
                Vehicle vehicle = null!;
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

                // Validate seat total if provided
                if (dto.SeatTotal.HasValue)
                {
                    if (dto.SeatTotal.Value < trip.SeatAvailable)
                    {
                        response.IsSuccess = false;
                        response.Message = $"Seat total cannot be less than available seats ({trip.SeatAvailable})";
                        return response;
                    }
                    if (dto.SeatTotal.Value <= 0)
                    {
                        response.IsSuccess = false;
                        response.Message = "Seat total must be greater than 0";
                        return response;
                    }
                }

                // Validate prices if provided
                if (dto.PricePerSeat.HasValue && dto.PricePerSeat.Value < 0)
                {
                    response.IsSuccess = false;
                    response.Message = "Price per seat cannot be negative";
                    return response;
                }

                if (dto.PriceFullRide.HasValue && dto.PriceFullRide.Value < 0)
                {
                    response.IsSuccess = false;
                    response.Message = "Price full ride cannot be negative";
                    return response;
                }

                // Validate note length if provided
                if (!string.IsNullOrEmpty(dto.Note) && dto.Note.Length > 500)
                {
                    response.IsSuccess = false;
                    response.Message = "Note must be 500 characters or less";
                    return response;
                }

                // Update trip properties
                if (dto.VehicleId.HasValue)
                {
                    trip.VehicleId = dto.VehicleId.Value;
                }

                if (dto.SeatTotal.HasValue)
                {
                    var oldSeatTotal = trip.SeatTotal;
                    trip.SeatTotal = (byte)dto.SeatTotal.Value;
                    // Adjust available seats if total increased
                    if (dto.SeatTotal.Value > oldSeatTotal)
                    {
                        trip.SeatAvailable += (byte)(dto.SeatTotal.Value - oldSeatTotal);
                    }
                }

                if (dto.PricePerSeat.HasValue)
                {
                    trip.PricePerSeat = dto.PricePerSeat.Value;
                }

                if (dto.PriceFullRide.HasValue)
                {
                    trip.PriceFullRide = dto.PriceFullRide.Value;
                }

                if (dto.Note != null) // Allow empty string to clear note
                {
                    trip.Note = dto.Note;
                }

                // Update the trip
                await _tripRepo.UpdateAsync(trip);

                // Get updated vehicle info if changed
                if (dto.VehicleId.HasValue && vehicle == null)
                {
                    vehicle = await _vehicleRepo.GetByIdAsync(dto.VehicleId.Value);
                }
                else if (!dto.VehicleId.HasValue && trip.VehicleId.HasValue)
                {
                    vehicle = await _vehicleRepo.GetByIdAsync(trip.VehicleId.Value);
                }

                // Get driver info
                var driver = await _userRepo.GetByIdAsync(driverId);

                // Return updated trip info
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
                    TripType = trip.TripType,
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
                };
                response.Message = "Trip updated successfully";
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
