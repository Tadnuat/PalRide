using System;

namespace PalService.DTOs
{
    // Booking DTOs
    // Removed: CreateBookingDto (use ConfirmBookingDto)

    public class BookingDto
    {
        public int BookingId { get; set; }
        public int TripId { get; set; }
        public int PassengerId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public int SeatCount { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime BookingTime { get; set; }
    }

    // Trip DTOs
    public class CreateTripDto
    {
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public decimal PricePerSeat { get; set; }
        public decimal? PriceFullRide { get; set; }
        public int SeatTotal { get; set; } = 4;
        public int? VehicleId { get; set; }
        public string? Note { get; set; }
    }

    public class TripDto
    {
        public int TripId { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal PricePerSeat { get; set; }
        public decimal? PriceFullRide { get; set; }
        public int SeatTotal { get; set; }
        public int SeatAvailable { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public VehicleDto? Vehicle { get; set; }
        public DriverInfoDto? Driver { get; set; }
    }

    public class VehicleDto
    {
        public int VehicleId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int SeatCount { get; set; }
    }

    public class DriverInfoDto
    {
        public int DriverId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumberMasked { get; set; } = string.Empty;
        public decimal RatingAverage { get; set; }
        public int ReviewsCount { get; set; }
        public bool GmailVerified { get; set; }
    }

    // Removed: AcceptBookingDto (not used in new flow)

    public class SearchTripsDto
    {
        public string? PickupLocation { get; set; }
        public string? DropoffLocation { get; set; }
        public DateTime? StartDate { get; set; }
    }

    // Search history
    public class SearchHistoryItemDto
    {
        public int Id { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Voucher and booking preview
    public class VoucherPreviewDto
    {
        public int VoucherId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty; // Percent | Fixed
        public decimal DiscountValue { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool IsApplicable { get; set; }
    }

    public class BookingQuoteRequestDto
    {
        public int TripId { get; set; }
        public int SeatCount { get; set; } = 1;
        public bool FullRide { get; set; } = false;
        public string? VoucherCode { get; set; }
    }

    public class BookingQuoteDto
    {
        public int TripId { get; set; }
        public int SeatCount { get; set; }
        public bool FullRide { get; set; }
        public decimal BasePrice { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal VoucherDiscount { get; set; }
        public decimal TotalPrice { get; set; }
        public string? AppliedVoucherCode { get; set; }
    }

    public class ConfirmBookingDto
    {
        public int TripId { get; set; }
        public int SeatCount { get; set; } = 1;
        public bool FullRide { get; set; } = false;
        public string? VoucherCode { get; set; }
        public string? Note { get; set; }
    }
}


