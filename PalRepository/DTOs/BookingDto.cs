using System;
using System.Collections.Generic;

namespace PalRepository.DTOs.PalRide.API.Models.DTOs
{
    // Booking DTOs
    public class CreateBookingDto
    {
        public int TripId { get; set; }
        public int SeatCount { get; set; } = 1;
    }

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

    public class AcceptBookingDto
    {
        public int BookingId { get; set; }
    }

    public class SearchTripsDto
    {
        public string? PickupLocation { get; set; }
        public string? DropoffLocation { get; set; }
        public DateTime? StartDate { get; set; }
    }
}