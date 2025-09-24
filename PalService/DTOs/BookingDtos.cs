using System;

namespace PalService.DTOs
{
    // Booking DTOs
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






