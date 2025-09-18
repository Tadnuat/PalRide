using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.DTOs.PalRide.API.Models.DTOs;
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

        public BookingService(PalRideContext context, UserRepository userRepo, BookingRepository bookingRepo, GenericRepository<Trip> tripRepo)
        {
            _context = context;
            _userRepo = userRepo;
            _bookingRepo = bookingRepo;
            _tripRepo = tripRepo;
        }

        public async Task<ResponseDto<BookingDto>> CreateBookingAsync(CreateBookingDto dto, int passengerId)
        {
            var response = new ResponseDto<BookingDto>();
            try
            {
                // Check if trip exists and has available seats
                var trip = await _tripRepo.GetByIdAsync(dto.TripId);
                if (trip == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Trip not found";
                    return response;
                }

                if (trip.SeatAvailable < dto.SeatCount)
                {
                    response.IsSuccess = false;
                    response.Message = "Not enough seats available";
                    return response;
                }

                if (trip.DriverId == passengerId)
                {
                    response.IsSuccess = false;
                    response.Message = "Cannot book your own trip";
                    return response;
                }

                // Check if user already has a booking for this trip
                var existingBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.TripId == dto.TripId && b.PassengerId == passengerId);
                
                if (existingBooking != null)
                {
                    response.IsSuccess = false;
                    response.Message = "You already have a booking for this trip";
                    return response;
                }

                // Create booking
                var booking = new Booking
                {
                    TripId = dto.TripId,
                    PassengerId = passengerId,
                    SeatCount = (byte)dto.SeatCount,
                    TotalPrice = dto.SeatCount * trip.PricePerSeat,
                    Status = "Pending",
                    BookingTime = DateTime.UtcNow
                };

                await _bookingRepo.CreateAsync(booking);

                // Update trip available seats
                trip.SeatAvailable -= (byte)dto.SeatCount;
                await _tripRepo.UpdateAsync(trip);

                // Get passenger name
                var passenger = await _userRepo.GetByIdAsync(passengerId);

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
                response.Message = "Booking created successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
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
                await _bookingRepo.UpdateAsync(booking);

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
                    .Where(b => b.PassengerId == userId)
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
                response.Message = "Bookings retrieved successfully";
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
    }
}
