using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalService.DTOs;
using PalRepository.Models;
using PalRepository.PalRepository;
using PalRepository.UnitOfWork;
using PalService.Interface;

namespace PalService
{
    public class BookingService : IBookingService
    {
        private readonly PalRideContext _context;
        private readonly UserRepository _userRepo;
        private readonly BookingRepository _bookingRepo;
        private readonly GenericRepository<Trip> _tripRepo;
        private readonly GenericRepository<UserVoucher> _userVoucherRepo;
        private readonly GenericRepository<Voucher> _voucherRepo;
        private readonly INotificationService _notificationService;

        public BookingService(PalRideContext context, UserRepository userRepo, BookingRepository bookingRepo, GenericRepository<Trip> tripRepo, GenericRepository<UserVoucher> userVoucherRepo, GenericRepository<Voucher> voucherRepo, INotificationService notificationService)
        {
            _context = context;
            _userRepo = userRepo;
            _bookingRepo = bookingRepo;
            _tripRepo = tripRepo;
            _userVoucherRepo = userVoucherRepo;
            _voucherRepo = voucherRepo;
            _notificationService = notificationService;
        }

        // Removed: CreateBookingAsync replaced by quote + confirm

        private static string MaskPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 4) return string.Empty;
            var last4 = phone[^4..];
            return new string('*', Math.Max(0, phone.Length - 4)) + last4;
        }

        public async Task<ResponseDto<BookingDto>> AcceptBookingAsync(int bookingId, int driverId)
        {
            var response = new ResponseDto<BookingDto>();
            try
            {
                var booking = await _bookingRepo.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Booking not found";
                    return response;
                }

                var trip = await _tripRepo.GetByIdAsync(booking.TripId);
                if (trip == null || trip.DriverId != driverId)
                {
                    response.IsSuccess = false;
                    response.Message = "You can only accept bookings for your own trips";
                    return response;
                }

                if (booking.Status != "Pending")
                {
                    response.IsSuccess = false;
                    response.Message = "Booking is not in pending status";
                    return response;
                }

                booking.Status = "Accepted";
                booking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepo.UpdateAsync(booking);
                await _context.SaveChangesAsync();

                // Send notification to passenger
                await _notificationService.SendAndSaveNotificationToUserAsync(
                    booking.PassengerId, 
                    "Tài xế chấp nhận booking", 
                    "Tài xế đã chấp nhận booking của bạn.", 
                    "Important", 
                    "Booking", 
                    bookingId);

                var passenger = await _userRepo.GetByIdAsync(booking.PassengerId);

                response.Result = new BookingDto
                {
                    BookingId = booking.BookingId,
                    TripId = booking.TripId,
                    PassengerId = booking.PassengerId,
                    PassengerName = passenger?.FullName ?? "Unknown",
                    SeatCount = booking.SeatCount,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status,
                    BookingTime = booking.BookingTime
                };
                response.Message = "Booking accepted successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<BookingDto>> CancelBookingAsync(int bookingId, int userId)
        {
            var response = new ResponseDto<BookingDto>();
            try
            {
                var booking = await _bookingRepo.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Booking not found";
                    return response;
                }

                // Check if user is the passenger or the driver
                var trip = await _tripRepo.GetByIdAsync(booking.TripId);
                if (booking.PassengerId != userId && trip?.DriverId != userId)
                {
                    response.IsSuccess = false;
                    response.Message = "You can only cancel your own bookings or bookings for your trips";
                    return response;
                }

                if (booking.Status == "Cancelled")
                {
                    response.IsSuccess = false;
                    response.Message = "Booking is already cancelled";
                    return response;
                }

                booking.Status = "Cancelled";
                await _bookingRepo.UpdateAsync(booking);

                // Return seats to trip
                trip.SeatAvailable += booking.SeatCount;
                await _tripRepo.UpdateAsync(trip);
                await _context.SaveChangesAsync();

                // Send notification to the other party
                if (booking.PassengerId == userId)
                {
                    // Passenger cancelled, notify driver
                    await _notificationService.SendAndSaveNotificationToUserAsync(
                        trip.DriverId, 
                        "Hành khách hủy booking", 
                        "Hành khách đã hủy booking chuyến đi của bạn.", 
                        "Important", 
                        "Booking", 
                        bookingId);
                }
                else
                {
                    // Driver cancelled, notify passenger
                    await _notificationService.SendAndSaveNotificationToUserAsync(
                        booking.PassengerId, 
                        "Tài xế hủy booking", 
                        "Tài xế đã hủy booking của bạn.", 
                        "Important", 
                        "Booking", 
                        bookingId);
                }

                var passenger = await _userRepo.GetByIdAsync(booking.PassengerId);

                response.Result = new BookingDto
                {
                    BookingId = booking.BookingId,
                    TripId = booking.TripId,
                    PassengerId = booking.PassengerId,
                    PassengerName = passenger?.FullName ?? "Unknown",
                    SeatCount = booking.SeatCount,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status,
                    BookingTime = booking.BookingTime
                };
                response.Message = "Booking cancelled successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<BookingDto>>> GetUserBookingsAsync(int userId)
        {
            var response = new ResponseDto<List<BookingDto>>();
            try
            {
                var bookings = await _context.Bookings
                    .Where(b => b.PassengerId == userId && b.Trip.Status != "Completed")
                    .OrderByDescending(b => b.BookingTime)
                    .ToListAsync();

                var result = new List<BookingDto>();

                foreach (var booking in bookings)
                {
                    var passenger = await _userRepo.GetByIdAsync(booking.PassengerId);
                    var trip = await _tripRepo.GetByIdAsync(booking.TripId);
                    Vehicle vehicle = null!;
                    User driver = null!;
                    if (trip != null)
                    {
                        if (trip.VehicleId.HasValue)
                        {
                            vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == trip.VehicleId.Value);
                        }
                        driver = await _userRepo.GetByIdAsync(trip.DriverId);
                    }

                    var tripDto = trip == null ? null : new TripDto
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
                            ReviewsCount = await _context.Reviews.CountAsync(r => r.ToUserId == driver.UserId),
                            GmailVerified = driver.GmailVerified,
                            Introduce = driver.Introduce
                        } : null
                    };

                    result.Add(new BookingDto
                    {
                        BookingId = booking.BookingId,
                        TripId = booking.TripId,
                        PassengerId = booking.PassengerId,
                        PassengerName = passenger?.FullName ?? "Unknown",
                        SeatCount = booking.SeatCount,
                        TotalPrice = booking.TotalPrice,
                        Status = booking.Status,
                        BookingTime = booking.BookingTime,
                        Trip = tripDto
                    });
                }

                response.Result = result;
                response.Message = "Bookings retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<BookingDto>>> GetUserBookingHistoryAsync(int userId)
        {
            var response = new ResponseDto<List<BookingDto>>();
            try
            {
                var bookings = await _context.Bookings
                    .Where(b => b.PassengerId == userId && b.Trip.Status == "Completed")
                    .OrderByDescending(b => b.BookingTime)
                    .ToListAsync();

                var result = new List<BookingDto>();

                foreach (var booking in bookings)
                {
                    var passenger = await _userRepo.GetByIdAsync(booking.PassengerId);
                    var trip = await _tripRepo.GetByIdAsync(booking.TripId);
                    Vehicle vehicle = null!;
                    User driver = null!;
                    if (trip != null)
                    {
                        if (trip.VehicleId.HasValue)
                        {
                            vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == trip.VehicleId.Value);
                        }
                        driver = await _userRepo.GetByIdAsync(trip.DriverId);
                    }

                    var tripDto = trip == null ? null : new TripDto
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
                            ReviewsCount = await _context.Reviews.CountAsync(r => r.ToUserId == driver.UserId),
                            GmailVerified = driver.GmailVerified,
                            Introduce = driver.Introduce
                        } : null
                    };

                    result.Add(new BookingDto
                    {
                        BookingId = booking.BookingId,
                        TripId = booking.TripId,
                        PassengerId = booking.PassengerId,
                        PassengerName = passenger?.FullName ?? "Unknown",
                        SeatCount = booking.SeatCount,
                        TotalPrice = booking.TotalPrice,
                        Status = booking.Status,
                        BookingTime = booking.BookingTime,
                        Trip = tripDto
                    });
                }

                response.Result = result;
                response.Message = "Booking history retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<BookingDto>>> GetTripBookingsAsync(int tripId, int driverId)
        {
            var response = new ResponseDto<List<BookingDto>>();
            try
            {
                var trip = await _tripRepo.GetByIdAsync(tripId);
                if (trip == null || trip.DriverId != driverId)
                {
                    response.IsSuccess = false;
                    response.Message = "Trip not found or you are not the driver";
                    return response;
                }

                var bookings = await _context.Bookings
                    .Where(b => b.TripId == tripId)
                    .OrderByDescending(b => b.BookingTime)
                    .ToListAsync();

                var bookingDtos = new List<BookingDto>();
                foreach (var booking in bookings)
                {
                    var passenger = await _userRepo.GetByIdAsync(booking.PassengerId);
                    bookingDtos.Add(new BookingDto
                    {
                        BookingId = booking.BookingId,
                        TripId = booking.TripId,
                        PassengerId = booking.PassengerId,
                        PassengerName = passenger?.FullName ?? "Unknown",
                        SeatCount = booking.SeatCount,
                        TotalPrice = booking.TotalPrice,
                        Status = booking.Status,
                        BookingTime = booking.BookingTime
                    });
                }

                response.Result = bookingDtos;
                response.Message = "Trip bookings retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        private static decimal CalculateBasePrice(Trip trip, int seatCount, bool fullRide)
        {
            if (fullRide && trip.PriceFullRide.HasValue) return trip.PriceFullRide.Value;
            return seatCount * trip.PricePerSeat;
        }

        private static decimal CalculateServiceFee(decimal basePrice)
        {
            // Simple 2% service fee
            return Math.Round(basePrice * 0.02m, 0);
        }

        private static decimal ApplyDiscount(decimal basePrice, decimal serviceFee, Voucher voucher)
        {
            var subtotal = basePrice + serviceFee;
            if (voucher.DiscountType == "Percent")
                return Math.Round(subtotal * (voucher.DiscountValue / 100m), 0);
            return Math.Min(voucher.DiscountValue, subtotal);
        }

        public async Task<ResponseDto<List<VoucherPreviewDto>>> GetApplicableVouchersAsync(int userId, int tripId, int seatCount, bool fullRide)
        {
            var response = new ResponseDto<List<VoucherPreviewDto>>();
            try
            {
                var trip = await _tripRepo.GetByIdAsync(tripId) ?? throw new KeyNotFoundException("Trip not found");
                var basePrice = CalculateBasePrice(trip, seatCount, fullRide);
                var serviceFee = CalculateServiceFee(basePrice);

                var userVouchers = await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .Where(uv => uv.UserId == userId && !uv.IsUsed)
                    .ToListAsync();

                var result = new List<VoucherPreviewDto>();
                foreach (var uv in userVouchers)
                {
                    var v = uv.Voucher;
                    var subtotal = basePrice + serviceFee;
                    var meetsMin = !v.MinOrderValue.HasValue || subtotal >= v.MinOrderValue.Value;
                    var notExpired = !v.ExpiryDate.HasValue || v.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) >= DateTime.UtcNow.Date;
                    var hasQuota = !v.UsageLimit.HasValue || v.UsageLimit.Value > 0;
                    var isApplicable = meetsMin && notExpired && hasQuota;

                    var discount = isApplicable ? ApplyDiscount(basePrice, serviceFee, v) : 0m;
                    result.Add(new VoucherPreviewDto
                    {
                        VoucherId = v.VoucherId,
                        Code = v.Code,
                        Description = v.Description ?? string.Empty,
                        DiscountType = v.DiscountType,
                        DiscountValue = v.DiscountValue,
                        DiscountAmount = discount,
                        IsApplicable = isApplicable
                    });
                }

                response.Result = result
                    .OrderByDescending(x => x.IsApplicable)
                    .ThenByDescending(x => x.DiscountAmount)
                    .ToList();
                response.Message = "Vouchers retrieved";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<BookingQuoteDto>> GetBookingQuoteAsync(int userId, BookingQuoteRequestDto dto)
        {
            var response = new ResponseDto<BookingQuoteDto>();
            try
            {
                var trip = await _tripRepo.GetByIdAsync(dto.TripId) ?? throw new KeyNotFoundException("Trip not found");
                var basePrice = CalculateBasePrice(trip, dto.SeatCount, dto.FullRide);
                var serviceFee = CalculateServiceFee(basePrice);

                decimal discount = 0m;
                string? appliedCode = null;

                if (!string.IsNullOrWhiteSpace(dto.VoucherCode))
                {
                    var uv = await _context.UserVouchers
                        .Include(x => x.Voucher)
                        .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsUsed && x.Voucher.Code == dto.VoucherCode);

                    if (uv?.Voucher != null)
                    {
                        var subtotal = basePrice + serviceFee;
                        var meetsMin = !uv.Voucher.MinOrderValue.HasValue || subtotal >= uv.Voucher.MinOrderValue.Value;
                        var notExpired = !uv.Voucher.ExpiryDate.HasValue || uv.Voucher.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) >= DateTime.UtcNow.Date;
                        var hasQuota = !uv.Voucher.UsageLimit.HasValue || uv.Voucher.UsageLimit.Value > 0;
                        if (meetsMin && notExpired && hasQuota)
                        {
                            discount = ApplyDiscount(basePrice, serviceFee, uv.Voucher);
                            appliedCode = uv.Voucher.Code;
                        }
                    }
                }

                response.Result = new BookingQuoteDto
                {
                    TripId = dto.TripId,
                    SeatCount = dto.SeatCount,
                    FullRide = dto.FullRide,
                    BasePrice = basePrice,
                    ServiceFee = serviceFee,
                    VoucherDiscount = discount,
                    TotalPrice = Math.Max(0, basePrice + serviceFee - discount),
                    AppliedVoucherCode = appliedCode
                };
                response.Message = "Quote calculated";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<BookingDto>> ConfirmBookingAsync(int userId, ConfirmBookingDto dto)
        {
            var response = new ResponseDto<BookingDto>();
            try
            {
                var trip = await _tripRepo.GetByIdAsync(dto.TripId) ?? throw new KeyNotFoundException("Trip not found");
                if (trip.SeatAvailable < dto.SeatCount)
                    throw new InvalidOperationException("Not enough seats available");
                if (trip.DriverId == userId)
                    throw new InvalidOperationException("Cannot book your own trip");

                var basePrice = CalculateBasePrice(trip, dto.SeatCount, dto.FullRide);
                var serviceFee = CalculateServiceFee(basePrice);
                decimal discount = 0m;

                UserVoucher usedUv = null;
                if (!string.IsNullOrWhiteSpace(dto.VoucherCode))
                {
                    usedUv = await _context.UserVouchers
                        .Include(x => x.Voucher)
                        .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsUsed && x.Voucher.Code == dto.VoucherCode);
                    if (usedUv?.Voucher != null)
                    {
                        var subtotal = basePrice + serviceFee;
                        var meetsMin = !usedUv.Voucher.MinOrderValue.HasValue || subtotal >= usedUv.Voucher.MinOrderValue.Value;
                        var notExpired = !usedUv.Voucher.ExpiryDate.HasValue || usedUv.Voucher.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) >= DateTime.UtcNow.Date;
                        var hasQuota = !usedUv.Voucher.UsageLimit.HasValue || usedUv.Voucher.UsageLimit.Value > 0;
                        if (meetsMin && notExpired && hasQuota)
                        {
                            // For limited vouchers, decrement usage atomically first
                            if (usedUv.Voucher.UsageLimit.HasValue)
                            {
                                var rows = await _context.Vouchers
                                    .Where(v => v.VoucherId == usedUv.VoucherId && v.UsageLimit.HasValue && v.UsageLimit.Value > 0)
                                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.UsageLimit, v => v.UsageLimit - 1));
                                if (rows == 0)
                                {
                                    usedUv = null;
                                }
                                else
                                {
                                    discount = ApplyDiscount(basePrice, serviceFee, usedUv.Voucher);
                                }
                            }
                            else
                            {
                                // Unlimited voucher
                                discount = ApplyDiscount(basePrice, serviceFee, usedUv.Voucher);
                            }
                        }
                        else
                        {
                            usedUv = null; // don't mark as used
                        }
                    }
                }

                var total = Math.Max(0, basePrice + serviceFee - discount);

                var booking = new Booking
                {
                    TripId = dto.TripId,
                    PassengerId = userId,
                    SeatCount = (byte)dto.SeatCount,
                    TotalPrice = total,
                    Status = "Pending",
                    BookingTime = DateTime.UtcNow
                };

                await _bookingRepo.CreateAsync(booking);

                // reduce seats
                trip.SeatAvailable -= (byte)dto.SeatCount;
                await _tripRepo.UpdateAsync(trip);

                // mark voucher used (server-side update to avoid tracking issues)
                if (usedUv != null)
                {
                    await _context.UserVouchers
                        .Where(x => x.UserVoucherId == usedUv.UserVoucherId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(u => u.IsUsed, true)
                            .SetProperty(u => u.UsedAt, DateTime.UtcNow));
                }

                var passenger = await _userRepo.GetByIdAsync(userId);
                response.Result = new BookingDto
                {
                    BookingId = booking.BookingId,
                    TripId = booking.TripId,
                    PassengerId = booking.PassengerId,
                    PassengerName = passenger?.FullName ?? "",
                    SeatCount = booking.SeatCount,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status,
                    BookingTime = booking.BookingTime
                };
                response.Message = "Booking created";
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
